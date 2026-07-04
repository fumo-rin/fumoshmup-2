using UnityEngine;
using rinCore;
using System.Linq;
namespace FumoQuake
{
    public class Imp : MonoBehaviour
    {
        [SerializeField] protected RunnableObjectNavigator navigation;
        protected ITargetting target;
        protected Vector3 Origin;
        private void Awake()
        {
            Origin = transform.position;
        }
        private void Update()
        {
            target = ITargetting.StaticTarget;
            IFumoUnit targetUnit = IFumoUnit.Player;
            Think(target, targetUnit, Time.deltaTime);
        }
        float StallTimeEnd = 0;
        float RandomAttackTime;
        float nextPathTick;

        public bool IsAlive { get; set; } = true;
        public Vector3 CurrentPosition => transform.position;

        void Think(ITargetting target, IFumoUnit targetUnit, float dt)
        {
            if (StallTimeEnd > Time.time)
            {
                return;
            }
            void GetNewAttackTime(ref float f)
            {
                f = f.Max(Time.time + RNG.FloatRange(0.6f, 0.85f));
            }
            void Pathing(ref float f)
            {
                if (f < Time.time)
                {
                    return;
                }
                f = (Time.time.Max(f)) + 0.1f;
                if (targetUnit == null)
                    return;

                Vector3 targetPos = targetUnit.CurrentPosition;
                bool PathTowards = targetUnit.IsAlive && targetUnit.CurrentPosition.SquareDistanceToGreaterThan(transform.position, 3f);
                if (!PathTowards)
                {
                    navigation.Nav.StopPath();
                    return;
                }
                Vector3 navPos = targetPos;
                if (!navigation.Nav.TryProjectToNavmesh(targetPos, out navPos, 5f))
                {
                    transform.position = Origin;
                    navigation.Nav.StopPath();
                    return;
                }
                navigation.SetNewTarget(navPos);
            }
            void Targetting()
            {
                if (target != null && Time.time > RandomAttackTime)
                {
                    Transform t = target.RandomOrderedTargets.First();
                }
            }
            Pathing(ref nextPathTick);
            Targetting();
        }
    }
}
