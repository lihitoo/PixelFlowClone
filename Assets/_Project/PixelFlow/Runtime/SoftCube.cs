using System.Collections.Generic;
using UnityEngine;

namespace PixelFlow
{
    // A small shared bevelled mesh: no dependency on a recovered shader or opaque prefab.
    public static class SoftCube
    {
        public static Mesh Create(float bevel = .13f)
        {
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            float a = .5f, b = .5f - bevel;
            void Face(params Vector3[] points)
            {
                int start = vertices.Count; vertices.AddRange(points);
                for (int i = 1; i < points.Length - 1; i++) { triangles.Add(start); triangles.Add(start + i); triangles.Add(start + i + 1); }
            }
            // Construct each face from a local right/up basis with an outward winding.
            Vector3[] normals = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            foreach (var n in normals)
            {
                Vector3 u = Mathf.Abs(n.y) > .5f ? Vector3.right : Vector3.up;
                Vector3 v = Vector3.Cross(n, u);
                Face(n * a - u * b - v * b, n * a + u * b - v * b, n * a + u * b + v * b, n * a - u * b + v * b);
            }
            // Twelve edge chamfers.
            for (int axis = 0; axis < 3; axis++)
            for (int first = -1; first <= 1; first += 2)
            for (int second = -1; second <= 1; second += 2)
            {
                int one = (axis + 1) % 3, two = (axis + 2) % 3;
                var p = Vector3.zero; var q = Vector3.zero;
                p[one] = first * a; p[two] = second * b;
                q[one] = first * b; q[two] = second * a;
                var along = Vector3.zero; along[axis] = b;
                var normal = Vector3.zero; normal[one] = first; normal[two] = second;
                Vector3[] points = { p - along, q - along, q + along, p + along };
                if (Vector3.Dot(Vector3.Cross(points[1] - points[0], points[2] - points[0]), normal) < 0) System.Array.Reverse(points);
                Face(points);
            }
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                var points = new[] { new Vector3(x * a, y * b, z * b), new Vector3(x * b, y * a, z * b), new Vector3(x * b, y * b, z * a) };
                if (Vector3.Dot(Vector3.Cross(points[1] - points[0], points[2] - points[0]), new Vector3(x, y, z)) < 0) System.Array.Reverse(points);
                Face(points);
            }
            var mesh = new Mesh { name = "PixelFlow Shared Soft Cube" };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }
    }
}
