using UnityEngine;

public class OrderQueue : MonoBehaviour
{
    public GameObject orderPrefab;          // your order image prefab
    public Transform queueParent;           // UI object with a HorizontalLayoutGroup
    public float[] spawnTimes = { 5f, 23f, 30f };

    private int nextIndex;
    private float t;

    void Update()
    {
        t += Time.deltaTime;
        if (nextIndex < spawnTimes.Length && t >= spawnTimes[nextIndex])
        {
            Instantiate(orderPrefab, queueParent);
            nextIndex++;
        }
    }
}
