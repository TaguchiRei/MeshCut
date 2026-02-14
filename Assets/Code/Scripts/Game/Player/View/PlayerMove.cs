using UnityEngine;

public class PlayerMove : MonoBehaviour, IPlayerMove
{
    [Header("BasicSetting")] [SerializeField]
    private int _walkSpeed = 5;

    [SerializeField] private int _runSpeed = 8;
    [SerializeField] private int _jumpPower = 5;

    public int WalkSpeed => _walkSpeed;
    public int RunSpeed => _runSpeed;
    public int JumpPawer => _jumpPower;
    public bool OnGround { get; private set; }

    public Vector3 UpVector { get; private set; }

    [Header("Components")] [SerializeField]
    private Rigidbody _rigidbody;

    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private CameraMove _cameraMove;

    [Header("GroundCheck")] [SerializeField]
    private Vector3 _boxHalfExtents = new Vector3(0.4f, 0.05f, 0.4f);

    [SerializeField] private float _checkDistance = 0.1f;
    [SerializeField] private LayerMask _groundLayer;


    private Vector3 _gravity = Vector3.down * 9.81f;
    private Vector3 _desiredMoveDirection;
    private bool _running;

    PlayerController _playerController;

    private void Start()
    {
        ServiceLocator.Instance.TryGetService(out IInputDispatcher inputDispatcher);
        _playerController = new PlayerController(inputDispatcher, this, _cameraMove);
        _playerController.EnableInput();

        _rigidbody.useGravity = false;
        _running = false;
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
        if (_gravity != Vector3.zero)
        {
            _rigidbody.AddForce(_gravity, ForceMode.Acceleration);
        }
    }


    private void ApplyMovement()
    {
        Vector3 gravityDir = _gravity.normalized;
        if (gravityDir == Vector3.zero)
            return;

        // 接平面を取得（重力方向に直交する平面）
        Vector3 currentVelocity = _rigidbody.linearVelocity;
        Vector3 currentTangentVelocity = currentVelocity - Vector3.Project(currentVelocity, gravityDir);

        int speed = _running ? _runSpeed : _walkSpeed;

        Vector3 desiredTangent = Vector3.zero;
        if (_desiredMoveDirection != Vector3.zero)
        {
            // 入力方向を接平面に投影
            desiredTangent = Vector3.ProjectOnPlane(_desiredMoveDirection, gravityDir).normalized * speed;
        }

        // 補正ベクトル
        Vector3 delta = desiredTangent - currentTangentVelocity;

        // 入力がゼロの場合は減衰させる
        if (_desiredMoveDirection == Vector3.zero)
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

        Vector3 gravityDir = _gravity.normalized;
        if (gravityDir == Vector3.zero)
            return;

        // 現在のRigidbody速度
        Vector3 velocity = _rigidbody.linearVelocity;

        // 重力方向成分だけリセット
        Vector3 horizontalVelocity = velocity - Vector3.Project(velocity, gravityDir);

        // 上方向速度を設定
        Vector3 jumpVelocity = horizontalVelocity + (-gravityDir * _jumpPower);

        // Rigidbodyに反映
        _rigidbody.linearVelocity = jumpVelocity;
    }

    public void SetGravity(Vector3 gravity)
    {
        _gravity = gravity;
    }

    public void SetVelocity(Vector3 velocity, float magnitude)
    {
        _rigidbody.linearVelocity = velocity.normalized * magnitude;
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

    Vector3 UpVector { get; }

    void Move(Vector3 movementDirection);
    void Jump();
    void SetGravity(Vector3 gravity);
    void SetVelocity(Vector3 velocity, float magnitude);
}