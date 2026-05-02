using UnityEngine;
using rinCore;
using UnityEngine.Rendering;
using System.Collections;
namespace FumoShmup2
{
    public class SweepFlash : MonoBehaviour
    {
        public static void TriggerFlash(float duration)
        {
            if (RinHelper.ValidGameObjects(runner) && runner is SweepFlash f)
            {
                if (f.running != null)
                {
                    f.StopCoroutine(f.running);
                }
                f.running = f.StartCoroutine(CO_Run());
            }
            IEnumerator CO_Run()
            {
                if (Vol(out Volume v))
                {
                    v.weight = 1f;
                    float runningDuration = duration;
                    f.sweepSound.Play(ALHandler.Position);
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
                f.running = null;
            }
        }
        Coroutine running;
        static SweepFlash runner;
        [SerializeField] Volume sweepVolume;
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
            sweepVolume.weight = 0;
        }

    }
}
