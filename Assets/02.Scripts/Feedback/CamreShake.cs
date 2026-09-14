using UnityEngine;

public class CameraShake : MonoBehaviour
{
    // 씬에 하나뿐인 인스턴스.
    private static CameraShake _instance;

    [Tooltip("한 번에 흔들 수 있는 최대 거리 (유닛)")]
    [SerializeField, Min(0f)] private float _maxStrength = 20f;

    private Vector3 _basePosition;
    private float _strength;
    private float _remainingTime;
    private float _duration;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            enabled = false;
            return;
        }

        _instance = this;
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    private void LateUpdate()
    {
        if (_remainingTime <= 0f)
        {
            return;
        }

        // 히트스톱으로 timeScale이 0이어도 흔들림은 계속 보이도록 실제 시간을 쓴다.
        _remainingTime -= Time.unscaledDeltaTime;

        if (_remainingTime <= 0f)
        {
            _remainingTime = 0f;
            transform.localPosition = _basePosition;
            return;
        }

        // 남은 시간에 비례해 흔들림이 잦아들게 한다.
        float currentStrength = _strength * (_remainingTime / _duration);
        Vector2 offset = Random.insideUnitCircle * currentStrength;

        transform.localPosition = _basePosition + new Vector3(offset.x, offset.y, 0f);
    }

    /// 카메라를 흔든다. 씬에 CameraShake가 없으면 아무 일도 하지 않는다.
    /// <param name="strength">흔들리는 거리 (유닛)</param>
    /// <param name="duration">흔들리는 시간 (초)</param>
    public static void Play(float strength, float duration)
    {
        if (_instance == null || strength <= 0f || duration <= 0f || !GameFeelSettings.CameraShakeEnabled)
        {
            return;
        }

        _instance.Begin(strength, duration);
    }

    /// 실제로 흔들림을 시작한다. 이미 흔들리는 중이면 더 센 쪽을 따른다.
    private void Begin(float strength, float duration)
    {
        // 흔들리지 않을 때의 위치를 기준으로 삼는다. 흔들리는 중이면 이미 저장된 값을 쓴다.
        if (_remainingTime <= 0f)
        {
            _basePosition = transform.localPosition;
        }

        _strength = Mathf.Min(Mathf.Max(strength, _strength), _maxStrength);
        _duration = duration;
        _remainingTime = duration;
    }
}