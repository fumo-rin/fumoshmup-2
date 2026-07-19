using rinCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace FumoQuake
{
    public interface IStrafe
    {
        public bool TryStrafe(ref Vector3 velocity, ITargetting target);
    }
    [SelectionBase]
    public abstract class QuakeEnemy : MonoBehaviour, IFumoUnit
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
        [SerializeField] RinRaycast friendScan;
        public Vector3 Center => box.bounds.center;
        public Ray TargetRay(Vector3 other) => new()
        {
            direction = other - Center,
            origin = Center
        };
        public bool Action_DamageAlert(IQuakeHitable.HitPacket packet)
        {
            if (packet.Sender == null)
            {
                return false;
            }
            return Action_AlertAndLockTarget(AliveEnemies, packet.Sender);
        }
        public bool Action_AlertAndLockTarget(IEnumerable<QuakeEnemy> others, ITargetting target)
        {
            if (target == null)
                return false;
            Action_LockTarget(target, 5f);
            bool anyAlerted = false;
            foreach (var item in others.Where(x => x.target == null && x.CurrentPosition.SquareDistanceToLessThan(CurrentPosition, scan.distance)))
            {
                float distance = item.box.bounds.center.DistanceTo(box.bounds.center);
                Ray r = new()
                {
                    direction = (item.box.bounds.center - box.bounds.center).ScaleToMagnitude(distance),
                    origin = box.bounds.center
                };
                if (Physics.Raycast(r, out RaycastHit hit, distance, friendScan.mask))
                {
                    QuakeEnemy other = hit.collider.GetComponentInParent<QuakeEnemy>();
                    if (other != null)
                    {
                        Debug.DrawLine(r.origin, hit.point, ColorHelper.PastelCyan, 0.5f);
                        other.Action_LockTarget(target, 5f);
                        anyAlerted = true;
                    }
                    else
                    {
                        Debug.DrawLine(r.origin, hit.point, ColorHelper.Gray1, 0.5f);
                    }
                }
            }
            return anyAlerted;
        }
        public bool CanSeeTarget(ITargetting target)
        {
            if (target == null)
                return false;
            return CanSee(target.RandomOrderedTargets.OrderByRandom().FirstOrDefault().position, scan.distance, out ITargetting result) && result != null;
        }
        public bool CanSee<T>(Vector3 target, float distance, out T other)
        {
            other = default;
            if (target.SquareDistanceToGreaterThan(transform.position, distance))
                return false;
            Vector3 center = box.bounds.center;
            Ray r = new(center, (target - center).ScaleToMagnitude(scan.distance));
            if (Physics.Raycast(r, out RaycastHit hit, distance, scan.mask))
            {
                other = hit.collider.GetComponentInParent<T>();
                Debug.DrawLine(r.origin, hit.point, other != null ? ColorHelper.PastelGreen : ColorHelper.PastelRed, 0.5f);
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
        public Vector3 lastKnownTarget;
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
        protected void Path_AwayFrom(IFumoUnit other)
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

            Vector3 awayFrom = transform.position - other.CurrentPosition;
            Vector3 targetPos = transform.position + awayFrom.normalized * 5f;
            if (navigation.Nav.TryProjectToNavmesh(targetPos, out Vector3 navPos, 5f))
            {
                navigation.SetNewTarget(navPos);
                lastKnownTarget = navPos;
            }
        }
        #endregion
        #region Alive Enemies Lookup
        static HashSet<QuakeEnemy> aliveEnemiesLookup;
        IEnumerable<QuakeEnemy> AliveEnemies
        {
            get
            {
                if (aliveEnemiesLookup == null)
                    yield break;
                foreach (var item in aliveEnemiesLookup.ToList())
                {
                    yield return item;
                }
            }
        }
        private static void MaintainAlive(QuakeEnemy e, bool state)
        {
            if (aliveEnemiesLookup == null) aliveEnemiesLookup = new();
            aliveEnemiesLookup.RemoveWhere(x => x == null);
            switch (state)
            {
                case true:
                    aliveEnemiesLookup.Add(e);
                    break;
                default:
                    aliveEnemiesLookup.Remove(e);
                    break;
            }
        }
        #endregion
        [SerializeField] protected BoxCollider box;
        public BoxCollider UnitCollider => box;
        [SerializeField] protected QGrounded grounded;
        [SerializeField] public RunnableObjectNavigator navigation;
        protected ITargetting target;
        protected Vector3 Origin;
        protected float nextScanTime;
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
            MaintainAlive(this, true);
        }
        protected abstract void WhenEnable();
        private void OnDisable()
        {
            WhenDisable();
            MaintainAlive(this, false);
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

        public GameObject unitGameObject => gameObject;

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
        protected void Pathing(ref float pathTick, Action<QuakeEnemy, IFumoUnit> a, IFumoUnit target)
        {
            if (Time.time < pathTick)
            {
                return;
            }
            pathTick = (Time.time.Max(pathTick)) + 0.1f;
            a?.Invoke(this, target);
        }
        void Think(IFumoUnit targetUnit, float dt)
        {
            if (Stalled)
            {
                navigation.StopMovement();
                return;
            }

            if (Time.time > nextScanTime)
            {
                ITargetting visibleTarget = null;
                if (targetUnit != null &&
                    CanSee(targetUnit.CurrentPosition, scan.distance, out visibleTarget))
                {
                    Action_AlertAndLockTarget(AliveEnemies, visibleTarget);
                    //this sets this.target.
                    //it always scans and if it finds the constant player(IFumoUnit)
                    //it will start chasing the ITargetting it finds.
                }
                nextScanTime = Time.time + 0.5f;

                if (this.target == null && visibleTarget == null)
                {
                    navigation.SetNewTarget(Origin);
                }
            }

            WhenThink(this.target, targetUnit, dt);
        }
        public void SnapTo(Vector3 v, Vector3? offset = null)
        {
            transform.position = v + new Vector3(0f, 0.25f, 0f);
        }
        public void SnapTo(Transform t)
        {
            SnapTo(t.position);
        }
    }
}
