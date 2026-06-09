using System;
using UnityEngine;
using UnityEngine.InputSystem;

// One spawned per joined player. LobbyManager calls AssignColor() at spawn to
// pin the player to a color row (0=Blue, 1=Red, 2=Green, 3=Yellow). Player
// cycles through the shapes in that row and locks in. All variants live as
// inactive children of this prefab; we just SetActive the chosen one.
[RequireComponent(typeof(PlayerInput))]
public class LobbyPlayer : MonoBehaviour
{
    [Serializable]
    public class ColorRow
    {
        public string colorName = "Blue";
        [Tooltip("All shapes for this color, in cycle order. Each Transform is a child of this prefab and will be toggled SetActive(true/false).")]
        public Transform[] shapes = new Transform[0];
    }

    [Header("Variants")]
    [Tooltip("One row per color. Index 0 = first joiner, 1 = second, etc.")]
    public ColorRow[] colors = new ColorRow[4];

    [Header("Input")]
    [Range(0.1f, 0.9f)] public float cycleDeadzone = 0.5f;

    [Header("State (read-only)")]
    [SerializeField] int colorIndex = 0;
    [SerializeField] int shapeIndex = 0;
    [SerializeField] bool locked = false;

    PlayerInput playerInput;
    float lastCycleX;

    public int PlayerIndex => playerInput != null ? playerInput.playerIndex : -1;
    public int ColorIndex => colorIndex;
    public int ShapeIndex => shapeIndex;
    public bool IsLocked => locked;

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        DeactivateAll();
    }

    public void AssignColor(int color)
    {
        colorIndex = Mathf.Clamp(color, 0, Mathf.Max(0, colors.Length - 1));
        shapeIndex = 0;
        locked = false;
        ShowCurrent();
    }

    public void OnMove(InputValue value)
    {
        Vector2 v = value.Get<Vector2>();
        float x = v.x;
        if (!locked && Mathf.Abs(lastCycleX) < cycleDeadzone)
        {
            if (x >= cycleDeadzone) Cycle(+1);
            else if (x <= -cycleDeadzone) Cycle(-1);
        }
        lastCycleX = x;
    }

    public void OnTurn(InputValue _) { }

    public void OnPickup(InputValue value)
    {
        if (!value.isPressed) return;
        if (!locked) Lock();
    }

    public void OnAction(InputValue value)
    {
        if (!value.isPressed) return;
        if (locked) Unlock();
    }

    void Cycle(int dir)
    {
        int n = ShapeCount(colorIndex);
        if (n <= 0) return;
        shapeIndex = ((shapeIndex + dir) % n + n) % n;
        ShowCurrent();
    }

    void Lock()
    {
        locked = true;
        PlayerSelections.Set(PlayerIndex, colorIndex, shapeIndex);
        Debug.Log($"[LobbyPlayer] P{PlayerIndex} LOCKED (color {colorIndex}, shape {shapeIndex}) at t={Time.time:F3}s");
    }

    void Unlock()
    {
        locked = false;
        Debug.Log($"[LobbyPlayer] P{PlayerIndex} UNLOCKED at t={Time.time:F3}s");
    }

    int ShapeCount(int c)
        => (colors != null && c >= 0 && c < colors.Length && colors[c].shapes != null) ? colors[c].shapes.Length : 0;

    void DeactivateAll()
    {
        if (colors == null) return;
        for (int c = 0; c < colors.Length; c++)
        {
            var row = colors[c];
            if (row == null || row.shapes == null) continue;
            for (int s = 0; s < row.shapes.Length; s++)
                if (row.shapes[s] != null) row.shapes[s].gameObject.SetActive(false);
        }
    }

    void ShowCurrent()
    {
        DeactivateAll();
        int n = ShapeCount(colorIndex);
        if (n <= 0) return;
        var t = colors[colorIndex].shapes[shapeIndex];
        if (t != null) t.gameObject.SetActive(true);
    }
}
