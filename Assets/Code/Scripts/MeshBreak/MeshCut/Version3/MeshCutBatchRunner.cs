using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UsefulAttribute;
using Debug = UnityEngine.Debug;

namespace MeshBreak.MeshCut.Version3
{
    public class MeshCutBatchRunner : MonoBehaviour
    {
        [SerializeField] private ObjectShardPool _objectShardPool;
        [SerializeField] private Collider _collider;

        public const int MaxVertexCount = 50000;

        private static readonly ThreadLocal<CutBuffers> ThreadBuffers
            = new ThreadLocal<CutBuffers>(() => new CutBuffers(MaxVertexCount));

        private class CutBuffers
        {
            public readonly Vector3[] LeftVertex, RightVertex;
            public readonly Vector3[] LeftNormal, RightNormal;
            public readonly Vector2[] LeftUv, RightUv;

            public CutBuffers(int size)
            {
                LeftVertex = new Vector3[size];
                RightVertex = new Vector3[size];
                LeftNormal = new Vector3[size];
                RightNormal = new Vector3[size];
                LeftUv = new Vector2[size];
                RightUv = new Vector2[size];
            }
        }

        [MethodExecutor]
        public async void CutMesh()
        {
            var targets = CheckOverlapObjects();
            if (targets.Length == 0) return;

            Plane blade = new Plane(_collider.transform.up, _collider.transform.position);
            var results = await CutMeshBatchAsync(targets, blade);
            foreach (var result in results)
            {
                result.SetActive(true);
            }
        }

        public async UniTask<List<GameObject>> CutMeshBatchAsync(BreakableObject[] targets, Plane blade)
        {
#if UNITY_EDITOR
            var sw = Stopwatch.StartNew();
#endif
            // メインスレッドで全オブジェクトのデータを収集
            // キャッシュ済みグループと断片グループに分けて収集
            var inputs =
                new List<(MeshCutInput input, Material[] materials, Material capMaterial, int vertexCount)>(
                    targets.Length);

            foreach (var target in targets)
            {
                if (target == null) continue;

                MeshCutInput input;

                if (!target.IsCutFragment)
                {
                    // 元モデル → キャッシュから取得
                    MeshDataCache.Instance.Get(target.MeshId, out var cached);
                    input = CollectInputFromCache(cached, blade, target.transform);
                }
                else
                {
                    // 切断済み断片 → MeshFilter.meshから直接読む
                    input = CollectInputOnMainThread(target.MeshFilter.mesh, blade, target.transform);
                }

                var materials = target.MeshRenderer.materials;
                inputs.Add((input, materials, target.CutFaceMaterial, input.Vertices.Length));
            }

#if UNITY_EDITOR
            Debug.Log($"[Batch] 全データ収集完了 ({inputs.Count}件) {sw.ElapsedMilliseconds}ms");
#endif
            // 頂点数でソートして重いものと軽いものを分けて並列投入
            // 重いメッシュが軽いメッシュの処理を待たせないようにソート
            var sortedInputs = inputs
                .Select((item, idx) => (item, originalIdx: idx))
                .OrderByDescending(x => x.item.vertexCount)
                .ToList();

            var allTasks = sortedInputs
                .Select(x => UniTask.RunOnThreadPool(() => Calculate(x.item.input)))
                .ToArray();

            var allResults = await UniTask.WhenAll(allTasks);

#if UNITY_EDITOR
            Debug.Log($"[Batch] 全計算完了 ({inputs.Count}件) {sw.ElapsedMilliseconds}ms");
#endif
            // メインスレッドで全結果をGameObjectに反映
            // ソート前のインデックスに対応するtargetsと突き合わせる
            List<GameObject> results = new();
            for (int i = 0; i < sortedInputs.Count; i++)
            {
                var originalIdx = sortedInputs[i].originalIdx;
                var target = targets[originalIdx];
                if (target == null) continue;

                var (item, _) = sortedInputs[i];
                results.AddRange(
                    ApplyResultOnMainThread(target, allResults[i], item.materials, item.capMaterial));
            }

#if UNITY_EDITOR
            Debug.Log($"[Batch] 全GameObject生成完了 {sw.ElapsedMilliseconds}ms");
            sw.Stop();
#endif
            return results;
        }

