using TransitCore.Model;
using TransitCore.Simulation;
using UnityEngine;

namespace TransitGame
{
    public class LineView : MonoBehaviour
    {
        public int LineId { get; private set; }
        public Color Color { get; private set; }

        Bootstrap _boot;
        SimulationEngine _engine;
        LineRenderer _renderer;

        public void Init(Bootstrap boot, Line line)
        {
            _boot = boot;
            _engine = boot.Engine;
            LineId = line.Id;
            Color = VisualFactory.LineColors[line.ColorIndex % VisualFactory.LineColors.Length];
            _renderer = gameObject.AddComponent<LineRenderer>();
            _renderer.material = VisualFactory.MakeMaterial(Color);
            _renderer.startWidth = _renderer.endWidth = 0.18f;
            _renderer.useWorldSpace = true;
            _renderer.numCornerVertices = 4;
            _renderer.numCapVertices = 4;
            Refresh(line);
        }

        public void Refresh(Line line)
        {
            int n = line.Stations.Count;
            _renderer.loop = line.IsLoop;
            _renderer.positionCount = n;
            for (int i = 0; i < n; i++)
            {
                var p = _engine.Network.Stations[line.Stations[i]].Position;
                // Vertex offset = average of the lane offsets of its adjacent segments,
                // so parallel runs stay parallel and corners stay joined.
                Vector2 offset = Vector2.zero;
                int count = 0;
                if (i > 0 || line.IsLoop)
                {
                    int prev = (i - 1 + n) % n;
                    offset += SegmentOffset(line, prev);
                    count++;
                }
                if (i < n - 1 || line.IsLoop)
                {
                    offset += SegmentOffset(line, i);
                    count++;
                }
                if (count > 0) offset /= count;
                _renderer.SetPosition(i, new Vector3(p.X + offset.x, p.Y + offset.y, 0.3f));
            }
        }

        Vector2 SegmentOffset(Line line, int segmentIndex)
        {
            int n = line.Stations.Count;
            return _boot.GetEdgeOffsetFor(LineId,
                line.Stations[segmentIndex], line.Stations[(segmentIndex + 1) % n]);
        }
    }
}
