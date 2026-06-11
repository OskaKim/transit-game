using System.Collections.Generic;

namespace TransitCore.Model
{
    public class TransitNetwork
    {
        public Dictionary<int, Station> Stations { get; } = new Dictionary<int, Station>();
        public Dictionary<int, Line> Lines { get; } = new Dictionary<int, Line>();
        // Bumped whenever topology changes; router caches key off this.
        public int Version { get; private set; }

        public void BumpVersion() => Version++;

        public List<(int next, int lineId)> GetNeighbors(int stationId)
        {
            var result = new List<(int, int)>();
            foreach (var line in Lines.Values)
            {
                var s = line.Stations;
                for (int i = 0; i < s.Count; i++)
                {
                    if (s[i] != stationId) continue;
                    if (i > 0) result.Add((s[i - 1], line.Id));
                    if (i < s.Count - 1) result.Add((s[i + 1], line.Id));
                }
            }
            return result;
        }
    }
}
