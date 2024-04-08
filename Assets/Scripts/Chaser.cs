using UnityEngine;

public class Chaser : MonoBehaviour
{
    [SerializeField] private VectorField2D vectorField;

    public float moveSpeed = 3.0f;
    private Rigidbody2D rb;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        var direction = vectorField.GetDirection(transform.position);
        var moveVelocity = direction.normalized * Time.fixedDeltaTime * moveSpeed;
        rb.velocity = Vector2.zero;
        rb.MovePosition(rb.position + moveVelocity);
    }
}