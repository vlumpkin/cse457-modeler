using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OrderQueue : MonoBehaviour
{
    public static OrderQueue Instance { get; private set; }

    public GameObject orderPrefab;
    public Transform queueParent;
    public float[] spawnTimes = { 5f, 23f, 30f };

    private class OrderTimer
    {
        public RectTransform fillRect;
        public float remaining = 60f;
        public VegetableType soupType;
        public GameObject ticketObject;
    }

    private readonly List<OrderTimer> timers = new List<OrderTimer>();
    private int nextIndex;
    private float t;

    private void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        t += Time.deltaTime;
        if (nextIndex < spawnTimes.Length && t >= spawnTimes[nextIndex])
        {
            SpawnOrder();
            nextIndex++;
        }

        foreach (OrderTimer timer in timers)
        {
            if (timer.fillRect == null) continue;

            timer.remaining -= Time.deltaTime;
            if (timer.remaining < 0f) timer.remaining = 0f;

            float fraction = timer.remaining / 60f;

            // Shrink fill width from right by moving anchorMax.x
            timer.fillRect.anchorMax = new Vector2(fraction, 1f);
            timer.fillRect.offsetMax = Vector2.zero;

            Image img = timer.fillRect.GetComponent<Image>();
            if (img != null) img.color = timer.remaining < 10f ? Color.red : Color.green;
        }
    }

    public bool TryFulfillOrder(VegetableType type)
    {
        int oldestIndex = -1;
        float minRemaining = float.MaxValue;
        for (int i = 0; i < timers.Count; i++)
        {
            if (timers[i].soupType == type && timers[i].remaining < minRemaining)
            {
                minRemaining = timers[i].remaining;
                oldestIndex = i;
            }
        }
        if (oldestIndex < 0) return false;
        if (timers[oldestIndex].ticketObject != null) Destroy(timers[oldestIndex].ticketObject);
        timers.RemoveAt(oldestIndex);
        return true;
    }

    private void SpawnOrder()
    {
        VegetableType type = (VegetableType)Random.Range(0, System.Enum.GetValues(typeof(VegetableType)).Length);
        GameObject go = Instantiate(orderPrefab, queueParent);

        // Dark background strip
        GameObject bgObj = new GameObject("TimerBarBG");
        bgObj.transform.SetParent(go.transform, false);
        Image bg = bgObj.AddComponent<Image>();
        bg.color = new Color(0.15f, 0.15f, 0.15f, 1f);
        RectTransform bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0f, 1f);
        bgRt.anchorMax = new Vector2(1f, 1f);
        bgRt.pivot = new Vector2(0.5f, 1f);
        bgRt.offsetMin = new Vector2(0f, -10f);
        bgRt.offsetMax = new Vector2(0f, 0f);

        // Green fill strip (child of background)
        GameObject fillObj = new GameObject("TimerBarFill");
        fillObj.transform.SetParent(bgObj.transform, false);
        Image fill = fillObj.AddComponent<Image>();
        fill.color = Color.green;
        RectTransform fillRt = fillObj.GetComponent<RectTransform>();
        fillRt.anchorMin = new Vector2(0f, 0f);
        fillRt.anchorMax = new Vector2(1f, 1f);
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;

        timers.Add(new OrderTimer { fillRect = fillRt, soupType = type, ticketObject = go });
    }
}
