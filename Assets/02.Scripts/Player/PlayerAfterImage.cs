using UnityEngine;

/// 대시 중 일정 간격으로 잔상을 남긴다. 플레이어의 Sprite Renderer가 있는 오브젝트에 붙인다.
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerAfterImage : MonoBehaviour
{
    // 미리 만들어둘 잔상 개수. 간격을 줄이거나 수명을 늘려도 모자라지 않게 넉넉히 잡는다.
    private const int PoolSize = 32;

    [Tooltip("잔상을 남기는 간격 (초). 짧을수록 촘촘해진다.")]
    [SerializeField, Min(0.01f)] private float _spawnInterval = 0.04f;

    [Tooltip("잔상 하나가 사라지기까지의 시간 (초)")]
    [SerializeField, Min(0.01f)] private float _lifeTime = 0.4f;

    [Tooltip("잔상의 색. 알파값이 잔상의 투명도가 된다.")]
    [SerializeField] private Color _color = new Color(1f, 0.6f, 0.2f, 0.6f);

    [Tooltip("켜면 잔상이 서서히 투명해지고, 끄면 같은 진하기로 있다가 사라진다.")]
    [SerializeField] private bool _fadeOut = false;

    [Tooltip("버스트 대시에서만 잔상을 남긴다. 끄면 일반 대시에서도 남긴다.")]
    [SerializeField] private bool _burstOnly = true;

    // 풀에 미리 만들어둔 잔상들. Instantiate와 Destroy를 반복하지 않도록 재사용한다.
    private SpriteRenderer[] _pool;

    // 각 잔상의 남은 수명. 0 이하면 비어 있는 슬롯이다.
    private float[] _remainingTimes;

    private SpriteRenderer _renderer;
    private PlayerMove _playerMove;
    private Transform _poolRoot;
    private float _spawnTimer;

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        _playerMove = GetComponentInParent<PlayerMove>();

        if (_playerMove == null)
        {
            Debug.LogError($"[{name}] PlayerMove를 찾을 수 없어 잔상을 끕니다.", this);
            enabled = false;
            return;
        }

        CreatePool();
    }

    private void OnDestroy()
    {
        // 잔상은 플레이어 밖에 두므로 플레이어가 사라져도 남는다. 같이 정리한다.
        if (_poolRoot != null)
        {
            Destroy(_poolRoot.gameObject);
        }
    }

    private void LateUpdate()
    {
        UpdateAfterImages();

        if (!ShouldSpawn())
        {
            // 다음에 대시를 시작하면 곧바로 첫 잔상이 나오게 한다.
            _spawnTimer = 0f;
            return;
        }

        _spawnTimer -= Time.deltaTime;

        if (_spawnTimer <= 0f)
        {
            Spawn();
            _spawnTimer = _spawnInterval;
        }
    }

    /// 지금 잔상을 남길 상태인지 확인한다.
    private bool ShouldSpawn()
    {
        if (!GameFeelSettings.AfterImageEnabled)
        {
            return false;
        }

        return _burstOnly ? _playerMove.IsBursting : _playerMove.IsDashing;
    }

    /// 잔상으로 쓸 오브젝트를 미리 만들어 꺼둔다.
    private void CreatePool()
    {
        // 플레이어의 자식으로 두면 플레이어를 따라 움직여서 제자리에 남지 않는다.
        _poolRoot = new GameObject($"{name}_AfterImages").transform;

        _pool = new SpriteRenderer[PoolSize];
        _remainingTimes = new float[PoolSize];

        for (int i = 0; i < PoolSize; i++)
        {
            var afterImage = new GameObject($"AfterImage_{i}", typeof(SpriteRenderer));
            afterImage.transform.SetParent(_poolRoot);
            afterImage.SetActive(false);

            SpriteRenderer afterImageRenderer = afterImage.GetComponent<SpriteRenderer>();

            // 플레이어보다 뒤에 그려서 잔상이 본체를 가리지 않게 한다.
            afterImageRenderer.sortingLayerID = _renderer.sortingLayerID;
            afterImageRenderer.sortingOrder = _renderer.sortingOrder - 1;

            _pool[i] = afterImageRenderer;
        }
    }

    /// 현재 모습을 복제해 잔상 하나를 남긴다.
    private void Spawn()
    {
        int index = FindFreeIndex();

        // 빈 슬롯이 없으면 이번 잔상은 건너뛴다.
        // 살아 있는 잔상을 덮어쓰면 그 잔상이 현재 위치로 끌려와 순서가 뒤엉킨다.
        if (index < 0)
        {
            return;
        }

        SpriteRenderer afterImage = _pool[index];

        afterImage.transform.SetPositionAndRotation(transform.position, transform.rotation);
        afterImage.transform.localScale = transform.lossyScale;

        afterImage.sprite = _renderer.sprite;
        afterImage.flipX = _renderer.flipX;
        afterImage.color = _color;
        afterImage.gameObject.SetActive(true);

        // 모든 잔상이 같은 수명을 가지므로, 먼저 남긴 잔상부터 순서대로 사라진다.
        _remainingTimes[index] = _lifeTime;
    }

    /// 지금 쓰이지 않는 잔상 슬롯을 찾는다.
    /// <returns>빈 슬롯 번호. 없으면 -1</returns>
    private int FindFreeIndex()
    {
        for (int i = 0; i < PoolSize; i++)
        {
            if (_remainingTimes[i] <= 0f)
            {
                return i;
            }
        }

        return -1;
    }

    /// 잔상의 수명을 줄이고, 끝난 것은 꺼서 풀에 되돌린다.
    private void UpdateAfterImages()
    {
        for (int i = 0; i < PoolSize; i++)
        {
            if (_remainingTimes[i] <= 0f)
            {
                continue;
            }

            _remainingTimes[i] -= Time.deltaTime;

            if (_remainingTimes[i] <= 0f)
            {
                _remainingTimes[i] = 0f;
                _pool[i].gameObject.SetActive(false);
                continue;
            }

            if (!_fadeOut)
            {
                continue;
            }

            Color color = _color;
            color.a = _color.a * (_remainingTimes[i] / _lifeTime);
            _pool[i].color = color;
        }
    }
}