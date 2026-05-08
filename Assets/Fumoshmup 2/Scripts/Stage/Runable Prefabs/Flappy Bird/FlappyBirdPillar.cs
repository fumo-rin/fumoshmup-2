using UnityEngine;

namespace FumoShmup2
{
    public class FlappyBirdPillar : MonoBehaviour
    {
        [SerializeField] float horizontalSpeed = -5f;
        [SerializeField] Rigidbody2D rb;
        private void Start()
        {
            rb.gravityScale = 0f;
            rb.linearVelocityX = horizontalSpeed;
        }
        private void OnTriggerEnter2D(Collider2D collision)
        {

        }
    }
}
