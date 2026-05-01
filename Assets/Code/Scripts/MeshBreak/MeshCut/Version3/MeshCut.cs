using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace MeshBreak.MeshCut.Version3
{
    public class MeshCut : MonoBehaviour
    {
        [SerializeField] private ObjectShardPool objectShardPool;
        
        /// <summary>
        /// メッシュ切断に利用するデータの取得を行う
        /// </summary>
        /// <param name="target"></param>
        /// <param name="blade"></param>
        /// <returns></returns>
        public static MeshCutInput CollectInputOnMainThread(GameObject target, Plane blade)
        {
            var mesh = target.GetComponent<MeshFilter>().mesh;

            int subCount = mesh.subMeshCount;
            var subMeshTriangles = new int[subCount][];
            for (int i = 0; i < subCount; i++)
                subMeshTriangles[i] = mesh.GetTriangles(i);

            return new MeshCutInput(
                mesh.vertices,
                mesh.normals,
                mesh.uv,
                subMeshTriangles,
                blade,
                target.transform.localToWorldMatrix
            );
        }
        
        /// <summary>
        /// メッシュの切断を行う
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static MeshCutResult Calculate(MeshCutInput input)
        {
            // インスタンス変数をすべてローカルに 
            var blade      = input.Blade;
            var baseVerts  = input.Vertices;
            var baseNorms  = input.Normals;
            var baseUVs    = input.UVs;

            var leftMeshData  = new CutMeshData(baseVerts, baseNorms, baseUVs);
            var rightMeshData = new CutMeshData(baseVerts, baseNorms, baseUVs);
            var centers       = new List<Vector3>();
            var capConnections = new Dictionary<Vector3, List<Vector3>>();

            // 頂点の左右判定
            var baseVertsSide = new bool[baseVerts.Length];
            for (int i = 0; i < baseVerts.Length; i++)
                baseVertsSide[i] = blade.GetSide(baseVerts[i]);

            // サブメッシュごとに処理
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

                    bool left  = baseVertsSide[p1] || baseVertsSide[p2] || baseVertsSide[p3];
                    bool right = !baseVertsSide[p1] || !baseVertsSide[p2] || !baseVertsSide[p3];

                    if (left && !right) { leftMeshData.AddTriangle(p1, p2, p3, submesh);  continue; }
                    if (right && !left) { rightMeshData.AddTriangle(p1, p2, p3, submesh); continue; }
                    
                    var triangleData = new TriangleData();
                    CutFace(submesh, p1, p2, p3,
                        blade, baseVerts, baseNorms, baseUVs, baseVertsSide,
                        leftMeshData, rightMeshData, capConnections,
                        ref triangleData);
                }
            }
            
            {
                var triangleData = new TriangleData();
                leftMeshData.AddSubMesh();
                rightMeshData.AddSubMesh();
                Capping(blade, capConnections, leftMeshData, rightMeshData, centers, ref triangleData);
            }

            return new MeshCutResult(leftMeshData, rightMeshData, centers);
        }
        
        /// <summary>
        /// 切断結果をGameObjectに変換する
        /// </summary>
        public GameObject[] ApplyResultOnMainThread(
            GameObject target,
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

            var centers      = result.Centers;
            GameObject leftObj  = null;
            GameObject rightObj = null;

            if (result.LeftMeshData.Vertices.Count >= 2)
            {
                var leftMesh   = CreateMeshFromCutData(result.LeftMeshData, "Split Mesh Left");
                var leftResult = objectShardPool.GenerateCutObject(target, result.LeftMeshData.Vertices, mats, centers);
                if (!leftResult.Item2) leftResult.Item1.GetComponent<MeshCollider>().sharedMesh = leftMesh;
                leftResult.Item1.GetComponent<MeshFilter>().mesh = leftMesh;
                leftObj = leftResult.Item1;
            }

            var rightMesh   = CreateMeshFromCutData(result.RightMeshData, "Split Mesh Right");
            var rightResult = objectShardPool.GenerateCutObject(target, result.RightMeshData.Vertices, mats, centers);
            if (!rightResult.Item2) rightResult.Item1.GetComponent<MeshCollider>().sharedMesh = rightMesh;
            rightResult.Item1.GetComponent<MeshFilter>().mesh = rightMesh;
            rightObj = rightResult.Item1;

            target.SetActive(false);
            return new[] { leftObj, rightObj };
        }

        private Mesh CreateMeshFromCutData(CutMeshData data, string name)
        {
            var mesh = new Mesh { name = name };
            if (data.Vertices.Count > 65535) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            
            mesh.SetVertices(data.Vertices);
            mesh.SetNormals(data.Normals);
            mesh.SetUVs(0, data.Uvs);
            mesh.subMeshCount = data.SubIndices.Count;
            for (int i = 0; i < data.SubIndices.Count; i++)
                mesh.SetTriangles(data.SubIndices[i], i);
            
            return mesh;
        }
        
        public async Task<GameObject[]> CutAsync(
            GameObject target, Plane blade, Material capMaterial)
        {
#if UNITY_EDITOR
            var sw = Stopwatch.StartNew();
#endif
            var input    = CollectInputOnMainThread(target, blade);
            var materials = target.GetComponent<MeshRenderer>().materials;
#if UNITY_EDITOR
            Debug.Log($"データ収集完了 {sw.ElapsedMilliseconds}ms");
#endif
            
            var result = await Task.Run(() => Calculate(input));
#if UNITY_EDITOR
            Debug.Log($"切断計算完了 {sw.ElapsedMilliseconds}ms");
#endif
            
            var objects = ApplyResultOnMainThread(target, result, materials, capMaterial);
#if UNITY_EDITOR
            Debug.Log($"GameObject生成完了 {sw.ElapsedMilliseconds}ms");
            sw.Stop();
#endif
            return objects;
        }

        /// <summary>
        /// 面を切断する
        /// </summary>
        private static void CutFace(
            int submesh, int index1, int index2, int index3,
            in Plane blade,
            Vector3[] baseVertices, Vector3[] baseNormals, Vector2[] baseUVs,
            bool[] baseVerticesSide,
            CutMeshData leftMeshData, CutMeshData rightMeshData,
            Dictionary<Vector3, List<Vector3>> capConnections,
            ref TriangleData triangleData)
        {
            var leftPoints  = new Vector3[2];
            var leftNormals = new Vector3[2];
            var leftUvs     = new Vector2[2];
            var rightPoints  = new Vector3[2];
            var rightNormals = new Vector3[2];
            var rightUvs     = new Vector2[2];

            bool setLeft = false, setRight = false;

            for (int side = 0; side < 3; side++)
            {
                int p = side == 0 ? index1 : side == 1 ? index2 : index3;

                if (baseVerticesSide[p])
                {
                    if (!setLeft)
                    {
                        setLeft = true;
                        leftPoints[0]  = baseVertices[p];
                        leftUvs[0]     = baseUVs[p];
                        leftNormals[0] = baseNormals[p];
                        leftPoints[1]  = leftPoints[0];
                        leftUvs[1]     = leftUvs[0];
                        leftNormals[1] = leftNormals[0];
                    }
                    else
                    {
                        leftPoints[1]  = baseVertices[p];
                        leftUvs[1]     = baseUVs[p];
                        leftNormals[1] = baseNormals[p];
                    }
                }
                else
                {
                    if (!setRight)
                    {
                        setRight = true;
                        rightPoints[0]  = baseVertices[p];
                        rightUvs[0]     = baseUVs[p];
                        rightNormals[0] = baseNormals[p];
                        rightPoints[1]  = rightPoints[0];
                        rightUvs[1]     = rightUvs[0];
                        rightNormals[1] = rightNormals[0];
                    }
                    else
                    {
                        rightPoints[1]  = baseVertices[p];
                        rightUvs[1]     = baseUVs[p];
                        rightNormals[1] = baseNormals[p];
                    }
                }
            }

            // 交差点1
            Vector3 dir1 = rightPoints[0] - leftPoints[0];
            float t1 = (-Vector3.Dot(blade.normal, leftPoints[0]) - blade.distance)
                       / Vector3.Dot(blade.normal, dir1);
            Vector3 newVertex1 = leftPoints[0] + dir1 * t1;
            Vector2 newUv1     = leftUvs[0] + (rightUvs[0] - leftUvs[0]) * t1;
            Vector3 newNormal1 = leftNormals[0] + (rightNormals[0] - leftNormals[0]) * t1;

            // 交差点2
            Vector3 dir2 = rightPoints[1] - leftPoints[1];
            float t2 = (-Vector3.Dot(blade.normal, leftPoints[1]) - blade.distance)
                       / Vector3.Dot(blade.normal, dir2);
            Vector3 newVertex2 = leftPoints[1] + dir2 * t2;
            Vector2 newUv2     = leftUvs[1] + (rightUvs[1] - leftUvs[1]) * t2;
            Vector3 newNormal2 = leftNormals[1] + (rightNormals[1] - leftNormals[1]) * t2;

            AddCapConnection(capConnections, newVertex1, newVertex2);
            AddCapConnection(capConnections, newVertex2, newVertex1);

            bool leftDoubleCheck = false;

            triangleData.SetVertexes(leftPoints[0], newVertex1, newVertex2);
            triangleData.SetNormals(leftNormals[0], newNormal1, newNormal2);
            triangleData.SetUVs(leftUvs[0], newUv1, newUv2);
            leftMeshData.AddTriangle(triangleData, newNormal1, submesh);

            if (leftPoints[0] != leftPoints[1])
            {
                triangleData.SetVertexes(leftPoints[0], leftPoints[1], newVertex2);
                triangleData.SetNormals(leftNormals[0], leftNormals[1], newNormal2);
                triangleData.SetUVs(leftUvs[0], leftUvs[1], newUv2);
                leftMeshData.AddTriangle(triangleData, newNormal2, submesh);
                leftDoubleCheck = true;
            }

            triangleData.SetVertexes(rightPoints[0], newVertex1, newVertex2);
            triangleData.SetNormals(rightNormals[0], newNormal1, newNormal2);
            triangleData.SetUVs(rightUvs[0], newUv1, newUv2);
            rightMeshData.AddTriangle(triangleData, newNormal1, submesh);

            if (!leftDoubleCheck)
            {
                triangleData.SetVertexes(rightPoints[0], rightPoints[1], newVertex2);
                triangleData.SetNormals(rightNormals[0], rightNormals[1], newNormal2);
                triangleData.SetUVs(rightUvs[0], rightUvs[1], newUv2);
                rightMeshData.AddTriangle(triangleData, newNormal2, submesh);
            }
        }

        /// <summary>
        /// 切断面を埋める
        /// </summary>
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

        /// <summary>
        /// 埋めた切断面のUVを作成
        /// </summary>
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
            Vector3 left   = Vector3.Cross(blade.normal, upward);

            for (int i = 0; i < vertices.Count; i++)
            {
                var d1   = vertices[i] - center;
                var uv1  = new Vector2(0.5f + Vector3.Dot(d1, left), 0.5f + Vector3.Dot(d1, upward));
                var d2   = vertices[(i + 1) % vertices.Count] - center;
                var uv2  = new Vector2(0.5f + Vector3.Dot(d2, left), 0.5f + Vector3.Dot(d2, upward));
                var uvC  = new Vector2(0.5f, 0.5f);

                triangleData.SetVertexes(vertices[i], vertices[(i + 1) % vertices.Count], center);
                triangleData.SetNormals(-blade.normal, -blade.normal, -blade.normal);
                triangleData.SetUVs(uv1, uv2, uvC);
                leftMeshData.AddTriangle(triangleData, -blade.normal, leftMeshData.SubIndices.Count - 1);

                triangleData.SetNormals(blade.normal, blade.normal, blade.normal);
                rightMeshData.AddTriangle(triangleData, blade.normal, rightMeshData.SubIndices.Count - 1);
            }
        }
        
        /// <summary>
        /// 切断面の外周を保持する
        /// </summary>
        /// <param name="capConnections"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
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
    }
}