        private BreakableObject[] CheckOverlapObjects()
        {
            List<BreakableObject> objects = new();
            Collider[] hits = Physics.OverlapBox(
                _collider.bounds.center,
                _collider.bounds.extents,
                Quaternion.identity
            );

            foreach (Collider hit in hits)
            {
                if (!hit.gameObject.TryGetComponent(out BreakableObject breakable)) continue;

                if (breakable.MeshFilter.mesh.vertexCount > MaxVertexCount)
                {
                    Debug.LogWarning($"{hit.gameObject.name} は頂点数が {MaxVertexCount} を超えるため切断をスキップします");
                    continue;
                }

                objects.Add(breakable);
            }

            return objects.ToArray();
        }

        /// <summary>
        /// キャッシュ済みデータからMeshCutInputを生成する（元モデル用）
        /// </summary>
        public static MeshCutInput CollectInputFromCache(CachedMeshData cached, Plane blade, Transform transform)
        {
            return new MeshCutInput(
                cached.Vertices,
                cached.Normals,
                cached.UVs,
                cached.SubMeshTriangles,
                TransformPlane(blade, transform.worldToLocalMatrix),
                transform.localToWorldMatrix
            );
        }

        /// <summary>
        /// MeshFilter.meshから直接読む（切断済み断片用）
        /// </summary>
        public static MeshCutInput CollectInputOnMainThread(Mesh mesh, Plane blade, Transform transform)
        {
            int subCount = mesh.subMeshCount;
            var subMeshTriangles = new int[subCount][];
            for (int i = 0; i < subCount; i++)
                subMeshTriangles[i] = mesh.GetTriangles(i);

            return new MeshCutInput(
                mesh.vertices,
                mesh.normals,
                mesh.uv,
                subMeshTriangles,
                TransformPlane(blade, transform.worldToLocalMatrix),
                transform.localToWorldMatrix
            );
        }

        private static Plane TransformPlane(Plane plane, Matrix4x4 matrix)
        {
            Vector3 newNormal = matrix.inverse.transpose.MultiplyVector(plane.normal).normalized;
            Vector3 pointOnPlane = plane.normal * (-plane.distance);
            Vector3 newPoint = matrix.MultiplyPoint3x4(pointOnPlane);
            return new Plane(newNormal, newPoint);
        }

