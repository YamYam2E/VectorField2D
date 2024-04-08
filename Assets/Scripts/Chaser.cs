using UnityEngine;

public class Chaser : MonoBehaviour
{
    [SerializeField] private VectorField2D vectorField;

    public float moveSpeed = 10.0f;
    private Rigidbody2D rb;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        var direction = vectorField.GetDirection(transform.position);
        var moveVelocity = direction.normalized * moveSpeed;
        rb.velocity = Vector2.zero;
        rb.AddForce(moveVelocity);
        
        // rb.MovePosition(rb.position + moveVelocity);
    }
}