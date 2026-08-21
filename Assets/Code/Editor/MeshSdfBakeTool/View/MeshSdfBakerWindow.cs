using UnityEditor;
using UnityEngine;
using UsefulMesh.Application;
using UsefulMesh.DataAccess;
using UsefulMesh.Infrastructure;
using UsefulMesh.Presentation;

namespace UsefulMesh.View
{
    public class MeshSDFBakerWindow : EditorWindow, IMeshSdfBakeView
    {
        private Mesh _targetMesh;
        private string _statusMessage = "待機中";
        private float _currentProgress = 0f;
        private bool _isBaking = false;
        
        private readonly BakePresenter _presenter = new();

        [MenuItem("UsefulTools/UsefulMesh/SDF/MeshSdfBakerWindow")]
        public static void ShowWindow()
        {
            GetWindow<MeshSDFBakerWindow>("Mesh SDF Baker");
        }

        private void OnEnable()
        {
            _presenter.Bind(this);
        }

        private void OnDisable()
        {
            _presenter.Unbind();
        }

        private void OnGUI()
        {
            GUILayout.Label("SDF Bake Tool (試作版)", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            _targetMesh = (Mesh)EditorGUILayout.ObjectField("対象メッシュ", _targetMesh, typeof(Mesh), false);

            EditorGUILayout.Space();

            EditorGUI.BeginDisabledGroup(_isBaking || _targetMesh == null);
            if (GUILayout.Button("SDFをベイクする", GUILayout.Height(30)))
            {
                SetupAndExecuteBake();
            }

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();

            // 進捗状況の描画
            if (_isBaking)
            {
                Rect r = EditorGUILayout.GetControlRect(false, 20);
                EditorGUI.ProgressBar(r, _currentProgress, _statusMessage);
            }
            else
            {
                EditorGUILayout.HelpBox(_statusMessage, MessageType.Info);
            }
        }

        /// <summary>
        /// Composition Root としての役割をここで果たす
        /// </summary>
        private void SetupAndExecuteBake()
        {
            _isBaking = true;

            // 依存性の手動注入 (Composition)
            IMeshGeometryGateway geometryGateway = new MeshGeometryGateway();
            ISdfAssetRepository assetRepository = new SdfAssetRepository();

            IBakeInputPort useCase = new BakeUseCase(geometryGateway, assetRepository, _presenter);

            // 同期処理として即時実行（要件定義：10秒以上かかっても不問）
            try
            {
                useCase.ExecuteBake(_targetMesh);
            }
            finally
            {
                _isBaking = false;
                EditorUtility.ClearProgressBar();
            }
        }

        // --- IMeshSdfBakeViewの実装 ---
        public void DisplayProgress(string message, float progress)
        {
            _statusMessage = message;
            _currentProgress = progress;

            // エディタの同期処理中にインジケータを視覚化
            EditorUtility.DisplayProgressBar("SDF Bake", _statusMessage, _currentProgress);
        }

        public void ClearProgress(string logMessage)
        {
            _statusMessage = logMessage;
            _currentProgress = 0f;
            EditorUtility.ClearProgressBar();
            Debug.Log(logMessage);
        }
    }
}