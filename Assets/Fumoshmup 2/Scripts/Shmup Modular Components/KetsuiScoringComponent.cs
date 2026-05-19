using rinCore;
using System.Runtime.InteropServices.WindowsRuntime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FumoShmup2
{
    [DefaultExecutionOrder(-50)]
    public class KetsuiScoringComponent : MonoBehaviour
    {
        [SerializeField] TMP_Text hitText, comboText;
        [SerializeField] Slider comboSlider;
        float visibleHit;
        float comboDecaySeconds = 0f;
        float lowComboDecayStall;
        float enemyKillLockTimeEnd;
        int CurrentCombo;

        const float MAX_COMBO_DECAY = 2f;

        private void Graze(int delta, int total)
        {
            if (ShmupSession.CurrentAs(out ShmupSession s))
            {
                s.ChangeFloat(ShmupSession.keys.HitCount, 3, 0f, 99999f);
            }
        }
        private float DetermineCombo()
        {
            float combo = CurrentCombo.AsFloat(1f).Clamp(1f, 5f);
            return combo;
        }
        private void WhenEnemyKilled(EnemyUnit e)
        {
            CurrentCombo = CurrentCombo.Add(1).Clamp(1, 5);
            enemyKillLockTimeEnd = Time.time + 0.2f;
            comboDecaySeconds = (comboDecaySeconds + (e.CurrentMaxHealth > 0f ? e.CurrentMaxHealth * 0.004f : 0.25f)).Clamp(0f, MAX_COMBO_DECAY);
        }
        private void WhenContinue()
        {
            visibleHit = 0f;
            comboDecaySeconds = 0f;
            lowComboDecayStall = 0f;
            enemyKillLockTimeEnd = 0f;
            CurrentCombo = 1;
        }
        private void WhenEnemiesDamaged(float damage)
        {
            lowComboDecayStall = Time.time + 0.3f;
            if (ShmupSession.CurrentAs(out ShmupSession s))
            {
                s.ChangeFloat(ShmupSession.keys.HitCount, damage * 0.2f, 0, 99999f);
            }
        }
        private void OnEnable()
        {
            ShmupGamemode.WhenGraze += Graze;
            PointItemRunner.WhenGetComboValue += DetermineCombo;
            EnemyUnit.WhenEnemyKilled += WhenEnemyKilled;
            EnemyUnit.WhenAnyEnemyDamaged += WhenEnemiesDamaged;
            ShmupSession.WhenContinue += WhenContinue;
        }
        private void OnDisable()
        {
            ShmupGamemode.WhenGraze -= Graze;
            PointItemRunner.WhenGetComboValue -= DetermineCombo;
            EnemyUnit.WhenEnemyKilled -= WhenEnemyKilled;
            EnemyUnit.WhenAnyEnemyDamaged -= WhenEnemiesDamaged;
            ShmupSession.WhenContinue -= WhenContinue;
        }
        private void Update()
        {
            float hit = 0;
            if (ShmupSession.CurrentAs(out ShmupSession s))
            {
                #region Stalled
                if (s.GameLogicStalled || EnemyUnit.BossPhaseStall)
                {
                    lowComboDecayStall = Time.time + 0.3f;
                    return;
                }
                #endregion
                #region Player Shooting
                bool hasPlayer = ShmupPlayer.PlayerAs(out ShmupPlayer p);
                if (hasPlayer)
                {
                    if (p.IsAlive && !ShmupInput.Focus && ShmupInput.Shoot)
                    {

                    }
                    else if (comboDecaySeconds <= 0)
                    {

                    }
                }
                else
                {

                }
                #endregion
                #region Combo
                void RunCombo()
                {
                    if (hasPlayer && !p.IsAlive)
                    {
                        comboDecaySeconds = (comboDecaySeconds - Time.deltaTime.Multiply(4f)).Clamp(0f, MAX_COMBO_DECAY);
                        if (CurrentCombo > 1 && comboDecaySeconds <= 0f)
                        {
                            CurrentCombo = 1;
                        }
                        return;
                    }
                    if (Time.time < enemyKillLockTimeEnd)
                        return;
                    if (Time.time <= lowComboDecayStall && comboDecaySeconds.IsBetween(0.01f, 0.25f))
                    {
                        comboDecaySeconds = 0.25f;
                    }
                    else
                    {
                        comboDecaySeconds = (comboDecaySeconds - Time.deltaTime).Clamp(0f, MAX_COMBO_DECAY);
                        if (CurrentCombo > 1 && comboDecaySeconds <= 0f)
                        {
                            CurrentCombo = 1;
                        }
                    }
                }
                RunCombo();
                comboSlider.SetValuesInt(comboDecaySeconds.Multiply(8f).ToInt(), 8.MultiplyAndFloorAsFloat(MAX_COMBO_DECAY).ToInt(), 0);
                comboText.text = CurrentCombo <= 1 ? "" : CurrentCombo.Clamp(1, 5).ToString() + "x";
                #endregion
                #region Hit Decay & Text
                hit = s.GetFloat(ShmupSession.keys.HitCount);
                if (comboDecaySeconds <= 0f)
                {
                    hit = (hit - Time.deltaTime * hit.Clamp(1000f, 20000f) * 0.2f).Clamp(0, 99999);
                }
                s.SetFloat(ShmupSession.keys.HitCount, hit, 0, 99999f);

                visibleHit = visibleHit.LerpTowards(hit, 20f * Time.deltaTime);

                if (visibleHit <= 1f)
                    hitText.text = "";
                else
                {
                    string number = visibleHit.Floor().ToInt().Clamp(1, 99999).ToString();
                    hitText.text = (visibleHit >= 1f && comboDecaySeconds <= 0.1f) ? number.Color(ColorHelper.RedHealthBackground) : number;
                }
                #endregion
            }
        }
    }
}
