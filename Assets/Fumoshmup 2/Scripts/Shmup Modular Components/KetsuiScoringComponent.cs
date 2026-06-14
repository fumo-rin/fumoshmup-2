using rinCore;
using System.Collections;
using System.Runtime.InteropServices.WindowsRuntime;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FumoShmup2
{
    [DefaultExecutionOrder(-50)]
    public class KetsuiScoringComponent : MonoBehaviour
    {
        [SerializeField] TMP_Text hitText, comboText;
        [SerializeField] Slider comboSlider;
        float visibleHit;
        float freezeTimeEnd;

        const int MAX_COMBO = 9;
        float ComboValue100 = 0f;
        bool Spending => PointItemRunner.SuperMechanic;
        Coroutine RunningKillChange;
        bool RedHit => RunningKillChange != null;
        bool GreenHit => Spending;
        int VisibleCombo => DetermineCombo().ToInt();
        private void Graze(int delta, int total)
        {
            if (ShmupSession.CurrentAs(out ShmupSession s))
            {
                s.ChangeFloat(ShmupSession.keys.HitCount, delta.AsFloat(3f), 0f, 100000f);
            }
            ComboValue100 += delta;
        }
        private float DetermineCombo()
        {
            float combo = ComboValue100.Multiply(0.01f).Floor().Clamp(0f, MAX_COMBO - 1).Add(1);
            return combo;
        }
        private void ChangeCombo(float value)
        {
            ComboValue100 = ComboValue100.Add(value).Clamp(0f, MAX_COMBO.Add(-1) * 100f + 99f);
        }
        private void WhenEnemyKilled(EnemyUnit e)
        {
            float comboAdd = (e.CurrentMaxHealth * 0.4f).Clamp(20f, 100f);
            ChangeCombo(comboAdd);
        }
        private void WhenContinue()
        {
            visibleHit = 0f;
            ComboValue100 = 0f;
            //in the future reset the economy in the session. obviouslyin theh session itself.
        }
        void LowerHitCount(float delta)
        {
            if (ShmupSession.CurrentAs(out ShmupSession s))
            {
                if (RunningKillChange != null)
                {
                    StopCoroutine(RunningKillChange);
                }
                RunningKillChange = StartCoroutine(CO_Run(s));
            }
            IEnumerator CO_Run(ShmupSession s)
            {
                float lowerCount = delta.Absolute();
                float stepSize = lowerCount / 50f;
                while (lowerCount > 0f)
                {
                    float trueDelta = stepSize.Min(lowerCount).AbsoluteNegative();
                    s.ChangeFloat(ShmupSession.keys.HitCount, trueDelta, 0f, 100000f);
                    lowerCount -= stepSize;
                    yield return 0.015f.WaitForSeconds();
                }
                RunningKillChange = null;
            }
        }
        private void WhenBomb()
        {
            float count = visibleHit * 0.2f;
            ChangeCombo(ComboValue100.Multiply(0.666f).Clamp(0f, 200f).AbsoluteNegative());
            LowerHitCount(count.Clamp(2500f, 25000f));
        }
        private void WhenDie()
        {
            float count = visibleHit * 0.5f;
            LowerHitCount(count.Clamp(10000f, 25000f));
        }
        private void WhenEnemiesDamaged(float damage)
        {
            if (ShmupSession.CurrentAs(out ShmupSession s))
            {
                s.ChangeFloat(ShmupSession.keys.HitCount, damage * VisibleCombo, 0, 100000f);
            }
        }
        private void OnEnable()
        {
            ShmupGamemode.WhenGraze += Graze;
            PointItemRunner.WhenGetComboValue += DetermineCombo;
            EnemyUnit.WhenEnemyKilled += WhenEnemyKilled;
            EnemyUnit.WhenAnyEnemyDamaged += WhenEnemiesDamaged;
            ShmupSession.WhenContinue += WhenContinue;
            PlayerBomb.WhenBomba += WhenBomb;
            ShmupPlayer.WhenPlayerDieFrame += WhenDie;
        }
        private void OnDisable()
        {
            ShmupGamemode.WhenGraze -= Graze;
            PointItemRunner.WhenGetComboValue -= DetermineCombo;
            EnemyUnit.WhenEnemyKilled -= WhenEnemyKilled;
            EnemyUnit.WhenAnyEnemyDamaged -= WhenEnemiesDamaged;
            ShmupSession.WhenContinue -= WhenContinue;
            PlayerBomb.WhenBomba -= WhenBomb;
            ShmupPlayer.WhenPlayerDieFrame -= WhenDie;
        }
        private void Update()
        {
            float hit = 0;
            float ComboValueDecay = 30f;
            if (ShmupSession.CurrentAs(out ShmupSession s))
            {
                #region Combo & Logic

                const float comboMod = 16f / 100f;
                int comboSliderNumber = (ComboValue100 % 100f).Multiply(comboMod).ToInt();
                comboSlider.SetValuesInt(ComboValue100 < 1f ? 0 : comboSliderNumber, 16, 0);
                comboText.text = VisibleCombo <= 1 ? "" : VisibleCombo.ToString() + "x";

                if (Spending)
                {
                    ComboValueDecay *= 7f;
                }
                #endregion
                #region Stalled & Draw
                bool stall = s.GameLogicStalled || EnemyUnit.BossPhaseStall;
                hit = s.GetFloat(ShmupSession.keys.HitCount);
                s.SetFloat(ShmupSession.keys.HitCount, hit, 0, 99999f);
                visibleHit = visibleHit.LerpTowards(hit, 20f * Time.deltaTime);

                if (visibleHit <= 1f)
                    hitText.text = "";
                else
                {
                    string number = visibleHit.Floor().ToInt().Clamp(1, 100000).ToString();
                    hitText.text = (GreenHit) ? number.Color(ColorHelper.PastelGreen) : (visibleHit >= 1f && RedHit) ? number.Color(ColorHelper.RedHealthBackground) : number;
                }

                if (stall)
                {
                    return;
                }
                else
                {
                    ChangeCombo(Time.deltaTime * ComboValueDecay.AbsoluteNegative());
                }
                #endregion
                #region Player State
                bool hasPlayer = ShmupPlayer.PlayerAs(out ShmupPlayer p);
                if (hasPlayer)
                {
                    if (p.IsAlive && !ShmupInput.Focus && ShmupInput.Shoot)
                    {

                    }
                    if (!p.IsAlive)
                    {
                        ComboValue100 = 0f;
                    }
                }
                else
                {

                }
                #endregion
            }
        }
    }
}
