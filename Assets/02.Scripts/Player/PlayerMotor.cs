using UnityEngine;

// 플레이어 물리 이동 담당
public class PlayerMotor : MonoBehaviour
{
    // 바닥으로 인정할 접촉 방향 범위. 90도가 정확히 위쪽이고, 45~135도면 45도 경사까지 바닥으로 본다.
    private const float MinGroundNormalAngle = 45f;
    private const float MaxGroundNormalAngle = 135f;

    // 이보다 작은 입력은 무시
    private const float InputDeadZone = 0.01f;

    [SerializeField] private PlayerMovementConfig _movement = new PlayerMovementConfig();
    [SerializeField] private LayerMask _groundLayers;
    
    private Rigidbody2D _body;
    private PlayerInputReader _input;
    private ContactFilter2D _groundFilter;
    private bool _isGrounded;

    private void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
        _input = GetComponent<PlayerInputReader>();
        _groundFilter = CreateGroundFilter();
    }

    private void FixedUpdate()
    {
        _isGrounded = _body.IsTouching(_groundFilter);
        UpdateHorizontalSpeed(_input.MoveX);
        TryJump();
    }

    private void UpdateHorizontalSpeed(float moveInput)
    {
        float currentSpeed = _body.linearVelocityX;
        float targetSpeed = moveInput * _movement.WalkMaxSpeed;
        float changeRate = GetSpeedChangeRate(moveInput, currentSpeed);
        
        _body.linearVelocityX = Mathf.MoveTowards(currentSpeed, targetSpeed, changeRate);
    }

    // 속도 변화량
    private float GetSpeedChangeRate(float moveInput, float currentSpeed)
    {
        float rate;

        if (Mathf.Abs(moveInput) < InputDeadZone)
        {
            rate = _movement.GroundDeceleration;
        }
        else if (moveInput * currentSpeed < 0f)
        {
            // 입력 방향과 이동 방향의 부호가 다르면 곱이 음수 → 방향 전환 중
            rate = _movement.TurnDeceleration;
        }
        else
        {
            rate = _movement.GroundAcceleration;
        }

        return _isGrounded ? rate : rate * _movement.AirControlMultiplier;
    }

    private void TryJump()
    {
        bool isJumpPressed = _input.ConsumeJumpPressed();
        // 바닥인 경우에만 점프된다.
        if (isJumpPressed && _isGrounded)
        {
            _body.linearVelocityY = _movement.JumpVelocity;
        }
    }

    // 바닥 레이어면서 위쪽을 향한 접촉만 걸러낸다.
    // 지면 체크용 필터
    private ContactFilter2D CreateGroundFilter()
    {
        var filter = new ContactFilter2D();
        filter.SetLayerMask(_groundLayers);
        filter.SetNormalAngle(MinGroundNormalAngle, MaxGroundNormalAngle);
        filter.useTriggers = false;
        return filter;
    }
}