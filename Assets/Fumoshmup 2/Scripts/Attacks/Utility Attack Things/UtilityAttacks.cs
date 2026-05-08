using rinCore;
using System.Collections;
using UnityEngine;

namespace FumoShmup2
{
    [System.Serializable]
    public class UtilityAttacks
    {
        [System.Serializable]
        public class AttackWait : UnitAttack
        {
            public float waitDuration = 1f;
            protected override IEnumerator CO_AttackPayload(ShmupUnit sender, Projectile.InputSettings input)
            {
                yield return waitDuration.WaitForSeconds();
            }
        }
    }
}
