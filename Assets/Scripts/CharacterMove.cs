using UnityEngine;

public class CharacterMove : MonoBehaviour
{
    public float moveSpeed = 5.0f; // 이동 속도

    private Rigidbody2D rb; // 캐릭터의 Rigidbody2D 컴포넌트

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>(); // Rigidbody2D 컴포넌트 캐싱
    }

    private void FixedUpdate()
    {
        // 입력 방향 벡터 계산
        var moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        // 이동 속도 벡터 계산
        var moveVelocity = moveInput.normalized * Time.fixedDeltaTime * moveSpeed;

        // 캐릭터 이동
        rb.velocity = Vector2.zero;
        rb.MovePosition(rb.position + moveVelocity);
    }
}
