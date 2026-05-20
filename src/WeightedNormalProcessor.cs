using System;
using System.Collections.Generic;
using Autodesk.Max;

namespace EasyMaxWeightedNormal
{
    internal static class WeightedNormalProcessor
    {
        private const float Epsilon = 1.0e-8f;

        public static void Apply(IMesh mesh, WeightedNormalSettings settings)
        {
            var global = GlobalInterface.Instance;
            int faceCount = mesh.NumFaces;
            int cornerCount = faceCount * 3;

            var faces = new FaceData[faceCount];
            var cornerContributions = new Vec3[cornerCount];
            var cornerSmoothingGroups = new uint[cornerCount];
            var cornersByVertex = new Dictionary<int, List<int>>();

            for (int faceIndex = 0; faceIndex < faceCount; faceIndex++)
            {
                IFace face = mesh.Faces[faceIndex];
                var data = FaceData.FromMeshFace(mesh, face);
                faces[faceIndex] = data;

                for (int corner = 0; corner < 3; corner++)
                {
                    int cornerIndex = ToCornerIndex(faceIndex, corner);
                    // System.Diagnostics.Debug.WriteLine(cornerIndex.ToString());
                    
                    cornerSmoothingGroups[cornerIndex] = face.SmGroup;

                    if (!cornersByVertex.TryGetValue(data.VertexIndices[corner], out var vertexCorners))
                    {
                        vertexCorners = new List<int>();
                        cornersByVertex.Add(data.VertexIndices[corner], vertexCorners);
                    }

                    vertexCorners.Add(cornerIndex);
                    cornerContributions[cornerIndex] = BuildContribution(data, corner, settings);
                }
            }

            var disjointSet = new DisjointSet(cornerCount);
            if (settings.RespectSmoothingGroups)
            {
                UnionSmoothingGroupCorners(disjointSet, cornersByVertex, cornerSmoothingGroups);
            }
            else
            {
                UnionAllVertexCorners(disjointSet, cornersByVertex);
            }

            var sumsByRoot = new Dictionary<int, Vec3>();
            for (int cornerIndex = 0; cornerIndex < cornerCount; cornerIndex++)
            {
                int root = disjointSet.Find(cornerIndex);
                sumsByRoot[root] = sumsByRoot.TryGetValue(root, out var sum)
                    ? sum + cornerContributions[cornerIndex]
                    : cornerContributions[cornerIndex];
            }

            var normalIdByRoot = new Dictionary<int, int>();
            var normalValues = new List<Vec3>();
            foreach (var pair in sumsByRoot)
            {
                normalIdByRoot.Add(pair.Key, normalValues.Count);
                normalValues.Add(pair.Value.NormalizedOr(Vec3.UnitZ));
            }

            mesh.SpecifyNormals();
            IMeshNormalSpec normalSpec = mesh.SpecifiedNormals;
            normalSpec.SetParent(mesh);
            normalSpec.SetFlag(PluginConstants.MeshNormalModifierSupport, true);
            normalSpec.SetNumFaces(faceCount);
            normalSpec.SetNumNormals(normalValues.Count);

            for (int i = 0; i < normalValues.Count; i++)
            {
                Vec3 n = normalValues[i];
                normalSpec.Normal(i).Set(n.X, n.Y, n.Z);
                normalSpec.SetNormalExplicit(i, true);
            }

            for (int faceIndex = 0; faceIndex < faceCount; faceIndex++)
            {
                for (int corner = 0; corner < 3; corner++)
                {
                    int cornerIndex = ToCornerIndex(faceIndex, corner);
                    int normalId = normalIdByRoot[disjointSet.Find(cornerIndex)];
                    Vec3 n = normalValues[normalId];

                    normalSpec.SetNormalIndex(faceIndex, corner, normalId);
                    normalSpec.SetNormal(faceIndex, corner, global.Point3.Create(n.X, n.Y, n.Z));
                }
            }

            normalSpec.SetAllExplicit(true);
            normalSpec.CheckNormals();
        }

        private static void UnionSmoothingGroupCorners(
            DisjointSet disjointSet,
            Dictionary<int, List<int>> cornersByVertex,
            uint[] cornerSmoothingGroups)
        {
            foreach (var vertexCorners in cornersByVertex.Values)
            {
                for (int i = 0; i < vertexCorners.Count; i++)
                {
                    int a = vertexCorners[i];
                    uint groupA = cornerSmoothingGroups[a];
                    if (groupA == 0)
                    {
                        continue;
                    }

                    for (int j = i + 1; j < vertexCorners.Count; j++)
                    {
                        int b = vertexCorners[j];
                        uint groupB = cornerSmoothingGroups[b];
                        if (groupB != 0 && (groupA & groupB) != 0)
                        {
                            disjointSet.Union(a, b);
                        }
                    }
                }
            }
        }

