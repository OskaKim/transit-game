using TransitCore.Model;
using UnityEngine;

namespace TransitGame
{
    /// <summary>Procedural meshes and unlit materials so the prototype needs zero art assets.</summary>
    public static class VisualFactory
    {
        public static readonly Color[] LineColors =
        {
            new Color(0.90f, 0.22f, 0.21f),
            new Color(0.18f, 0.49f, 0.90f),
            new Color(0.98f, 0.72f, 0.15f),
            new Color(0.30f, 0.75f, 0.38f),
            new Color(0.65f, 0.35f, 0.85f),
        };

        static Shader _shader;

        static Shader UnlitShader
        {
            get
            {
                if (_shader == null)
                {
                    _shader = Shader.Find("Universal Render Pipeline/Unlit");
                    if (_shader == null) _shader = Shader.Find("Sprites/Default");
                }
                return _shader;
            }
        }

        public static Material MakeMaterial(Color color)
        {
            var m = new Material(UnlitShader);
            SetColor(m, color);
            if (m.HasProperty("_Cull")) m.SetFloat("_Cull", 0f); // double-sided, winding-proof
            return m;
        }

        public static void SetColor(Material m, Color color)
        {
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            m.color = color;
        }

        public static Mesh BuildShapeMesh(StationShape shape)
        {
            switch (shape)
            {
                case StationShape.Circle: return BuildPolygon(0.5f, 32, 0f);
                case StationShape.Triangle: return BuildPolygon(0.58f, 3, 90f);
                default: return BuildPolygon(0.62f, 4, 45f);
            }
        }

        public static Mesh BuildPolygon(float radius, int sides, float startAngleDeg)
        {
            var mesh = new Mesh();
            var verts = new Vector3[sides + 1];
            verts[0] = Vector3.zero;
            for (int i = 0; i < sides; i++)
            {
                float a = (startAngleDeg + 360f * i / sides) * Mathf.Deg2Rad;
                verts[i + 1] = new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f);
            }
            var tris = new int[sides * 3];
            for (int i = 0; i < sides; i++)
            {
                tris[i * 3] = 0;
                tris[i * 3 + 1] = i + 2 > sides ? 1 : i + 2;
                tris[i * 3 + 2] = i + 1;
            }
            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh BuildQuad(float w, float h)
        {
            var mesh = new Mesh
            {
                vertices = new[]
                {
                    new Vector3(-w / 2f, -h / 2f, 0f),
                    new Vector3(w / 2f, -h / 2f, 0f),
                    new Vector3(w / 2f, h / 2f, 0f),
                    new Vector3(-w / 2f, h / 2f, 0f),
                },
                triangles = new[] { 0, 2, 1, 0, 3, 2 },
            };
            mesh.RecalculateBounds();
            return mesh;
        }

        public static GameObject MakeMeshObject(string name, Mesh mesh, Color color, Transform parent, float z)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 0f, z);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = MakeMaterial(color);
            return go;
        }
    }
}
