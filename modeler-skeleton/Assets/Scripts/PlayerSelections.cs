using System.Collections.Generic;

// Persists per-player choices from the lobby into the gameplay scene.
public static class PlayerSelections
{
    public struct Entry
    {
        public int playerIndex;
        public int colorIndex; // index into CharacterRoster.groups (0=Blue, 1=Red, ...)
        public int shapeIndex; // index into that group's shapes[]
        // Device ids the lobby player was paired with. Persisted across the
        // scene load so the gameplay spawner can re-pair the same controllers.
        public int[] deviceIds;
        public string controlScheme;
    }

    static readonly List<Entry> entries = new List<Entry>();

    public static IReadOnlyList<Entry> All => entries;
    public static int Count => entries.Count;

    public static void Clear() => entries.Clear();

    public static void Set(int playerIndex, int colorIndex, int shapeIndex, int[] deviceIds = null, string controlScheme = null)
    {
        var e = new Entry
        {
            playerIndex = playerIndex,
            colorIndex = colorIndex,
            shapeIndex = shapeIndex,
            deviceIds = deviceIds,
            controlScheme = controlScheme,
        };
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].playerIndex == playerIndex)
            {
                // Preserve any prior device pairing if the caller didn't pass one.
                if (deviceIds == null) e.deviceIds = entries[i].deviceIds;
                if (controlScheme == null) e.controlScheme = entries[i].controlScheme;
                entries[i] = e;
                return;
            }
        }
        entries.Add(e);
    }

    public static bool TryGet(int playerIndex, out Entry entry)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].playerIndex == playerIndex)
            {
                entry = entries[i];
                return true;
            }
        }
        entry = default;
        return false;
    }
}
