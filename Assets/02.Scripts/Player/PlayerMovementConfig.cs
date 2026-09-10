using UnityEngine;

/// 플레이어 이동 튜닝값 묶음.
/// PlayerMotor의 Inspector에 접을 수 있는 그룹으로 표시된다.
[System.Serializable]
public class PlayerMovementConfig
{
    [Header("Run")]
    [Tooltip("기본 최고 속도 (유닛/초)")]
    [SerializeField, Min(0f)] private float _walkMaxSpeed = 7f;

    [Tooltip("입력 방향으로 속도를 올리는 정도 (유닛/초²)")]
    [SerializeField, Min(0f)] private float _groundAcceleration = 40f;

    [Tooltip("입력이 없을 때 멈추는 정도 (유닛/초²)")]
    [SerializeField, Min(0f)] private float _groundDeceleration = 50f;

    [Tooltip("이동 중 반대 방향을 누를 때 감속하는 정도 (유닛/초²)")]
    [SerializeField, Min(0f)] private float _turnDeceleration = 80f;

    [Tooltip("공중에서의 가감속 배율. 1이면 지상과 같다.")]
    [SerializeField, Range(0f, 1f)] private float _airControlMultiplier = 0.5f;

    [Header("Jump")]
    [Tooltip("점프 순간의 위쪽 속도 (유닛/초)")]
    [SerializeField, Min(0f)] private float _jumpVelocity = 14f;

    /// 기본 최고 속도 (유닛/초)
    public float WalkMaxSpeed => _walkMaxSpeed;

    /// 지상 가속도 (유닛/초²)
    public float GroundAcceleration => _groundAcceleration;

    /// 입력이 없을 때의 지상 감속도 (유닛/초²)
    public float GroundDeceleration => _groundDeceleration;

    /// 반대 방향 입력 시 감속도 (유닛/초²)
    public float TurnDeceleration => _turnDeceleration;

    /// 공중 가감속 배율 (0~1)
    public float AirControlMultiplier => _airControlMultiplier;

    /// 점프 초기 속도 (유닛/초)
    public float JumpVelocity => _jumpVelocity;
}