using UnityEngine;

namespace UsefulMesh.Application
{
    public class BakeUseCase : IBakeInputPort
    {
        private readonly IMeshGeometryGateway _geometryGateway;
        private readonly ISdfAssetRepository _assetRepository;
        private readonly IBakeStatusPresenter _presenter;

        private const int Resolution = 64;

        public BakeUseCase(
            IMeshGeometryGateway geometryGateway,
            ISdfAssetRepository assetRepository,
            IBakeStatusPresenter presenter)
        {
            _geometryGateway = geometryGateway;
            _assetRepository = assetRepository;
            _presenter = presenter;
        }

        public void ExecuteBake(Mesh targetMesh)
        {
            if (targetMesh == null)
            {
                _presenter.CompleteBake("エラー: メッシュが指定されていません。");
                return;
            }

            _geometryGateway.SetTargetMesh(targetMesh);
            Vector3[] triangles = _geometryGateway.GetTriangles();
            Bounds bounds = _geometryGateway.GetBounds();

            int totalVoxels = Resolution * Resolution * Resolution;
            float[] sdfData = new float[totalVoxels];

            int triangleCount = triangles.Length / 3;

            // CPUでの愚直な3重ループ（将来ここをComputeShader等にする場合も、Gatewayの裏側に隠蔽可能）
            for (int z = 0; z < Resolution; z++)
            {
                // 人間の認知負荷・プロトタイプ速度を優先し、適度にプログレスを通知
                _presenter.UpdateProgress($"SDFを計算中... ({z}/{Resolution})", (float)z / Resolution);

                for (int y = 0; y < Resolution; y++)
                {
                    for (int x = 0; x < Resolution; x++)
                    {
                        int index = x + (y * Resolution) + (z * Resolution * Resolution);
                        Vector3 voxelCenter =
                            SDFVoxelCalculator.GetVoxelCenter(x, y, z, bounds.min, bounds.max, Resolution);

                        // Step 1: 最短距離計算
                        float minDistanceSq = float.MaxValue;
                        for (int t = 0; t < triangleCount; t++)
                        {
                            float distSq = SDFVoxelCalculator.CalculateMinDistanceSq(
                                voxelCenter,
                                triangles[t * 3],
                                triangles[t * 3 + 1],
                                triangles[t * 3 + 2]
                            );
                            if (distSq < minDistanceSq)
                            {
                                minDistanceSq = distSq;
                            }
                        }

                        float distance = Mathf.Sqrt(minDistanceSq);

                        // Step 2: 内外判定
                        bool isInside = _geometryGateway.IsInside(voxelCenter);

                        // 要件定義：内側は負、外側は正
                        sdfData[index] = isInside ? -distance : distance;
                    }
                }
            }

            // アセット保存
            _assetRepository.SaveAsset(sdfData, bounds.min, bounds.max, Resolution, targetMesh.name);
            _presenter.CompleteBake($"{targetMesh.name} のSDFアセットを正常に生成しました。");
        }
    }
}