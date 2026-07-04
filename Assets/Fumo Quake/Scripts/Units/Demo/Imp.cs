using UnityEngine;
using rinCore;
using System.Linq;
using System;
using UnityEngine.AI;
namespace FumoQuake
{
    public abstract class QuakeEnemy : MonoBehaviour
    {
        #region Pathfinding Algo
        protected void Path_DirectlyTowards(IFumoUnit other)
        {
            if (other == null)
                return;

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
        }
        #endregion
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
            WhenStart();
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
            target = ITargetting.StaticTarget;
            IFumoUnit targetUnit = IFumoUnit.Player;

            Think(target, targetUnit, Time.deltaTime);
            WhenThink(target, targetUnit, Time.deltaTime);
        }
        protected float StallTimeEnd = 0;
        public bool Stalled => Time.time < StallTimeEnd;
        protected float RandomAttackTime;
        protected float nextPathTick;
        public bool IsAlive { get; set; } = true;
        public Vector3 CurrentPosition => transform.position;
        protected abstract
        void WhenThink(ITargetting target, IFumoUnit targetUnit, float dt);
        protected void Pathing(ref float pathTick, Action<IFumoUnit> a, IFumoUnit target)
        {
            if (Time.time < pathTick)
            {
                return;
            }
            pathTick = (Time.time.Max(pathTick)) + 0.1f;
            a?.Invoke(target);
        }
        void Think(ITargetting target, IFumoUnit targetUnit, float dt)
        {
            if (Stalled)
                return;

            void GetNewAttackTime(ref float f)
            {
                f = f.Max(Time.time + RNG.FloatRange(0.6f, 0.85f));
            }
        }
    }
    public class Imp : QuakeEnemy
    {
        void Targetting(ITargetting target)
        {
            if (target != null && Time.time > RandomAttackTime)
            {
                Transform t = target.RandomOrderedTargets.First();
            }
        }
        protected override void WhenThink(ITargetting target, IFumoUnit targetUnit, float dt)
        {
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
