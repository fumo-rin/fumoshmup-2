using UnityEngine;
using rinCore;
using System.Linq;
using System;
using UnityEngine.AI;
using System.Collections;
namespace FumoQuake
{
    public abstract class QuakeEnemy : MonoBehaviour
    {
        #region Targetting
        Coroutine loseTarget;
        public void Action_LockTarget(ITargetting newTarget, float duration)
        {
            IEnumerator CO_Run(float time)
            {
                while (this != null && time > 0f)
                {
                    time -= Time.deltaTime;
                    yield return null;
                }
                this.target = null;
                loseTarget = null;
            }
            if (loseTarget != null)
                StopCoroutine(loseTarget);

            this.target = newTarget;
            loseTarget = StartCoroutine(CO_Run(duration));
        }
        #endregion
        #region Vision
        [SerializeField] RinRaycast scan;
        public bool CanSee<T>(Vector3 target, float distance, out T other)
        {
            Vector3 center = box.bounds.center;
            Ray r = new(center, (target - center).ScaleToMagnitude(scan.distance));
            if (Physics.Raycast(r, out RaycastHit hit, distance, scan.mask))
            {
                other = hit.collider.GetComponentInParent<T>();
                return other != null;
            }
            other = default;
            return false;
        }
        #endregion
        #region Walking Animation
        [SerializeField] protected Animator WalkingAnimator;
        [SerializeField] private string WalkingAnimStringKey = "WALKVELOCITY";
        Vector3 _walkingAnimBackingfield;
        protected Vector3 CurrentWalkingAnimationVelocity_WithSideEffects
        {
            get
            {
                return _walkingAnimBackingfield;
            }
            set
            {
                _walkingAnimBackingfield = value;
                if (WalkingAnimator != null) WalkingAnimator.SetFloat(WalkingAnimStringKey, value.magnitude);
            }
        }
        #endregion
        #region Gun
        public WeaponsController gun;
        #endregion
        #region Health & state
        public float CurrentHealth = 100f;
        public float StartingHealth { get; private set; }
        #endregion
        #region Pathfinding Algo
        Vector3 lastKnownTarget;
        protected void Path_DirectlyTowards(IFumoUnit other)
        {
            if (other == null || !other.IsAlive)
            {
                if (navigation.Nav.HasDestination)
                    return;
                if (navigation.Nav.TryProjectToNavmesh(lastKnownTarget + RNG.SeededRandomInsideUnitSphere * 3f, out Vector3 celebration, 5f))
                {
                    navigation.SetNewTarget(celebration);
                }
                return;
            }

            Vector3 targetPos = other.CurrentPosition;
            bool PathTowards = other.IsAlive && other.CurrentPosition.SquareDistanceToGreaterThan(transform.position, 3f);

            if (!PathTowards)
            {
                navigation.Nav.StopPath();
                return;
            }

            if (!navigation.Nav.TryProjectToNavmesh(targetPos, out Vector3 navPos, 5f))
            {
                if (NavMesh.FindClosestEdge(targetPos, out NavMeshHit hit, NavMesh.AllAreas))
                {
                    navPos = hit.position;
                }
                else
                {
                    navigation.Nav.StopPath();
                    return;
                }
            }

            navigation.SetNewTarget(navPos);
            lastKnownTarget = navPos;
        }
        #endregion
        [SerializeField] protected BoxCollider box;
        public BoxCollider UnitCollider => box;
        [SerializeField] protected QGrounded grounded;
        [SerializeField] protected RunnableObjectNavigator navigation;
        protected ITargetting target;
        protected Vector3 Origin;
        private void Awake()
        {
            Origin = transform.position;
            WhenAwake();
        }
        protected abstract void WhenAwake();
        private void Start()
        {
            RandomAttackTime = gun.RandomAggressionTimeAfterLock(0.6f, 1.25f);
            WhenStart();
            StartingHealth = CurrentHealth;
        }
        protected abstract void WhenStart();
        private void OnEnable()
        {
            WhenEnable();
        }
        protected abstract void WhenEnable();
        private void OnDisable()
        {
            WhenDisable();
        }
        protected abstract void WhenDisable();
        private void Update()
        {

            IFumoUnit targetUnit = IFumoUnit.Player;

            Think(targetUnit, Time.deltaTime);

            Vector3 anim_Velocity = navigation.LastFrameMoveVelocity;
            if (!navigation.HasPath)
            {
                anim_Velocity = Vector3.zero;
            }
            CurrentWalkingAnimationVelocity_WithSideEffects = anim_Velocity;
        }
        protected float StallTimeEnd = 0;
        public bool Stalled => Time.time < StallTimeEnd;
        protected float RandomAttackTime;
        protected float nextPathTick;
        public bool IsAlive { get; set; } = true;
        public Vector3 CurrentPosition => transform.position;
        protected abstract void WhenThink(ITargetting target, IFumoUnit targetUnit, float dt);
        protected void Pathing(ref float pathTick, Action<IFumoUnit> a, IFumoUnit target)
        {
            if (Time.time < pathTick)
            {
                return;
            }
            pathTick = (Time.time.Max(pathTick)) + 0.1f;
            a?.Invoke(target);
        }
        void Think(IFumoUnit targetUnit, float dt)
        {
            if (Stalled)
                return;

            ITargetting visibleTarget = null;
            if (targetUnit != null &&
                CanSee(targetUnit.CurrentPosition, scan.distance, out visibleTarget))
            {
                Action_LockTarget(visibleTarget, 5f);
            }

            WhenThink(this.target, targetUnit, dt);
        }
    }
    public class Imp : QuakeEnemy, IQuakeHitable
    {
        public GameObject hitGameObject => gameObject;
        public void Hit(IQuakeHitable.HitPacket packet)
        {
            float damageTaken = packet.Damage.Clamp(0f, CurrentHealth);
            CurrentHealth -= damageTaken;
            if (CurrentHealth < 0f + Mathf.Epsilon)
            {
                Destroy(gameObject);
            }
        }
        void Targetting(ITargetting target)
        {
            Debug.Log("Target: " + target);
            if (target != null && Time.time > RandomAttackTime)
            {
                Transform t = target.RandomOrderedTargets.First();

                if (gun != null)
                {
                    Vector3 origin = transform.position + new Vector3(0f, 0.75f, 0f);
                    Ray targetRay = new()
                    {
                        direction = target.RandomOrderedTargets.First().position - origin,
                        origin = origin
                    };
                    gun.TryShootWith(targetRay);
                    RandomAttackTime = gun.RandomAggressionTimeAfterLock(0.6f, 1.25f);
                }
            }
        }
        protected override void WhenThink(ITargetting target, IFumoUnit targetUnit, float dt)
        {
            bool targetTooFar = targetUnit != null && targetUnit.CurrentPosition.SquareDistanceToGreaterThan(transform.position, 35f);
            if (targetTooFar)
                return;

            if (targetUnit != null)
                Pathing(ref nextPathTick, Path_DirectlyTowards, targetUnit);
            Targetting(target);
            if (navigation.Nav.HasDestination && navigation.rb.linearVelocity.Y(0f).magnitude.Absolute() < 2f && grounded.IsGrounded)
            {
                navigation.rb.linearVelocity = new Vector3(0f, 4f, 0f) + RNG.SeededRandomInsideUnitSphere;
            }
        }

        protected override void WhenAwake()
        {

        }

        protected override void WhenStart()
        {

        }

        protected override void WhenEnable()
        {

        }

        protected override void WhenDisable()
        {

        }
    }
}
