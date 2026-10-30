using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace AbejaRA
{
    /// <summary>
    /// Editor-side, deterministic vertex clustering for dense textured props.
    /// Returns a new mesh and never changes source. Target is an approximate
    /// triangle budget, not a guarantee; review the result at its final AR size.
    /// UV-aware cells keep distant atlas coordinates separate. This is not QEM:
    /// fine petals and narrow stems can lose detail at aggressive budgets.
    /// </summary>
    public static class BeeMeshReducer
    {
        struct Cell : IEquatable<Cell>
        {
            public int x, y, z, u, v;
            public bool Equals(Cell b) => x == b.x && y == b.y && z == b.z && u == b.u && v == b.v;
            public override bool Equals(object b) => b is Cell && Equals((Cell)b);
            public override int GetHashCode()
            {
                unchecked
                {
                    int h = x * 73856093 ^ y * 19349663 ^ z * 83492791;
                    return h ^ u * 49979687 ^ v * 86028121;
                }
            }
        }

        struct Accumulator
        {
            public Vector3 position, normal;
            public Vector2 uv;
            public Vector4 color;
            public int count;
        }

        struct Face : IEquatable<Face>
        {
            public int a, b, c, submesh;
            // Rotation-invariant, but reversed faces stay distinct. Thin props
            // sometimes deliberately contain both orientations.
            public Face(int x, int y, int z, int material)
            {
                submesh = material;
                if (x <= y && x <= z) { a = x; b = y; c = z; }
                else if (y <= x && y <= z) { a = y; b = z; c = x; }
                else { a = z; b = x; c = y; }
            }
            public bool Equals(Face f) => a == f.a && b == f.b && c == f.c && submesh == f.submesh;
            public override bool Equals(object f) => f is Face && Equals((Face)f);
            public override int GetHashCode()
            {
                unchecked { return ((a * 73856093 ^ b * 19349663) * 397 ^ c * 83492791) * 397 ^ submesh; }
            }
        }

        sealed class Input
        {
            public Vector3[] vertices, normals;
            public Vector2[] uv;
            public Color32[] colors;
            public int[][] triangles;
            public Vector3 minimum;
            public float inverseExtent;
            public bool hasNormals, hasUV, hasColors;
        }

        public static Mesh Reduce(Mesh source, int targetTriangles = 10000)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (targetTriangles < 4) throw new ArgumentOutOfRangeException(nameof(targetTriangles), "Use a budget of at least four triangles.");
            var input = new Input();
            try
            {
                input.vertices = source.vertices;
                input.normals = source.normals;
                input.uv = source.uv;
                input.colors = source.colors32;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Mesh data must be CPU readable while generating the reduced copy: " + source.name, ex);
            }
            if (input.vertices.Length == 0) throw new ArgumentException("The source mesh has no vertices.", nameof(source));
            input.hasNormals = input.normals.Length == input.vertices.Length;
            input.hasUV = input.uv.Length == input.vertices.Length;
            input.hasColors = input.colors.Length == input.vertices.Length;
            input.minimum = source.bounds.min;
            Vector3 size = source.bounds.size;
            // Normalize by the largest axis, preserving aspect ratio. In
            // particular, imported FBX meshes may be only 0.001 units wide.
            input.inverseExtent = 1f / Mathf.Max(Mathf.Max(size.x, Mathf.Max(size.y, size.z)), 1e-12f);
            input.triangles = new int[source.subMeshCount][];
            long originalCount = 0;
            for (int s = 0; s < source.subMeshCount; s++)
            {
                if (source.GetTopology(s) != MeshTopology.Triangles)
                    throw new NotSupportedException("Only triangle submeshes can be reduced.");
                input.triangles[s] = source.GetTriangles(s);
                originalCount += input.triangles[s].Length / 3;
            }
            if (originalCount <= targetTriangles)
            {
                var clone = UnityEngine.Object.Instantiate(source);
                clone.name = source.name + "_Reduced";
                return clone;
            }

            Mesh best = null;
            float bestError = float.PositiveInfinity;
            int low = 0, high = 0;
            var visited = new HashSet<int>();
            int Evaluate(int grid)
            {
                visited.Add(grid);
                int count;
                Mesh candidate = Build(input, grid, out count);
                float error = count == 0 ? float.PositiveInfinity : Mathf.Abs((float)count - targetTriangles) / targetTriangles;
                if (error < bestError)
                {
                    if (best != null) UnityEngine.Object.DestroyImmediate(best);
                    best = candidate;
                    bestError = error;
                }
                else UnityEngine.Object.DestroyImmediate(candidate);
                return count;
            }

            int resolution = 24;
            int triangles = Evaluate(resolution);
            if (triangles > targetTriangles)
            {
                high = resolution;
                while (resolution > 1)
                {
                    resolution = Mathf.Max(1, resolution / 2);
                    triangles = Evaluate(resolution);
                    if (triangles <= targetTriangles) { low = resolution; break; }
                    high = resolution;
                }
            }
            else
            {
                low = resolution;
                while (resolution < 384)
                {
                    resolution *= 2;
                    triangles = Evaluate(resolution);
                    if (triangles >= targetTriangles) { high = resolution; break; }
                    low = resolution;
                    if (bestError <= .08f) break;
                }
            }
            for (int iteration = 0; iteration < 7 && high > low + 1; iteration++)
            {
                int middle = (low + high) / 2;
                if (visited.Contains(middle)) break;
                int count = Evaluate(middle);
                if (count < targetTriangles) low = middle; else high = middle;
                if (bestError <= .06f) break;
            }
            if (best == null || best.vertexCount == 0)
                throw new InvalidOperationException("The mesh could not be reduced without collapsing all faces: " + source.name);
            best.name = source.name + "_AR_Reduced";
            return best;
        }

        static Mesh Build(Input input, int resolution, out int triangleCount)
        {
            int uvResolution = Mathf.Max(8, resolution * 2);
            var cells = new Dictionary<Cell, int>(Mathf.Min(input.vertices.Length, 150000));
            var sums = new List<Accumulator>(Mathf.Min(input.vertices.Length, 150000));
            var remap = new int[input.vertices.Length];
            for (int i = 0; i < input.vertices.Length; i++)
            {
                Vector3 p = (input.vertices[i] - input.minimum) * input.inverseExtent;
                Vector2 uv = input.hasUV ? input.uv[i] : Vector2.zero;
                var key = new Cell
                {
                    x = Mathf.RoundToInt(p.x * resolution),
                    y = Mathf.RoundToInt(p.y * resolution),
                    z = Mathf.RoundToInt(p.z * resolution),
                    u = Mathf.RoundToInt(uv.x * uvResolution),
                    v = Mathf.RoundToInt(uv.y * uvResolution)
                };
                int cluster;
                if (!cells.TryGetValue(key, out cluster))
                {
                    cluster = sums.Count;
                    cells.Add(key, cluster);
                    sums.Add(new Accumulator());
                }
                remap[i] = cluster;
                var a = sums[cluster];
                a.position += input.vertices[i];
                a.uv += uv;
                if (input.hasNormals) a.normal += input.normals[i];
                if (input.hasColors)
                {
                    Color32 color = input.colors[i];
                    a.color += new Vector4(color.r, color.g, color.b, color.a);
                }
                a.count++;
                sums[cluster] = a;
            }
            cells.Clear();
            var outputTriangles = new List<int>[input.triangles.Length];
            var unique = new HashSet<Face>();
            var used = new bool[sums.Count];
            triangleCount = 0;
            for (int s = 0; s < input.triangles.Length; s++)
            {
                var indices = input.triangles[s];
                var output = new List<int>();
                for (int j = 0; j < indices.Length; j += 3)
                {
                    int a = remap[indices[j]], b = remap[indices[j + 1]], c = remap[indices[j + 2]];
                    if (a == b || b == c || c == a) continue;
                    if (!unique.Add(new Face(a, b, c, s))) continue;
                    output.Add(a); output.Add(b); output.Add(c);
                    used[a] = used[b] = used[c] = true;
                    triangleCount++;
                }
                outputTriangles[s] = output;
            }
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uv0 = new List<Vector2>();
            var colors = new List<Color32>();
            var compact = new int[sums.Count];
            for (int i = 0; i < sums.Count; i++)
            {
                if (!used[i]) continue;
                var a = sums[i];
                compact[i] = vertices.Count;
                vertices.Add(a.position / a.count);
                if (input.hasNormals) normals.Add(a.normal.sqrMagnitude > 1e-16f ? a.normal.normalized : Vector3.up);
                if (input.hasUV) uv0.Add(a.uv / a.count);
                if (input.hasColors)
                {
                    Vector4 c = a.color / a.count;
                    colors.Add(new Color32((byte)c.x, (byte)c.y, (byte)c.z, (byte)c.w));
                }
            }
            var mesh = new Mesh();
            mesh.indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.SetVertices(vertices);
            if (input.hasUV) mesh.SetUVs(0, uv0);
            if (input.hasNormals) mesh.SetNormals(normals);
            if (input.hasColors) mesh.SetColors(colors);
            mesh.subMeshCount = outputTriangles.Length;
            for (int s = 0; s < outputTriangles.Length; s++)
            {
                var indices = outputTriangles[s];
                for (int j = 0; j < indices.Count; j++) indices[j] = compact[indices[j]];
                mesh.SetTriangles(indices, s, false);
            }
            if (!input.hasNormals && vertices.Count > 0) mesh.RecalculateNormals();
            if (input.hasUV && vertices.Count > 0) mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
