using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField]
    private float _weight = 100f;
    private Rigidbody2D _rigidbody;
    void Start()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        
    }

    public void KnockBack(float speed, Vector2 direction)
    {
        _rigidbody.linearVelocity = Vector2.zero; // 충돌 자체의 자동 반발 velocity 제거

        if (direction.x > direction.y * 1.5)
        {
            direction.x = 1;
            direction.y = 0;
        } else if (direction.x * 1.5 < -direction.y)
        {
            direction.x = 0;
            direction.y = 1;
        }
        
        float safeWeight = Mathf.Max(_weight, 0.01f);
        float power = speed / safeWeight;

        _rigidbody.AddForce(direction * power, (ForceMode2D)ForceMode.Impulse);
    }
}
