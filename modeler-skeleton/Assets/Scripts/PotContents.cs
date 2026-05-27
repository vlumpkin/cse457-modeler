using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Pickupable))]
public class PotContents : MonoBehaviour
{
    public const int MaxVegetables = 3;
    public const float SecondsPerVegetable = 10f;
    public const float OvercookSecondsUntilFire = 10f;

    [Serializable]
    public struct VegetableVisual
    {
        public VegetableType type;
        [Tooltip("Child GameObject shown while at least one vegetable of this type is in the pot.")]
        public GameObject visual;
    }

    [Header("Per-type visuals (toggled while that vegetable is present)")]
    public List<VegetableVisual> visuals = new List<VegetableVisual>();

    [Header("State (read-only-ish)")]
    public int vegCount;
    public float cookSeconds;
    public float overcookSeconds;
    public bool onFire;

    // Per-type counts, indexed by (int)VegetableType.
    private readonly Dictionary<VegetableType, int> perType = new Dictionary<VegetableType, int>();

    public float TotalCookTime => SecondsPerVegetable * vegCount;
    public bool IsFull => vegCount >= MaxVegetables;
    public bool IsFullyCooked => vegCount == MaxVegetables && cookSeconds >= TotalCookTime;

    private void Awake()
    {
        RefreshVisuals();
    }

    public bool TryAddVegetable(VegetableType type)
    {
        if (IsFull || onFire) return false;
        vegCount++;
        perType.TryGetValue(type, out int n);
        perType[type] = n + 1;
        overcookSeconds = 0f;
        RefreshVisuals();
        return true;
    }

    public void Empty()
    {
        vegCount = 0;
        cookSeconds = 0f;
        overcookSeconds = 0f;
        onFire = false;
        perType.Clear();
        RefreshVisuals();
    }

    public int CountOf(VegetableType type)
    {
        perType.TryGetValue(type, out int n);
        return n;
    }

    public void Tick(float dt)
    {
        if (vegCount == 0 || onFire) return;

        float total = TotalCookTime;
        if (cookSeconds < total)
        {
            cookSeconds = Mathf.Min(cookSeconds + dt, total);
            return;
        }

        overcookSeconds += dt;
        if (overcookSeconds >= OvercookSecondsUntilFire) onFire = true;
    }

    private void RefreshVisuals()
    {
        for (int i = 0; i < visuals.Count; i++)
        {
            var v = visuals[i];
            if (v.visual == null) continue;
            v.visual.SetActive(CountOf(v.type) > 0);
        }
    }
}
