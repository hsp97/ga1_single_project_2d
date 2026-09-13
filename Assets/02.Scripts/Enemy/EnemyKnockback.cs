using UnityEngine;

/// 적이 넉백과 발사를 받고 회복하는 과정을 담당한다.
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyKnockback : MonoBehaviour
{
    [SerializeField]
    private Animator _animator;

    // 바닥으로 인정할 접촉 방향 범위. PlayerMove와 같은 기준이다.
    private const float MinGroundNormalAngle = 45f;
    private const float MaxGroundNormalAngle = 135f;

    // 벽으로 인정할 접촉 방향의 최소 수평 성분. 1이면 완전한 수직 벽이다.
    private const float MinWallNormalX = 0.7f;

    // 적의 넉백 관련 상태
    private enum State
    {
        // 평소 상태. 넉백을 받을 수 있다.
        Normal,

        // 일반 넉백으로 밀려나는 중. 맞은 직후 잠깐은 추가 넉백을 무시한다.
        Knockback,

        // 대시 충돌로 날아가는 중. 중력이 약해 거의 수평으로 날아간다.
        Launched,

        // 벽에 박혀서 잠시 멈춘 상태.
        WallPinned,

        // 기절해서 쓰러진 상태. 이 동안은 넉백을 받지 않는다.
        Stunned
    }

    [Tooltip("바닥으로 인정할 레이어")]
    [SerializeField] private LayerMask _groundLayers;

    [Header("Knockback")]
    [Tooltip("넉백 저항. 1이 기본이고, 2면 절반만 날아가고 0.5면 2배로 날아간다.")]
    [SerializeField, Min(0.01f)] private float _knockbackResistance = 1f;
    [Tooltip("바닥에서 미끄러지는 속도를 줄이는 정도 (유닛/초²)")]
    [SerializeField, Min(0f)] private float _groundDeceleration = 200f;

    [Tooltip("넉백 직후 이 시간(초) 동안은 감속과 회복을 하지 않는다.")]
    [SerializeField, Min(0f)] private float _minKnockbackTime = 0.2f;

    [Tooltip("바닥에서 이 속도(유닛/초) 이하로 느려지면 넉백이 끝난다.")]
    [SerializeField, Min(0f)] private float _recoverSpeed = 5f;

    [Tooltip("넉백을 받은 뒤 이 시간(초) 동안은 다시 맞지 않는다.")]
    [SerializeField, Min(0f)] private float _hitCooldown = 0.2f;

    [Header("Launch")]
    [Tooltip("발사 중 중력 배율. 낮을수록 수평으로 멀리 날아간다.")]
    [SerializeField, Min(0f)] private float _launchGravityScale = 0.2f;

    [Tooltip("발사 중 회전 속도 (도/초)")]
    [SerializeField] private float _launchRotationSpeed = 720f;

    [Tooltip("벽에 닿지 않아도 이 시간(초)이 지나면 발사가 끝난다.")]
    [SerializeField, Min(0f)] private float _maxLaunchTime = 0.6f;

    [Header("Wall Slam")]
    [Tooltip("벽에 박힌 채 멈춰 있는 시간 (초)")]
    [SerializeField, Min(0f)] private float _wallPinTime = 0.25f;

    [Tooltip("벽꽝이나 발사 후 기절 상태로 있는 시간 (초)")]
    [SerializeField, Min(0f)] private float _stunTime = 1f;

    [Header("Feedback")]
    [Tooltip("벽꽝 기준 속도 (유닛/초). 이 속도로 부딪히면 아래 연출 값이 그대로 적용된다.")]
    [SerializeField, Min(1f)] private float _slamReferenceSpeed = 200f;

    [Tooltip("기준 속도로 부딪혔을 때 멈추는 시간 (초)")]
    [SerializeField, Min(0f)] private float _slamHitStop = 0.1f;

    [Tooltip("기준 속도로 부딪혔을 때 카메라가 흔들리는 거리 (유닛)")]
    [SerializeField, Min(0f)] private float _slamShakeStrength = 12f;

    [Tooltip("카메라가 흔들리는 시간 (초)")]
    [SerializeField, Min(0f)] private float _slamShakeTime = 0.25f;

    [Header("Test")]
    [Tooltip("컴포넌트 메뉴의 Test로 줄 속도 (유닛/초)")]
    [SerializeField] private Vector2 _testVelocity = new Vector2(50f, 80f);

    private Rigidbody2D _body;
    private ContactFilter2D _groundFilter;
    private State _state = State.Normal;
    private float _stateTimer;
    private bool _isGrounded;
    private float _defaultGravityScale;
    private float _launchDirectionX;
    private float _lastSpeed;

    private void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
        _groundFilter = CreateGroundFilter();
        _defaultGravityScale = _body.gravityScale;
    }

    private void FixedUpdate()
    {
        _isGrounded = _body.IsTouching(_groundFilter);
        _stateTimer += Time.fixedDeltaTime;

        switch (_state)
        {
            case State.Normal:
                UpdateNormal();
                break;

            case State.Knockback:
                UpdateKnockback();
                break;

            case State.Launched:
                UpdateLaunched();
                break;

            case State.WallPinned:
                UpdateWallPinned();
                break;

            case State.Stunned:
                UpdateStunned();
                break;
        }
        // 벽에 닿는 순간의 속도는 이미 0에 가까우므로, 직전 스텝의 속도를 연출 강도에 쓴다.
        _lastSpeed = _body.linearVelocity.magnitude;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (_state != State.Launched)
        {
            return;
        }

        // 날아가는 방향의 반대쪽을 향한 수직에 가까운 접촉만 벽으로 본다. 바닥이나 천장은 제외된다.
        Vector2 normal = collision.GetContact(0).normal;
        bool isWall = normal.x * _launchDirectionX < 0f && Mathf.Abs(normal.x) >= MinWallNormalX;

        if (isWall)
        {
            PinToWall();
        }
    }

    /// 넉백이나 발사를 적용한다.
    /// <param name="velocity">적에게 줄 속도 (유닛/초)</param>
    /// <param name="isLaunch">대시 충돌에 의한 강한 발사인지 여부</param>
    /// <returns>적용했으면 true, 무시했으면 false</returns>
    public bool TryApplyKnockback(Vector2 velocity, bool isLaunch = false)
    {
        if (!CanBeHit())
        {
            return false;
        }
        _animator.SetTrigger("hit");

        // AddForce 대신 속도를 직접 넣어서, 질량과 상관없이 계산한 값 그대로 날아가게 한다.
        // 저항이 클수록 덜 날아간다. 발사에도 같이 적용해서 무거운 적은 벽까지 닿지 않게 한다.
        Vector2 resistedVelocity = velocity / _knockbackResistance;
        _body.linearVelocity = resistedVelocity;

        if (isLaunch)
        {
            StartLaunch(resistedVelocity.x);
        }
        else
        {
            ChangeState(State.Knockback);
        }

        _animator.SetTrigger("idle");
        return true;
    }

    /// 지금 넉백을 받을 수 있는 상태인지 확인한다.
    private bool CanBeHit()
    {
        switch (_state)
        {
            case State.Normal:
                return true;

            // 맞은 직후만 무시한다. 대기 시간이 지나면 따라잡은 플레이어가 다시 밀 수 있다.
            case State.Knockback:
                return _stateTimer >= _hitCooldown;

            // 날아가는 중이거나 기절 중에는 받지 않는다.
            default:
                return false;
        }
    }

    /// 발사를 시작한다. 중력을 낮추고 회전을 풀어 물리로 돌게 한다.
    /// <param name="velocityX">발사 속도의 수평 성분</param>
    private void StartLaunch(float velocityX)
    {
        _launchDirectionX = Mathf.Sign(velocityX);
        _body.gravityScale = _launchGravityScale;

        // Transform을 직접 돌리면 물리 계산과 서로 값을 덮어쓰므로, 회전 고정을 풀고 물리로 돌린다.
        _body.constraints = RigidbodyConstraints2D.None;
        _body.angularVelocity = -_launchDirectionX * _launchRotationSpeed;

        ChangeState(State.Launched);
    }

    /// 평소 상태: 바닥에서는 미끄러지지 않게 멈춘다.
    private void UpdateNormal()
    {
        if (_isGrounded)
        {
            SlowDownOnGround();
        }
    }

    /// 넉백 상태: 착지 후 감속하다가 충분히 느려지면 평소 상태로 돌아간다.
    private void UpdateKnockback()
    {
        // 넉백 직후에는 접촉 정보가 직전 물리 갱신 기준이라 아직 바닥에 닿은 것으로 나온다.
        // 이때 감속이나 회복이 먼저 일어나지 않도록 최소 시간 동안은 건드리지 않는다.
        if (_stateTimer < _minKnockbackTime || !_isGrounded)
        {
            return;
        }

        SlowDownOnGround();

        if (Mathf.Abs(_body.linearVelocityX) <= _recoverSpeed)
        {
            ChangeState(State.Normal);
        }
    }

    /// 발사 상태: 벽에 닿지 못한 채 시간이 지나면 기절로 넘어간다.
    private void UpdateLaunched()
    {
        if (_stateTimer >= _maxLaunchTime)
        {
            StartStun();
        }
    }

    /// 벽에 박힌 상태: 잠시 멈춰 있다가 기절로 넘어가며 떨어진다.
    private void UpdateWallPinned()
    {
        if (_stateTimer >= _wallPinTime)
        {
            StartStun();
        }
    }

    /// 기절 상태: 착지해서 멈추고 시간이 지나면 평소 상태로 돌아간다.
    private void UpdateStunned()
    {
        if (_isGrounded)
        {
            SlowDownOnGround();
        }

        if (_stateTimer >= _stunTime)
        {
            EndStun();
        }
    }

    /// 벽에 닿은 순간 속도와 중력을 0으로 만들어 벽에 박힌 것처럼 멈춘다.
    private void PinToWall()
    {
        PlaySlamFeedback(_lastSpeed);

        _body.linearVelocity = Vector2.zero;
        _body.angularVelocity = 0f;
        _body.gravityScale = 0f;

        ChangeState(State.WallPinned);
    }

    /// 부딪힌 속도에 비례해 히트스톱과 카메라 흔들림을 재생한다.
    /// <param name="impactSpeed">벽에 부딪힌 속도 (유닛/초)</param>
    private void PlaySlamFeedback(float impactSpeed)
    {
        // 기준 속도의 몇 배로 부딪혔는지로 강도를 정한다. 최대 2배까지만 반영한다.
        float ratio = Mathf.Clamp(impactSpeed / _slamReferenceSpeed, 0f, 2f);

        HitStop.Play(_slamHitStop * ratio);
        CameraShake.Play(_slamShakeStrength * ratio, _slamShakeTime);
    }

    /// 기절을 시작한다. 중력을 되돌려 떨어지게 한다.
    private void StartStun()
    {
        _body.gravityScale = _defaultGravityScale;
        ChangeState(State.Stunned);
    }

    /// 기절을 끝낸다. 회전을 원래대로 되돌리고 다시 고정한다.
    private void EndStun()
    {
        _body.angularVelocity = 0f;
        _body.rotation = 0f;
        _body.constraints = RigidbodyConstraints2D.FreezeRotation;

        ChangeState(State.Normal);
    }

    /// 마찰 대신 코드로 수평 속도를 줄인다.
    private void SlowDownOnGround()
    {
        float speedX = _body.linearVelocityX;
        _body.linearVelocityX = Mathf.MoveTowards(speedX, 0f, _groundDeceleration * Time.fixedDeltaTime);
    }

    /// 상태를 바꾸고 상태 경과 시간을 초기화한다.
    /// <param name="nextState">바꿀 상태</param>
    private void ChangeState(State nextState)
    {
        _state = nextState;
        _stateTimer = 0f;
    }

    /// 바닥 레이어이면서 위쪽을 향한 접촉만 걸러내는 필터를 만든다.
    /// <returns>지면 체크용 필터</returns>
    private ContactFilter2D CreateGroundFilter()
    {
        var filter = new ContactFilter2D();
        filter.SetLayerMask(_groundLayers);
        filter.SetNormalAngle(MinGroundNormalAngle, MaxGroundNormalAngle);
        filter.useTriggers = false;
        return filter;
    }

    /// [테스트용] Play 중 컴포넌트의 ⋮ 메뉴에서 실행해 일반 넉백을 확인한다.
    [ContextMenu("Test Knockback")]
    private void TestKnockback()
    {
        TestHit(_testVelocity, false);
    }

    /// [테스트용] Play 중 컴포넌트의 ⋮ 메뉴에서 실행해 발사를 확인한다.
    [ContextMenu("Test Launch")]
    private void TestLaunch()
    {
        TestHit(new Vector2(Mathf.Abs(_testVelocity.x) * 4f, 20f), true);
    }

    private void TestHit(Vector2 velocity, bool isLaunch)
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("테스트는 Play 중에만 실행할 수 있습니다.", this);
            return;
        }

        bool isApplied = TryApplyKnockback(velocity, isLaunch);
        Debug.Log($"[{name}] Test: {(isApplied ? "적용" : $"무시 ({_state})")}", this);
    }
}