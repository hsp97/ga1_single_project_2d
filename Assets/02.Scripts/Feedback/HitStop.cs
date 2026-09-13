using UnityEngine;

/// 타격 순간 게임 전체를 아주 잠깐 멈춰서 충격을 강조한다.
/// Time.timeScale이 전역 값이므로 씬에 하나만 둔다.
public class HitStop : MonoBehaviour
{
    // 씬에 하나뿐인 인스턴스. 전역 값을 다루는 기능이라 제한적으로 사용한다.
    private static HitStop _instance;

    [Tooltip("한 번에 멈출 수 있는 최대 시간 (초). 너무 길면 조작이 끊긴 것처럼 느껴진다.")]
    [SerializeField, Min(0f)] private float _maxDuration = 0.2f;

    private float _remainingTime;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning($"[{name}] HitStop이 이미 있어 이 컴포넌트를 끕니다.", this);
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

        // 멈춘 채로 씬이 바뀌면 게임이 정지한 상태로 남으므로 되돌린다.
        if (_remainingTime > 0f)
        {
            Time.timeScale = 1f;
        }
    }

    private void Update()
    {
        if (_remainingTime <= 0f)
        {
            return;
        }

        // timeScale이 0이면 deltaTime도 0이므로, 영향을 받지 않는 unscaledDeltaTime으로 센다.
        _remainingTime -= Time.unscaledDeltaTime;

        if (_remainingTime <= 0f)
        {
            _remainingTime = 0f;
            Time.timeScale = 1f;
        }
    }

    /// 지정한 시간만큼 게임을 멈춘다. 씬에 HitStop이 없으면 아무 일도 하지 않는다.
    /// <param name="duration">멈출 시간 (초)</param>
    public static void Play(float duration)
    {
        if (_instance == null || duration <= 0f || !GameFeelSettings.HitStopEnabled)
        {
            return;
        }

        _instance.Begin(duration);
    }

    /// 실제로 멈춤을 시작한다. 이미 멈춰 있으면 더 긴 쪽을 따른다.
    /// <param name="duration">멈출 시간 (초)</param>
    private void Begin(float duration)
    {
        float clamped = Mathf.Min(duration, _maxDuration);

        _remainingTime = Mathf.Max(_remainingTime, clamped);
        Time.timeScale = 0f;
    }
}