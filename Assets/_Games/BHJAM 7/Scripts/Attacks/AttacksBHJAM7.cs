using UnityEngine;
using rinCore;
using FumoShmup2;
using System.Collections;
namespace BHJAM7
{
    public class AttacksBHJAM7
    {
        [System.Serializable]
        public class Fodder1 : UnitAttack
        {
            public ProjectileDefineSO Shot;
            protected override IEnumerator CO_AttackPayload(ShmupUnit sender, Projectile.InputSettings input)
            {
                for (int i = 0; i < 8; i++)
                {
                    Arc(3f.RandomPositiveNegativeRange(), 60f, 3, 7f).Spawn(input, Shot, out _);
                    input.ReAimWithOptionalTarget();
                    yield return 0.08f.WaitForSeconds();
                }
                yield return 0.75f.WaitForSeconds();
            }
        }
        [System.Serializable]
        public class Fodder2 : UnitAttack
        {
            public ProjectileDefineSO Shot;
            protected override IEnumerator CO_AttackPayload(ShmupUnit sender, Projectile.InputSettings input)
            {
                Arc(0f, 45f, 4, 6.5f).Spawn(input, Shot, out _);
                for (int i = 0; i < 8; i++)
                {
                    Arc(-3f + 1f.RandomPositiveNegativeRange(), 60f, 3, i.AsFloat(0.75f) + 6f).Spawn(input, Shot, out _);
                }
                yield return 0.35f.WaitForSeconds();
                input.ReAimWithOptionalTarget();
                Arc(0f, 85f, 6, 9f).Spawn(input, Shot, out _);
                for (int i = 0; i < 8; i++)
                {
                    Arc(3f + 1f.RandomPositiveNegativeRange(), 60f, 3, i.AsFloat(0.75f) + 8f).Spawn(input, Shot, out _);
                }
                yield return 1.25f.WaitForSeconds();
            }
        }
        [System.Serializable]
        public class FodderArc : UnitAttack
        {
            public ProjectileDefineSO Shot;
            protected override IEnumerator CO_AttackPayload(ShmupUnit sender, Projectile.InputSettings input)
            {
                yield break;
            }
        }
        [System.Serializable]
        public class FodderArc2 : UnitAttack
        {
            public ProjectileDefineSO Shot;
            protected override IEnumerator CO_AttackPayload(ShmupUnit sender, Projectile.InputSettings input)
            {
                yield break;
            }
        }
    }
}
