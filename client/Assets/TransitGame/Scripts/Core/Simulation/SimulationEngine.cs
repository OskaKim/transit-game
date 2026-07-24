using System;
using System.Collections.Generic;
using System.Numerics;
using TransitCore.Model;

namespace TransitCore.Simulation
{
    /// <summary>
    /// Tick-driven simulation core. No UnityEngine references: Unity calls in via
    /// Tick / TryCreateLine etc., and the core reports back via C# events only.
    /// </summary>
    public class SimulationEngine
    {
        public SimConfig Config { get; }
        public TransitNetwork Network { get; } = new TransitNetwork();
        public List<Train> Trains { get; } = new List<Train>();
        public PassengerRouter Router { get; }
        public float ElapsedTime { get; private set; }
        public int Score { get; private set; }
        public bool IsGameOver { get; private set; }
        public int LinesAvailable => Config.MaxLines - Network.Lines.Count;

        public event Action<Station> StationSpawned;
        public event Action<Line> LineChanged;
        public event Action<int> LineRemoved;
        public event Action<Train> TrainAdded;
        public event Action<int> TrainRemoved;
        public event Action<int> ScoreChanged;
        public event Action GameOverTriggered;

        readonly Random _rng;
        readonly SpawnSystem _spawner;
        readonly GameRules _rules;
        int _nextStationId;
        int _nextLineId;
        int _nextTrainId;
        int _nextPassengerId;

        public SimulationEngine(SimConfig config, int? seedOverride = null)
        {
            Config = config;
            _rng = new Random(seedOverride ?? config.Seed);
            _spawner = new SpawnSystem(config, _rng);
            _rules = new GameRules(config);
            Router = new PassengerRouter(Network, config.TransferPenalty);
        }

        public void Initialize()
        {
            var shapes = new[] { StationShape.Circle, StationShape.Triangle, StationShape.Square };
            for (int i = 0; i < Config.InitialStationCount; i++)
            {
                var pos = _spawner.FindStationPosition(Network)
                          ?? new Vector2(i * 3f - (Config.InitialStationCount - 1) * 1.5f, 0f);
                AddStationAt(shapes[i % shapes.Length], pos);
            }
        }

        public void Tick(float dt)
        {
            if (IsGameOver || dt <= 0f) return;
            ElapsedTime += dt;
            _spawner.Tick(dt, this);
            foreach (var train in Trains) TickTrain(train, dt);
            if (_rules.Tick(dt, Network))
            {
                IsGameOver = true;
                GameOverTriggered?.Invoke();
            }
        }

        // ---- Stations & passengers ----

        public Station AddStationAt(StationShape shape, Vector2 pos)
        {
            var s = new Station(_nextStationId++, shape, pos);
            Network.Stations[s.Id] = s;
            Network.BumpVersion();
            _spawner.RegisterStation(s);
            StationSpawned?.Invoke(s);
            return s;
        }

        public bool TrySpawnStation()
        {
            var pos = _spawner.FindStationPosition(Network);
            if (pos == null) return false;
            AddStationAt((StationShape)_rng.Next(0, 3), pos.Value);
            return true;
        }

        public void SpawnPassenger(int stationId)
        {
            if (IsGameOver) return;
            if (!Network.Stations.TryGetValue(stationId, out var station)) return;
            var candidates = new List<StationShape>();
            foreach (var s in Network.Stations.Values)
                if (s.Shape != station.Shape && !candidates.Contains(s.Shape))
                    candidates.Add(s.Shape);
            if (candidates.Count == 0) return;
            station.Enqueue(new Passenger(_nextPassengerId++, candidates[_rng.Next(candidates.Count)]));
        }

        // ---- Lines ----

        public bool TryCreateLine(int a, int b, out int lineId)
        {
            lineId = -1;
            if (IsGameOver || a == b || LinesAvailable <= 0) return false;
            if (!Network.Stations.ContainsKey(a) || !Network.Stations.ContainsKey(b)) return false;

            var usedColors = new HashSet<int>();
            foreach (var l in Network.Lines.Values) usedColors.Add(l.ColorIndex);
            int color = 0;
            while (usedColors.Contains(color)) color++;

            var line = new Line(_nextLineId++, color);
            line.Stations.Add(a);
            line.Stations.Add(b);
            Network.Lines[line.Id] = line;
            Network.BumpVersion();
            lineId = line.Id;
            LineChanged?.Invoke(line);

            var train = new Train
            {
                Id = _nextTrainId++,
                LineId = line.Id,
                FromIndex = 0,
                ToIndex = 1,
                Direction = 1,
                DwellRemaining = Config.DwellTime,
            };
            Trains.Add(train);
            TrainAdded?.Invoke(train);
            return true;
        }

