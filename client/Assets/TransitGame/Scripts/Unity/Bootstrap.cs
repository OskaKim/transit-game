using System.Collections.Generic;
using TransitCore.Model;
using TransitCore.Simulation;
using UnityEngine;

namespace TransitGame
{
    /// <summary>
    /// Entry point: creates the pure-C# SimulationEngine, drives it from Update,
    /// and mirrors core events into view objects.
    /// </summary>
    public class Bootstrap : MonoBehaviour
    {
        public GameConfig config;

        public SimulationEngine Engine { get; private set; }
        /// <summary>Debug speed multiplier driven by the HUD buttons.</summary>
        public float TimeScale { get; set; } = 1f;

        Transform _world;
        Dictionary<(int, int), List<int>> _edgeMap;
        readonly Dictionary<int, StationView> _stationViews = new Dictionary<int, StationView>();
        readonly Dictionary<int, LineView> _lineViews = new Dictionary<int, LineView>();
        readonly Dictionary<int, TrainView> _trainViews = new Dictionary<int, TrainView>();
        HUDController _hud;
        LineEditController _input;

        void Start()
        {
            SetupCamera();
            _hud = gameObject.AddComponent<HUDController>();
            _input = gameObject.AddComponent<LineEditController>();
            StartGame();
        }

        public void StartGame()
        {
            if (_world != null) Destroy(_world.gameObject);
            _world = new GameObject("World").transform;
            _edgeMap = null;
            _stationViews.Clear();
            _lineViews.Clear();
            _trainViews.Clear();

            var simConfig = config != null ? config.ToSimConfig() : new SimConfig();
            int seed = config == null || config.useRandomSeed ? System.Environment.TickCount : simConfig.Seed;
            Engine = new SimulationEngine(simConfig, seed);
            Engine.StationSpawned += OnStationSpawned;
            Engine.LineChanged += OnLineChanged;
            Engine.LineRemoved += OnLineRemoved;
            Engine.TrainAdded += OnTrainAdded;
            Engine.TrainRemoved += OnTrainRemoved;
            Engine.Initialize();

            _hud.Bind(this);
            _input.Bind(this);
        }

        void Update()
        {
            if (Engine != null && !Engine.IsGameOver) Engine.Tick(Time.deltaTime * TimeScale);
        }

        /// <summary>Lane offset for a line on a given station-to-station segment (view concern).</summary>
        public Vector2 GetEdgeOffsetFor(int lineId, int stationA, int stationB) =>
            LineLaneLayout.GetOffset(_edgeMap, Engine.Network, lineId, stationA, stationB);

        /// <summary>Rebuilds lane assignments and redraws every line (shared segments shift).</summary>
        void RefreshAllLines()
        {
            _edgeMap = LineLaneLayout.BuildEdgeMap(Engine.Network);
            foreach (var view in _lineViews.Values)
                if (Engine.Network.Lines.TryGetValue(view.LineId, out var line))
                    view.Refresh(line);
        }

        /// <summary>Adds a train to the line that currently has the fewest trains.</summary>
        public bool AddTrainToSparsestLine()
        {
            int bestLine = -1;
            int bestCount = int.MaxValue;
            foreach (var lineId in Engine.Network.Lines.Keys)
            {
                int count = 0;
                foreach (var t in Engine.Trains)
                    if (t.LineId == lineId) count++;
                if (count < bestCount)
                {
                    bestCount = count;
                    bestLine = lineId;
                }
            }
            return bestLine >= 0 && Engine.TryAddTrain(bestLine);
        }

        void SetupCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = go.AddComponent<Camera>();
            }
            cam.orthographic = true;
            cam.orthographicSize = 5.2f;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.transform.rotation = Quaternion.identity;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.94f, 0.93f, 0.89f);
        }

        void OnStationSpawned(Station station)
        {
            var go = new GameObject($"Station_{station.Id}");
            go.transform.SetParent(_world, false);
            var view = go.AddComponent<StationView>();
            view.Init(station, Engine.Config.OvercrowdGrace);
            _stationViews[station.Id] = view;
            RefreshAllLines();
        }

        void OnLineChanged(Line line)
        {
            if (!_lineViews.ContainsKey(line.Id))
            {
                var go = new GameObject($"Line_{line.Id}");
                go.transform.SetParent(_world, false);
                var lineView = go.AddComponent<LineView>();
                lineView.Init(this, line);
                _lineViews[line.Id] = lineView;
            }
            // A change to one line can shift lane assignments on shared segments.
            RefreshAllLines();
        }

        void OnLineRemoved(int lineId)
        {
            if (!_lineViews.TryGetValue(lineId, out var view)) return;
            Destroy(view.gameObject);
            _lineViews.Remove(lineId);
            RefreshAllLines();
        }

        void OnTrainAdded(Train train)
        {
            var go = new GameObject($"Train_{train.Id}");
            go.transform.SetParent(_world, false);
            var view = go.AddComponent<TrainView>();
            var color = _lineViews.TryGetValue(train.LineId, out var lineView)
                ? lineView.Color
                : Color.gray;
            view.Init(this, train, color);
            _trainViews[train.Id] = view;
        }

        void OnTrainRemoved(int trainId)
        {
            if (!_trainViews.TryGetValue(trainId, out var view)) return;
            Destroy(view.gameObject);
            _trainViews.Remove(trainId);
        }
    }
}
