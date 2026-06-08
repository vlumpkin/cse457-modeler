using System.Collections.Generic;

// Persists per-player choices from the lobby into the gameplay scene.
public static class PlayerSelections
{
    public struct Entry
    {
        public int playerIndex;
        public int colorIndex; // index into CharacterRoster.groups (0=Blue, 1=Red, ...)
        public int shapeIndex; // index into that group's shapes[]
    }

    static readonly List<Entry> entries = new List<Entry>();

    public static IReadOnlyList<Entry> All => entries;
    public static int Count => entries.Count;

    public static void Clear() => entries.Clear();

    public static void Set(int playerIndex, int colorIndex, int shapeIndex)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].playerIndex == playerIndex)
            {
                entries[i] = new Entry { playerIndex = playerIndex, colorIndex = colorIndex, shapeIndex = shapeIndex };
                return;
            }
        }
        entries.Add(new Entry { playerIndex = playerIndex, colorIndex = colorIndex, shapeIndex = shapeIndex });
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
