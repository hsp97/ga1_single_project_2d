using UnityEngine;

/// 플레이어가 이동하기 직전에 몸 콜라이더 모양으로 진행 방향을 훑어서 적을 감지한다.
/// 적이 넉백을 받을 수 있으면 속도에 비례한 넉백을 주고, 방금 맞아서 받을 수 없으면 적 앞에서 멈춘다.
/// 대시 충돌의 강한 발사는 5단계에서 추가한다.
// PlayerMove(기본 순서 0)가 이번 스텝의 속도를 정한 다음에 실행되도록 순서를 뒤로 미룬다.
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(PlayerMove))]
public class PlayerImpactDetector : MonoBehaviour
{
    // 이 속도(유닛/초)보다 느리면 멈춰 있는 것으로 보고 판정하지 않는다.
    private const float MinCastSpeed = 0.01f;

    // 한 번의 캐스트로 받을 최대 결과 수
    private const int MaxHitCount = 4;

    [Tooltip("넉백 대상으로 볼 레이어")]
    [SerializeField] private LayerMask _enemyLayers;

    [Tooltip("이번 스텝 이동 거리보다 이만큼(유닛) 더 앞까지 확인한다. 딱 붙어 있는 적을 놓치지 않게 한다.")]
    [SerializeField, Min(0f)] private float _skinWidth = 0.5f;

    [Header("Knockback")]
    [Tooltip("넉백 속도 = 플레이어 속도 × 이 값")]
    [SerializeField, Min(0f)] private float _knockbackPerSpeed = 1f;

    [Tooltip("넉백 속도의 하한 (유닛/초). 천천히 밀어도 이만큼은 날아간다.")]
    [SerializeField, Min(0f)] private float _minKnockbackSpeed = 30f;

    [Tooltip("넉백 속도의 상한 (유닛/초). 하한보다 낮게 설정할 수 없다.")]
    [SerializeField, Min(0f)] private float _maxKnockbackSpeed = 90f;

    [Tooltip("넉백 방향 각도. 0이면 수평, 90이면 수직 위")]
    [SerializeField, Range(0f, 90f)] private float _knockbackAngle = 40f;

    [Header("Player Recoil")]
    [Tooltip("넉백을 준 뒤 플레이어에게 남는 속도 비율. 0이면 멈추고 1이면 그대로")]
    [SerializeField, Range(-1f, 1f)] private float _playerSpeedRetain = -1f;

    [Header("Debug")]
    [Tooltip("넉백을 줄 때마다 Console에 속도를 출력한다.")]
    [SerializeField] private bool _logHits = true;

    // 매 스텝 새로 만들지 않고 재사용해서 GC 할당을 없앤다.
    private readonly RaycastHit2D[] _hits = new RaycastHit2D[MaxHitCount];

    private PlayerMove _playerMove;
    private Rigidbody2D _body;
    private Collider2D _bodyCollider;
    private ContactFilter2D _enemyFilter;

    private void Awake()
    {
        _playerMove = GetComponent<PlayerMove>();
        _body = GetComponent<Rigidbody2D>();
        _bodyCollider = GetComponent<Collider2D>();

        if (_bodyCollider == null)
        {
            Debug.LogError($"[{name}] 몸 콜라이더(Collider 2D)가 없어 충돌 판정을 끕니다.", this);
            enabled = false;
            return;
        }

        _enemyFilter = new ContactFilter2D();
        _enemyFilter.SetLayerMask(_enemyLayers);
        _enemyFilter.useTriggers = false;
    }

    private void OnValidate()
    {
        // 상한이 하한보다 낮으면 넉백 세기 계산이 뒤집히므로 보정한다.
        _maxKnockbackSpeed = Mathf.Max(_maxKnockbackSpeed, _minKnockbackSpeed);
    }

    private void FixedUpdate()
    {
        float speedX = _playerMove.SpeedX;

        if (Mathf.Abs(speedX) < MinCastSpeed)
        {
            return;
        }

        float direction = Mathf.Sign(speedX);
        float castDistance = Mathf.Abs(speedX) * Time.fixedDeltaTime + _skinWidth;

        if (!TryFindEnemyAhead(direction, castDistance, out EnemyKnockback enemy, out float distanceToEnemy))
        {
            return;
        }

        Vector2 knockbackVelocity = CalculateKnockbackVelocity(speedX);

        if (enemy.TryApplyKnockback(knockbackVelocity))
        {
            _playerMove.ApplyImpactRecoil(_playerSpeedRetain);

            if (_logHits)
            {
                Debug.Log($"[Hit] player {speedX:F1} → knockback {knockbackVelocity}", this);
            }

            return;
        }

        // 적이 방금 맞아서 넉백을 받을 수 없으면, 이번 스텝에 적 표면을 넘지 않도록 속도를 줄인다.
        _playerMove.LimitSpeedX(distanceToEnemy / Time.fixedDeltaTime);
    }

    /// 진행 방향으로 몸 콜라이더를 훑어서 가장 가까운 적을 찾는다.
    /// <param name="direction">진행 방향. 오른쪽 1, 왼쪽 -1</param>
    /// <param name="castDistance">훑을 거리 (유닛)</param>
    /// <param name="nearestEnemy">찾은 적</param>
    /// <param name="nearestDistance">적 표면까지 거리 (유닛)</param>
    /// <returns>적을 찾았으면 true</returns>
    private bool TryFindEnemyAhead(float direction, float castDistance, out EnemyKnockback nearestEnemy, out float nearestDistance)
    {
        nearestEnemy = null;
        nearestDistance = float.MaxValue;

        int hitCount = _bodyCollider.Cast(new Vector2(direction, 0f), _enemyFilter, _hits, castDistance);

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D hit = _hits[i];
            Rigidbody2D enemyBody = hit.rigidbody;

            if (enemyBody == null || hit.distance >= nearestDistance)
            {
                continue;
            }

            // 이미 겹친 적은 진행 방향과 상관없이 잡히므로, 플레이어 중심보다 앞에 있는 적만 인정한다.
            bool isAhead = (enemyBody.position.x - _body.position.x) * direction > 0f;

            if (isAhead && enemyBody.TryGetComponent(out EnemyKnockback enemy))
            {
                nearestEnemy = enemy;
                nearestDistance = hit.distance;
            }
        }

        return nearestEnemy != null;
    }

    /// 플레이어 속도에 비례한 넉백 속도를 계산한다.
    /// <param name="playerSpeedX">플레이어의 수평 속도. 부호가 날아갈 방향이 된다.</param>
    /// <returns>적에게 줄 속도 (유닛/초)</returns>
    private Vector2 CalculateKnockbackVelocity(float playerSpeedX)
    {
        float directionX = Mathf.Sign(playerSpeedX);
        float speed = Mathf.Clamp(Mathf.Abs(playerSpeedX) * _knockbackPerSpeed, _minKnockbackSpeed, _maxKnockbackSpeed);

        float angleRadians = _knockbackAngle * Mathf.Deg2Rad;
        var direction = new Vector2(Mathf.Cos(angleRadians) * directionX, Mathf.Sin(angleRadians));

        return direction * speed;
    }
}