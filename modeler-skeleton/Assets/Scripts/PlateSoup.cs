using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Pickupable))]
public class PlateSoup : MonoBehaviour
{
    [Serializable]
    public struct SoupVisual
    {
        public VegetableType type;
        [Tooltip("Child GameObject shown when the plate holds soup of this vegetable type.")]
        public GameObject visual;
    }

    [Header("Per-type soup visuals (toggled when plate holds that soup)")]
    public List<SoupVisual> visuals = new List<SoupVisual>();

    private Pickupable plate;

    private void Awake()
    {
        plate = GetComponent<Pickupable>();
        RefreshVisuals();
    }

    public void SetSoup(VegetableType type)
    {
        plate.plateContents = PlateContents.Soup;
        plate.soupType = type;
        RefreshVisuals();
    }

    public void Clear()
    {
        plate.plateContents = PlateContents.Empty;
        RefreshVisuals();
    }

    private void RefreshVisuals()
    {
        bool hasSoup = plate.plateContents == PlateContents.Soup;
        for (int i = 0; i < visuals.Count; i++)
        {
            var v = visuals[i];
            if (v.visual == null) continue;
            v.visual.SetActive(hasSoup && v.type == plate.soupType);
        }
    }
}
