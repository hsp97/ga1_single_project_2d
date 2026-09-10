using UnityEngine;

/// <summary>
/// 플레이어 이동 튜닝값 묶음.
/// PlayerMotor의 Inspector에 접을 수 있는 그룹으로 표시된다.
/// 기본값은 캐릭터 크기 약 10×20 유닛 기준이다.
/// </summary>
[System.Serializable]
public class PlayerMovementConfig
{
    // 걷기 최고 속도의 이 비율 이상이면 "최고 속도 근처"로 보고 대시 차지를 시작한다.
    private const float DashChargeSpeedRatio = 0.95f;

    [Header("Run")]
    [Tooltip("기본 최고 속도 (유닛/초)")]
    [SerializeField, Min(0f)] private float _walkMaxSpeed = 70f;

    [Tooltip("입력 방향으로 속도를 올리는 정도 (유닛/초²)")]
    [SerializeField, Min(0f)] private float _groundAcceleration = 400f;

    [Tooltip("입력이 없을 때 멈추는 정도 (유닛/초²)")]
    [SerializeField, Min(0f)] private float _groundDeceleration = 500f;

    [Tooltip("이동 중 반대 방향을 누를 때 감속하는 정도 (유닛/초²)")]
    [SerializeField, Min(0f)] private float _turnDeceleration = 800f;

    [Tooltip("공중에서의 가속/감속 배율. 1이면 지상과 같다.")]
    [SerializeField, Range(0f, 1f)] private float _airControlMultiplier = 0.5f;

    [Header("Dash")]
    [Tooltip("최고 속도 근처를 이 시간(초) 동안 유지하면 대시에 들어간다.")]
    [SerializeField, Min(0f)] private float _dashChargeTime = 0.5f;

    [Tooltip("대시 중 최고 속도 (유닛/초). 걷기 최고 속도보다 낮게 설정할 수 없다.")]
    [SerializeField, Min(0f)] private float _dashMaxSpeed = 130f;

    [Tooltip("걷기 최고 속도를 넘어 대시 최고 속도까지 올리는 정도 (유닛/초²)")]
    [SerializeField, Min(0f)] private float _dashAcceleration = 150f;

    [Tooltip("이 속도 아래로 느려지면 대시가 풀린다 (유닛/초). 대시 진입 속도보다 높게 설정할 수 없다.")]
    [SerializeField, Min(0f)] private float _dashExitSpeed = 50f;

    [Header("Jump")]
    [Tooltip("점프 순간의 위쪽 속도 (유닛/초)")]
    [SerializeField, Min(0f)] private float _jumpVelocity = 140f;

    /// <summary>기본 최고 속도 (유닛/초).</summary>
    public float WalkMaxSpeed => _walkMaxSpeed;

    /// <summary>지상 가속도 (유닛/초²).</summary>
    public float GroundAcceleration => _groundAcceleration;

    /// <summary>입력이 없을 때의 지상 감속도 (유닛/초²).</summary>
    public float GroundDeceleration => _groundDeceleration;

    /// <summary>반대 방향 입력 시 감속도 (유닛/초²).</summary>
    public float TurnDeceleration => _turnDeceleration;

    /// <summary>공중 가감속 배율 (0~1).</summary>
    public float AirControlMultiplier => _airControlMultiplier;

    /// <summary>대시 진입까지 최고 속도를 유지해야 하는 시간 (초).</summary>
    public float DashChargeTime => _dashChargeTime;

    /// <summary>대시 중 최고 속도 (유닛/초).</summary>
    public float DashMaxSpeed => _dashMaxSpeed;

    /// <summary>걷기 최고 속도 이후 구간의 가속도 (유닛/초²).</summary>
    public float DashAcceleration => _dashAcceleration;

    /// <summary>대시가 풀리는 속도 (유닛/초).</summary>
    public float DashExitSpeed => _dashExitSpeed;

    /// <summary>대시 차지를 시작하는 속도 (유닛/초).</summary>
    public float DashChargeSpeed => _walkMaxSpeed * DashChargeSpeedRatio;

    /// <summary>점프 초기 속도 (유닛/초).</summary>
    public float JumpVelocity => _jumpVelocity;

    /// <summary>
    /// 서로 의존하는 값이 어긋나지 않게 보정한다.
    /// </summary>
    public void Validate()
    {
        // 대시 최고 속도가 걷기보다 낮으면 대시에 들어가도 빨라지지 않는다.
        _dashMaxSpeed = Mathf.Max(_dashMaxSpeed, _walkMaxSpeed);

        // 해제 속도가 진입 속도보다 높으면 대시에 들어가자마자 풀린다.
        _dashExitSpeed = Mathf.Min(_dashExitSpeed, DashChargeSpeed);
    }
}