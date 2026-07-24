using System.Collections.Generic;
using TransitCore.Model;

namespace TransitCore.Simulation
{
    public readonly struct RouteStep
    {
        public readonly int LineId;
        public readonly int NextStationId;

        public RouteStep(int lineId, int nextStationId)
        {
            LineId = lineId;
            NextStationId = nextStationId;
        }
    }

    /// <summary>
    /// Dijkstra over (station, line) states: 1 cost per hop, +TransferPenalty when
    /// switching lines. Returns only the first step; passengers re-query at every stop,
    /// which makes route changes after network edits self-healing.
    /// </summary>
    public class PassengerRouter
    {
        readonly TransitNetwork _network;
        readonly int _transferPenalty;
        readonly Dictionary<(int, StationShape), RouteStep?> _cache = new Dictionary<(int, StationShape), RouteStep?>();
        int _cachedVersion = -1;

        public PassengerRouter(TransitNetwork network, int transferPenalty)
        {
            _network = network;
            _transferPenalty = transferPenalty;
        }

        public RouteStep? NextStep(int stationId, StationShape target)
        {
            if (_cachedVersion != _network.Version)
            {
                _cache.Clear();
                _cachedVersion = _network.Version;
            }
            var key = (stationId, target);
            if (_cache.TryGetValue(key, out var cached)) return cached;
            var step = Compute(stationId, target);
            _cache[key] = step;
            return step;
        }

        RouteStep? Compute(int startId, StationShape target)
        {
            if (!_network.Stations.TryGetValue(startId, out var startStation)) return null;
            if (startStation.Shape == target) return null;

            // State: (stationId, lineId currently riding); -1 = not boarded yet.
            var dist = new Dictionary<(int, int), int>();
            var firstStep = new Dictionary<(int, int), RouteStep>();
            var open = new List<((int station, int line) state, int cost)>();
            var start = (startId, -1);
            dist[start] = 0;
            open.Add((start, 0));

            while (open.Count > 0)
            {
                int best = 0;
                for (int i = 1; i < open.Count; i++)
                    if (open[i].cost < open[best].cost) best = i;
                var (state, cost) = open[best];
                open.RemoveAt(best);
                if (dist.TryGetValue(state, out var known) && cost > known) continue;

                if (_network.Stations[state.station].Shape == target)
                    return firstStep[state];

                foreach (var (next, edgeLine) in _network.GetNeighbors(state.station))
                {
                    int c = cost + 1;
                    if (state.line != -1 && edgeLine != state.line) c += _transferPenalty;
                    var ns = (next, edgeLine);
                    if (dist.TryGetValue(ns, out var d) && d <= c) continue;
                    dist[ns] = c;
                    firstStep[ns] = state.station == startId && state.line == -1
                        ? new RouteStep(edgeLine, next)
                        : firstStep[state];
                    open.Add((ns, c));
                }
            }
            return null;
        }
    }
}
