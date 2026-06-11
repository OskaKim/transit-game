using System.Collections.Generic;
using TransitCore.Model;
using UnityEngine;

namespace TransitGame
{
    /// <summary>
    /// Assigns parallel "lanes" to lines that share the same station-to-station
    /// segment so they render side by side instead of on top of each other.
    /// Pure view concern - the core knows nothing about lanes.
    /// </summary>
    public static class LineLaneLayout
    {
        public const float LaneSpacing = 0.24f;

        public static (int, int) EdgeKey(int a, int b) => a < b ? (a, b) : (b, a);

        /// <summary>Undirected edge -> sorted lineIds using that edge.</summary>
        public static Dictionary<(int, int), List<int>> BuildEdgeMap(TransitNetwork network)
        {
            var map = new Dictionary<(int, int), List<int>>();
            foreach (var line in network.Lines.Values)
            {
                int n = line.Stations.Count;
                int segCount = line.IsLoop ? n : n - 1;
                for (int i = 0; i < segCount; i++)
                {
                    var key = EdgeKey(line.Stations[i], line.Stations[(i + 1) % n]);
                    if (!map.TryGetValue(key, out var list)) map[key] = list = new List<int>();
                    if (!list.Contains(line.Id)) list.Add(line.Id);
                }
            }
            foreach (var list in map.Values) list.Sort();
            return map;
        }

        /// <summary>
        /// Perpendicular offset for `lineId` on the segment between two stations.
        /// Zero when the segment is not shared. The perpendicular is derived from
        /// the canonical (smaller-id -> larger-id) direction so every line agrees.
        /// </summary>
        public static Vector2 GetOffset(Dictionary<(int, int), List<int>> edgeMap,
            TransitNetwork network, int lineId, int stationA, int stationB)
        {
            if (edgeMap == null) return Vector2.zero;
            var key = EdgeKey(stationA, stationB);
            if (!edgeMap.TryGetValue(key, out var list) || list.Count < 2) return Vector2.zero;
            int index = list.IndexOf(lineId);
            if (index < 0) return Vector2.zero;
            if (!network.Stations.TryGetValue(key.Item1, out var sa)
                || !network.Stations.TryGetValue(key.Item2, out var sb)) return Vector2.zero;

            var dir = new Vector2(sb.Position.X - sa.Position.X, sb.Position.Y - sa.Position.Y);
            if (dir.sqrMagnitude < 0.0001f) return Vector2.zero;
            dir.Normalize();
            var perp = new Vector2(-dir.y, dir.x);
            float centered = index - (list.Count - 1) * 0.5f;
            return perp * (centered * LaneSpacing);
        }
    }
}
