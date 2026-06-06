using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OrderQueue : MonoBehaviour
{
    public GameObject orderPrefab;
    public Transform queueParent;
    public float[] spawnTimes = { 5f, 23f, 30f };

    private class OrderTimer
    {
        public RectTransform fillRect;
        public float remaining = 30f;
    }

    private readonly List<OrderTimer> timers = new List<OrderTimer>();
    private int nextIndex;
    private float t;

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

            float fraction = timer.remaining / 30f;

            // Shrink fill width from right by moving anchorMax.x
            timer.fillRect.anchorMax = new Vector2(fraction, 1f);
            timer.fillRect.offsetMax = Vector2.zero;

            Image img = timer.fillRect.GetComponent<Image>();
            if (img != null) img.color = timer.remaining < 10f ? Color.red : Color.green;
        }
    }

    private void SpawnOrder()
    {
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

        timers.Add(new OrderTimer { fillRect = fillRt });
    }
}
