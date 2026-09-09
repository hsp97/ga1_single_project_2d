using UnityEngine;

public class HitBox : MonoBehaviour
{
    [SerializeField] private PlayerDash _dash;
    private void Awake()
    {
        if (_dash == null) _dash = GetComponentInParent<PlayerDash>();
    }

    private void Update()
    {
        
    }
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            Enemy enemy = collision.gameObject.GetComponent<Enemy>();
            Vector2 direction = (enemy.transform.position - transform.position).normalized;
            //enemy.KnockBack(_speed, direction);
            //_speed = 1;
        }
    }
}