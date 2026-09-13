using UnityEngine;
using UnityEngine.UI;

/// 게임필 요소를 켜고 끄는 스위치. 요소별 체감 차이를 비교하기 위한 테스트용이다.
/// 씬 어디에나 하나만 두면 되고, 없으면 모든 요소가 켜진 것으로 본다.
public class GameFeelSettings : MonoBehaviour
{
    [SerializeField] private GameObject _player;
    [SerializeField] private GameObject _enemy;
    [SerializeField] private GameObject _enemyHeavy;

    private static GameFeelSettings _instance;
    [Header("UI")]
    [Tooltip("히트스톱 토글")]
    [SerializeField] private Toggle _hitStopToggle;

    [Tooltip("카메라 흔들림 토글")]
    [SerializeField] private Toggle _cameraShakeToggle;

    [Tooltip("잔상 토글")]
    [SerializeField] private Toggle _afterImageToggle;

    [Tooltip("넉백 토글")]
    [SerializeField] private Toggle _knockbackToggle;

    [Tooltip("타격 순간 화면을 잠깐 멈춘다.")]
    [SerializeField] private bool _hitStop = true;

    [Tooltip("타격과 벽꽝에 카메라를 흔든다.")]
    [SerializeField] private bool _cameraShake = true;

    [Tooltip("대시할 때 잔상을 남긴다.")]
    [SerializeField] private bool _afterImage = true;

    [Tooltip("적이 부딪히면 넉백으로 날아간다.")]
    [SerializeField] private bool _knockback = true;

    /// 히트스톱 사용 여부. 설정 오브젝트가 없으면 켜진 것으로 본다.
    public static bool HitStopEnabled => _instance == null || _instance._hitStop;

    /// 카메라 흔들림 사용 여부
    public static bool CameraShakeEnabled => _instance == null || _instance._cameraShake;

    /// 잔상 사용 여부
    public static bool AfterImageEnabled => _instance == null || _instance._afterImage;

    /// 넉백 사용 여부
    public static bool KnockbackEnabled => _instance == null || _instance._knockback;


    private Vector3 _playerTransform;
    private Vector3 _enemyTransform;
    private Vector3 _enemyHeavyTransform;
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning($"[{name}] GameFeelSettings가 이미 있어 이 컴포넌트를 끕니다.", this);
            enabled = false;
            return;
        }

        _instance = this;
    }
    private void Start()
    {
        _playerTransform = _player.transform.position;
        _enemyTransform = _enemy.transform.position;
        _enemyHeavyTransform = _enemyHeavy.transform.position;
        
        // 시작할 때 토글의 표시를 현재 설정과 맞춘다.
        SyncToggle(_hitStopToggle, _hitStop);
        SyncToggle(_cameraShakeToggle, _cameraShake);
        SyncToggle(_afterImageToggle, _afterImage);
        SyncToggle(_knockbackToggle, _knockback);
    }

    private void Update()
    {
        // 이벤트 대신 매 프레임 토글 상태를 읽는다. 테스트용 UI라 비용은 무시할 수준이다.
        ReadToggle(_hitStopToggle, ref _hitStop);
        ReadToggle(_cameraShakeToggle, ref _cameraShake);
        ReadToggle(_afterImageToggle, ref _afterImage);
        ReadToggle(_knockbackToggle, ref _knockback);

        if (Input.GetKeyDown(KeyCode.R))
        {
            _player.transform.position = _playerTransform;
            _enemy.transform.position = _enemyTransform;
            _enemyHeavy.transform.position = _enemyHeavyTransform;
        }
    }
    /// 토글 표시를 현재 설정값에 맞춘다.
    /// <param name="toggle">대상 토글. 비어 있으면 아무 일도 하지 않는다.</param>
    /// <param name="value">설정값</param>
    private static void SyncToggle(Toggle toggle, bool value)
    {
        if (toggle != null)
        {
            toggle.isOn = value;
        }
    }

    /// 토글 상태를 읽어 설정값에 반영한다.
    /// <param name="toggle">대상 토글. 비어 있으면 아무 일도 하지 않는다.</param>
    /// <param name="value">반영할 설정값</param>
    private static void ReadToggle(Toggle toggle, ref bool value)
    {
        if (toggle != null)
        {
            value = toggle.isOn;
        }
    }
    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    /// 히트스톱을 켜거나 끈다. UI 토글에서 호출한다.
    public void SetHitStop(bool isOn)
    {
        _hitStop = isOn;
    }

    /// 카메라 흔들림을 켜거나 끈다.
    public void SetCameraShake(bool isOn)
    {
        _cameraShake = isOn;
    }

    /// 잔상을 켜거나 끈다.
    public void SetAfterImage(bool isOn)
    {
        _afterImage = isOn;
    }

    /// 넉백을 켜거나 끈다.
    public void SetKnockback(bool isOn)
    {
        _knockback = isOn;
    }
}