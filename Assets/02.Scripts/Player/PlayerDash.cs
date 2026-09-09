using UnityEngine;

public class PlayerDash : MonoBehaviour
{
    [SerializeField] private Rigidbody2D _rigidbody;
    [SerializeField] private PlayerHitbox _hitbox;
    [SerializeField] private Player _player;

    [Header("대시")]
    [SerializeField] private KeyCode _dashKey = KeyCode.LeftShift;
    [SerializeField] private float _dashSpeed = 250f;
    [SerializeField] private float _dashDuration = 0.18f;
    [SerializeField] private float _dashCooldown = 0.5f;

    [SerializeField] private bool _disableGravity = true;

    [Header("Bash")]
    [SerializeField] private float _minBashSpeed = 40f;
    [SerializeField] private float _knockbackSpeed = 200f;
    [SerializeField] private float _knockbackDuration = 0.25f;
    [SerializeField] private bool _scaleKnockbackBySpeed = true;

    // 대시 중인지 여부. Player는 이 값이 true면 이동 처리를 건너뛴다
    public bool IsDashing { get; private set; }
    private Vector2 _dashDirection;
    private float _dashEndTime;      // 대시가 끝나는 시각
    private float _nextDashTime;     // 다음 대시가 가능해지는 시각
    private float _cachedGravityScale;

    private void Reset()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _hitbox = GetComponentInChildren<PlayerHitbox>(true);
        _player = GetComponent<Player>();
    }
    private void Awake()
    {
        if (_rigidbody == null) _rigidbody = GetComponent<Rigidbody2D>();
        if (_hitbox == null) _hitbox = GetComponentInChildren<PlayerHitbox>(true);
        if (_player == null) _player = GetComponent<Player>();

        _cachedGravityScale = _rigidbody.gravityScale;
    }

    private void Update()
    {
        if (!Input.GetKeyDown(_dashKey)) return;
        if (IsDashing) return;
        if (Time.time < _nextDashTime) return;

        StartDash();
    }

    private void FixedUpdate()
    {
        if (!IsDashing) return;

        if (Time.time >= _dashEndTime)
        {
            EndDash();
            return;
        }

        // 대시 중에는 이 스크립트가 속도의 주인
        _rigidbody.linearVelocity = _dashDirection * _dashSpeed;
    }

    public void OnEnemyHit(Enemy enemy)
    {
        float currentSpeed = GetCurrentSpeed();

        // 너무 느리면 박치기가 아니다. 그냥 스쳐 지나간 것으로 취급.
        if (currentSpeed < _minBashSpeed) return;

        float power = _knockbackSpeed;
        if (_scaleKnockbackBySpeed)
        {
            // 최소 속도일 때 1배, 그보다 빠를수록 비례해서 강해진다.
            power *= currentSpeed / _minBashSpeed;
        }

        Vector2 direction = (Vector2)enemy.transform.position - _rigidbody.position;
        enemy.KnockBack(direction, power, _knockbackDuration);

        if (IsDashing) EndDash();

        // 플레이어 반동. 속도와 관성을 모두 죽여야 딱 멈춘다.
        _rigidbody.linearVelocity = Vector2.zero;
        if (_player != null) _player.ApplyBashRecoil();
    }

    private float GetCurrentSpeed()
    {
        if (IsDashing) return _rigidbody.linearVelocity.magnitude;
        return _player != null ? _player.CurrentSpeed : 0f;
    }

    private void StartDash()
    {
        _dashDirection = GetDashDirection();
        IsDashing = true;
        _dashEndTime = Time.time + _dashDuration;
        _nextDashTime = _dashEndTime + _dashCooldown;

        if (_disableGravity)
        {
            _cachedGravityScale = _rigidbody.gravityScale;
            _rigidbody.gravityScale = 0f;
        }

        _rigidbody.linearVelocity = _dashDirection * _dashSpeed;
    }

    private void EndDash()
    {
        if (!IsDashing) return;

        IsDashing = false;
        _rigidbody.gravityScale = _cachedGravityScale;
    }

    private Vector2 GetDashDirection()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector2 input = new Vector2(h, v);

        if (input.sqrMagnitude > 0.01f) return input.normalized;

        return transform.localScale.x < 0f ? Vector2.left : Vector2.right;
    }
}