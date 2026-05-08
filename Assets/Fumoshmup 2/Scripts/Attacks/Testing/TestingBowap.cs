using rinCore;
using System.Collections;
using UnityEngine;

namespace FumoShmup2
{
    public class TestingAttacks
    {
        [System.Serializable]
        public class ShotArc : UnitAttack
        {
            public ProjectileDefineSO shot;
            protected override IEnumerator CO_Attackpayload(ShmupUnit sender, Projectile.InputSettings input)
            {
                for (int i = 0; i < 13; i++)
                {
                    input.ReAimWithOptionalTarget(sender.CurrentPosition);
                    if (Arc(0f, 60f, 7, 6f).Spawn(input, shot, out _))
                    {
                        yield return 0.06f.WaitForSeconds();
                    }
                }
                yield return 0.1f.WaitForSeconds();
            }
        }
        [System.Serializable]
        public class ShotCircle : UnitAttack
        {
            public ProjectileDefineSO shot;
            protected override IEnumerator CO_Attackpayload(ShmupUnit sender, Projectile.InputSettings input)
            {
                Circle(0f, 16, 7.5f).Spawn(input, shot, out _);
                Circle(360f / 32, 16, 8.5f).Spawn(input, shot, out _);
                Circle(0f, 16, 9.5f).Spawn(input, shot, out _);
                Circle(360f / 32, 16, 10.5f).Spawn(input, shot, out _);
                yield return 0.2f.WaitForSeconds();
            }
        }
        [System.Serializable]
        public class StressTestSpam : UnitAttack
        {
            public ProjectileDefineSO shot;
            public int projectilePerSecond = 10000;
            public int seconds = 8;
            protected override IEnumerator CO_Attackpayload(ShmupUnit sender, Projectile.InputSettings input)
            {
                int repeats = 60;
                int shotCount = projectilePerSecond / repeats;
                int durationLeft = seconds;
                while (durationLeft > 0 && sender != null && sender.IsAlive)
                {
                    for (int i = 0; i < repeats; i++)
                    {
                        Circle(RNG.FloatRange(0f, 360f), shotCount, 11f).Spawn(input, shot, out _);
                        yield return 0.016f.WaitForSeconds();
                    }
                    durationLeft -= 1;
                }
            }
        }
        [System.Serializable]
        public class TestingBowap : UnitAttack
        {
            public ProjectileDefineSO bowapProjectile;
            public BowapSettings bowap = new()
            {
                angleSpin = -0.2f,
                anglePower = 1.75f,
                frameCount = 1,
                arms = 8,
                initialOffset = -20,
                startingSpeed = 7,
                maxSpeed = 11,
                speedRampTime = 6,
                duration = 20
            };
            [System.Serializable]
            public struct BowapSettings
            {
                public float angleSpin;
                public float anglePower;
                public int frameCount;
                public int arms;
                public float initialOffset;
                public float startingSpeed;
                public float maxSpeed;
                public float speedRampTime;
                public float duration;
            }
            protected override IEnumerator CO_Attackpayload(ShmupUnit sender, Projectile.InputSettings input)
            {
                float lerpTime = 0f;
                int iteration = 0;
                while (lerpTime < bowap.duration && sender != null && sender.IsAlive)
                {
                    lerpTime = iteration * 0.01667f;
                    input.addedForward = 0.5f;
                    float ramp01 = lerpTime.MapTo01(0f, bowap.speedRampTime).Clamp(0f, 1f);
                    float angle = Mathf.Pow(iteration, bowap.anglePower) * bowap.angleSpin;
                    input.SetOrigin(sender.CurrentPosition);
                    if (iteration % bowap.frameCount.Max(1) == 0)
                    {
                        float lerpSpeed = bowap.startingSpeed.LerpUnclamped(bowap.maxSpeed, ramp01);
                        Circle(angle + bowap.initialOffset, bowap.arms, lerpSpeed).Spawn(input, bowapProjectile, out iterationList);
                    }
                    iteration += 1;
                    yield return 0.01667f.WaitForSeconds();
                }
            }
        }
    }
}
