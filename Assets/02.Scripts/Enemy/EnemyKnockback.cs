using UnityEngine;

/// 적이 넉백을 받고 회복하는 과정을 담당한다.
/// 현재는 일반 넉백만 처리하고, 발사와 벽꽝은 5단계에서 추가한다.
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyKnockback : MonoBehaviour
{
    // 바닥으로 인정할 접촉 방향 범위. PlayerMove와 같은 기준이다.
    private const float MinGroundNormalAngle = 45f;
    private const float MaxGroundNormalAngle = 135f;

    //적의 넉백 관련 상태
    private enum State
    {
        //평소 상태. 넉백을 받을 수 있다
        Normal,

        //넉백으로 밀려나는 중. 추가 넉백을 무시한다
        Knockback
    }

    [Tooltip("바닥으로 인정할 레이어")]
    [SerializeField] private LayerMask _groundLayers;

    [Header("Knockback")]
    [Tooltip("바닥에서 미끄러지는 속도를 줄이는 정도 (유닛/초²)")]
    [SerializeField, Min(0f)] private float _groundDeceleration = 200f;

    [Tooltip("넉백 직후 이 시간(초) 동안은 감속과 회복을 하지 않는다.")]
    [SerializeField, Min(0f)] private float _minKnockbackTime = 0.2f;

    [Tooltip("바닥에서 이 속도(유닛/초) 이하로 느려지면 넉백이 끝난다.")]
    [SerializeField, Min(0f)] private float _recoverSpeed = 5f;

    [Tooltip("넉백을 받은 뒤 이 시간(초) 동안은 다시 맞지 않는다.")]
    [SerializeField, Min(0f)] private float _hitCooldown = 0.2f;

    [Header("Test")]
    [Tooltip("컴포넌트 메뉴의 Test Knockback으로 줄 속도 (유닛/초)")]
    [SerializeField] private Vector2 _testVelocity = new Vector2(50f, 80f);

    private Rigidbody2D _body;
    private ContactFilter2D _groundFilter;
    private State _state = State.Normal;
    private float _stateTimer;
    private bool _isGrounded;

    private void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
        _groundFilter = CreateGroundFilter();
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
        }
    }

    /// 넉백을 적용한다.
    /// <param name="velocity">적에게 줄 속도 (유닛/초)</param>
    /// <returns>넉백을 받았으면 true, 방금 맞아서 무시했으면 false</returns>
    public bool TryApplyKnockback(Vector2 velocity)
    {
        // 맞은 직후에는 무시한다. 한 번 부딪힐 때 여러 물리 스텝에 걸쳐 중복으로 맞는 것을 막는다.
        // 대기 시간이 지나면 넉백 중이어도 다시 맞는다. 따라잡은 플레이어가 통과하지 않게 하기 위해서다.
        bool isHitRecently = _state == State.Knockback && _stateTimer < _hitCooldown;

        if (isHitRecently)
        {
            return false;
        }

        // AddForce 대신 속도를 직접 넣어서, 질량과 상관없이 계산한 값 그대로 날아가게 한다.
        _body.linearVelocity = velocity;
        ChangeState(State.Knockback);
        return true;
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

    /// [테스트용] Play 중 컴포넌트의 ⋮ 메뉴에서 실행해 넉백을 확인한다.
    [ContextMenu("Test Knockback")]
    private void TestKnockback()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Test Knockback은 Play 중에만 실행할 수 있습니다.", this);
            return;
        }

        bool isApplied = TryApplyKnockback(_testVelocity);
        Debug.Log($"[{name}] Test Knockback: {(isApplied ? "적용" : "무시 (넉백 중)")}", this);
    }
}