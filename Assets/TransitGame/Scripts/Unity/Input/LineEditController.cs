using System.Numerics;
using UnityEngine;
using UnityEngine.InputSystem;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace TransitGame
{
    /// <summary>
    /// Left-drag station to station: extends an existing line whose endpoint was grabbed,
    /// otherwise creates a new line (if stock remains). Right-click near a line removes it.
    /// </summary>
    public class LineEditController : MonoBehaviour
    {
        const float StationPickRadius = 0.75f;
        const float LinePickRadius = 0.3f;

        Bootstrap _boot;
        int _dragStationId = -1;
        LineRenderer _preview;

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
                int station = FindStationNear(world);
                if (station >= 0)
                {
                    _dragStationId = station;
                    EnsurePreview();
                    _preview.enabled = true;
                }
            }

            if (_dragStationId >= 0)
            {
                var start = StationPosition(_dragStationId);
                _preview.SetPosition(0, new Vector3(start.x, start.y, -0.3f));
                _preview.SetPosition(1, new Vector3(world.x, world.y, -0.3f));

                if (mouse.leftButton.wasReleasedThisFrame)
                {
                    int target = FindStationNear(world);
                    if (target >= 0 && target != _dragStationId)
                        Connect(_dragStationId, target);
                    CancelDrag();
                }
            }

            if (mouse.rightButton.wasPressedThisFrame)
            {
                int lineId = FindLineNear(world);
                if (lineId >= 0) _boot.Engine.TryRemoveLine(lineId);
            }
        }

        void Connect(int a, int b)
        {
            var engine = _boot.Engine;
            foreach (var line in engine.Network.Lines.Values)
            {
                if (line.IsEndpoint(a) && !line.Contains(b) && engine.TryExtendLine(line.Id, a, b)) return;
                if (line.IsEndpoint(b) && !line.Contains(a) && engine.TryExtendLine(line.Id, b, a)) return;
            }
            engine.TryCreateLine(a, b, out _);
        }

        void CancelDrag()
        {
            _dragStationId = -1;
            if (_preview != null) _preview.enabled = false;
        }

        void EnsurePreview()
        {
            if (_preview != null) return;
            var go = new GameObject("LinePreview");
            _preview = go.AddComponent<LineRenderer>();
            _preview.material = VisualFactory.MakeMaterial(new Color(0.4f, 0.4f, 0.4f, 0.8f));
            _preview.startWidth = _preview.endWidth = 0.1f;
            _preview.positionCount = 2;
            _preview.useWorldSpace = true;
        }

        Vector2 StationPosition(int id)
        {
            var p = _boot.Engine.Network.Stations[id].Position;
            return new Vector2(p.X, p.Y);
        }

        int FindStationNear(Vector3 world)
        {
            int bestId = -1;
            float bestDist = StationPickRadius;
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

        int FindLineNear(Vector3 world)
        {
            var p = new Vector2(world.x, world.y);
            int bestId = -1;
            float bestDist = LinePickRadius;
            foreach (var line in _boot.Engine.Network.Lines.Values)
            {
                for (int i = 0; i < line.Stations.Count - 1; i++)
                {
                    var a = StationPosition(line.Stations[i]);
                    var b = StationPosition(line.Stations[i + 1]);
                    float d = DistancePointSegment(p, a, b);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        bestId = line.Id;
                    }
                }
            }
            return bestId;
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
