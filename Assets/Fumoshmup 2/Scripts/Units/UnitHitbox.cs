using FumoShmup2;
using UnityEngine;

public enum ShmupFaction
{
    Default,
    Player,
    Hostile
}
public class UnitHitbox : MonoBehaviour
{
    [SerializeField] ShmupFaction hitboxFaction = ShmupFaction.Hostile;
}
public interface IHit
{
    public struct HitPacket
    {
        public Vector2 position;
        public Projectile.ProjectileDamage damagePacket;
        public HitPacket(Vector2 position, Projectile.ProjectileDamage damagePacket)
        {
            this.position = position;
            this.damagePacket = damagePacket;
        }
    }
    public void Sendhit(HitPacket packet, out float damageDealt);
}