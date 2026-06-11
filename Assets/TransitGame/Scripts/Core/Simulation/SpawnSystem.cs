using System;
using System.Collections.Generic;
using System.Numerics;
using TransitCore.Model;

namespace TransitCore.Simulation
{
    public class SpawnSystem
    {
        readonly SimConfig _cfg;
        readonly Random _rng;
        readonly Dictionary<int, float> _passengerTimers = new Dictionary<int, float>();
        readonly List<int> _timerKeys = new List<int>();
        float _stationTimer;
        float _currentInterval;

        public SpawnSystem(SimConfig cfg, Random rng)
        {
            _cfg = cfg;
            _rng = rng;
            _currentInterval = cfg.StationSpawnInterval;
            _stationTimer = _currentInterval;
        }

        public void RegisterStation(Station station) =>
            _passengerTimers[station.Id] = NextPassengerDelay();

        float NextPassengerDelay() =>
            _cfg.PassengerSpawnMin + (float)_rng.NextDouble() * (_cfg.PassengerSpawnMax - _cfg.PassengerSpawnMin);

        public void Tick(float dt, SimulationEngine engine)
        {
            _stationTimer -= dt;
            if (_stationTimer <= 0f)
            {
                engine.TrySpawnStation();
                _currentInterval = Math.Max(_cfg.StationSpawnIntervalMin, _currentInterval * _cfg.StationSpawnDecay);
                _stationTimer = _currentInterval;
            }

            _timerKeys.Clear();
            _timerKeys.AddRange(_passengerTimers.Keys);
            foreach (var id in _timerKeys)
            {
                float t = _passengerTimers[id] - dt;
                if (t <= 0f)
                {
                    engine.SpawnPassenger(id);
                    t = NextPassengerDelay();
                }
                _passengerTimers[id] = t;
            }
        }

        public Vector2? FindStationPosition(TransitNetwork network)
        {
            float hw = _cfg.WorldWidth * 0.5f;
            float hh = _cfg.WorldHeight * 0.5f;
            for (int attempt = 0; attempt < 30; attempt++)
            {
                var pos = new Vector2(
                    (float)(_rng.NextDouble() * 2.0 - 1.0) * hw,
                    (float)(_rng.NextDouble() * 2.0 - 1.0) * hh);
                bool ok = true;
                foreach (var s in network.Stations.Values)
                {
                    if (Vector2.Distance(s.Position, pos) < _cfg.MinStationDistance)
                    {
                        ok = false;
                        break;
                    }
                }
                if (ok) return pos;
            }
            return null;
        }
    }
}
