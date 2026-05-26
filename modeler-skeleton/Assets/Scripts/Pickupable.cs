using UnityEngine;

public enum PickupableKind { Food, Plate, Pot }
public enum FoodState { Raw, Cut }
public enum PlateState { Clean, Dirty }

public class Pickupable : MonoBehaviour
{
    public PickupableKind kind = PickupableKind.Food;
    public FoodState foodState = FoodState.Raw;
    public PlateState plateState = PlateState.Clean;

    [Tooltip("Disable colliders while held so the character capsule doesn't fight the item.")]
    public bool disableColliderWhileHeld = true;

    [Header("Cutting (only relevant for Food)")]
    public int cutsRequired = 5;
    [Tooltip("Read-only-ish — incremented by OvercookedCharacter when a chop lands.")]
    public int cutProgress = 0;
    [Tooltip("Auto-create a 3D progress meter above this item while it's being cut.")]
    public bool showCutMeter = true;
    [Tooltip("Vertical offset (world units) above the item's pivot to place the meter.")]
    public float cutMeterYOffset = 1.0f;

    private Collider[] cachedColliders;
    private CutProgressMeter cutMeter;

    public bool RegisterCut()
    {
        if (kind != PickupableKind.Food || foodState != FoodState.Raw) return false;
        cutProgress++;
        if (cutProgress >= cutsRequired)
        {
            foodState = FoodState.Cut;
            if (cutMeter != null) cutMeter.Hide();
            return true; // finished
        }
        if (showCutMeter)
        {
            if (cutMeter == null) cutMeter = CutProgressMeter.AttachTo(this);
            cutMeter.SetProgress((float)cutProgress / cutsRequired);
        }
        return false;
    }

    public void OnPickedUp(Transform anchor)
    {
        if (disableColliderWhileHeld)
        {
            if (cachedColliders == null) cachedColliders = GetComponentsInChildren<Collider>();
            foreach (var c in cachedColliders) c.enabled = false;
        }

        SnapTo(anchor);
    }

    public void OnPlaced(Transform slot)
    {
        SnapTo(slot);

        if (cachedColliders != null)
            foreach (var c in cachedColliders) c.enabled = true;
    }

    private void SnapTo(Transform target)
    {
        // SetParent(target, true) preserves world scale by adjusting localScale.
        // Then zero localPosition/Rotation so we snap to the anchor.
        transform.SetParent(target, true);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }
}
