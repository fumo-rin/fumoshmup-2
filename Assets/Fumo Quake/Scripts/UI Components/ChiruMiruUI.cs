using rinCore;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace FumoQuake
{
    partial struct Events_Quake
    {
        public struct ChiruMiruDuration : IRinEvent
        {
            public Vector3 callerPosition;
            public float startTime;
            public float endTime => startTime + duration;
            public float duration;
            public bool IsRunning => Time.time <= endTime;
        }
    }
    public class ChiruMiruUI : MonoBehaviour
    {
        public ACWrapper chiruSound;
        public Image chiruUI, chiruBarFill;
        public Slider chiruSlider;
        public Volume chiruVolume;
        void OnEnable()
        {
            RinBus.Bind<Events_Quake.ChiruMiruDuration>(ResolveChiru);
        }
        void OnDisable()
        {
            RinBus.Release<Events_Quake.ChiruMiruDuration>(ResolveChiru);
        }
        Coroutine current;
        float blueGlow;
        void Awake()
        {
            chiruUI.color = ColorHelper.White.Opacity(0);
            chiruSlider.gameObject.SetActive(false);
            chiruVolume.weight = 0f;
        }
        void ResolveChiru(Events_Quake.ChiruMiruDuration chir)
        {
            chiruSound.Play(chir.callerPosition);
            IEnumerator CO_Run(Events_Quake.ChiruMiruDuration chir)
            {
                chiruSlider.gameObject.SetActive(true);
                while (chir.IsRunning)
                {
                    if (GeneralManager.IsPaused)
                        yield return null;
                    float elapsed = Time.time - chir.startTime;
                    float in01 = (elapsed / 2f).Clamp(0f, 1f);
                    float t01 = (elapsed / chir.duration.Max(0.001f)).Clamp(0f, 1f).MapTo01(1f, 0f);
                    chiruUI.color = ColorHelper.White.Opacity(255).SineAB(ColorHelper.PastelBlue, 3f);
                    chiruBarFill.color = ColorHelper.PastelBlue
                        .SineAB(new ColorHelper.Rainbow(5.NextPrime(3).AsFloat(0.1f), 0f),
                        5.NextPrime(1).AsFloat(0.1f));

                    chiruSlider.SetValues_Nullsafe(t01, 1f, 0f, false);
                    float pulseT = (0.5f + 0.38f.SineWave(Time.time, 0.5f)).MapFrom01(0.15f, 0.35f);
                    pulseT *= in01;
                    chiruVolume.weight = chiruVolume.weight.MoveTowards(pulseT, 12f * Time.deltaTime);
                    yield return null;
                }
                while (chiruVolume.weight > 0f)
                {
                    chiruVolume.weight = chiruVolume.weight.MoveTowards(0f, 1.5f * Time.deltaTime);
                    yield return null;
                }
            }
            if (current != null)
            {
                StopCoroutine(current);
            }
            current = StartCoroutine(CO_Run(chir).Wrap(() =>
            {
                current = null;
                chiruUI.color = ColorHelper.White.Opacity(0);
                chiruSlider.gameObject.SetActive(false);
                chiruVolume.weight = 0f;
            }));
        }
    }
}
