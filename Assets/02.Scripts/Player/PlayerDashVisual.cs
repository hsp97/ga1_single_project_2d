using UnityEngine;

/// 대시 상태를 스프라이트 색으로 보여준다. (프로토타입 확인용)
/// PlayerMotor의 상태를 매 프레임 확인하고, 상태가 바뀐 순간에만 색을 바꾼다.
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerDashVisual : MonoBehaviour
{
    [Tooltip("대시 중일 때의 색")]
    [SerializeField] private Color _dashColor = new Color(1f, 0.5f, 0f);

    private SpriteRenderer _renderer;
    private PlayerMove _motor;
    private Color _normalColor;
    private bool _wasDashing;

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        _normalColor = _renderer.color;

        // 자기 자신부터 부모 방향으로 찾는다. Visual 자식에 붙여도, Player 루트에 붙여도 동작한다.
        _motor = GetComponentInParent<PlayerMove>();

        if (_motor == null)
        {
            Debug.LogError($"[{name}] PlayerMotor를 찾을 수 없어 대시 표시를 끕니다.", this);
            enabled = false;
        }
    }

    private void LateUpdate()
    {
        bool isDashing = _motor.IsDashing;

        if (isDashing == _wasDashing)
        {
            return;
        }

        _wasDashing = isDashing;
        _renderer.color = isDashing ? _dashColor : _normalColor;
    }
}