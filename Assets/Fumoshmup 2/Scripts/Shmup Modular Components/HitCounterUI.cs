using rinCore;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FumoShmup2
{
    [DefaultExecutionOrder(-50)]
    public class HitCounterUI : MonoBehaviour
    {
        [SerializeField] TMP_Text hitText;
        [SerializeField] Slider hitSlider;
        float decayStallEnd;
        float visibleHit;
        float current = 0f;
        private void Graze(int delta, int total)
        {
            if (ShmupSession.CurrentAs(out ShmupSession s))
            {
                s.ChangeFloat(ShmupSession.keys.HitCounter, 1, 0f, 99999f);
            }
        }
        private void OnEnable()
        {
            ShmupGamemode.WhenGraze += Graze;
        }
        private void OnDisable()
        {
            ShmupGamemode.WhenGraze -= Graze;
        }
        private void Update()
        {
            float hit = 0;
            if (ShmupSession.CurrentAs(out ShmupSession s))
            {
                if (ShmupPlayer.PlayerAs(out ShmupPlayer p))
                {
                    current = p && !p.IsAlive ? 0f : s.GetFloat(ShmupSession.keys.CashoutActivation060);
                    if (p.IsAlive && !ShmupInput.Focus && ShmupInput.Shoot)
                    {
                        current = current.Max(2f);
                        current = current.MoveTowards(65f, Time.deltaTime * (current < 20f ? 200f : 40f));
                        decayStallEnd = Time.time;
                    }
                    else if (Time.time >= decayStallEnd)
                    {
                        float decayMod = (decayStallEnd - Time.time).Absolute().MapTo01(0f, 0.35f, true);
                        current = current.MoveTowards(-10, decayMod * Time.deltaTime * (ShmupInput.ShootReleasedLongerThan(0.35f) ? 120f : 15f));
                    }
                }
                else
                {
                    current = current.MoveTowards(-10, Time.deltaTime * (ShmupInput.ShootReleasedLongerThan(0.35f) ? 120f : 15f));
                }
                s.SetFloat(ShmupSession.keys.CashoutActivation060, current, -10f, 65f);
                hitSlider.SetValuesInt(current.Multiply(0.5f).ToInt(), 30, 0);
                hit = s.GetFloat(ShmupSession.keys.HitCounter);
                if (current <= 0.1f)
                {
                    hit = (hit - Time.deltaTime * hit.Clamp(1000f, 20000f) * 0.2f).Clamp(0, 99999);
                }
                s.SetFloat(ShmupSession.keys.HitCounter, hit, 0, 99999f);
                visibleHit = visibleHit.LerpTowards(hit, 20f * Time.deltaTime);
                if (visibleHit <= 0.1f)
                    hitText.text = "";
                else
                {
                    string number = visibleHit.Ceil().ToInt().Clamp(1, 99999).ToString();
                    hitText.text = (visibleHit >= 1f && current <= 0.1f) ? number.Color(ColorHelper.RedHealthBackground) : number;
                }
            }
        }
    }
}
