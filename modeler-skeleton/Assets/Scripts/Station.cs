using UnityEngine;

public enum StationKind { Counter, CuttingBoard, Sink, FireExtinguisher }

public class Station : MonoBehaviour
{
    public StationKind kind = StationKind.Counter;

    [Tooltip("Optional anchor where placed items sit. Falls back to this transform.")]
    public Transform placementAnchor;

    [Tooltip("Item currently sitting on the station (auto-detected on Start if left null and a Pickupable is a child).")]
    public Pickupable current;

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
        if (current == null) return null;
        Pickupable p = current;
        current = null;
        return p;
    }
}