        public static MeshCutResult Calculate(MeshCutInput input)
        {
            var sw = Stopwatch.StartNew();

            var buf = ThreadBuffers.Value;

            var blade = input.Blade;
            var baseVerts = input.Vertices;
            var baseNorms = input.Normals;
            var baseUVs = input.UVs;

            var leftMeshData = new CutMeshData(baseVerts, baseNorms, baseUVs,
                buf.LeftVertex, buf.LeftNormal, buf.LeftUv);
            var rightMeshData = new CutMeshData(baseVerts, baseNorms, baseUVs,
                buf.RightVertex, buf.RightNormal, buf.RightUv);

            var centers = new List<Vector3>();
            var capConnections = new Dictionary<Vector3, List<Vector3>>(1024);

            Debug.Log($"[Phase1] 初期化完了 {sw.ElapsedMilliseconds}ms  頂点数:{baseVerts.Length}");

            var baseVertsSide = new bool[baseVerts.Length];
            for (int i = 0; i < baseVerts.Length; i++)
                baseVertsSide[i] = blade.GetSide(baseVerts[i]);

            Debug.Log($"[Phase2a] 左右判定完了 {sw.ElapsedMilliseconds}ms");

            for (int submesh = 0; submesh < input.SubMeshTriangles.Length; submesh++)
            {
                var triangles = input.SubMeshTriangles[submesh];
                leftMeshData.AddSubMesh();
                rightMeshData.AddSubMesh();

                for (int i = 0; i < triangles.Length; i += 3)
                {
                    int p1 = triangles[i];
                    int p2 = triangles[i + 1];
                    int p3 = triangles[i + 2];

                    bool left = baseVertsSide[p1] || baseVertsSide[p2] || baseVertsSide[p3];
                    bool right = !baseVertsSide[p1] || !baseVertsSide[p2] || !baseVertsSide[p3];

                    if (left && !right)
                    {
                        leftMeshData.AddTriangle(p1, p2, p3, submesh);
                        continue;
                    }

                    if (right && !left)
                    {
                        rightMeshData.AddTriangle(p1, p2, p3, submesh);
                        continue;
                    }

                    var triangleData = new TriangleData();
                    CutFace(submesh, p1, p2, p3,
                        blade, baseVerts, baseNorms, baseUVs, baseVertsSide,
                        leftMeshData, rightMeshData, capConnections,
                        ref triangleData);
                }
            }

            Debug.Log($"[Phase2b] 三角形仕分け完了 {sw.ElapsedMilliseconds}ms  capConnections数:{capConnections.Count}");

            {
                var triangleData = new TriangleData();
                leftMeshData.AddSubMesh();
                rightMeshData.AddSubMesh();
                Capping(blade, capConnections, leftMeshData, rightMeshData, centers, ref triangleData);
            }

            Debug.Log($"[Phase2c] Capping完了 {sw.ElapsedMilliseconds}ms  centers数:{centers.Count}");

            var result = new MeshCutResult(leftMeshData, rightMeshData, centers);

            Debug.Log(
                $"[Phase3] 完了 {sw.ElapsedMilliseconds}ms  左:{leftMeshData.VertexCount}頂点  右:{rightMeshData.VertexCount}頂点");
            sw.Stop();

            return result;
        }

        public GameObject[] ApplyResultOnMainThread(
            BreakableObject target,
            MeshCutResult result,
            Material[] originalMaterials,
            Material capMaterial)
        {
            var mats = originalMaterials;
            if (mats[^1].name != capMaterial.name)
            {
                var newMats = new Material[mats.Length + 1];
                mats.CopyTo(newMats, 0);
                newMats[mats.Length] = capMaterial;
                mats = newMats;
            }

            var centers = result.Centers;
            GameObject leftObj = null;
            GameObject rightObj = null;

            if (result.LeftMeshData.VertexCount >= 2)
            {
                var leftMesh = CreateMeshFromCutData(result.LeftMeshData, "Split Mesh Left");
                var leftResult = _objectShardPool.GenerateCutObject(
                    target.gameObject, result.LeftMeshData.Vertices, mats, centers);
                if (!leftResult.Item2) leftResult.Item1.GetComponent<MeshCollider>().sharedMesh = leftMesh;
                leftResult.Item1.GetComponent<MeshFilter>().mesh = leftMesh;
                leftObj = leftResult.Item1;
            }

            var rightMesh = CreateMeshFromCutData(result.RightMeshData, "Split Mesh Right");
            var rightResult = _objectShardPool.GenerateCutObject(
                target.gameObject, result.RightMeshData.Vertices, mats, centers);
            if (!rightResult.Item2) rightResult.Item1.GetComponent<MeshCollider>().sharedMesh = rightMesh;
            rightResult.Item1.GetComponent<MeshFilter>().mesh = rightMesh;
            rightObj = rightResult.Item1;

            // 生成した断片に切断済みフラグをマーク
            if (leftObj != null && leftObj.TryGetComponent<BreakableObject>(out var leftBreakable))
                leftBreakable.MarkAsCutFragment();
            if (rightObj != null && rightObj.TryGetComponent<BreakableObject>(out var rightBreakable))
                rightBreakable.MarkAsCutFragment();

            target.gameObject.SetActive(false);
            return new[] { leftObj, rightObj };
        }

