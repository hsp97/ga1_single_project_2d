using UnityEngine;

public class PlayerInputReader : MonoBehaviour
{
    private const string HorizontalAxisName = "Horizontal";
    private const string JumpButtonName = "Jump";
    private const KeyCode BurstKey = KeyCode.LeftAlt;

    private bool _isJumpPressed;
    private bool _isBurstPressed;
    public float MoveX { get; private set; }
    
    private void Update()
    {
        // GetAxis는 자체 스무딩이 있어서, 직접 만드는 가속 로직과 겹치지 않게 Raw를 쓴다.
        MoveX = Input.GetAxisRaw(HorizontalAxisName);

        // GetButtonDown은 누른 프레임에만 true라서, FixedUpdate가 가져갈 때까지 기억해둔다.
        if (Input.GetButtonDown(JumpButtonName))
        {
            _isJumpPressed = true;
        }

        // 점프와 같은 이유로, 누른 순간을 FixedUpdate가 가져갈 때까지 기억해둔다.
        if (Input.GetKeyDown(BurstKey))
        {
            _isBurstPressed = true;
        }
    }
    /// 마지막으로 꺼낸 이후 점프 버튼이 눌렸는지 반환하고 기록을 지운다.
    public bool ConsumeJumpPressed()
    {
        bool wasPressed = _isJumpPressed;
        _isJumpPressed = false;
        return wasPressed;
    }

    /// 마지막으로 꺼낸 이후 버스트 키가 눌렸는지 반환하고 기록을 지운다.
    public bool ConsumeBurstPressed()
    {
        bool wasPressed = _isBurstPressed;
        _isBurstPressed = false;
        return wasPressed;
    }
}