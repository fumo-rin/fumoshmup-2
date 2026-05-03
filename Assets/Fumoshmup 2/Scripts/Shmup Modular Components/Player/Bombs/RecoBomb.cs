using rinCore;
using System.Collections;
using UnityEngine;

namespace FumoShmup2
{
    public class RecoBomb : PlayerBomb
    {
        float lastBombTime = 0f;
        [SerializeField] SpriteRenderer bombSpritePrefab;
        [SerializeField] ParticleSystem explosionParticle;
        [SerializeField] float spriteSize = 8f;
        [SerializeField] ACWrapper throwSound, blowupSound;
        [SerializeField] float BombDamage = 800f;
        [SerializeField] float BombIframesTime = 2.25f;
        protected override void WhenAwake()
        {
            bombSpritePrefab.gameObject.SetActive(false);
        }
        protected override bool canBomb
        {
            get
            {
                if (Time.time < lastBombTime + 0.75f)
                    return false;
                bool canBomb = false;
                if (ShmupSession.CurrentAs(out ShmupSession session))
                {
                    canBomb = session.GetResource(ShmupSession.keys.CurrentBombs) > 0;
                }
                if (ShmupPlayer.PlayerAs(out ShmupPlayer p) && p.IsAlive)
                {
                    return canBomb;
                }
                return false;
            }
        }

        protected override IEnumerator BombPayload(ShmupUnit Owner)
        {
            lastBombTime = Time.time;
            if (ShmupSession.CurrentAs(out ShmupSession session))
            {
                session.ChangeResource(ShmupSession.keys.CurrentBombs, -1);
            }
            ShmupPlayer ownerPlayer = Owner as ShmupPlayer;
            ownerPlayer.SetCurrentIFrames(BombIframesTime);

            Vector2 startPos = Owner.CurrentPosition + Vector2.up.ScaleToMagnitude(0.5f);
            SpriteRenderer sr = Instantiate(bombSpritePrefab, startPos, Quaternion.identity);
            sr.gameObject.SetActive(true);
            Vector2 target = startPos + Vector2.up.ScaleToMagnitude(4f);
            ShmupWorldspace.MapWorldspaceToNormalized(target, out Vector2 norm, true);
            ShmupWorldspace.MapToWorldspaceUnclamped(norm.x.Clamp(0.25f, 0.75f), norm.y.Clamp(0.5f, 0.7f), out Vector2 endPos);

            float lerp = 0f;
            throwSound.Play(startPos);
            while (lerp < 0.95f)
            {
                lerp += Time.deltaTime * 3.5f;
                float size = (1f - lerp) * spriteSize;
                Vector2 pos = startPos.LerpUnclamped(endPos, lerp);
                sr.transform.localScale = new Vector3(size, size, 1f);
                sr.transform.position = pos;
                yield return null;
            }
            Destroy(sr.gameObject);
            explosionParticle.transform.position = endPos;
            explosionParticle.transform.SetParent(null);
            explosionParticle.Play();
            ProjectileRunner.TriggerSweep(1f, 0, false, out _);
            ownerPlayer.SetCurrentIFrames(2f);
            blowupSound.Play(endPos);
            float remainingBombDamage = BombDamage;
            while (remainingBombDamage > 0f)
            {
                float damage = (BombDamage / 30f).Clamp(0f, remainingBombDamage);
                if (damage <= 0f)
                {
                    break;
                }
                foreach (var item in EnemyUnit.AliveEnemiesOnScreen)
                {
                    if (item == null)
                    {
                        continue;
                    }
                    item.SendHit(new IHit.HitPacket(item.CurrentPosition - new Vector2(0.25f.RandomPositiveNegativeRange(), -0.5f), new(Owner, damage, 1f)), out _);
                }
                remainingBombDamage -= damage;
                yield return (0.025f).WaitForSeconds();
            }
        }
    }
}