        private Mesh CreateMeshFromCutData(CutMeshData data, string name)
        {
            var mesh = new Mesh { name = name };
            if (data.VertexCount > 65535) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            mesh.SetVertices(data.Vertices, 0, data.VertexCount);
            mesh.SetNormals(data.Normals, 0, data.VertexCount);
            mesh.SetUVs(0, data.Uvs, 0, data.VertexCount);
            mesh.subMeshCount = data.SubIndices.Count;
            for (int i = 0; i < data.SubIndices.Count; i++)
                mesh.SetTriangles(data.SubIndices[i], i);

            return mesh;
        }

        private static void CutFace(
            int submesh, int index1, int index2, int index3,
            in Plane blade,
            Vector3[] baseVertices, Vector3[] baseNormals, Vector2[] baseUVs,
            bool[] baseVerticesSide,
            CutMeshData leftMeshData, CutMeshData rightMeshData,
            Dictionary<Vector3, List<Vector3>> capConnections,
            ref TriangleData triangleData)
        {
            // new Vector3[2] × 6 をスタック変数に置き換え
            Vector3 leftPoint0 = Vector3.zero, leftPoint1 = Vector3.zero;
            Vector3 leftNorm0 = Vector3.zero, leftNorm1 = Vector3.zero;
            Vector2 leftUv0 = Vector2.zero, leftUv1 = Vector2.zero;
            Vector3 rightPoint0 = Vector3.zero, rightPoint1 = Vector3.zero;
            Vector3 rightNorm0 = Vector3.zero, rightNorm1 = Vector3.zero;
            Vector2 rightUv0 = Vector2.zero, rightUv1 = Vector2.zero;

            bool setLeft = false, setRight = false;

            for (int side = 0; side < 3; side++)
            {
                int p = side == 0 ? index1 : side == 1 ? index2 : index3;

                if (baseVerticesSide[p])
                {
                    if (!setLeft)
                    {
                        setLeft = true;
                        leftPoint0 = leftPoint1 = baseVertices[p];
                        leftUv0 = leftUv1 = baseUVs[p];
                        leftNorm0 = leftNorm1 = baseNormals[p];
                    }
                    else
                    {
                        leftPoint1 = baseVertices[p];
                        leftUv1 = baseUVs[p];
                        leftNorm1 = baseNormals[p];
                    }
                }
                else
                {
                    if (!setRight)
                    {
                        setRight = true;
                        rightPoint0 = rightPoint1 = baseVertices[p];
                        rightUv0 = rightUv1 = baseUVs[p];
                        rightNorm0 = rightNorm1 = baseNormals[p];
                    }
                    else
                    {
                        rightPoint1 = baseVertices[p];
                        rightUv1 = baseUVs[p];
                        rightNorm1 = baseNormals[p];
                    }
                }
            }

            Vector3 dir1 = rightPoint0 - leftPoint0;
            float t1 = (-Vector3.Dot(blade.normal, leftPoint0) - blade.distance) / Vector3.Dot(blade.normal, dir1);
            Vector3 newVertex1 = leftPoint0 + dir1 * t1;
            Vector2 newUv1 = leftUv0 + (rightUv0 - leftUv0) * t1;
            Vector3 newNormal1 = leftNorm0 + (rightNorm0 - leftNorm0) * t1;

            Vector3 dir2 = rightPoint1 - leftPoint1;
            float t2 = (-Vector3.Dot(blade.normal, leftPoint1) - blade.distance) / Vector3.Dot(blade.normal, dir2);
            Vector3 newVertex2 = leftPoint1 + dir2 * t2;
            Vector2 newUv2 = leftUv1 + (rightUv1 - leftUv1) * t2;
            Vector3 newNormal2 = leftNorm1 + (rightNorm1 - leftNorm1) * t2;

            AddCapConnection(capConnections, newVertex1, newVertex2);
            AddCapConnection(capConnections, newVertex2, newVertex1);

            bool leftDoubleCheck = false;

            triangleData.SetVertexes(leftPoint0, newVertex1, newVertex2);
            triangleData.SetNormals(leftNorm0, newNormal1, newNormal2);
            triangleData.SetUVs(leftUv0, newUv1, newUv2);
            leftMeshData.AddTriangle(triangleData, newNormal1, submesh);

            if (leftPoint0 != leftPoint1)
            {
                triangleData.SetVertexes(leftPoint0, leftPoint1, newVertex2);
                triangleData.SetNormals(leftNorm0, leftNorm1, newNormal2);
                triangleData.SetUVs(leftUv0, leftUv1, newUv2);
                leftMeshData.AddTriangle(triangleData, newNormal2, submesh);
                leftDoubleCheck = true;
            }

            triangleData.SetVertexes(rightPoint0, newVertex1, newVertex2);
            triangleData.SetNormals(rightNorm0, newNormal1, newNormal2);
            triangleData.SetUVs(rightUv0, newUv1, newUv2);
            rightMeshData.AddTriangle(triangleData, newNormal1, submesh);

            if (!leftDoubleCheck)
            {
                triangleData.SetVertexes(rightPoint0, rightPoint1, newVertex2);
                triangleData.SetNormals(rightNorm0, rightNorm1, newNormal2);
                triangleData.SetUVs(rightUv0, rightUv1, newUv2);
                rightMeshData.AddTriangle(triangleData, newNormal2, submesh);
            }
        }

