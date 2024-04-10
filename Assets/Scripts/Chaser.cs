using UnityEngine;

public class Chaser : MonoBehaviour
{
    [SerializeField] private VectorField2D vectorField;

    public float moveSpeed = 0.4f;
    private Rigidbody2D rb;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.mass = Random.Range(1f, 3f);
    }

    private void FixedUpdate()
    {
        var direction = vectorField.GetDirection(transform.position);
        var moveVelocity = direction.normalized * Time.fixedDeltaTime * moveSpeed;
        rb.velocity = Vector2.zero;
        rb.MovePosition(rb.position + moveVelocity);
    }
}