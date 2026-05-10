using rinCore;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
namespace FumoShmup2
{
    public class SweepFlash : MonoBehaviour
    {
        public static void TriggerFlash(float duration)
        {
            if (RinHelper.ValidGameObjects(runner) && runner is SweepFlash f)
            {
                if (f.runningVolume != null)
                {
                    f.StopCoroutine(f.runningVolume);
                }
                f.runningVolume = f.StartCoroutine(CO_RunVolume());
                if (f.sweepAnim != null)
                    f.sweepAnim.SetTrigger(f.sweepAnimKey);
            }
            IEnumerator CO_RunVolume()
            {
                f.sweepSound.Play(ALHandler.Position);
                if (Vol(out Volume v))
                {
                    v.weight = 1f;
                    float runningDuration = duration;
                    yield return 0.02f.WaitForSeconds();
                    while (runningDuration > 0)
                    {
                        float lerp = runningDuration / duration.Max(0.01f);
                        lerp = Mathf.Pow(lerp, 2f);

                        v.weight = lerp;
                        runningDuration -= Time.deltaTime;
                        yield return null;
                    }
                    v.weight = 0f;
                }
                f.runningVolume = null;
            }
        }
        Coroutine runningVolume;
        static SweepFlash runner;
        [SerializeField] Volume sweepVolume;
        [SerializeField] Animator sweepAnim;
        [SerializeField] string sweepAnimKey = "SWEEP";
        [SerializeField] ACWrapper sweepSound;
        private static bool Vol(out Volume v)
        {
            v = null;
            if (RinHelper.ValidGameObjects(runner, runner.sweepVolume))
            {
                v = runner.sweepVolume;
            }
            return v != null;
        }
        private void Awake()
        {
            runner = this;
            if (sweepVolume != null)
                sweepVolume.weight = 0;
        }

    }
}
