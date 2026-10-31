using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TurboPattes.Game.Visuals
{
    /// <summary>Formes simples construites par code, et raccourcis pour poser des primitives.</summary>
    public static class MeshKit
    {
        static Mesh _cone, _hemisphere;

        /// <summary>Primitive Unity sans collider, enfant de <paramref name="parent"/>.</summary>
        public static GameObject Prim(PrimitiveType type, Transform parent, Vector3 localPos, Vector3 scale, Material mat,
            Quaternion? rot = null, string name = null)
        {
            // Pas de CreatePrimitive : il ajoute un collider, or le module physique est retiré du build mobile.
            var go = MeshObject(name ?? type.ToString(), PrimitiveMesh(type), mat, parent);
            go.transform.localPosition = localPos;
            go.transform.localRotation = rot ?? Quaternion.identity;
            go.transform.localScale = scale;
            return go;
        }

        static readonly Dictionary<PrimitiveType, Mesh> PrimitiveMeshes = new();

        public static Mesh PrimitiveMesh(PrimitiveType type)
        {
            if (PrimitiveMeshes.TryGetValue(type, out var mesh)) return mesh;
            string file = type switch
            {
                PrimitiveType.Sphere => "New-Sphere.fbx",
                PrimitiveType.Capsule => "New-Capsule.fbx",
                PrimitiveType.Cylinder => "New-Cylinder.fbx",
                PrimitiveType.Plane => "New-Plane.fbx",
                PrimitiveType.Quad => "Quad.fbx",
                _ => "Cube.fbx",
            };
            mesh = Resources.GetBuiltinResource<Mesh>(file);
            PrimitiveMeshes[type] = mesh;
            return mesh;
        }

        public static GameObject MeshObject(string name, Mesh mesh, Material mat, Transform parent, bool castShadows = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            return go;
        }

        public static GameObject Shape(Mesh mesh, Transform parent, Vector3 localPos, Vector3 scale, Material mat,
            Quaternion? rot = null, string name = "Shape")
        {
            var go = MeshObject(name, mesh, mat, parent);
            go.transform.localPosition = localPos;
            go.transform.localRotation = rot ?? Quaternion.identity;
            go.transform.localScale = scale;
            return go;
        }

        /// <summary>Cône de rayon 0,5 et hauteur 1, base en y = 0, pointe en y = 1.</summary>
        public static Mesh Cone
        {
            get
            {
                if (_cone != null) return _cone;
                const int seg = 16;
                var v = new List<Vector3>();
                var t = new List<int>();
                for (int i = 0; i < seg; i++)
                {
                    float a0 = i * Mathf.PI * 2f / seg, a1 = (i + 1) * Mathf.PI * 2f / seg;
                    var p0 = new Vector3(Mathf.Cos(a0) * 0.5f, 0, Mathf.Sin(a0) * 0.5f);
                    var p1 = new Vector3(Mathf.Cos(a1) * 0.5f, 0, Mathf.Sin(a1) * 0.5f);
                    int b = v.Count;
                    v.Add(p0); v.Add(Vector3.up); v.Add(p1);
                    t.Add(b); t.Add(b + 1); t.Add(b + 2);
                    b = v.Count;
                    v.Add(p0); v.Add(p1); v.Add(Vector3.zero);
                    t.Add(b); t.Add(b + 1); t.Add(b + 2);
                }
                _cone = new Mesh { name = "Cone" };
                _cone.SetVertices(v);
                _cone.SetTriangles(t, 0);
                _cone.RecalculateNormals();
                _cone.RecalculateBounds();
                return _cone;
            }
        }

        /// <summary>Demi-sphère de rayon 0,5 (dôme vers +y), sans fond.</summary>
        public static Mesh Hemisphere
        {
            get
            {
                if (_hemisphere != null) return _hemisphere;
                const int seg = 20, rings = 8;
                var v = new List<Vector3>();
                var n = new List<Vector3>();
                var t = new List<int>();
                for (int r = 0; r <= rings; r++)
                {
                    float phi = r / (float)rings * Mathf.PI * 0.5f;
                    for (int s = 0; s <= seg; s++)
                    {
                        float th = s / (float)seg * Mathf.PI * 2f;
                        var d = new Vector3(Mathf.Cos(th) * Mathf.Cos(phi), Mathf.Sin(phi), Mathf.Sin(th) * Mathf.Cos(phi));
                        v.Add(d * 0.5f);
                        n.Add(d);
                    }
                }
                for (int r = 0; r < rings; r++)
                for (int s = 0; s < seg; s++)
                {
                    int a = r * (seg + 1) + s, b = a + seg + 1;
                    t.Add(a); t.Add(b); t.Add(a + 1);
                    t.Add(a + 1); t.Add(b); t.Add(b + 1);
                }
                _hemisphere = new Mesh { name = "Hemisphere" };
                _hemisphere.SetVertices(v);
                _hemisphere.SetNormals(n);
                _hemisphere.SetTriangles(t, 0);
                _hemisphere.RecalculateBounds();
                return _hemisphere;
            }
        }

        /// <summary>Active ou coupe l'ombre portée de tous les rendus enfants.</summary>
        public static void SetShadows(GameObject root, bool cast)
        {
            foreach (var r in root.GetComponentsInChildren<Renderer>())
                r.shadowCastingMode = cast ? ShadowCastingMode.On : ShadowCastingMode.Off;
        }
    }
}
