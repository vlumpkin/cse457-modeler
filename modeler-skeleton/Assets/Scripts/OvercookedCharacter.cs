using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class OvercookedCharacter : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 7.5f;
    public float turnSpeed = 720f;
    public float gravity = 20f;
    public float worldFloorY = 0f;

    [Header("CharacterController (only used if one isn't already attached)")]
    public float controllerHeight = 2f;
    public float controllerRadius = 0.75f;
    public Vector3 controllerCenter = new Vector3(0f, 1f, 0f);

    [Header("Sway (Z-axis roll, degrees)")]
    public float swayAmplitude = 0.75f;
    public float swayFrequency = 14f;

    [Header("Arm swing (empty hands, X-axis degrees)")]
    public float leftArmMin = -20f;
    public float leftArmMax = 13f;
    public float rightArmMin = -13f;
    public float rightArmMax = 20f;
    public float armSwingFrequency = 14f;

    [Header("Carry pose (arm container local position + Z roll)")]
    public Vector3 leftCarryPos = new Vector3(-1f, 3f, 0.8f);
    public Vector3 rightCarryPos = new Vector3(1f, 3f, 0.8f);
    public Vector3 leftCarryEuler = new Vector3(0f, 0f, 23f);
    public Vector3 rightCarryEuler = new Vector3(0f, 0f, -23f);

    [Header("Hierarchy paths (relative to this Frame)")]
    public string leftArmContainerName = "left_arm_container";
    public string rightArmContainerName = "right_arm_container";

    [Header("Input")]
    [Tooltip("Extra radial deadzone applied on top of the Input System's built-in stick deadzone. 0 = trust the asset's deadzone only.")]
    [Range(0f, 0.5f)] public float stickDeadzone = 0.1f;
    [Tooltip("Log pickup/action events and device pair/unpair changes for this character.")]
    public bool debugInput = false;

    [Header("Interaction")]
    [Tooltip("Where held items parent to. Auto-created in front of torso if null.")]
    public Transform holdAnchor;
    public Vector3 holdAnchorLocalPos = new Vector3(0f, 3.4f, 1.1f);
    [Tooltip("Optional separate anchor (e.g. PlacementAnchor child) whose local position CarryPose can override per item.")]
    public Transform placementAnchor;
    public float interactRange = 2.5f;
    public float interactHeight = 1.5f;
    public LayerMask interactMask = ~0;
    public bool drawInteractGizmo = true;
    [Tooltip("Dot product threshold between forward and direction-to-station. 1=directly ahead, 0=90°, -1=behind. ~0.3 is roughly a 70° cone.")]
    [Range(-1f, 1f)] public float facingDotThreshold = 0.3f;

    [Header("Facing highlight")]
    public bool showFacingHighlight = true;
    public Color highlightColor = new Color(0.3f, 1f, 0.6f, 0.35f);
    [Tooltip("Vertical offset above the station's top surface (avoids z-fighting).")]
    public float highlightYOffset = 0.01f;
    [Tooltip("Optional material. If left null, an unlit transparent material is generated at runtime.")]
    public Material highlightMaterial;

    [Header("Cutting")]
    [Tooltip("Seconds between chops. Each X press triggers one chop animation lasting this long.")]
    public float cutCooldown = 0.25f;
    [Tooltip("Right-arm X-axis euler at the top of the chop (knife raised).")]
    public float chopMinX = -25f;
    [Tooltip("Right-arm X-axis euler at the bottom of the chop (knife down).")]
    public float chopMaxX = 60f;
    [Tooltip("Optional anchor inside the right arm where the knife is parented. Falls back to right_arm_container if null.")]
    public Transform rightHandAnchor;
    [Tooltip("Local position of the knife relative to the hand anchor.")]
    public Vector3 knifeGripLocalPos = Vector3.zero;
    [Tooltip("Local euler rotation of the knife relative to the hand anchor.")]
    public Vector3 knifeGripLocalEuler = Vector3.zero;

    [Header("Debug ray")]
    public bool drawDebugRay = true;
    public Color rayHitColor = Color.green;
    public Color rayMissColor = Color.red;

    [Header("State (read-only)")]
    public Pickupable heldItem;

    private CharacterController controller;
    private Transform leftArm;
    private Transform rightArm;

    private Quaternion frameRestRot;
    private Quaternion leftArmRestRot;
    private Quaternion rightArmRestRot;
    private Vector3 leftArmRestPos;
    private Vector3 rightArmRestPos;
    private Vector3 placementAnchorRestPos;
    private bool hasPlacementAnchorRest;

    private Station facingStation;
    private GameObject highlightQuad;
    private Transform highlightTf;
    private MeshRenderer highlightRenderer;

    private bool hasKnife;
    private bool isChopping;
    private float chopTimer;
    private Station knifeStation;
    private Transform borrowedKnife;
    private Transform knifeOriginalParent;
    private Vector3 knifeOriginalLocalPos;
    private Quaternion knifeOriginalLocalRot;
    private KnifeGrip currentGrip;

    public bool HasKnife => hasKnife;
    public bool IsChopping => isChopping;

    private float swayPhase;
    private float swingPhase;
    private float verticalVelocity;

    private Vector2 moveInput;
    private PlayerInput playerInput;

    public bool IsHolding => heldItem != null;
    public Pickupable Held => heldItem;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
    }

    private void OnEnable()
    {
        if (playerInput == null) playerInput = GetComponent<PlayerInput>();
        playerInput.onDeviceLost += HandleDeviceLost;
        playerInput.onDeviceRegained += HandleDeviceRegained;
    }

    private void OnDisable()
    {
        if (playerInput == null) return;
        playerInput.onDeviceLost -= HandleDeviceLost;
        playerInput.onDeviceRegained -= HandleDeviceRegained;
    }

    private void HandleDeviceLost(PlayerInput pi)
    {
        moveInput = Vector2.zero;
        if (debugInput) Debug.Log($"[Overcooked][{name}] device lost (player {pi.playerIndex})");
    }

    private void HandleDeviceRegained(PlayerInput pi)
    {
        if (debugInput) Debug.Log($"[Overcooked][{name}] device regained (player {pi.playerIndex})");
    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnPickup(InputValue value)
    {
        if (!value.isPressed) return;
        if (debugInput) Debug.Log($"[Overcooked][{name}] PICKUP fired");
        OnPickupPressed();
    }

    public void OnAction(InputValue value)
    {
        if (!value.isPressed) return;
        if (debugInput) Debug.Log($"[Overcooked][{name}] ACTION fired");
        OnActionPressed();
    }

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            controller = gameObject.AddComponent<CharacterController>();
            controller.height = controllerHeight;
            controller.radius = controllerRadius;
            controller.center = controllerCenter;
            controller.skinWidth = 0.02f;
        }

        frameRestRot = transform.localRotation;

        leftArm = transform.Find(leftArmContainerName);
        rightArm = transform.Find(rightArmContainerName);

        if (leftArm == null) Debug.LogWarning($"OvercookedCharacter: '{leftArmContainerName}' not found under {name}");
        if (rightArm == null) Debug.LogWarning($"OvercookedCharacter: '{rightArmContainerName}' not found under {name}");

        if (leftArm != null)
        {
            leftArmRestRot = leftArm.localRotation;
            leftArmRestPos = leftArm.localPosition;
        }
        if (rightArm != null)
        {
            rightArmRestRot = rightArm.localRotation;
            rightArmRestPos = rightArm.localPosition;
        }

        if (placementAnchor != null)
        {
            placementAnchorRestPos = placementAnchor.localPosition;
            hasPlacementAnchorRest = true;
        }

        if (holdAnchor == null)
        {
            GameObject anchor = new GameObject("HoldAnchor");
            anchor.transform.SetParent(transform, false);
            anchor.transform.localPosition = holdAnchorLocalPos;
            anchor.transform.localRotation = Quaternion.identity;
            holdAnchor = anchor.transform;
        }

        if (debugInput)
        {
            string dev = playerInput != null && playerInput.devices.Count > 0
                ? playerInput.devices[0].displayName + " (" + playerInput.devices[0].deviceId + ")"
                : "<none>";
            Debug.Log($"[Overcooked][{name}] player {playerInput?.playerIndex} paired to {dev}");
        }
    }

    private void Update()
    {
        Vector3 move = ReadMoveInput();
        bool moving = move.sqrMagnitude > 0.0001f;

        ApplyMovement(move);
        ApplySway(moving);
        ApplyArmPose(moving);

        UpdateFacing();
        UpdateKnifeEquip();
        TickChop();
    }

    private Vector3 ReadMoveInput()
    {
        Vector3 dir = new Vector3(moveInput.x, 0f, moveInput.y);

        // Extra radial deadzone on top of the InputAction's built-in stick deadzone.
        float mag = dir.magnitude;
        if (mag < stickDeadzone) return Vector3.zero;
        if (stickDeadzone > 0f)
        {
            float rescaled = (mag - stickDeadzone) / (1f - stickDeadzone);
            dir = dir / mag * Mathf.Clamp01(rescaled);
        }
        return dir;
    }

    private void ApplyMovement(Vector3 move)
    {
        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -1f;
        verticalVelocity -= gravity * Time.deltaTime;

        if (move.sqrMagnitude > 0.0001f)
        {
            Quaternion target = Quaternion.LookRotation(move, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * Time.deltaTime);
        }

        Vector3 horizontal = move * moveSpeed;
        Vector3 motion = (horizontal + Vector3.up * verticalVelocity) * Time.deltaTime;
        controller.Move(motion);

        if (transform.position.y < worldFloorY)
        {
            Vector3 p = transform.position;
            p.y = worldFloorY;
            transform.position = p;
            verticalVelocity = 0f;
        }
    }

    private void ApplySway(bool moving)
    {
        if (moving)
        {
            swayPhase += swayFrequency * Time.deltaTime;
            float roll = Mathf.Sin(swayPhase) * swayAmplitude;
            // Preserve current yaw (set by ApplyMovement), only override local Z roll.
            Vector3 e = transform.localEulerAngles;
            transform.localRotation = Quaternion.Euler(e.x, e.y, roll);
        }
        else
        {
            swayPhase = 0f;
            Vector3 e = transform.localEulerAngles;
            transform.localRotation = Quaternion.Euler(e.x, e.y, 0f);
        }
    }

    private void ApplyArmPose(bool moving)
    {
        if (hasKnife)
        {
            ApplyKnifePose();
            return;
        }

        if (IsHolding)
        {
            ApplyCarryPose();
            return;
        }

        ApplyPlacementAnchorOverride(null);

        if (moving)
        {
            swingPhase += armSwingFrequency * Time.deltaTime;
            float s = Mathf.Sin(swingPhase);

            float leftMid = (leftArmMin + leftArmMax) * 0.5f;
            float leftAmp = (leftArmMax - leftArmMin) * 0.5f;
            float rightMid = (rightArmMin + rightArmMax) * 0.5f;
            float rightAmp = (rightArmMax - rightArmMin) * 0.5f;

            float leftAngle = leftMid + leftAmp * s;
            float rightAngle = rightMid - rightAmp * s; // opposite phase

            if (leftArm != null)
            {
                leftArm.localPosition = leftArmRestPos;
                leftArm.localRotation = leftArmRestRot * Quaternion.Euler(leftAngle, 0f, 0f);
            }
            if (rightArm != null)
            {
                rightArm.localPosition = rightArmRestPos;
                rightArm.localRotation = rightArmRestRot * Quaternion.Euler(rightAngle, 0f, 0f);
            }
        }
        else
        {
            swingPhase = 0f;
            if (leftArm != null)
            {
                leftArm.localPosition = leftArmRestPos;
                leftArm.localRotation = leftArmRestRot;
            }
            if (rightArm != null)
            {
                rightArm.localPosition = rightArmRestPos;
                rightArm.localRotation = rightArmRestRot;
            }
        }
    }

    private void ApplyCarryPose()
    {
        Vector3 lPos = leftCarryPos;
        Vector3 rPos = rightCarryPos;
        Vector3 lEuler = leftCarryEuler;
        Vector3 rEuler = rightCarryEuler;

        CarryPose pose = heldItem != null ? heldItem.GetComponent<CarryPose>() : null;
        if (pose != null)
        {
            if (pose.overrideLeftPos)
            {
                lPos = pose.leftHandPos;
                rPos = pose.GetMirroredRightPos();
            }
            if (pose.overrideLeftEuler)
            {
                lEuler = pose.leftHandEuler;
                rEuler = pose.GetMirroredRightEuler();
            }
        }

        ApplyPlacementAnchorOverride(pose);

        if (leftArm != null)
        {
            leftArm.localPosition = lPos;
            leftArm.localRotation = leftArmRestRot * Quaternion.Euler(lEuler);
        }
        if (rightArm != null)
        {
            rightArm.localPosition = rPos;
            rightArm.localRotation = rightArmRestRot * Quaternion.Euler(rEuler);
        }
    }

    private void ApplyPlacementAnchorOverride(CarryPose pose)
    {
        if (placementAnchor == null || !hasPlacementAnchorRest) return;
        placementAnchor.localPosition = (pose != null && pose.overridePlacementAnchor)
            ? pose.placementAnchorLocalPos
            : placementAnchorRestPos;
    }

    private void OnPickupPressed()
    {
        if (isChopping) return;
        if (hasKnife) UnequipKnife();

        Station station = FindFacingStation();
        if (station == null)
        {
            Debug.Log("[Overcooked] Pickup pressed — no Station in front of character");
            return;
        }

        if (IsHolding)
        {
            if (station.TryPlace(heldItem))
            {
                Debug.Log($"[Overcooked] Placed {heldItem.kind} on {station.name}");
                heldItem = null;
            }
            else
            {
                Debug.Log($"[Overcooked] {station.name} already has an item, can't place");
            }
        }
        else
        {
            Pickupable taken = station.TryTake();
            if (taken != null)
            {
                heldItem = taken;
                heldItem.OnPickedUp(holdAnchor);
                Debug.Log($"[Overcooked] Picked up {taken.kind} from {station.name}");
            }
            else
            {
                Debug.Log($"[Overcooked] {station.name} is empty");
            }
        }
    }

    private void OnActionPressed()
    {
        if (!hasKnife) return;
        if (isChopping) return;

        // Begin a chop cycle. Animation runs for cutCooldown; the cut lands at the bottom of the chop.
        isChopping = true;
        chopTimer = 0f;

        if (knifeStation != null && knifeStation.current != null)
        {
            bool finished = knifeStation.current.RegisterCut();
            if (finished)
                Debug.Log($"[Overcooked] {knifeStation.current.name} is now Cut");
        }
    }

    private void UpdateKnifeEquip()
    {
        bool wantKnife = !IsHolding
                         && facingStation != null
                         && facingStation.kind == StationKind.CuttingBoard
                         && facingStation.knife != null;

        if (wantKnife && !hasKnife)
        {
            EquipKnife(facingStation);
        }
        else if (hasKnife && !isChopping)
        {
            // Return the knife if we're no longer at the same cutting board (or we picked something up).
            if (facingStation != knifeStation || IsHolding || !wantKnife)
                UnequipKnife();
        }
    }

    private void EquipKnife(Station station)
    {
        Transform handAnchor = rightHandAnchor != null ? rightHandAnchor : rightArm;
        if (handAnchor == null) return;

        knifeStation = station;
        borrowedKnife = station.knife;
        knifeOriginalParent = borrowedKnife.parent;
        knifeOriginalLocalPos = borrowedKnife.localPosition;
        knifeOriginalLocalRot = borrowedKnife.localRotation;

        currentGrip = borrowedKnife.GetComponent<KnifeGrip>();
        Vector3 gripPos = currentGrip != null ? currentGrip.localPos : knifeGripLocalPos;
        Vector3 gripEuler = currentGrip != null ? currentGrip.localEuler : knifeGripLocalEuler;

        borrowedKnife.SetParent(handAnchor, true);
        borrowedKnife.localPosition = gripPos;
        borrowedKnife.localRotation = Quaternion.Euler(gripEuler);

        hasKnife = true;
    }

    private void UnequipKnife()
    {
        if (borrowedKnife != null)
        {
            borrowedKnife.SetParent(knifeOriginalParent, true);
            borrowedKnife.localPosition = knifeOriginalLocalPos;
            borrowedKnife.localRotation = knifeOriginalLocalRot;
        }
        hasKnife = false;
        isChopping = false;
        knifeStation = null;
        borrowedKnife = null;
        knifeOriginalParent = null;
        currentGrip = null;
    }

    private float EffectiveChopDuration()
        => (currentGrip != null && currentGrip.overrideChop) ? Mathf.Max(0.01f, currentGrip.chopDuration) : cutCooldown;

    private float ChopWave(float t)
    {
        // Default symmetric sine when no grip override.
        if (currentGrip == null || !currentGrip.overrideChop)
            return Mathf.Sin(t * Mathf.PI);

        float down = Mathf.Clamp(currentGrip.downFraction, 0.01f, 0.99f);
        float hold = Mathf.Clamp(currentGrip.holdFraction, 0f, 1f - down);
        float up = Mathf.Max(0.01f, 1f - down - hold);

        if (t < down)
        {
            float u = t / down;
            return Mathf.Pow(u, Mathf.Max(0.01f, currentGrip.downEasePower));
        }
        if (t < down + hold)
        {
            return 1f;
        }
        float r = (t - down - hold) / up;
        return 1f - Mathf.Pow(r, Mathf.Max(0.01f, currentGrip.upEasePower));
    }

    private void TickChop()
    {
        if (!isChopping) return;
        chopTimer += Time.deltaTime;
        if (chopTimer >= EffectiveChopDuration()) isChopping = false;
    }

    private void ApplyKnifePose()
    {
        // Re-apply grip pos/rot each frame so Inspector tweaks show up live.
        // Skip when the grip is in tuneMode — lets the user drag the knife in the Scene view.
        if (borrowedKnife != null && (currentGrip == null || !currentGrip.tuneMode))
        {
            Vector3 gripPos = currentGrip != null ? currentGrip.localPos : knifeGripLocalPos;
            Vector3 gripEuler = currentGrip != null ? currentGrip.localEuler : knifeGripLocalEuler;
            borrowedKnife.localPosition = gripPos;
            borrowedKnife.localRotation = Quaternion.Euler(gripEuler);
        }

        // Left arm stays at rest.
        if (leftArm != null)
        {
            leftArm.localPosition = leftArmRestPos;
            leftArm.localRotation = leftArmRestRot;
        }

        // Choose rest/slice eulers from KnifeGrip if it overrides; else fall back to character chopMinX/chopMaxX.
        Vector3 restEuler;
        Vector3 sliceEuler;
        if (currentGrip != null && currentGrip.overrideChop)
        {
            restEuler = currentGrip.restEuler;
            sliceEuler = currentGrip.sliceEuler;
        }
        else
        {
            restEuler = new Vector3(chopMinX, 0f, 0f);
            sliceEuler = new Vector3(chopMaxX, 0f, 0f);
        }

        Vector3 angle = restEuler;
        float duration = EffectiveChopDuration();
        float wave = 0f;
        if (isChopping && duration > 0f)
        {
            float t = Mathf.Clamp01(chopTimer / duration);
            wave = ChopWave(t);
            angle = Vector3.Lerp(restEuler, sliceEuler, wave);
        }

        // Right arm position: KnifeGrip can override rest/slice positions; otherwise stay at scene-edit rest.
        Vector3 armPos = rightArmRestPos;
        if (currentGrip != null && currentGrip.overrideArmPosition)
        {
            armPos = Vector3.Lerp(currentGrip.restArmPos, currentGrip.sliceArmPos, wave);
        }

        if (rightArm != null)
        {
            rightArm.localPosition = armPos;
            rightArm.localRotation = rightArmRestRot * Quaternion.Euler(angle);
        }
    }

    private Station FindFacingStation() => facingStation;

    private static readonly Collider[] facingBuffer = new Collider[32];

    private void UpdateFacing()
    {
        Vector3 origin = transform.position + Vector3.up * interactHeight;
        Vector3 fwd = transform.forward;
        Vector3 fwdFlat = new Vector3(fwd.x, 0f, fwd.z);
        if (fwdFlat.sqrMagnitude > 0.0001f) fwdFlat.Normalize();

        Station bestStation = null;
        Collider bestCollider = null;
        float bestDist = float.MaxValue;

        int count = Physics.OverlapSphereNonAlloc(origin, interactRange, facingBuffer, interactMask, QueryTriggerInteraction.Collide);
        for (int i = 0; i < count; i++)
        {
            Collider col = facingBuffer[i];
            Station s = col.GetComponentInParent<Station>();
            if (s == null) continue;

            Vector3 closest = col.ClosestPoint(origin);
            Vector3 toClosest = closest - origin;
            float dist = toClosest.magnitude;
            if (dist > interactRange) continue;

            Vector3 toFlat = new Vector3(toClosest.x, 0f, toClosest.z);
            if (toFlat.sqrMagnitude < 0.0001f)
            {
                // Character is essentially on top of the collider — accept it.
            }
            else
            {
                toFlat.Normalize();
                if (Vector3.Dot(toFlat, fwdFlat) < facingDotThreshold) continue;
            }

            if (dist < bestDist)
            {
                bestDist = dist;
                bestStation = s;
                bestCollider = col;
            }
        }

        facingStation = bestStation;

        if (drawDebugRay)
        {
            Color c = bestStation != null ? rayHitColor : rayMissColor;
            Vector3 end = bestCollider != null ? bestCollider.ClosestPoint(origin) : origin + fwd * interactRange;
            Debug.DrawLine(origin, end, c);
        }

        UpdateFacingHighlight(bestCollider);
    }

    private void UpdateFacingHighlight(Collider hitCollider)
    {
        if (!showFacingHighlight)
        {
            if (highlightQuad != null) highlightQuad.SetActive(false);
            return;
        }

        if (facingStation == null || hitCollider == null)
        {
            if (highlightQuad != null) highlightQuad.SetActive(false);
            return;
        }

        EnsureHighlightQuad();

        Bounds b = hitCollider.bounds;
        highlightTf.position = new Vector3(b.center.x, b.max.y + highlightYOffset, b.center.z);
        highlightTf.rotation = Quaternion.Euler(90f, 0f, 0f); // flat on top
        highlightTf.localScale = new Vector3(b.size.x, b.size.z, 1f);
        highlightQuad.SetActive(true);
    }

    private void EnsureHighlightQuad()
    {
        if (highlightQuad != null) return;

        highlightQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        highlightQuad.name = "FacingHighlight";
        Object.Destroy(highlightQuad.GetComponent<Collider>());
        highlightTf = highlightQuad.transform;
        highlightRenderer = highlightQuad.GetComponent<MeshRenderer>();
        highlightRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        highlightRenderer.receiveShadows = false;

        Material mat = highlightMaterial;
        if (mat == null)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Transparent")
                ?? Shader.Find("Sprites/Default");
            mat = new Material(sh);
            // Best-effort transparency setup for URP Unlit and built-in.
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f); // 1 = Transparent (URP)
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);     // 0 = Alpha
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", highlightColor);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", highlightColor);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_SURFACE_TYPE_OPAQUE");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
        highlightRenderer.sharedMaterial = mat;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawInteractGizmo) return;
        Gizmos.color = Color.cyan;
        Vector3 origin = transform.position + Vector3.up * interactHeight;
        Gizmos.DrawLine(origin, origin + transform.forward * interactRange);
        Gizmos.DrawWireSphere(origin + transform.forward * interactRange, 0.05f);
    }
}
