using UnityEngine;

public class PlayerMove : MonoBehaviour, IPlayerMove
{
    [Header("BasicSetting")]
    [SerializeField] private int _walkSpeed = 5;
    [SerializeField] private int _runSpeed = 8;
    [SerializeField] private int _jumpPower = 5;
    [SerializeField] private float _longPressTime = 0.5f;
    [SerializeField] private float _aimTime = 10;
    [SerializeField, Range(0, 1f)] private float slowMotionTimeScale = 0.5f;
    [SerializeField] private float _aimReleaseTime = 10;
    public int WalkSpeed => _walkSpeed;
    public int RunSpeed => _runSpeed;
    public int JumpPawer => _jumpPower;
    public bool OnGround { get; private set; }
    public Vector3 UpVector { get; private set; }
    public bool Running { get; set; }

    [Header("Components")] 
    [SerializeField] private Rigidbody _rigidbody;

    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private CameraMove _cameraMove;

    [Header("GroundCheck")] [SerializeField]
    private Vector3 _boxHalfExtents = new(0.4f, 0.05f, 0.4f);

    [SerializeField] private float _checkDistance = 0.1f;
    [SerializeField] private LayerMask _groundLayer;


    private Vector3 _gravityDirection = Vector3.down;
    private Vector3 _desiredMoveDirection;
    private float _gravityMagnitude = 9.81f;

    private PlayerController _playerController;
    private ITimeScaleManagement _timeManager;

    private void Start()
    {
        ServiceLocator.Instance.TryGetService(out IInputDispatcher inputDispatcher);
        ServiceLocator.Instance.TryGetService(out _timeManager);

        _playerController = new PlayerController(
            inputDispatcher,
            this,
            _cameraMove,
            new PlayerInputState(_longPressTime, _aimReleaseTime));
        _playerController.SetActiveInput(true);

        _rigidbody.useGravity = false;
        Running = false;
    }

    private void Update()
    {
        GroundCheck();
    }

    private void FixedUpdate()
    {
        ApplyGravity();
        ApplyMovement();
    }

    /// <summary>
    /// 設置判定を行い、当たった地面の法線を取得する
    /// </summary>
    /// <returns>法線</returns>
    private Vector3 GroundCheck()
    {
        Vector3 origin = transform.position;

        OnGround = Physics.BoxCast(
            origin,
            _boxHalfExtents,
            -transform.up,
            out RaycastHit hit,
            Quaternion.identity,
            _checkDistance,
            _groundLayer
        );

        return hit.normal;
    }

    private void ApplyGravity()
    {
        if (_gravityDirection != Vector3.zero)
        {
            _rigidbody.AddForce(_gravityDirection * _gravityMagnitude, ForceMode.Acceleration);
        }
    }


    private void ApplyMovement()
    {
        // 空中では移動させない
        if (!OnGround)
            return;

        if (_cameraTransform == null)
            return;

        Vector3 gravityDir = _gravityDirection.normalized;
        if (gravityDir == Vector3.zero)
            return;

        // カメラの基準ベクトルを水平面に射影
        Vector3 camForward = Vector3.ProjectOnPlane(_cameraTransform.forward, gravityDir).normalized;
        Vector3 camRight = Vector3.ProjectOnPlane(_cameraTransform.right, gravityDir).normalized;

        // 入力方向をカメラ基準に変換
        Vector3 desiredMove = camForward * _desiredMoveDirection.z + camRight * _desiredMoveDirection.x;

        // 接平面上の現在速度
        Vector3 currentVelocity = _rigidbody.linearVelocity;
        Vector3 currentTangentVelocity = currentVelocity - Vector3.Project(currentVelocity, gravityDir);

        int speed = Running ? _runSpeed : _walkSpeed;

        Vector3 desiredTangent = Vector3.zero;
        if (desiredMove != Vector3.zero)
        {
            desiredTangent = desiredMove.normalized * speed;
        }

        // 補正ベクトル
        Vector3 delta = desiredTangent - currentTangentVelocity;

        // 入力がゼロの場合は減衰させる
        if (desiredMove == Vector3.zero)
        {
            float stopFactor = 5f; // 値が大きいほど早く止まる
            delta = -currentTangentVelocity * (stopFactor * Time.fixedDeltaTime);
        }

        _rigidbody.AddForce(delta, ForceMode.VelocityChange);
    }


    public void Move(Vector3 movementDirection)
    {
        _desiredMoveDirection = movementDirection;
    }

    public void Jump()
    {
        if (!OnGround)
            return;

        if (_gravityDirection == Vector3.zero)
            return;

        // 現在のRigidbody速度
        Vector3 velocity = _rigidbody.linearVelocity;

        // 重力方向成分だけリセット
        Vector3 horizontalVelocity = velocity - Vector3.Project(velocity, _gravityDirection);

        // 上方向速度を設定
        Vector3 jumpVelocity = horizontalVelocity + (-_gravityDirection * _jumpPower);

        // Rigidbodyに反映
        _rigidbody.linearVelocity = jumpVelocity;
    }

    public void AimStart()
    {
        _timeManager.SetTimeScale(slowMotionTimeScale, _aimReleaseTime);
    }

    public void AimEnd()
    {
        _timeManager.Release();
    }

    public void SetGravity(Vector3 gravityDirection)
    {
        _gravityDirection = gravityDirection;
        transform.up = -gravityDirection;
    }

    public void SetVelocity(Vector3 velocity, float magnitude)
    {
        _rigidbody.linearVelocity = velocity.normalized * magnitude;
    }

    public void ChangeGround()
    {
        if (Physics.Raycast(_cameraTransform.position, _cameraTransform.forward, out RaycastHit hit))
        {
            _cameraMove.ParentUpChange(hit.normal);
            SetGravity(-hit.normal);
        }
    }

    private void OnDrawGizmos()
    {
        Vector3 origin = transform.position;
        Vector3 castEnd = origin + (-transform.up * _checkDistance);

        Gizmos.color = OnGround ? Color.green : Color.red;

        Gizmos.DrawWireCube(origin, _boxHalfExtents * 2f);
        Gizmos.DrawWireCube(castEnd, _boxHalfExtents * 2f);
        Gizmos.DrawLine(origin, castEnd);
    }
}

public interface IPlayerMove
{
    int WalkSpeed { get; }
    int RunSpeed { get; }
    int JumpPawer { get; }
    bool OnGround { get; }

    bool Running { get; set; }

    Vector3 UpVector { get; }

    void Move(Vector3 movementDirection);
    void Jump();

    void AimStart();
    void AimEnd();
    void SetGravity(Vector3 gravityDirection);
    void SetVelocity(Vector3 velocity, float magnitude);

    void ChangeGround();
}