        private static void Capping(
            in Plane blade,
            Dictionary<Vector3, List<Vector3>> capConnections,
            CutMeshData leftMeshData, CutMeshData rightMeshData,
            List<Vector3> centers,
            ref TriangleData triangleData)
        {
            var visited = new HashSet<Vector3>();

            foreach (var kv in capConnections)
            {
                if (visited.Contains(kv.Key)) continue;

                var polygon = new List<Vector3>();
                Vector3 current = kv.Key;
                polygon.Add(current);
                visited.Add(current);

                while (true)
                {
                    if (!capConnections.TryGetValue(current, out var neighbors)) break;
                    Vector3 next = neighbors.FirstOrDefault(v => !visited.Contains(v));
                    if (next == default) break;
                    polygon.Add(next);
                    visited.Add(next);
                    current = next;
                }

                FillCap(polygon, blade, leftMeshData, rightMeshData, centers, ref triangleData);
            }
        }

        private static void FillCap(
            List<Vector3> vertices,
            in Plane blade,
            CutMeshData leftMeshData, CutMeshData rightMeshData,
            List<Vector3> centers,
            ref TriangleData triangleData)
        {
            Vector3 center = Vector3.zero;
            foreach (var p in vertices) center += p;
            center /= vertices.Count;
            centers.Add(center);

            Vector3 upward = new Vector3(blade.normal.y, -blade.normal.x, blade.normal.z);
            Vector3 left = Vector3.Cross(blade.normal, upward);

            for (int i = 0; i < vertices.Count; i++)
            {
                var d1 = vertices[i] - center;
                var uv1 = new Vector2(0.5f + Vector3.Dot(d1, left), 0.5f + Vector3.Dot(d1, upward));
                var d2 = vertices[(i + 1) % vertices.Count] - center;
                var uv2 = new Vector2(0.5f + Vector3.Dot(d2, left), 0.5f + Vector3.Dot(d2, upward));
                var uvC = new Vector2(0.5f, 0.5f);

                triangleData.SetVertexes(vertices[i], vertices[(i + 1) % vertices.Count], center);
                triangleData.SetNormals(-blade.normal, -blade.normal, -blade.normal);
                triangleData.SetUVs(uv1, uv2, uvC);
                leftMeshData.AddTriangle(triangleData, -blade.normal, leftMeshData.SubIndices.Count - 1);

                triangleData.SetNormals(blade.normal, blade.normal, blade.normal);
                rightMeshData.AddTriangle(triangleData, blade.normal, rightMeshData.SubIndices.Count - 1);
            }
        }

        private static void AddCapConnection(
            Dictionary<Vector3, List<Vector3>> capConnections, Vector3 a, Vector3 b)
        {
            if (!capConnections.TryGetValue(a, out var list))
            {
                list = new List<Vector3>();
                capConnections[a] = list;
            }

            list.Add(b);
        }

        private void OnDrawGizmos()
        {
            BladePlaneDebugger.OnDrawGizmos(transform);
        }
    }
}