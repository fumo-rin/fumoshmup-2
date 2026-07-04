using rinCore;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FumoQuake
{
    public interface ITargetting
    {
        static ITargetting StaticTarget;
        public IEnumerable<Transform> RandomOrderedTargets { get; }
    }
    public class QuakeController : MonoBehaviour, IFumoUnit, ITargetting
    {
        [SerializeField] QuakeDude quakeMover;
        [SerializeField] Transform CurrentPositionNest;
        [SerializeField] List<Transform> EnemyTargets = new();
        [SerializeField] InputActionReference shootAction;
        [SerializeField] LayerMask hitscan;
        public IEnumerable<Transform> RandomOrderedTargets
        {
            get
            {
                foreach (Transform t in EnemyTargets.OrderByRandom())
                {
                    yield return t;
                }
            }
        }
        public bool IsAlive { get; set; }
        public Vector3 CurrentPosition => CurrentPositionNest != null ? CurrentPositionNest.position : transform == null ? Vector3.zero : transform.position;
        private void OnEnable()
        {
            IFumoUnit.Player = this;
        }
        private void Update()
        {
            IFumoUnit.Player = this;
            IsAlive = true;
            ITargetting.StaticTarget = this;
            if (shootAction.JustPressed())
            {
                IEnumerator CO_Burst(int count)
                {
                    while (shootAction.IsPressedRaw())
                    {
                        Ray r = quakeMover.CameraRay;
                        if (Physics.Raycast(r, out RaycastHit hit, 20f, hitscan, QueryTriggerInteraction.Ignore))
                        {
                            if (!hit.transform.TryGetComponent(out IFumoUnit f) && hit.collider is Collider c)
                            {
                                c.AddImpactVelocity(new Impact(hit, r, 1.65f));
                            }
                        }
                        yield return 0.08f.WaitForSeconds();
                    }
                }
                StartCoroutine(CO_Burst(3));
            }
        }
        private void OnDisable()
        {
            ITargetting.StaticTarget = null;
            IsAlive = false;
        }
    }
}
