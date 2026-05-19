using UnityEngine;

public class OvercookedCharacter : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float turnSpeed = 720f;
    public float gravity = 20f;

    [Header("Sway (Z-axis roll, degrees)")]
    public float swayAmplitude = 0.3f;
    public float swayFrequency = 6f;

    [Header("Arm swing (empty hands, X-axis degrees)")]
    public float leftArmMin = -20f;
    public float leftArmMax = 13f;
    public float rightArmMin = -13f;
    public float rightArmMax = 20f;
    public float armSwingFrequency = 6f;

    [Header("Carry pose (arm container local position)")]
    public Vector3 leftCarryPos = new Vector3(-0.3f, 0f, 0.3f);
    public Vector3 rightCarryPos = new Vector3(0.3f, 0f, 0.3f);

    [Header("Hierarchy paths (relative to this Frame)")]
    public string leftArmContainerName = "Left Arm Container";
    public string rightArmContainerName = "Right Arm Container";

    [Header("Input (keyboard)")]
    public KeyCode pickupKey = KeyCode.Space;
    public KeyCode actionKey = KeyCode.X;
    public KeyCode pickupGamepad = KeyCode.JoystickButton0; // Xbox A
    public KeyCode actionGamepad = KeyCode.JoystickButton2; // Xbox X

    [Header("State (read-only)")]
    public Transform heldItem;

    private CharacterController controller;
    private Transform leftArm;
    private Transform rightArm;

    private Quaternion frameRestRot;
    private Quaternion leftArmRestRot;
    private Quaternion rightArmRestRot;
    private Vector3 leftArmRestPos;
    private Vector3 rightArmRestPos;

    private float swayPhase;
    private float swingPhase;
    private float verticalVelocity;

    public bool IsHolding => heldItem != null;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            controller = gameObject.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.4f;
            controller.center = new Vector3(0f, 1f, 0f);
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
    }

    private void Update()
    {
        Vector3 move = ReadMoveInput();
        bool moving = move.sqrMagnitude > 0.0001f;

        ApplyMovement(move);
        ApplySway(moving);
        ApplyArmPose(moving);

        if (Input.GetKeyDown(pickupKey) || Input.GetKeyDown(pickupGamepad))
            OnPickupPressed();

        if (Input.GetKeyDown(actionKey) || Input.GetKeyDown(actionGamepad))
            OnActionPressed();
    }

    private Vector3 ReadMoveInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 dir = new Vector3(h, 0f, v);
        if (dir.sqrMagnitude > 1f) dir.Normalize();
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
        if (IsHolding)
        {
            ApplyCarryPose();
            return;
        }

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
        if (leftArm != null)
        {
            leftArm.localPosition = new Vector3(leftCarryPos.x, leftArmRestPos.y, leftCarryPos.z);
            leftArm.localRotation = leftArmRestRot;
        }
        if (rightArm != null)
        {
            rightArm.localPosition = new Vector3(rightCarryPos.x, rightArmRestPos.y, rightCarryPos.z);
            rightArm.localRotation = rightArmRestRot;
        }
    }

    private void OnPickupPressed()
    {
        // Stub — real pickup/drop dispatch lands in step 2.
        if (IsHolding)
            Debug.Log("[Overcooked] Drop pressed (no station logic yet)");
        else
            Debug.Log("[Overcooked] Pickup pressed (no station logic yet)");
    }

    private void OnActionPressed()
    {
        // Stub — context action (cut/wash/extinguish) lands in step 3.
        Debug.Log("[Overcooked] Action pressed (no station logic yet)");
    }
}
