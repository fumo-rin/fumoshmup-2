using rinCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FumoShmup2
{
    #region SuperShot
    public partial class PlayerKetsuiShot
    {
        [SerializeField] LineRenderer laser;
        [SerializeField] ACWrapper laserSound, lockOnSound;
        float laserLerp;
        readonly List<EnemyUnit> LockOn = new(4);
        IEnumerator CO_Supershot()
        {
            if (laserLerp > 0f || ShmupInput.ShootReleasedLongerThan(0.65f))
            {
                laserLerp = 0f;
            }
            float nextProjectileShotTime = Time.time + 0.15f;
            float damageTime = Time.time;
            a.BuildInput(Owner, out Projectile.InputSettings input);
            float nextLockonTime = Time.time + 0.45f;
            while (ShmupInput.Focus && ShmupInput.Shoot)
            {
                nextProjectileShotTime -= Time.deltaTime;
                input.SetOrigin(Owner.CurrentPosition);
                void Shoot()
                {
                    LockOn.RemoveAll((EnemyUnit e) => !e.IsOnScreenAndAlive);
                    nextProjectileShotTime = Time.time + 0.06f;
                    float delta = Time.time - damageTime;
                    damageTime = Time.time;

                    RaycastHit2D[] laserhit = Physics2D.BoxCastAll(input.Origin, new(2f, 1f), 0f, Vector2.up, laserLerp);

                    laserSound.Play(Owner.CurrentPosition);
                    foreach (var item in laserhit)
                    {
                        if (item.collider is Collider2D col && col.GetComponent<IHit>() is IHit hit)
                        {
                            if (Owner == (object)hit)
                                continue;

                            hit.SendHit(new IHit.HitPacket(item.point, new Projectile.ProjectileDamage(Owner, 225f * delta, 1f)), out float damageDealt);
                            if (damageDealt > 0f)
                            {
                                ProjectileRenderer.HitParticle(item.point, item.normal, new()
                                {

                                });
                            }
                        }
                    }

                    Vector2[] options = Options(cachedFocusLerp).ToArray();
                    for (int i = 0; i < options.Length; i++)
                    {
                        a.BuildInput(Owner, out Projectile.InputSettings ketsui);
                        Vector2 option = options[i];
                        if (Time.time > nextLockonTime && ketsui.OptionalTarget is EnemyUnit autoAim)
                        {
                            if (LockOn.Count < 4)
                            {
                                LockOn.Add(autoAim);
                                nextLockonTime = Time.time + 0.15f;
                                lockOnSound.Play(option + Owner.CurrentPosition);
                            }
                        }

                        if (i >= LockOn.Count)
                            continue;

                        ShmupUnit target = LockOn[i];
                        ketsui.SetOptionalTarget(target);
                        if (ketsui.OptionalTarget == null)
                            continue;

                        ketsui.ReAimWithOptionalTarget(option + Owner.CurrentPosition);
                        ketsui.SetMods(new ProjectileModChase(new(3f, 0f), ketsui.OptionalTarget, 270f));
                        if (a.Single(6f.RandomPositiveNegativeRange(), 80f).Spawn(ketsui, superShot, out Projectile p))
                        {
                            p.SetDamage(new(Owner, 1.75f, 1f));
                        }
                    }
                }
                if (Time.time >= nextProjectileShotTime)
                {
                    Shoot();
                }
                Vector3[] pos = new Vector3[2] { Owner.transform.position, Owner.transform.position + Vector3.up * laserLerp };
                laser.SetPositions(pos);
                laserLerp = laserLerp.LerpTowards(20f, 4f * Time.deltaTime);
                laserLerp = laserLerp.Clamp(2f, 20f);
                yield return null;
                laser.enabled = true;
            }
            currentShot = null;
        }
    }
    #endregion
    public partial class PlayerKetsuiShot : MonoBehaviour
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
            Vector2[] a = new Vector2[4] { new(-1.35f, -0.55f), new(1.35f, -0.55f), new(0.85f, -0.35f), new(-0.85f, -0.35f), };
            Vector2[] b = new Vector2[4] { new(2.15f, -0.65f), new(-2.15f, -0.65f), new(-1.35f, -0.75f), new(1.35f, -0.75f) };
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
            if (ShmupInput.FocusReleasedLongerThan(0.25f) || ShmupInput.ShootReleasedLongerThan(0.65f))
            {
                LockOn.Clear();
            }
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
            if (ShmupSession.CurrentAs(out ShmupSession sess) && sess.GameLogicStalled)
            {
                if (currentShot != null)
                    StopCoroutine(currentShot);
                currentShot = null;
                laser.enabled = false;
                laserLerp = 0f;
                return;
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
                        if (a.Single(0f, speed).Spawn(forward, unfocusShot, out Projectile p1))
                        {
                            p1.SetDamage(new(Owner, 1.95f, 1f));
                        }
                        forward.SetOrigin(Owner.CurrentPosition + new Vector2(0.2f, 0.45f));
                        if (a.Single(0f, speed).Spawn(forward, unfocusShot, out Projectile p2))
                        {
                            p2.SetDamage(new(Owner, 1.95f, 1f));
                        }
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
                                    p.SetDamage(new(Owner, 2.75f, 1f));
                                }
                            }
                        }
                        yield return 0.016f.WaitForSeconds();
                    }
                yield return 0.05f.WaitForSeconds();
                laserLerp -= 1f;
                currentShot = null;
            }
            if (currentShot == null && Shooting)
            {
                TryStartCO(CanSuperShot ? CO_Supershot() : CO_Shot());
            }
            else if (currentShot == null || !ShmupInput.Focus)
            {
                laser.enabled = false;
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
