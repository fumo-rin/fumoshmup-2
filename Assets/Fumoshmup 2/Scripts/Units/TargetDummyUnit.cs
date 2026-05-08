using rinCore;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace FumoShmup2
{
    public class TargetDummyUnit : EnemyUnit, IHit
    {
        float recentHitTime;
        [SerializeField] TMP_Text dpsText;
        float totalDamage;
        float startDPSCountTime;
        float dpsDuration => Time.time - startDPSCountTime;
        float totalDPS => totalDamage / dpsDuration;
        public new void SendHit(IHit.HitPacket packet, out float damageDealt)
        {
            bool restartDPS = (Time.time - recentHitTime).Absolute() > 2.5f || recentHitTime <= 0f;
            if (restartDPS)
            {
                startDPSCountTime = Time.time;
                totalDamage = 0f;
            }
            recentHitTime = Time.time;
            damageDealt = packet.FinalDamage;
            totalDamage += damageDealt;
        }
        private void LateUpdate()
        {
            dpsText.text = "DPS:##".ReplaceLineBreaks("##") + IntExtensions.Clamp(totalDPS.ToInt(), 0, 99999999).ToString();
        }
    }
}
