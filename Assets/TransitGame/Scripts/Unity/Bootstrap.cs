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

        Transform _world;
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
            if (Engine != null && !Engine.IsGameOver) Engine.Tick(Time.deltaTime);
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

            // New station can change existing line geometry? No — but refresh keeps views honest.
            foreach (var lineView in _lineViews.Values)
                if (Engine.Network.Lines.TryGetValue(lineView.LineId, out var line))
                    lineView.Refresh(line);
        }

        void OnLineChanged(Line line)
        {
            if (_lineViews.TryGetValue(line.Id, out var view))
            {
                view.Refresh(line);
                return;
            }
            var go = new GameObject($"Line_{line.Id}");
            go.transform.SetParent(_world, false);
            var lineView = go.AddComponent<LineView>();
            lineView.Init(Engine, line);
            _lineViews[line.Id] = lineView;
        }

        void OnLineRemoved(int lineId)
        {
            if (!_lineViews.TryGetValue(lineId, out var view)) return;
            Destroy(view.gameObject);
            _lineViews.Remove(lineId);
        }

        void OnTrainAdded(Train train)
        {
            var go = new GameObject($"Train_{train.Id}");
            go.transform.SetParent(_world, false);
            var view = go.AddComponent<TrainView>();
            var color = _lineViews.TryGetValue(train.LineId, out var lineView)
                ? lineView.Color
                : Color.gray;
            view.Init(Engine, train, color);
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
