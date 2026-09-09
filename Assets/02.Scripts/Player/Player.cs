using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.EventSystems;
using static System.Math;
using Debug = UnityEngine.Debug;
using Object = System.Object;

public class Player : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D _rigidbody;
    [SerializeField] private PlayerDash _dash;

    [Header("Move")]
    [SerializeField] private float _maxSpeed = 100f;
    [SerializeField] private float _acceleration = 200f;
    [SerializeField] private float _deceleration = 300f;

    [Header("Bash")]
    [Tooltip("박치기 후 이동 입력을 무시할 시간")]
    [SerializeField] private float _bashStunDuration = 0.15f;
    private float _stunEndTime;
    private float _currentSpeed;

    private Vector2 _inputDirection;
    private Vector2 _moveDirection;
    public float CurrentSpeed => _currentSpeed;

    private void Reset()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _dash = GetComponent<PlayerDash>();
    }

    private void Awake()
    {
        if (_rigidbody == null) _rigidbody = GetComponent<Rigidbody2D>();
        if (_dash == null) _dash = GetComponent<PlayerDash>();
    }

    void Update()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        _inputDirection = new Vector2(h, v).normalized;

        UpdateSpeed();
        UpdateFacing(h);
    }

    private void FixedUpdate()
    {
        if (_dash != null && _dash.IsDashing) return;

        _rigidbody.MovePosition(_rigidbody.position + _moveDirection * _currentSpeed * Time.fixedDeltaTime);
    }

    private void UpdateSpeed()
    {
        bool hasInput = _inputDirection.sqrMagnitude > 0.01f;

        if (hasInput)
        {
            // 입력이 있으면 그 방향을 기억해둔다.
            _moveDirection = _inputDirection;
            _currentSpeed += _acceleration * Time.deltaTime;
        }
        else
        {
            // 입력이 없으면 감속. 방향은 유지해서 미끄러지듯 멈춘다.
            _currentSpeed -= _deceleration * Time.deltaTime;
        }

        _currentSpeed = Mathf.Clamp(_currentSpeed, 0f, _maxSpeed);
    }

    public void ApplyBashRecoil()
    {
        _currentSpeed = 0f;
        _stunEndTime = Time.time + _bashStunDuration;
    }

    private void UpdateFacing(float horizontal)
    {
        if (Mathf.Approximately(horizontal, 0f)) return;

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * Mathf.Sign(horizontal);
        transform.localScale = scale;
    }

}
