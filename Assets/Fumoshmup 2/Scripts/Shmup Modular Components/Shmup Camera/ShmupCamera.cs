using rinCore;
using UnityEngine;

namespace FumoShmup2
{
    public class ShmupCamera : MonoBehaviour
    {
        static ShmupCamera instance;
        static Vector2 origin;
        public float weight = 5f;
        public float cameraMoveAcceleration = 5f;

        static float targetX;
        public static void ApplyOffset(ref Vector3 v)
        {
            v = new Vector3(targetX, 0f, 0f);
        }
        private void Awake()
        {
            instance = this;
            origin = transform.position;
        }
        private void Update()
        {
            if (FumoUnit.PlayerAs<ShmupPlayer>(out ShmupPlayer p))
            {
                targetX = origin.x.LerpUnclamped(p.CurrentPosition.x, 1f / weight);

                Vector2 newPos = new Vector2(targetX, transform.position.y);

                transform.position = ((Vector2)(transform.position)).LerpUnclamped(
                    newPos,
                    cameraMoveAcceleration > 0.1f ? Time.deltaTime * cameraMoveAcceleration : 1f
                );
            }
        }
    }
}