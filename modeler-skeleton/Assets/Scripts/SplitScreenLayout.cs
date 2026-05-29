using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SplitScreenLayout : MonoBehaviour
{
    [Tooltip("Horizontal field of view (degrees) each player camera should render. Vertical FOV is derived per-viewport so framing stays consistent.")]
    public float horizontalFovDegrees = 90f;

    readonly List<PlayerInput> players = new List<PlayerInput>();

    void LateUpdate()
    {
        if (PlayerInputCountChanged())
        {
            players.Clear();
            players.AddRange(PlayerInput.all);
            Relayout();
        }
    }

    bool PlayerInputCountChanged()
    {
        if (PlayerInput.all.Count != players.Count) return true;
        for (int i = 0; i < players.Count; i++)
            if (players[i] != PlayerInput.all[i]) return true;
        return false;
    }

    void Relayout()
    {
        Debug.Log($"[SplitScreenLayout] Relayout for {players.Count} player(s)");
        for (int i = 0; i < players.Count; i++)
        {
            var cam = players[i].camera;
            if (cam == null)
            {
                Debug.LogWarning($"[SplitScreenLayout] Player {i} has no camera assigned on PlayerInput; skipping.", players[i]);
                continue;
            }
            Rect r = RectFor(i, players.Count);
            cam.rect = r;
            float pixelW = Screen.width * r.width;
            float pixelH = Screen.height * r.height;
            if (pixelH > 0f)
            {
                float aspect = pixelW / pixelH;
                cam.aspect = aspect;
                float hFovRad = horizontalFovDegrees * Mathf.Deg2Rad;
                float vFovRad = 2f * Mathf.Atan(Mathf.Tan(hFovRad * 0.5f) / aspect);
                cam.fieldOfView = vFovRad * Mathf.Rad2Deg;
            }
            Debug.Log($"[SplitScreenLayout]  P{i} ({cam.name}) rect={r} aspect={cam.aspect:F3} vFov={cam.fieldOfView:F1}");
        }
    }

    static Rect RectFor(int index, int count)
    {
        switch (count)
        {
            case 1:
                return new Rect(0f, 0f, 1f, 1f);

            case 2:
                return index == 0
                    ? new Rect(0f, 0.5f, 1f, 0.5f)
                    : new Rect(0f, 0f, 1f, 0.5f);

            case 3:
                switch (index)
                {
                    case 0: return new Rect(0f,   0.5f, 0.5f, 0.5f);
                    case 1: return new Rect(0.5f, 0.5f, 0.5f, 0.5f);
                    default: return new Rect(0f, 0f, 1f, 0.5f);
                }

            case 4:
            default:
                switch (index)
                {
                    case 0: return new Rect(0f,   0.5f, 0.5f, 0.5f);
                    case 1: return new Rect(0.5f, 0.5f, 0.5f, 0.5f);
                    case 2: return new Rect(0f,   0f,   0.5f, 0.5f);
                    default: return new Rect(0.5f, 0f, 0.5f, 0.5f);
                }
        }
    }
}
