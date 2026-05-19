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

        transform.SetParent(anchor, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    public void OnPlaced(Transform slot)
    {
        transform.SetParent(slot, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        if (cachedColliders != null)
            foreach (var c in cachedColliders) c.enabled = true;
    }
}
