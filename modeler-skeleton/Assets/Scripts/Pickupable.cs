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

    private Collider[] cachedColliders;

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