        private static void UnionAllVertexCorners(DisjointSet disjointSet, Dictionary<int, List<int>> cornersByVertex)
        {
            foreach (var vertexCorners in cornersByVertex.Values)
            {
                for (int i = 1; i < vertexCorners.Count; i++)
                {
                    disjointSet.Union(vertexCorners[0], vertexCorners[i]);
                }
            }
        }

        private static Vec3 BuildContribution(FaceData face, int corner, WeightedNormalSettings settings)
        {
            Vec3 normal = settings.UseAreaWeight
                ? face.UnnormalizedNormal
                : face.UnitNormal;

            if (settings.UseAngleWeight)
            {
                normal *= face.CornerAngles[corner];
            }

            return normal.LengthSquared > Epsilon ? normal : face.UnitNormal;
        }

        private static int ToCornerIndex(int faceIndex, int corner)
        {
            return faceIndex * 3 + corner;
        }

        private sealed class FaceData
        {
            public int[] VertexIndices { get; private set; }

            public Vec3 UnnormalizedNormal { get; private set; }

            public Vec3 UnitNormal { get; private set; }

            public float[] CornerAngles { get; private set; }

            public static FaceData FromMeshFace(IMesh mesh, IFace face)
            {
                var indices = new[]
                {
                    (int)face.GetVert(0),
                    (int)face.GetVert(1),
                    (int)face.GetVert(2)
                };

                Vec3 p0 = Vec3.FromPoint(mesh.Verts[indices[0]]);
                Vec3 p1 = Vec3.FromPoint(mesh.Verts[indices[1]]);
                Vec3 p2 = Vec3.FromPoint(mesh.Verts[indices[2]]);

                Vec3 e01 = p1 - p0;
                Vec3 e02 = p2 - p0;
                Vec3 faceNormal = Vec3.Cross(e01, e02);

                return new FaceData
                {
                    VertexIndices = indices,
                    UnnormalizedNormal = faceNormal,
                    UnitNormal = faceNormal.NormalizedOr(Vec3.UnitZ),
                    CornerAngles = new[]
                    {
                        CornerAngle(p1 - p0, p2 - p0),
                        CornerAngle(p0 - p1, p2 - p1),
                        CornerAngle(p0 - p2, p1 - p2)
                    }
                };
            }

            private static float CornerAngle(Vec3 a, Vec3 b)
            {
                float len = a.Length * b.Length;
                if (len <= Epsilon)
                {
                    return 0.0f;
                }

                float cos = Vec3.Dot(a, b) / len;
                cos = Math.Max(-1.0f, Math.Min(1.0f, cos));
                return (float)Math.Acos(cos);
            }
        }

        private struct Vec3
        {
            public static readonly Vec3 UnitZ = new Vec3(0.0f, 0.0f, 1.0f);

            public readonly float X;
            public readonly float Y;
            public readonly float Z;

            public Vec3(float x, float y, float z)
            {
                X = x;
                Y = y;
                Z = z;
            }

            public float Length
            {
                get { return (float)Math.Sqrt(LengthSquared); }
            }

            public float LengthSquared
            {
                get { return X * X + Y * Y + Z * Z; }
            }

            public Vec3 NormalizedOr(Vec3 fallback)
            {
                float length = Length;
                return length > Epsilon ? this * (1.0f / length) : fallback;
            }

            public static Vec3 FromPoint(IPoint3 point)
            {
                return new Vec3(point.X, point.Y, point.Z);
            }

            public static Vec3 Cross(Vec3 a, Vec3 b)
            {
                return new Vec3(
                    a.Y * b.Z - a.Z * b.Y,
                    a.Z * b.X - a.X * b.Z,
                    a.X * b.Y - a.Y * b.X);
            }

            public static float Dot(Vec3 a, Vec3 b)
            {
                return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
            }

            public static Vec3 operator +(Vec3 a, Vec3 b)
            {
                return new Vec3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
            }

            public static Vec3 operator -(Vec3 a, Vec3 b)
            {
                return new Vec3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
            }

            public static Vec3 operator *(Vec3 a, float scale)
            {
                return new Vec3(a.X * scale, a.Y * scale, a.Z * scale);
            }
        }

        private sealed class DisjointSet
        {
            private readonly int[] parent;
            private readonly byte[] rank;

            public DisjointSet(int count)
            {
                parent = new int[count];
                rank = new byte[count];

                for (int i = 0; i < count; i++)
                {
                    parent[i] = i;
                }
            }

            public int Find(int value)
            {
                if (parent[value] != value)
                {
                    parent[value] = Find(parent[value]);
                }

                return parent[value];
            }

            public void Union(int a, int b)
            {
                int rootA = Find(a);
                int rootB = Find(b);
                if (rootA == rootB)
                {
                    return;
                }

                if (rank[rootA] < rank[rootB])
                {
                    parent[rootA] = rootB;
                }
                else if (rank[rootA] > rank[rootB])
                {
                    parent[rootB] = rootA;
                }
                else
                {
                    parent[rootB] = rootA;
                    rank[rootA]++;
                }
            }
        }
    }
}
