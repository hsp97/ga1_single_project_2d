using UnityEngine;

/// 플레이어의 물리 이동을 담당한다
/// 입력은 PlayerInputReader에서 읽고, 결과는 Rigidbody2D 속도로 적용한다.
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInputReader))]
public class PlayerMove : MonoBehaviour
{
    // 바닥으로 인정할 접촉 방향 범위. 90도가 정확히 위쪽이고, 45~135도면 45도 경사까지 바닥으로 본다.
    private const float MinGroundNormalAngle = 45f;
    private const float MaxGroundNormalAngle = 135f;

    // 이 값보다 작은 입력은 입력 없음으로 본다.
    private const float InputDeadZone = 0.01f;

    [SerializeField] private PlayerMovementConfig _movement = new PlayerMovementConfig();

    [Tooltip("바닥으로 인정할 레이어")]
    [SerializeField] private LayerMask _groundLayers;

    private readonly DashTracker _dash = new DashTracker();

    private Rigidbody2D _body;
    private PlayerInputReader _input;
    private ContactFilter2D _groundFilter;
    private bool _isGrounded;

    // 현재 대시 상태인지 여부
    public bool IsDashing => _dash.IsDashing;

    // 현재 수평 속도 (유닛/초)
    public float SpeedX => _body.linearVelocityX;

    private void Awake()
    {
        // GetComponent를 매번 호출하지 않도록 시작할 때 한 번만 찾아서 저장한다.
        _body = GetComponent<Rigidbody2D>();
        _input = GetComponent<PlayerInputReader>();
        _groundFilter = CreateGroundFilter();
    }

    private void FixedUpdate()
    {
        _isGrounded = _body.IsTouching(_groundFilter);

        float moveInput = _input.MoveX;
        int inputDirection = GetInputDirection(moveInput);

        // 대시 상태를 먼저 갱신해야 이번 스텝의 최고 속도가 정해진다.
        _dash.Tick(_movement, inputDirection, _body.linearVelocityX, _isGrounded, Time.fixedDeltaTime);

        UpdateHorizontalSpeed(moveInput, inputDirection);
        TryJump();
    }

    private void OnValidate()
    {
        // Inspector에서 값을 바꿀 때마다 호출된다. (에디터 전용)
        _movement?.Validate();
    }

    /// 충돌 반동을 적용한다. 수평 속도를 비율만큼 남기고 대시를 해제한다.
    public void ApplyImpactRecoil(float speedRetainRatio)
    {
        _body.linearVelocityX *= speedRetainRatio;
        _dash.Cancel();
    }

    /// 입력값을 방향(-1, 0, 1)으로 바꾼다.
    /// <param name="moveInput">좌우 입력값 (-1 ~ 1)</param>
    /// <returns>입력 방향</returns>
    private static int GetInputDirection(float moveInput)
    {
        if (Mathf.Abs(moveInput) < InputDeadZone)
        {
            return 0;
        }

        return moveInput > 0f ? 1 : -1;
    }

    /// 입력 방향의 목표 속도를 향해 현재 수평 속도를 조금씩 바꾼다.
    /// <param name="moveInput">좌우 입력값 (-1 ~ 1)</param>
    /// <param name="inputDirection">입력 방향 (-1, 0, 1)</param>
    private void UpdateHorizontalSpeed(float moveInput, int inputDirection)
    {
        float currentSpeed = _body.linearVelocityX;
        float maxSpeed = _dash.IsDashing ? _movement.DashMaxSpeed : _movement.WalkMaxSpeed;
        float targetSpeed = inputDirection == 0 ? 0f : moveInput * maxSpeed;
        float changeRate = GetSpeedChangeRate(inputDirection, currentSpeed);

        _body.linearVelocityX = Mathf.MoveTowards(currentSpeed, targetSpeed, changeRate * Time.fixedDeltaTime);
    }

    
    /// 현재 상황(가속, 감속, 방향 전환, 대시, 공중)에 맞는 속도 변화량을 고른다.
    /// <param name="inputDirection">입력 방향 (-1, 0, 1)</param>
    /// <param name="currentSpeed">현재 수평 속도</param>
    /// <returns>초당 속도 변화량 (유닛/초²)</returns>
    private float GetSpeedChangeRate(int inputDirection, float currentSpeed)
    {
        float rate;

        if (inputDirection == 0)
        {
            rate = _movement.GroundDeceleration;
        }
        else if (inputDirection * currentSpeed < 0f)
        {
            // 입력 방향과 이동 방향의 부호가 다르면 곱이 음수 → 방향 전환 중
            rate = _movement.TurnDeceleration;
        }
        else if (_dash.IsDashing && Mathf.Abs(currentSpeed) >= _movement.WalkMaxSpeed)
        {
            // 걷기 최고 속도를 넘는 대시 구간은 더 완만하게 올린다.
            rate = _movement.DashAcceleration;
        }
        else
        {
            rate = _movement.GroundAcceleration;
        }

        return _isGrounded ? rate : rate * _movement.AirControlMultiplier;
    }

    /// 점프 입력이 있고 바닥에 있으면 위쪽 속도를 준다.
    private void TryJump()
    {
        // 바닥에 없어도 입력은 매번 꺼내서 버린다.
        // 남겨두면 공중에서 누른 입력이 착지 순간 뒤늦게 점프로 발동한다.
        bool isJumpPressed = _input.ConsumeJumpPressed();

        if (isJumpPressed && _isGrounded)
        {
            _body.linearVelocityY = _movement.JumpVelocity;
        }
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
}