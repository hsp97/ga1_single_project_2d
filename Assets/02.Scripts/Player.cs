using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using static System.Math;
using Debug = UnityEngine.Debug;
using Object = System.Object;

public class Player : MonoBehaviour
{
    [SerializeField] private float _maxSpeed = 100f;
    [SerializeField] private float _speed = 1f;
    [SerializeField] private float _accelerationRate = 5f;

    private Vector2 _normalizedDirection;
    private Rigidbody2D _rigidbody;

    void Start()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        float _h = Input.GetAxis("Horizontal");
        float _v = Input.GetAxis("Vertical");
        _normalizedDirection = new Vector2(_h, _v).normalized;
        
        if(Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow))
        {
            _speed *= 1f + _accelerationRate * Time.deltaTime;
            _speed = Math.Clamp(_speed, 0, _maxSpeed);
        }
    }

    private void FixedUpdate()
    {
        _rigidbody.MovePosition(_rigidbody.position + _normalizedDirection * _speed * Time.fixedDeltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            Enemy enemy = collision.gameObject.GetComponent<Enemy>();
            Vector2 direction = (enemy.transform.position - transform.position).normalized;
            Debug.Log(direction);
            enemy.KnockBack(_speed, direction);
            _speed = 1;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            Enemy enemy = collision.gameObject.GetComponent<Enemy>();
            Vector2 direction = (enemy.transform.position - transform.position).normalized;
            Debug.Log(direction);
            enemy.KnockBack(_speed, direction);
            _speed = 1;
        }
    }

}
