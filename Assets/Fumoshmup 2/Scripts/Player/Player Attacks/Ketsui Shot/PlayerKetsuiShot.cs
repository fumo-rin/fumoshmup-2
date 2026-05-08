using rinCore;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace FumoShmup2
{
    public class PlayerKetsuiShot : MonoBehaviour
    {
        [SerializeField] InputActionReference shootingAction, focusAction;
        [SerializeField] ShmupUnit Owner;
        [SerializeField] ProjectileDefineSO unfocusShot, optionShot, superShot;
        [SerializeField] ACWrapper unfocusShotSound;
        [SerializeField] Transform[] shotOptionNests4 = new Transform[4];
        AttackBuilder a = new();
        HashSet<Projectile> shotcap = new();
        IEnumerable<Vector2> Options(float lerp01)
        {
            lerp01 = lerp01.Clamp(0f, 1f);
            Vector2[] a = new Vector2[4] { new(1.35f, -0.55f), new(0.85f, -0.35f), new(-0.85f, -0.35f), new(-1.35f, -0.55f) };
            Vector2[] b = new Vector2[4] { new(-2.15f, -0.65f), new(-1.35f, -0.75f), new(1.35f, -0.75f), new(2.15f, -0.65f) };
            for (int i = 0; i < 4; i++)
            {
                yield return a[i].LerpUnclamped(b[i], lerp01);
            }
        }
        bool Shooting => shootingAction.IsPressedRaw();
        bool CanSuperShot => focusAction.PressedLongerThan(0.175f);
        float cachedFocusLerp = 0f;
        Coroutine currentShot;
        float forwardLockTimeEnd;
        void Update()
        {
            cachedFocusLerp = cachedFocusLerp.LerpUnclamped(focusAction.IsPressedRaw() ? 1f : 0f, Time.deltaTime * 12f).Clamp(0f, 1f);
            int optionIteration = 0;
            foreach (var a in Options(cachedFocusLerp))
            {
                var i = shotOptionNests4[optionIteration];
                if (i != null)
                {
                    i.transform.localPosition = a;
                }
                optionIteration++;
            }
            IEnumerator CO_Supershot()
            {
                yield return 0.05f.WaitForSeconds();
                a.BuildInput(Owner, out Projectile.InputSettings input);
                if (a.Single(0f, 30f).Spawn(input, superShot, out Projectile p))
                {
                    p.SetDamage(new(Owner, 100f, 1f));
                }
                currentShot = null;
            }
            IEnumerator CO_Shot()
            {
                void Forward()
                {
                    if (Time.time < forwardLockTimeEnd)
                        return;
                    a.BuildInput(Owner, out var forward);
                    forward.SetDirection(Vector2.up);
                    for (var i = 0; i < 3; i++)
                    {
                        float speed = 36f + i.AsFloat(8f);
                        forward.SetOrigin(Owner.CurrentPosition + new Vector2(-0.2f, 0.45f));
                        a.Single(0f, speed).Spawn(forward, unfocusShot, out Projectile p1);
                        forward.SetOrigin(Owner.CurrentPosition + new Vector2(0.2f, 0.45f));
                        a.Single(0f, speed).Spawn(forward, unfocusShot, out Projectile p2);
                    }
                    forwardLockTimeEnd = Time.time + 0.049f;
                    forward.PlaySound(unfocusShotSound);
                }
                shotcap.RemoveWhere(x => x == null || !x.IsActive || !x.isOnScreen);
                int remainingShots = 5;
                Forward();
                if (shotcap.Count < 25)
                    while (remainingShots > 0 && shotcap.Count < 50)
                    {
                        Forward();
                        shotcap.RemoveWhere(x => x == null || !x.IsActive || !x.isOnScreen);
                        remainingShots--;
                        if (!a.BuildInput(Owner, out var input))
                        {
                            Debug.LogError("Failed to build shot input");
                            continue;
                        }

                        foreach (var shot in Options(cachedFocusLerp))
                        {
                            input.SetOrigin(shot + Owner.CurrentPosition);
                            for (int i = 0; i < 3; i++)
                            {
                                float xMod = (shot.x.Absolute() - 0.5f);
                                xMod = xMod.MapTo01(0f, 1f, true);
                                float angle = xMod.MapFrom01(0f, 30f) * -shot.x.SignInt();
                                if (a.Single(angle, 25f + i.AsFloat(5f)).Spawn(input, optionShot, out Projectile p))
                                {
                                    shotcap.Add(p);
                                    p.AddForward(remainingShots.AsFloat(0.05f));
                                }
                            }
                        }
                        yield return 0.016f.WaitForSeconds();
                    }
                yield return 0.05f.WaitForSeconds();
                currentShot = null;
            }
            if (currentShot == null && Shooting)
            {
                TryStartCO(CanSuperShot ? CO_Supershot() : CO_Shot());
            }
        }
        private void TryStartCO(IEnumerator c)
        {
            if (currentShot != null)
            {
                return;
            }
            currentShot = StartCoroutine(c);
        }
        private void OnDisable()
        {
            if (currentShot != null)
                StopCoroutine(currentShot);
            currentShot = null;
        }
        private void OnEnable()
        {
            cachedFocusLerp = 0f;
        }
    }
}
