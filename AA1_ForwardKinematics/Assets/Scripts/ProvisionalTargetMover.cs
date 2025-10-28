using UnityEngine;

public class ProvisionalTargetMover : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float fastMultiplier = 2f; // mantén Shift para ir más rápido

    void Update()
    {
        float speed = moveSpeed * (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? fastMultiplier : 1f);

        Vector3 dir = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) dir += Vector3.forward;
        if (Input.GetKey(KeyCode.S)) dir += Vector3.back;
        if (Input.GetKey(KeyCode.A)) dir += Vector3.left;
        if (Input.GetKey(KeyCode.D)) dir += Vector3.right;

        // Movimiento en plano XZ (mundo). Si lo quieres relativo a cámara, usa Camera.main.transform.
        if (dir.sqrMagnitude > 0f)
        {
            dir.Normalize();
            transform.position += dir * speed * Time.deltaTime;
        }
    }
}