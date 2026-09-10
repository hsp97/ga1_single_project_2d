using UnityEngine;

public class DashTracker
{
    private float _chargeTimer;

    // 현재 대시 상태인지 여부
    public bool IsDashing { get; private set; }

    /// 한 물리 스텝만큼 대시 상태를 갱신한다.
    /// <param name="config">이동 튜닝값</param>
    /// <param name="inputDirection">입력 방향. 왼쪽 -1, 없음 0, 오른쪽 1</param>
    /// <param name="speedX">현재 수평 속도</param>
    /// <param name="isGrounded">바닥에 닿아 있는지 여부</param>
    /// <param name="deltaTime">이번 스텝의 시간 (초)</param>
    public void Tick(PlayerMovementConfig config, int inputDirection, float speedX, bool isGrounded, float deltaTime)
    {
        if (IsDashing)
        {
            if (ShouldExit(config, inputDirection, speedX))
            {
                SetDashing(false);
            }

            return;
        }

        if (!CanCharge(config, inputDirection, speedX, isGrounded))
        {
            _chargeTimer = 0f;
            return;
        }

        _chargeTimer += deltaTime;

        if (_chargeTimer >= config.DashChargeTime)
        {
            SetDashing(true);
        }
    }

    public void Cancel()
    {
        SetDashing(false);
    }

    /// 지상에서, 입력 방향으로, 걷기 최고 속도 근처를 유지 중인지 확인한다.
    private static bool CanCharge(PlayerMovementConfig config, int inputDirection, float speedX, bool isGrounded)
    {
        bool isMovingWithInput = inputDirection * speedX > 0f;
        bool isNearMaxSpeed = Mathf.Abs(speedX) >= config.DashChargeSpeed;

        return isGrounded && isMovingWithInput && isNearMaxSpeed;
    }

    /// 반대 방향을 누르거나 해제 속도 아래로 느려졌는지 확인한다.
    private static bool ShouldExit(PlayerMovementConfig config, int inputDirection, float speedX)
    {
        bool isTurning = inputDirection * speedX < 0f;
        bool isTooSlow = Mathf.Abs(speedX) < config.DashExitSpeed;

        return isTurning || isTooSlow;
    }

    /// 대시 상태를 바꾸고 차지 시간을 초기화한다.
    private void SetDashing(bool isDashing)
    {
        IsDashing = isDashing;
        _chargeTimer = 0f;
    }
}