using UnityEngine;

/// <summary>
/// 넉백을 수신하는 적. AddForce 대신 속도를 직접 대입해서
/// 질량이나 항력에 영향받지 않는 일정한 넉백을 만든다.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour
{
    [SerializeField] private Rigidbody2D _rigidbody;

    
    [SerializeField] private float _horizontal= 0.7f;
    [SerializeField] private float _verticalSnap = 0.85f;

    [SerializeField] private AnimationCurve _speedCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    [SerializeField] private bool _disableGravity = true;

    [Range(0f, 1f)]
    [SerializeField] private float _slamDotThreshold = 0.5f;

    /// 넉백 중인지 여부
    public bool IsKnockedBack { get; private set; }

    private Vector2 _direction;
    private float _speed;
    private float _duration;
    private float _elapsed;
    private float _cachedGravityScale;

    
    private void Reset()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
    }

    private void Awake()
    {
        if (_rigidbody == null) _rigidbody = GetComponent<Rigidbody2D>();
        _cachedGravityScale = _rigidbody.gravityScale;
    }

    public void KnockBack(Vector2 rawDirection, float speed, float duration)
    {
        // 중첩 호출 시 gravityScale이 0인 상태를 캐싱하지 않도록 최초 진입에서만 저장.
        if (!IsKnockedBack)
        {
            _cachedGravityScale = _rigidbody.gravityScale;
        }

        _direction = SnapDirection(rawDirection);
        _speed = speed;
        _duration = Mathf.Max(0.01f, duration);
        _elapsed = 0f;
        IsKnockedBack = true;

        if (_disableGravity)
        {
            _rigidbody.gravityScale = 0f;
        }

        // 기존 속도를 지워서 넉백 방향이 오염되지 않게 한다.
        _rigidbody.linearVelocity = _direction * _speed;
    }

    public void CancelKnockBack()
    {
        if (!IsKnockedBack) return;

        IsKnockedBack = false;
        _rigidbody.gravityScale = _cachedGravityScale;

        // 수평만 죽이고 낙하는 살려두면 벽에 박힌 뒤 자연스럽게 떨어진다.
        _rigidbody.linearVelocityX = 0f;
    }

    private void FixedUpdate()
    {
        if (!IsKnockedBack) return;

        _elapsed += Time.fixedDeltaTime;

        if (_elapsed >= _duration)
        {
            CancelKnockBack();
            return;
        }

        float t = _elapsed / _duration;

        // 물리 힘이 아니라 속도를 직접 대입한다.
        _rigidbody.linearVelocity = _direction * _speed * _speedCurve.Evaluate(t);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsKnockedBack) return;

        // contacts 프로퍼티는 배열을 새로 할당하므로 GetContact(0)을 쓴다.
        Vector2 normal = collision.GetContact(0).normal;

        // 충돌면 법선이 진행 방향과 정반대일수록 dot이 -1에 가깝다 = 정면 충돌.
        if (Vector2.Dot(normal, _direction) > -_slamDotThreshold) return;

        CancelKnockBack();
    }

    /// 한 축이 압도적이면 그 축으로 스냅해서 깔끔한 직선 궤적을 만든다.
    /// 크기는 절댓값으로 비교하고 결과는 부호를 살려서 대입한다.
    private Vector2 SnapDirection(Vector2 raw)
    {
        if (raw.sqrMagnitude < 0.0001f) return Vector2.right;

        Vector2 dir = raw.normalized;

        if (Mathf.Abs(dir.x) >= _horizontal)
        {
            return new Vector2(Mathf.Sign(dir.x), 0f);
        }

        if (Mathf.Abs(dir.y) >= _verticalSnap)
        {
            return new Vector2(0f, Mathf.Sign(dir.y));
        }

        return dir;
    }
}