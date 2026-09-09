using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHitbox : MonoBehaviour
{
    [SerializeField] private Collider2D _collider;
    [SerializeField] private PlayerDash _dash;
    private readonly HashSet<Enemy> _hitEnemies = new HashSet<Enemy>();

    private void Reset()
    {
        _collider = GetComponent<Collider2D>();
        _dash = GetComponentInParent<PlayerDash>();
    }

    private void Awake()
    {
        if (_collider == null) _collider = GetComponent<Collider2D>();
        if (_dash == null) _dash = GetComponentInParent<PlayerDash>();

        // 실수로 Inspector 설정이 바뀌어도 항상 트리거 + 비활성으로 시작하도록 강제한다.
        _collider.isTrigger = true;
    }
    public void ClearHitCache()
    {
        _hitEnemies.Clear();
    }
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_dash == null) return;

        Enemy enemy = collision.GetComponentInParent<Enemy>();
        if (enemy == null) return;

        if (!_hitEnemies.Add(enemy)) return;

        _dash.OnEnemyHit(enemy);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // 적이 히트박스를 벗어나면 기록에서 지운다. 다시 들어오면 또 때릴 수 있다.
        Enemy enemy = other.GetComponentInParent<Enemy>();
        if (enemy == null) return;

        _hitEnemies.Remove(enemy);
    }
}