using rinCore;
using UnityEngine;

namespace FumoShmup2
{
    [DefaultExecutionOrder(-999)]
    public class MoveLerpAction : UnitAction
    {
        float startTime;
        public LerpSettings settings;
        public MoveLerpAction(FumoUnit owner, float duration, LerpSettings settings) : base(owner, duration)
        {
            this.settings = settings;
            startTime = Time.time;
        }
        protected override ActionResult RunAction(FumoUnit Owner)
        {
            if (Time.time >= startTime + settings.duration)
            {
                return ActionResult.End;
            }
            float t = Mathf.Clamp01((Time.time - startTime) / settings.duration);
            Vector2 lerped = settings.UnMappedStart.LerpEaseInOut01(settings.UnMappedEnd, t);
            Owner.SetPosition(lerped);
            if (Owner is EnemyUnit e)
            {
                e.SetLeashPosition(e.CurrentPosition);
            }
            return ActionResult.Performed;
        }

        public override bool IsRunning()
        {
            return Time.time > startTime && duration > 0;
        }
        [System.Serializable]
        public struct LerpSettings
        {
            public Vector2 UnMappedStart;
            public Vector2 UnMappedEnd;
            public float duration;
            public LerpSettings(Vector2 start, Vector2 end, float duration)
            {
                this.UnMappedStart = start;
                this.UnMappedEnd = end;
                this.duration = duration;
            }
            public LerpSettings(Vector2Shmup start, Vector2Shmup end, float duration)
            {
                this.UnMappedStart = start.Vector2Now;
                this.UnMappedEnd = end.Vector2Now;
                this.duration = duration;
            }
            public static LerpSettings LerpDown(Vector2 start, float distance, float randomX, float duration, bool clampToWorldspace)
            {
                Vector2 end = start + new Vector2(randomX.RandomPositiveNegativeRange(), -1f * distance);
                return new()
                {
                    UnMappedStart = start,
                    UnMappedEnd = end,
                    duration = duration
                };
            }
        }
    }
}
