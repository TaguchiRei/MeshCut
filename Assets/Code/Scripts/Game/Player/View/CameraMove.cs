using UnityEngine;

public class CameraMove : MonoBehaviour, ICameraMove
{
    public bool CanMove { get; private set; }

    [Header("Target")] [SerializeField] private Transform _target; // プレイヤー
    [SerializeField] private Transform _cameraTransform; // 子オブジェクト（実際のカメラ）

    [Header("Settings")] [SerializeField] private float _rotationTime = 0.5f; // target.up に追従する時間
    [SerializeField] private float _sensitivityX = 1f; // 左右感度
    [SerializeField] private float _sensitivityY = 1f; // 上下感度
    [SerializeField] private float _maxPitchAngle = 80f; // target基準の上下最大角度
    [SerializeField] private float _parentRotationSpeed = 360f; // 1秒で回転する度数
    private Quaternion _targetRotation;

    private float _pitch = 0f; // 垂直回転（上下）
    private float _yaw = 0f; // 水平回転（左右）

    private void Start()
    {
        CanMove = true;
    }

    private void LateUpdate()
    {
        if (!CanMove || _target == null || _cameraTransform == null) return;

        transform.position = _target.position;
        // 毎フレーム _targetRotation に向かって回転
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            _targetRotation,
            _parentRotationSpeed * Time.deltaTime
        );

        // 親の位置追従
        if (_target != null)
            transform.position = _target.position;
    }

    // 入力による回転
    public void Look(Vector2 inputVector)
    {
        if (!CanMove || _target == null || _cameraTransform == null) return;

        // 入力に感度を掛ける
        _yaw += inputVector.x * _sensitivityX;
        _pitch -= inputVector.y * _sensitivityY;

        // target.up を基準に上下制限
        _pitch = Mathf.Clamp(_pitch, -_maxPitchAngle, _maxPitchAngle);

        // 子オブジェクトの回転に反映
        _cameraTransform.localRotation = Quaternion.Euler(_pitch, _yaw, 0f);
    }

    public void ParentUpChange(Vector3 newGroundNormal)
    {
        // 法線がほぼ同じ場合は処理しない
        if (Vector3.Angle(transform.up, newGroundNormal) < 0.01f)
            return;

        // 回転軸を計算
        Vector3 axis = Vector3.Cross(transform.up, newGroundNormal);
        if (axis.sqrMagnitude < 0.0001f)
            axis = Vector3.right;

        float angle = Vector3.Angle(transform.up, newGroundNormal);

        Quaternion rotationDelta = Quaternion.AngleAxis(angle, axis.normalized);

        // 目標回転として保存
        _targetRotation = rotationDelta * transform.rotation;
    }
}

public interface ICameraMove
{
    public bool CanMove { get; }
    public void Look(Vector2 inputVector);

    public void ParentUpChange(Vector3 newGroundNormal);
}