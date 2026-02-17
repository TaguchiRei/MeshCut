using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

public class EnemyManager : MonoBehaviour
{
    [SerializeField] private GameObject _enemyPrefab;
    [SerializeField] private GameObject _playerObject;

    [Header("敵の基礎パラメータを調整")] [SerializeField]
    private int _enemyCount;

    [SerializeField] private float _enemySpeed;
    [SerializeField] private float _enemyAccelaration;
    [SerializeField] private float _enemyMoveDistanceMax;
    [SerializeField] private float _enemyMoveDistanceMin;
    [SerializeField] private float _speedModifier;
    [SerializeField] private float _targetPositionOffset;

    [Header("アルキメデスの螺旋を調整")] [SerializeField]
    private float _vortexSpace;

    [SerializeField] private float _minRadius;
    [SerializeField] private float _maxRadius;

    [Header("敵の移動できる空間の設定")] [SerializeField]
    private Vector3 _maxPos;

    [SerializeField] private Vector3 _minPos;

    private GameObject[] _enemyObjects;
    private EnemyData[] _enemyData;
    private Vector3[] _playerPositions;

    private EnemyMovementCalculation _emc;
    private EnemyGroupContext _context;
    private Plane _field;
    private bool _initialized;

    private async void Start()
    {
        _playerPositions = new Vector3[2];
        _playerPositions[0] = _playerObject.transform.position;
        _initialized = false;
        _enemyObjects = await InstantiateAsync(_enemyPrefab, _enemyCount, transform);
        _enemyData = new EnemyData[_enemyCount];
        _emc = new();
        _context = new()
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
            _enemyData[i] = new EnemyData
            {
                TargetPositionOffset =
                    new(Random.Range(-_minRadius, _minRadius), 0, Random.Range(-_minRadius, _minRadius)),
                MoveStartDistance = Random.Range(_enemyMoveDistanceMin, _enemyMoveDistanceMax),
                MoveSpeedModifier = 1f + Random.Range(-_speedModifier, _speedModifier)
            };
        }

        _initialized = true;
        UpdateEnemyVelocity().Forget();
    }

    private void Update()
    {
        if (!_initialized) return;

        for (int i = 0; i < _enemyData.Length; i++)
        {
            // 1. まずターゲットに向かって速度を更新（毎フレーム行うことで滑らかに）
            // UpdateEnemyVelocityで行っていた計算をここ、もしくは頻度の高いループに移動

            // 2. 座標の更新
            _enemyData[i].UpdatePosition(Time.deltaTime, _minPos, _maxPos);

            // 3. GameObjectへの反映
            _enemyObjects[i].transform.position = _enemyData[i].Position;
        }
    }

    private async UniTask UpdateEnemyVelocity()
    {
        while (true)
        {
            _playerPositions[1] = _playerPositions[0];
            _playerPositions[0] = _playerObject.transform.position;

            _emc.UpdateEnemyVelocity(
                ref _enemyData,
                _playerPositions,
                _context,
                _field,
                _enemySpeed,
                _enemyAccelaration,
                Time.deltaTime);

            await UniTask.NextFrame();
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        // ローカル空間基準にする
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;

        Vector3 center = (_minPos + _maxPos) * 0.5f;
        Vector3 size = _maxPos - _minPos;

        Gizmos.DrawWireCube(center, size);

        Gizmos.matrix = oldMatrix;
    }
}