using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TransitGame
{
    /// <summary>
    /// Mini Metro style drag editing. A drag builds up a path of stations:
    /// passing over a station appends it, passing over the previous one undoes
    /// the last stop, passing over any other station already in the path removes
    /// it (the line will skip it). On release the whole path is committed at once
    /// (extend / create / close loop per consecutive pair). Releasing a single
    /// station onto a line segment inserts it mid-line. Right-click deletes a line.
    /// </summary>
    public class LineEditController : MonoBehaviour
    {
        const float StationPickRadius = 0.75f;
        const float StationEnterRadius = 0.6f;
        const float LinePickRadius = 0.3f;

        Bootstrap _boot;
        readonly List<int> _path = new List<int>();
        int _lastHover = -1;
        LineRenderer _preview;

        bool Dragging => _path.Count > 0;

        public void Bind(Bootstrap boot)
        {
            _boot = boot;
            CancelDrag();
        }

        void Update()
        {
            if (_boot == null || _boot.Engine == null || _boot.Engine.IsGameOver)
            {
                CancelDrag();
                return;
            }
            var mouse = Mouse.current;
            var cam = Camera.main;
            if (mouse == null || cam == null) return;

            Vector2 screen = mouse.position.ReadValue();
            Vector3 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z));
            world.z = 0f;

            if (mouse.leftButton.wasPressedThisFrame)
            {
                int station = FindStationNear(world, StationPickRadius);
                if (station >= 0)
                {
                    _path.Clear();
                    _path.Add(station);
                    _lastHover = station;
                    EnsurePreview();
                    _preview.enabled = true;
                }
            }

            if (Dragging)
            {
                int hover = FindStationNear(world, StationEnterRadius);
                if (hover != _lastHover)
                {
                    _lastHover = hover;
                    if (hover >= 0) OnEnterStation(hover);
                }
                UpdatePreview(world);

                if (mouse.leftButton.wasReleasedThisFrame)
                {
                    CommitPath(world);
                    CancelDrag();
                }
            }

            if (mouse.rightButton.wasPressedThisFrame)
            {
                var (lineId, _) = FindLineNear(world);
                if (lineId >= 0) _boot.Engine.TryRemoveLine(lineId);
            }
        }

        void OnEnterStation(int station)
        {
            int index = _path.IndexOf(station);
            if (index < 0)
            {
                _path.Add(station);
            }
            else if (index == _path.Count - 1)
            {
                // Hovering the current tip - nothing to do.
            }
            else if (index == _path.Count - 2)
            {
                // Dragged back onto the previous stop -> undo the last one.
                _path.RemoveAt(_path.Count - 1);
            }
            else
            {
                // Already a stop elsewhere in the path -> stop stopping there.
                _path.RemoveAt(index);
            }
        }

        void CommitPath(Vector3 world)
        {
            var engine = _boot.Engine;
            if (_path.Count == 1)
            {
                // Single station dropped onto a line segment -> insert mid-line.
                var (lineId, segIndex) = FindLineNear(world);
                if (lineId >= 0) engine.TryInsertStation(lineId, segIndex, _path[0]);
                return;
            }
            for (int i = 0; i < _path.Count - 1; i++)
                Connect(_path[i], _path[i + 1]);
        }

        void Connect(int a, int b)
        {
            var engine = _boot.Engine;
            if (AlreadyAdjacent(a, b)) return;
            foreach (var line in engine.Network.Lines.Values)
            {
                // Both ends of the same open line -> close it into a loop.
                if (line.IsEndpoint(a) && line.IsEndpoint(b) && !line.IsLoop
                    && engine.TryCloseLoop(line.Id)) return;
                if (line.IsEndpoint(a) && !line.Contains(b) && engine.TryExtendLine(line.Id, a, b)) return;
                if (line.IsEndpoint(b) && !line.Contains(a) && engine.TryExtendLine(line.Id, b, a)) return;
            }
            engine.TryCreateLine(a, b, out _);
        }

        bool AlreadyAdjacent(int a, int b)
        {
            foreach (var line in _boot.Engine.Network.Lines.Values)
            {
                int n = line.Stations.Count;
                int segCount = line.IsLoop ? n : n - 1;
                for (int i = 0; i < segCount; i++)
                {
                    int s0 = line.Stations[i];
                    int s1 = line.Stations[(i + 1) % n];
                    if ((s0 == a && s1 == b) || (s0 == b && s1 == a)) return true;
                }
            }
            return false;
        }

        void CancelDrag()
        {
            _path.Clear();
            _lastHover = -1;
            if (_preview != null) _preview.enabled = false;
        }

        void EnsurePreview()
        {
            if (_preview != null) return;
            var go = new GameObject("LinePreview");
            _preview = go.AddComponent<LineRenderer>();
            _preview.material = VisualFactory.MakeMaterial(new Color(0.4f, 0.4f, 0.4f, 0.8f));
            _preview.startWidth = _preview.endWidth = 0.1f;
            _preview.useWorldSpace = true;
            _preview.numCornerVertices = 4;
        }

        void UpdatePreview(Vector3 world)
        {
            _preview.positionCount = _path.Count + 1;
            for (int i = 0; i < _path.Count; i++)
            {
                var p = StationPosition(_path[i]);
                _preview.SetPosition(i, new Vector3(p.x, p.y, -0.3f));
            }
            _preview.SetPosition(_path.Count, new Vector3(world.x, world.y, -0.3f));
        }

        Vector2 StationPosition(int id)
        {
            var p = _boot.Engine.Network.Stations[id].Position;
            return new Vector2(p.X, p.Y);
        }

        int FindStationNear(Vector3 world, float radius)
        {
            int bestId = -1;
            float bestDist = radius;
            foreach (var s in _boot.Engine.Network.Stations.Values)
            {
                float d = Vector2.Distance(new Vector2(s.Position.X, s.Position.Y), new Vector2(world.x, world.y));
                if (d < bestDist)
                {
                    bestDist = d;
                    bestId = s.Id;
                }
            }
            return bestId;
        }

        (int lineId, int segmentIndex) FindLineNear(Vector3 world)
        {
            var p = new Vector2(world.x, world.y);
            int bestId = -1;
            int bestSeg = -1;
            float bestDist = LinePickRadius;
            foreach (var line in _boot.Engine.Network.Lines.Values)
            {
                int segCount = line.IsLoop ? line.Stations.Count : line.Stations.Count - 1;
                for (int i = 0; i < segCount; i++)
                {
                    var a = StationPosition(line.Stations[i]);
                    var b = StationPosition(line.Stations[(i + 1) % line.Stations.Count]);
                    float d = DistancePointSegment(p, a, b);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        bestId = line.Id;
                        bestSeg = i;
                    }
                }
            }
            return (bestId, bestSeg);
        }

        static float DistancePointSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            float len2 = ab.sqrMagnitude;
            if (len2 < 0.0001f) return Vector2.Distance(p, a);
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2);
            return Vector2.Distance(p, a + ab * t);
        }
    }
}
