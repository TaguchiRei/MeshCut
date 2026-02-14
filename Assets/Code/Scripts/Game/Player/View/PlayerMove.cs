using UnityEngine;

public class PlayerMove : MonoBehaviour, IPlayerMove
{
    [Header("BasicSetting")] public int WalkSpeed { get; private set; }
    public int RunSpeed { get; private set; }
    public int JumpPawer { get; private set; }
    public bool OnGround { get; private set; }

    [Header("Components")] [SerializeField]
    private Rigidbody _rigidbody;

    [Header("GroundCheck")] [SerializeField]
    private Vector3 _boxHalfExtents = new Vector3(0.4f, 0.05f, 0.4f);

    [SerializeField] private float _checkDistance = 0.1f;
    [SerializeField] private LayerMask _groundLayer;


    private IInputDispatcher _inputDispatcher;
    private Vector3 _gravity;
    private Vector3 _velocity;
    private bool _running;

    private void Start()
    {
        ServiceLocator.Instance.TryGetService(out _inputDispatcher);
        _rigidbody.useGravity = false;
        _running = false;
    }

    private void Update()
    {
        Vector3 origin = transform.position;

        RaycastHit hit;

        OnGround = Physics.BoxCast(
            origin,
            _boxHalfExtents,
            transform.up * -1,
            out hit,
            Quaternion.identity,
            _checkDistance,
            _groundLayer
        );
    }

    private void FixedUpdate()
    {
        _rigidbody.AddForce(_gravity, ForceMode.VelocityChange);
    }

    public void Move(Vector3 movementVelocity)
    {
        if (_running)
        {
            _rigidbody.linearVelocity = movementVelocity * RunSpeed;
        }
        else
        {
            _rigidbody.linearVelocity = movementVelocity * WalkSpeed;
        }
    }

    public void SetGravity(Vector3 gravity)
    {
        _gravity = gravity;
    }

    public void SetVelocity(Vector3 velocity, float magnitude)
    {
        _rigidbody.linearVelocity = velocity * magnitude;
    }

    private void OnDrawGizmos()
    {
        Vector3 origin = transform.position;
        Vector3 castEnd = origin + transform.up * -1 * _checkDistance;

        // 接地中なら緑、非接地なら赤
        Gizmos.color = OnGround ? Color.green : Color.red;

        // 開始位置のBox
        Gizmos.DrawWireCube(origin, _boxHalfExtents * 2f);

        // 終了位置のBox
        Gizmos.DrawWireCube(castEnd, _boxHalfExtents * 2f);

        // スイープ方向の線
        Gizmos.DrawLine(origin, castEnd);
    }
}

public interface IPlayerMove
{
    public int WalkSpeed { get; }
    public int RunSpeed { get; }
    public int JumpPawer { get; }
    public bool OnGround { get; }


    public void Move(Vector3 movementVelocity);

    public void SetGravity(Vector3 gravity);

    public void SetVelocity(Vector3 velocity, float magnitude);
}