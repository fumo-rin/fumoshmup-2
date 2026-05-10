using UnityEngine;

namespace FumoShmup2
{
    public interface IHit
    {
        public struct HitPacket
        {
            public Vector2 position;
            public Projectile.ProjectileDamage damagePacket;
            public float FinalDamage => damagePacket.DamageMultiplier * damagePacket.BaseDamage;
            public HitPacket(Vector2 position, Projectile.ProjectileDamage damagePacket)
            {
                this.position = position;
                this.damagePacket = damagePacket;
            }
        }
        public void SendHit(HitPacket packet, out float damageDealt);
    }
}