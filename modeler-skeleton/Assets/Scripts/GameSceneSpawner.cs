using UnityEngine;
using UnityEngine.InputSystem;

// Drop one of these in the gameplay scene (e.g. temp3). On Start it reads
// PlayerSelections (populated by the lobby) and tells the local PlayerInputManager
// to instantiate one player per saved entry, re-pairing the original devices.
//
// If PlayerSelections is empty (scene was entered directly, not via the lobby),
// this does nothing and the PIM keeps its normal press-to-join behavior.
public class GameSceneSpawner : MonoBehaviour
{
    [Tooltip("Optional. If left null, FindAnyObjectByType<PlayerInputManager>() is used.")]
    public PlayerInputManager inputManager;

    [Tooltip("Optional. If set, spawned players are repositioned to these transforms in order.")]
    public Transform[] spawnPoints;

    [Tooltip("Disable further joins on the PIM after auto-spawning, so a stray controller can't add a 5th player mid-game.")]
    public bool disableJoiningAfterSpawn = true;

    public bool verbose = true;

    void Start()
    {
        if (PlayerSelections.Count == 0)
        {
            if (verbose) Debug.Log("[GameSceneSpawner] No PlayerSelections — leaving PIM in manual-join mode.");
            return;
        }

        if (inputManager == null) inputManager = FindAnyObjectByType<PlayerInputManager>();
        if (inputManager == null)
        {
            Debug.LogError("[GameSceneSpawner] No PlayerInputManager in scene — cannot auto-spawn players.");
            return;
        }
        if (inputManager.playerPrefab == null)
        {
            Debug.LogError("[GameSceneSpawner] PlayerInputManager.playerPrefab is null — cannot auto-spawn players.");
            return;
        }

        // Make sure joining is on for the duration of the auto-spawn — DisableJoining()
        // from the lobby persists on the singleton if it isn't reset here.
        inputManager.EnableJoining();

        int spawned = 0;
        foreach (var e in PlayerSelections.All)
        {
            var devices = ResolveDevices(e.deviceIds);
            PlayerInput pi = null;
            try
            {
                if (devices != null && devices.Length > 0)
                {
                    pi = PlayerInput.Instantiate(
                        inputManager.playerPrefab,
                        controlScheme: string.IsNullOrEmpty(e.controlScheme) ? null : e.controlScheme,
                        pairWithDevices: devices);
                }
                else
                {
                    // No saved device (shouldn't happen via lobby) — let PIM auto-pair from unpaired devices.
                    pi = PlayerInput.Instantiate(inputManager.playerPrefab);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GameSceneSpawner] Failed to instantiate player {e.playerIndex}: {ex.Message}");
                continue;
            }

            if (pi == null) continue;

            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                var t = spawnPoints[spawned % spawnPoints.Length];
                var root = pi.transform.root;
                var cc = root.GetComponent<CharacterController>();
                var rb = root.GetComponent<Rigidbody>();
                bool ccWas = cc != null && cc.enabled;
                if (cc != null) cc.enabled = false;
                if (rb != null) rb.position = t.position;
                root.SetPositionAndRotation(t.position, t.rotation);
                if (cc != null) cc.enabled = ccWas;
            }

            if (verbose)
                Debug.Log($"[GameSceneSpawner] Spawned player {e.playerIndex} (color {e.colorIndex}, shape {e.shapeIndex}) with {(devices != null ? devices.Length : 0)} device(s).");
            spawned++;
        }

        if (disableJoiningAfterSpawn) inputManager.DisableJoining();
        if (verbose) Debug.Log($"[GameSceneSpawner] Auto-spawn complete — {spawned} player(s).");
    }

    static InputDevice[] ResolveDevices(int[] ids)
    {
        if (ids == null || ids.Length == 0) return null;
        var list = new System.Collections.Generic.List<InputDevice>(ids.Length);
        for (int i = 0; i < ids.Length; i++)
        {
            var dev = InputSystem.GetDeviceById(ids[i]);
            if (dev != null) list.Add(dev);
        }
        return list.Count > 0 ? list.ToArray() : null;
    }
}
