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
    private float _h;
    private float _v;

    [SerializeField] private float _maxSpeed = 100f;
    [SerializeField] private float _speed = 1f;
    [SerializeField] private float _accelerationRate = 5f;

    void Start()
    {
        
    }

    void Update()
    {
        _h = Input.GetAxis("Horizontal");
        _v = Input.GetAxis("Vertical");

        Vector3 normalizedDirection = new Vector3(_h, _v,0).normalized;
        
        if(Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow))
        {
            _speed *= 1f + _accelerationRate * Time.deltaTime;
            _speed = Mathf.Min(_speed, _maxSpeed);
        }

        transform.position += Math.Clamp(_speed, 1, _maxSpeed) * Time.deltaTime * normalizedDirection;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            _speed = 1;
        }
    }

}
