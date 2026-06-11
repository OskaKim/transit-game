using TransitCore.Model;
using TransitCore.Simulation;
using UnityEngine;

namespace TransitGame
{
    public class TrainView : MonoBehaviour
    {
        public int TrainId => _train.Id;

        Bootstrap _boot;
        SimulationEngine _engine;
        Train _train;
        Transform _body;
        TextMesh _label;

        public void Init(Bootstrap boot, Train train, Color color)
        {
            _boot = boot;
            _engine = boot.Engine;
            _train = train;

            var body = VisualFactory.MakeMeshObject("Body", VisualFactory.BuildQuad(0.72f, 0.42f), color, transform, 0f);
            _body = body.transform;

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(transform, false);
            labelGo.transform.localPosition = new Vector3(0f, 0f, -0.1f);
            labelGo.transform.localScale = Vector3.one * 0.12f;
            _label = labelGo.AddComponent<TextMesh>();
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _label.font = font;
            labelGo.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            _label.fontSize = 40;
            _label.anchor = TextAnchor.MiddleCenter;
            _label.alignment = TextAlignment.Center;
            _label.color = Color.white;
        }

        void LateUpdate()
        {
            if (_train == null) return;
            var pos = _engine.GetTrainPosition(_train);
            var lane = CurrentLaneOffset();
            transform.position = new Vector3(pos.X + lane.x, pos.Y + lane.y, -0.1f);

            var (from, to) = _engine.GetTrainSegment(_train);
            var dir = new Vector2(to.X - from.X, to.Y - from.Y);
            if (dir.sqrMagnitude > 0.0001f)
                _body.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);

            _label.text = _train.Riders.Count > 0 ? _train.Riders.Count.ToString() : "";
        }

        Vector2 CurrentLaneOffset()
        {
            if (!_engine.Network.Lines.TryGetValue(_train.LineId, out var line)
                || line.Stations.Count < 2) return Vector2.zero;
            int n = line.Stations.Count;
            int fi = Mathf.Clamp(_train.FromIndex, 0, n - 1);
            int ti = Mathf.Clamp(_train.ToIndex, 0, n - 1);
            return _boot.GetEdgeOffsetFor(_train.LineId, line.Stations[fi], line.Stations[ti]);
        }
    }
}
