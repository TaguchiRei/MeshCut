using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

public class EnemyManagerHP : MonoBehaviour
{
    [SerializeField] private GameObject _enemyPrefab;
    [SerializeField] private GameObject _playerObject;

    [Header("敵の基礎パラメータ")] [SerializeField] private int _enemyCount = 100;
    [SerializeField] private float _enemySpeed = 5f;
    [SerializeField] private float _enemyAcceleration = 8f;
    [SerializeField] private float _enemyMoveDistanceMax = 15f;
    [SerializeField] private float _enemyMoveDistanceMin = 5f;
    [SerializeField] private float _speedModifierRange = 0.2f;

    [Header("過去位置を更新する間隔（秒）")] [SerializeField]
    private float _playerHistoryUpdateInterval = 0.5f;

    [Header("アルキメデスの螺旋設定")] [SerializeField]
    private float _vortexSpace = 1.0f;

    [SerializeField] private float _minRadius = 0f;
    [SerializeField] private float _maxRadius = 10f;

    [Header("移動制限エリア")] [SerializeField] private Vector3 _maxPos = new Vector3(50, 0, 50);
    [SerializeField] private Vector3 _minPos = new Vector3(-50, 0, -50);

    private GameObject[] _enemyObjects;
    private EnemyDataHP[] _enemyData;
    private Vector3[] _playerPositions;

    private float _historyTimer; // 履歴更新用のタイマー

    private EnemyMovementCalculationHP _emc;
    private EnemyGroupContext _context;
    private Plane _field;
    private bool _initialized;

    private async void Start()
    {
        _initialized = false;

        _playerPositions = new Vector3[2];
        Vector3 playerPos = _playerObject.transform.position;
        _playerPositions[0] = playerPos;
        _playerPositions[1] = playerPos;
        _historyTimer = 0f;

        _enemyObjects = await InstantiateAsync(_enemyPrefab, _enemyCount, transform);
        _enemyData = new EnemyDataHP[_enemyCount];

        _emc = new EnemyMovementCalculationHP();
        _context = new EnemyGroupContext
        {
            GlobalIndex = 0,
            VertexSpace = _vortexSpace,
            MinRadius = _minRadius,
            MaxRadius = _maxRadius,
            MaxBounds = _maxPos,
            MinBounds = _minPos
        };

        _field = new Plane(transform.up, transform.position);

        for (int i = 0; i < _enemyCount; i++)
        {
            _enemyData[i] = new EnemyDataHP
            {
                Position = _enemyObjects[i].transform.localPosition,
                TargetPositionOffset = new Vector3(
                    Random.Range(-_minRadius, _minRadius),
                    0,
                    Random.Range(-_minRadius, _minRadius)
                ),
                MoveStartDistance = Random.Range(_enemyMoveDistanceMin, _enemyMoveDistanceMax),
                MoveSpeedModifier = 1f + Random.Range(-_speedModifierRange, _speedModifierRange),
                IsMoving = false
            };
            _enemyObjects[i].transform.localRotation = Quaternion.identity;
        }

        _initialized = true;
    }

    private void Update()
    {
        if (!_initialized) return;

        // 1. プレイヤー位置の更新
        // index 0 は毎フレーム最新を追う
        _playerPositions[0] = _playerObject.transform.position;

        // index 1 (過去位置) は指定秒数ごとに更新
        _historyTimer += Time.deltaTime;
        if (_historyTimer >= _playerHistoryUpdateInterval)
        {
            _playerPositions[1] = _playerPositions[0];
            _historyTimer = 0f;
        }

        // 2. HP版の思想：計算から更新まで一括実行
        _emc.MoveEnemy(
            ref _enemyData,
            _playerPositions,
            _context,
            _field,
            _enemySpeed,
            _enemyAcceleration,
            Time.deltaTime);

        // 3. GameObjectへの反映
        for (int i = 0; i < _enemyData.Length; i++)
        {
            _enemyObjects[i].transform.localPosition = _enemyData[i].Position;
        }
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // プレイヤーの追従ターゲットを可視化（デバッグ用）
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(_playerPositions[0], 0.5f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(_playerPositions[1], 0.5f);
    }
}