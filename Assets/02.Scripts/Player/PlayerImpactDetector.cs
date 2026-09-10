using UnityEngine;

/// 플레이어 히트박스에 붙어서 적과 겹치는 순간을 감지하고,
/// 플레이어의 수평 속도에 비례한 넉백을 적에게 준다.
public class PlayerImpactDetector : MonoBehaviour
{
    [Header("Knockback")]
    [Tooltip("이 속도(유닛/초)보다 느리게 부딪히면 넉백을 주지 않는다.")]
    [SerializeField, Min(0f)] private float _minImpactSpeed = 10f;

    [Tooltip("넉백 속도 = 플레이어 속도 × 이 값")]
    [SerializeField, Min(0f)] private float _knockbackPerSpeed = 1f;

    [Tooltip("넉백 속도의 상한 (유닛/초)")]
    [SerializeField, Min(0f)] private float _maxKnockbackSpeed = 90f;

    [Tooltip("넉백 방향 각도. 0이면 수평, 90이면 수직 위")]
    [SerializeField, Range(0f, 90f)] private float _knockbackAngle = 40f;

    [Header("Player Recoil")]
    [Tooltip("부딪힌 뒤 플레이어에게 남는 속도 비율. 0이면 멈추고 1이면 그대로")]
    [SerializeField, Range(0f, 1f)] private float _playerSpeedRetain = 0.4f;

    [Header("Debug")]
    [Tooltip("충돌할 때마다 Console에 속도를 출력한다.")]
    [SerializeField] private bool _logHits = true;

    private PlayerMove _playerMove;

    private void Awake()
    {
        _playerMove = GetComponentInParent<PlayerMove>();

        if (_playerMove == null)
        {
            Debug.LogError($"[{name}] 부모에서 PlayerMove를 찾을 수 없습니다.", this);
        }

        if (!TryGetComponent(out Collider2D hitbox) || !hitbox.isTrigger)
        {
            Debug.LogWarning($"[{name}] Is Trigger가 켜진 Collider 2D가 필요합니다.", this);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryHit(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // 겹친 채로 적의 재충돌 대기 시간이 끝나면 Enter는 다시 불리지 않으므로, 겹쳐 있는 동안에도 판정한다.
        TryHit(other);
    }

    /// 겹친 대상이 적이고 충분히 빠르면 넉백을 주고, 플레이어에게 반동을 준다.
    /// <param name="other">겹친 콜라이더</param>
    private void TryHit(Collider2D other)
    {
        if (_playerMove == null)
        {
            return;
        }

        // 콜라이더가 자식에 있어도 찾을 수 있게, 콜라이더가 붙은 Rigidbody2D 기준으로 찾는다.
        Rigidbody2D otherBody = other.attachedRigidbody;

        if (otherBody == null || !otherBody.TryGetComponent(out EnemyKnockback enemy))
        {
            return;
        }

        float playerSpeedX = _playerMove.SpeedX;

        if (Mathf.Abs(playerSpeedX) < _minImpactSpeed)
        {
            return;
        }

        Vector2 knockbackVelocity = CalculateKnockbackVelocity(playerSpeedX);

        // 적이 방금 맞아서 무시했다면 반동 없이 넘어간다.
        if (!enemy.TryApplyKnockback(knockbackVelocity))
        {
            return;
        }

        _playerMove.ApplyImpactRecoil(_playerSpeedRetain);

        if (_logHits)
        {
            // Stay에서도 호출되므로, 매 스텝 로그가 쌓이지 않게 실제로 맞았을 때만 출력한다.
            Debug.Log($"[Hit] player {playerSpeedX:F1} → knockback {knockbackVelocity}", this);
        }
    }

    /// 플레이어 속도에 비례한 넉백 속도를 계산한다.
    /// <param name="playerSpeedX">플레이어의 수평 속도. 부호가 날아갈 방향이 된다.</param>
    /// <returns>적에게 줄 속도 (유닛/초)</returns>
    private Vector2 CalculateKnockbackVelocity(float playerSpeedX)
    {
        float directionX = Mathf.Sign(playerSpeedX);
        float speed = Mathf.Min(Mathf.Abs(playerSpeedX) * _knockbackPerSpeed, _maxKnockbackSpeed);

        float angleRadians = _knockbackAngle * Mathf.Deg2Rad;
        var direction = new Vector2(Mathf.Cos(angleRadians) * directionX, Mathf.Sin(angleRadians));

        return direction * speed;
    }
}