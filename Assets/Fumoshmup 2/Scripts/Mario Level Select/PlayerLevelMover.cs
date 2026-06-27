using rinCore;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FumoShmup2
{
    public class PlayerLevelMover : MonoBehaviour
    {
        [SerializeField] FumoNav navigator;
        [SerializeField] Transform movedObject;
        Coroutine current;
        Vector2 lastInput;
        [SerializeField] InputActionReference moveAction;
        [SerializeField] AnimationCurve pathInterpolation;
        [SerializeField] Animator moveAnimator;
        [SerializeField] string moveAnimatorBoolKey = "MOVING";
        public void MoveTo(MarioLevelSelectItem item)
        {
            if (current != null)
            {
                StopCoroutine(current);
            }
            Vector3 target = item.transform.position;
            moveAnimator.SetBool(moveAnimatorBoolKey, true);
            current = navigator.StartABPath(this, movedObject, target, 25f, 0.4f, () =>
            {
                current = null;
                moveAnimator.SetBool(moveAnimatorBoolKey, false);
            }, pathInterpolation);
        }
        private void Start()
        {
            Vector3 target = movedObject.position;
            if (MarioLevelSelectItem.LoadStored(out MarioLevelSelectItem stored))
            {
                target = stored.transform.position;
            }
            if (navigator.TryProjectToNavmesh(target, out Vector3 nav, 5))
            {
                movedObject.position = nav;
            }
        }
        private void OnEnable()
        {
            current = null;
        }
        private void Update()
        {
            if (moveAction.ReadRawVector2() is Vector2 v && v != Vector2.zero && current == null)
            {
                if (MarioLevelSelectItem.TryGetDirection(v, out MarioLevelSelectItem result))
                {
                    MoveTo(result);
                }
            }
        }
    }
}