        public bool TryExtendLine(int lineId, int endStationId, int newStationId)
        {
            if (IsGameOver) return false;
            if (!Network.Lines.TryGetValue(lineId, out var line)) return false;
            if (line.IsLoop) return false;
            if (!Network.Stations.ContainsKey(newStationId)) return false;
            if (line.Contains(newStationId)) return false;

            if (line.Stations[0] == endStationId)
            {
                line.Stations.Insert(0, newStationId);
                foreach (var t in Trains)
                {
                    if (t.LineId != lineId) continue;
                    t.FromIndex++;
                    t.ToIndex++;
                }
            }
            else if (line.Stations[line.Stations.Count - 1] == endStationId)
            {
                line.Stations.Add(newStationId);
            }
            else return false;

            Network.BumpVersion();
            LineChanged?.Invoke(line);
            return true;
        }

        /// <summary>
        /// Inserts an existing station into the middle of a line, splitting segment
        /// `segmentIndex` (between Stations[i] and Stations[i+1]; for loops, index
        /// Count-1 is the wrap-around segment).
        /// </summary>
        public bool TryInsertStation(int lineId, int segmentIndex, int stationId)
        {
            if (IsGameOver) return false;
            if (!Network.Lines.TryGetValue(lineId, out var line)) return false;
            if (!Network.Stations.ContainsKey(stationId)) return false;
            if (line.Contains(stationId)) return false;
            int segCount = line.IsLoop ? line.Stations.Count : line.Stations.Count - 1;
            if (segmentIndex < 0 || segmentIndex >= segCount) return false;

            if (segmentIndex == line.Stations.Count - 1)
            {
                line.Stations.Add(stationId); // wrap-around segment of a loop
            }
            else
            {
                line.Stations.Insert(segmentIndex + 1, stationId);
                foreach (var t in Trains)
                {
                    if (t.LineId != lineId) continue;
                    if (t.FromIndex > segmentIndex) t.FromIndex++;
                    if (t.ToIndex > segmentIndex) t.ToIndex++;
                    // A train straddling the split segment now has a 2-wide span;
                    // ClampTrainToLine snaps it on its next tick.
                }
            }
            Network.BumpVersion();
            LineChanged?.Invoke(line);
            return true;
        }

        /// <summary>Closes an open line of 3+ stations into a loop.</summary>
        public bool TryCloseLoop(int lineId)
        {
            if (IsGameOver) return false;
            if (!Network.Lines.TryGetValue(lineId, out var line)) return false;
            if (line.IsLoop || line.Stations.Count < 3) return false;
            line.IsLoop = true;
            Network.BumpVersion();
            LineChanged?.Invoke(line);
            return true;
        }

        /// <summary>Adds an extra train to an existing line.</summary>
        public bool TryAddTrain(int lineId)
        {
            if (IsGameOver) return false;
            if (!Network.Lines.TryGetValue(lineId, out var line) || line.Stations.Count < 2) return false;
            var train = new Train
            {
                Id = _nextTrainId++,
                LineId = lineId,
                FromIndex = 0,
                ToIndex = 1,
                Direction = 1,
                DwellRemaining = Config.DwellTime,
            };
            Trains.Add(train);
            TrainAdded?.Invoke(train);
            return true;
        }

        public bool TryRemoveLine(int lineId)
        {
            if (!Network.Lines.TryGetValue(lineId, out var line)) return false;
            for (int i = Trains.Count - 1; i >= 0; i--)
            {
                var t = Trains[i];
                if (t.LineId != lineId) continue;
                // Dump riders at the segment's departure station.
                int idx = Math.Clamp(t.FromIndex, 0, line.Stations.Count - 1);
                var station = Network.Stations[line.Stations[idx]];
                foreach (var p in t.Riders) station.Enqueue(p);
                Trains.RemoveAt(i);
                TrainRemoved?.Invoke(t.Id);
            }
            Network.Lines.Remove(lineId);
            Network.BumpVersion();
            LineRemoved?.Invoke(lineId);
            return true;
        }

