using UnityEngine;

public enum StationKind { Counter, CuttingBoard, Sink, FireExtinguisher, Burner, SupplyBox, Trashcan }

public class Station : MonoBehaviour
{
    public StationKind kind = StationKind.Counter;

    [Tooltip("Optional anchor where placed items sit. Falls back to this transform.")]
    public Transform placementAnchor;

    [Tooltip("Item currently sitting on the station (auto-detected on Start if left null and a Pickupable is a child).")]
    public Pickupable current;

    [Tooltip("Knife transform (for CuttingBoard kind). Borrowed by the character while cutting.")]
    public Transform knife;

    [Tooltip("Prefab dispensed when a player picks up from an empty SupplyBox (e.g. an onion Pickupable).")]
    public Pickupable supplyPrefab;

    private void Start()
    {
        if (placementAnchor == null) placementAnchor = transform;
        if (current == null) current = GetComponentInChildren<Pickupable>();
        if (current != null) current.OnPlaced(placementAnchor);
    }

    public bool HasItem => current != null;

    public bool TryPlace(Pickupable item)
    {
        if (current != null) return false;
        current = item;
        item.OnPlaced(placementAnchor);
        return true;
    }

    public Pickupable TryTake()
    {
        if (current == null)
        {
            if (kind == StationKind.SupplyBox && supplyPrefab != null)
            {
                // Spawn at the placement anchor so it has a sensible world pose before OnPickedUp re-parents it.
                return Instantiate(supplyPrefab, placementAnchor.position, placementAnchor.rotation);
            }
            return null;
        }
        Pickupable p = current;
        current = null;
        return p;
    }

    private void Update()
    {
        if (kind != StationKind.Burner || current == null) return;
        PotContents pot = current.GetComponentInChildren<PotContents>();
        if (pot != null) pot.Tick(Time.deltaTime);
    }
}
