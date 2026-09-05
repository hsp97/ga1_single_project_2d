using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField]
    private float _weight = 1f;
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
        _rigidbody.AddForce(direction * speed / (_weight / 2) );
    }
}