        // ---- Trains ----

        public Vector2 GetTrainPosition(Train train)
        {
            var (from, to) = GetTrainSegment(train);
            return Vector2.Lerp(from, to, Math.Clamp(train.Progress, 0f, 1f));
        }

        public (Vector2 from, Vector2 to) GetTrainSegment(Train train)
        {
            if (!Network.Lines.TryGetValue(train.LineId, out var line) || line.Stations.Count < 2)
                return (Vector2.Zero, Vector2.Zero);
            int n = line.Stations.Count;
            var from = Network.Stations[line.Stations[Math.Clamp(train.FromIndex, 0, n - 1)]].Position;
            var to = Network.Stations[line.Stations[Math.Clamp(train.ToIndex, 0, n - 1)]].Position;
            return (from, to);
        }

        void TickTrain(Train train, float dt)
        {
            if (!Network.Lines.TryGetValue(train.LineId, out var line) || line.Stations.Count < 2) return;
            ClampTrainToLine(train, line);

            if (train.DwellRemaining > 0f)
            {
                train.DwellRemaining -= dt;
                return;
            }

            var from = Network.Stations[line.Stations[train.FromIndex]].Position;
            var to = Network.Stations[line.Stations[train.ToIndex]].Position;
            float len = Vector2.Distance(from, to);
            train.Progress = len < 0.001f ? 1f : train.Progress + Config.TrainSpeed * dt / len;
            if (train.Progress >= 1f) ArriveAtStation(train, line);
        }

        void ClampTrainToLine(Train train, Line line)
        {
            int n = line.Stations.Count;
            bool adjacent = Math.Abs(train.FromIndex - train.ToIndex) == 1
                || (line.IsLoop && Math.Abs(train.FromIndex - train.ToIndex) == n - 1);
            bool broken = train.FromIndex < 0 || train.FromIndex >= n
                       || train.ToIndex < 0 || train.ToIndex >= n
                       || train.FromIndex == train.ToIndex
                       || !adjacent;
            if (!broken) return;
            train.FromIndex = Math.Clamp(train.FromIndex, 0, n - 1);
            train.Direction = train.FromIndex == n - 1 ? -1 : 1;
            train.ToIndex = train.FromIndex + train.Direction;
            train.Progress = 0f;
        }

        void ArriveAtStation(Train train, Line line)
        {
            int arrivedIdx = train.ToIndex;
            var station = Network.Stations[line.Stations[arrivedIdx]];
            int n = line.Stations.Count;

            int dir = train.Direction;
            if (line.IsLoop)
            {
                train.ToIndex = (arrivedIdx + dir + n) % n;
            }
            else
            {
                if (arrivedIdx == n - 1) dir = -1;
                else if (arrivedIdx == 0) dir = 1;
                train.ToIndex = arrivedIdx + dir;
            }
            train.Direction = dir;
            train.FromIndex = arrivedIdx;
            train.Progress = 0f;
            train.DwellRemaining = Config.DwellTime;

            int nextStop = line.Stations[train.ToIndex];

            // Alight: delivered, or this train no longer heads the right way (transfer).
            for (int i = train.Riders.Count - 1; i >= 0; i--)
            {
                var p = train.Riders[i];
                if (station.Shape == p.Target)
                {
                    train.Riders.RemoveAt(i);
                    Score++;
                    ScoreChanged?.Invoke(Score);
                    continue;
                }
                var step = Router.NextStep(station.Id, p.Target);
                if (step == null || step.Value.NextStationId != nextStop)
                {
                    train.Riders.RemoveAt(i);
                    station.Enqueue(p);
                }
            }

            // Board: waiting passengers whose best next hop is this train's next stop.
            for (int i = 0; i < station.Queue.Count && train.Riders.Count < Config.TrainCapacity;)
            {
                var p = station.Queue[i];
                var step = Router.NextStep(station.Id, p.Target);
                if (step != null && step.Value.NextStationId == nextStop)
                {
                    station.Remove(p);
                    train.Riders.Add(p);
                }
                else i++;
            }
        }
    }
}
