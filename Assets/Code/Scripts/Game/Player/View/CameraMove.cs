using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class CameraMove : MonoBehaviour, ICameraMove
{
    public bool CanMove { get; private set; }

    [Header("Target")] [SerializeField] private Transform _target;
    [SerializeField] private Transform _cameraTransform;

    [Header("Settings")] [SerializeField] private float _rotationTime = 0.5f;
    [SerializeField] private float _sensitivityX = 1f;
    [SerializeField] private float _sensitivityY = 1f;
    [SerializeField] private float _maxPitchAngle = 80f;
    [SerializeField] private float _parentRotationSpeed = 360f;
    private Quaternion _targetRotation;

    private float _pitch;
    private float _yaw;
    private float _timeScale;
    private CancellationTokenSource _cts;
    private ITimeScaleManagement _gameTimeScaleManager;

    private void Start()
    {
        _targetRotation = transform.rotation;
        CanMove = true;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        if (ServiceLocator.Instance.TryGetService(out _gameTimeScaleManager))
        {
            _gameTimeScaleManager.ReleaseEvent += OnTimeScaleResume;
            _gameTimeScaleManager.TimeScaleChangeEvent += OnTimeScaleChange;
        }
    }

    private void LateUpdate()
    {
        transform.position = _target.position;

        // 毎フレーム _targetRotation に向かって回転
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            _targetRotation,
            _parentRotationSpeed * Time.deltaTime
        );
    }

    private void OnDestroy()
    {
        _gameTimeScaleManager.ReleaseEvent -= OnTimeScaleResume;
        _gameTimeScaleManager.TimeScaleChangeEvent -= OnTimeScaleChange;
    }

    // 入力による回転
    public void Look(Vector2 inputVector)
    {
        if (_timeScale < 1) return;
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

    public void LockCamera(float lockTime)
    {
        // 既に変更中なら解除
        ResumeCamera();

        _cts = new();

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        LockCamera(lockTime, _cts.Token).Forget();
    }

    public void ResumeCamera()
    {
        if (_cts == null) return;

        // タスクをキャンセルして CTS を破棄
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        _cts.Cancel();
        _cts.Dispose();
        _cts = null;
    }

    public async UniTask LockCamera(float lockTime, CancellationToken ct)
    {
        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(lockTime), cancellationToken: ct);
        }
        catch (OperationCanceledException)
        {
            ResumeCamera();
        }
    }

    private void OnTimeScaleChange(float timeScale)
    {
        _timeScale = timeScale;
    }

    private void OnTimeScaleResume()
    {
        _timeScale = 1;
    }
}

public interface ICameraMove
{
    public bool CanMove { get; }
    public void Look(Vector2 inputVector);

    public void ParentUpChange(Vector3 newGroundNormal);

    public void LockCamera(float lockTime);

    public void ResumeCamera();
}