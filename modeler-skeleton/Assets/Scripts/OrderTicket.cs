using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// One on-screen order card. Mirrors the mockup: a soup-bowl icon on top,
// a row of ingredient icons below, and an optional countdown bar.
public class OrderTicket : MonoBehaviour
{
    [Header("Wire in the prefab")]
    public Image bowlIcon;                 // top icon (the finished dish)
    public Transform ingredientRow;        // parent for ingredient icons (HorizontalLayoutGroup)
    public Image ingredientIconPrefab;     // small Image prefab used per ingredient
    public Image timerFill;                // optional: a filled Image (Image Type = Filled, Horizontal)

    public VegetableType SoupType { get; private set; }

    private float lifetime;
    private float remaining;
    private bool ticking;

    public void Configure(VegetableType soup, Sprite bowlSprite, Sprite ingredientSprite, int ingredientCount, float timeLimitSeconds)
    {
        SoupType = soup;

        if (bowlIcon != null) bowlIcon.sprite = bowlSprite;

        if (ingredientRow != null && ingredientIconPrefab != null)
        {
            for (int i = ingredientRow.childCount - 1; i >= 0; i--)
                Destroy(ingredientRow.GetChild(i).gameObject);
            for (int i = 0; i < ingredientCount; i++)
            {
                var img = Instantiate(ingredientIconPrefab, ingredientRow);
                img.sprite = ingredientSprite;
            }
        }

        lifetime = remaining = Mathf.Max(0f, timeLimitSeconds);
        ticking = lifetime > 0f;
        if (timerFill != null)
        {
            timerFill.gameObject.SetActive(ticking);
            timerFill.fillAmount = 1f;
        }
    }

    public bool Expired => ticking && remaining <= 0f;

    private void Update()
    {
        if (!ticking) return;
        remaining -= Time.deltaTime;
        if (timerFill != null) timerFill.fillAmount = Mathf.Clamp01(remaining / lifetime);
    }
}
