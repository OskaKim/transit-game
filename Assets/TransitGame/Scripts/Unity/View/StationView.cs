using TransitCore.Model;
using UnityEngine;

namespace TransitGame
{
    public class StationView : MonoBehaviour
    {
        static readonly Color IconColor = new Color(0.15f, 0.15f, 0.15f);
        static readonly Color OvercrowdColor = new Color(1f, 0.25f, 0.2f);

        Station _station;
        float _grace;
        Material _fillMaterial;
        Transform _iconRoot;
        int _lastQueueVersion = -1;

        public void Init(Station station, float overcrowdGrace)
        {
            _station = station;
            _grace = Mathf.Max(0.01f, overcrowdGrace);
            transform.position = new Vector3(station.Position.X, station.Position.Y, 0f);

            var mesh = VisualFactory.BuildShapeMesh(station.Shape);
            var outline = VisualFactory.MakeMeshObject("Outline", mesh, Color.black, transform, 0.1f);
            outline.transform.localScale = Vector3.one * 1.25f;
            var fill = VisualFactory.MakeMeshObject("Fill", mesh, Color.white, transform, 0.05f);
            _fillMaterial = fill.GetComponent<MeshRenderer>().sharedMaterial;

            _iconRoot = new GameObject("Icons").transform;
            _iconRoot.SetParent(transform, false);
            _iconRoot.localPosition = new Vector3(0f, 0.78f, 0f);
        }

        void Update()
        {
            if (_station == null) return;
            if (_station.QueueVersion != _lastQueueVersion)
            {
                _lastQueueVersion = _station.QueueVersion;
                RebuildIcons();
            }
            float t = Mathf.Clamp01(_station.OvercrowdTimer / _grace);
            VisualFactory.SetColor(_fillMaterial, Color.Lerp(Color.white, OvercrowdColor, t));
        }

        void RebuildIcons()
        {
            for (int i = _iconRoot.childCount - 1; i >= 0; i--)
                Destroy(_iconRoot.GetChild(i).gameObject);

            var queue = _station.Queue;
            const int perRow = 6;
            for (int i = 0; i < queue.Count; i++)
            {
                int row = i / perRow;
                int col = i % perRow;
                int rowCount = Mathf.Min(queue.Count - row * perRow, perRow);
                var icon = VisualFactory.MakeMeshObject(
                    "Passenger", VisualFactory.BuildShapeMesh(queue[i].Target), IconColor, _iconRoot, 0f);
                icon.transform.localScale = Vector3.one * 0.2f;
                icon.transform.localPosition = new Vector3(
                    (col - (rowCount - 1) * 0.5f) * 0.26f, row * 0.28f, 0f);
            }
        }
    }
}
