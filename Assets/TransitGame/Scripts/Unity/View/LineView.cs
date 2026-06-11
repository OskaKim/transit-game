using TransitCore.Model;
using TransitCore.Simulation;
using UnityEngine;

namespace TransitGame
{
    public class LineView : MonoBehaviour
    {
        public int LineId { get; private set; }
        public Color Color { get; private set; }

        SimulationEngine _engine;
        LineRenderer _renderer;

        public void Init(SimulationEngine engine, Line line)
        {
            _engine = engine;
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
            _renderer.positionCount = line.Stations.Count;
            for (int i = 0; i < line.Stations.Count; i++)
            {
                var p = _engine.Network.Stations[line.Stations[i]].Position;
                _renderer.SetPosition(i, new Vector3(p.X, p.Y, 0.3f));
            }
        }
    }
}
