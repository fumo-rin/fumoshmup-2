using rinCore;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FumoShmup2
{
    public class PointItemRunner : MonoBehaviour
    {
        public static void SpawnPointItem(Vector2 position)
        {
            bool scoringActivated = false;
            double scoreValue = 1000d;
            if (ShmupSession.CurrentAs(out ShmupSession s) && ShmupPlayer.PlayerAs(out ShmupPlayer p) && p.IsAlive)
            {
                scoringActivated = s.GetFloat(ShmupSession.keys.CashoutActivation060) <= 1f;
                if (scoringActivated)
                {
                    scoreValue += s.GetFloat(ShmupSession.keys.HitCounter) * 0.25f;
                }
                else
                {
                    if (ShmupInput.FocusReleasedLongerThan(0.15f))
                    {
                        s.ChangeFloat(ShmupSession.keys.CashoutActivation060, 1, 0, 60);
                    }
                }
            }
            PointItemRunner.Create(position, scoringActivated, scoreValue);
        }
        internal static readonly List<PointItem> items = new();
        public static int ItemCount => items == null ? 0 : items.Count;
        [SerializeField] ParticleSystem itemParticle, cashInRenderer;
        internal static readonly List<Vector2> pointItemPositions = new(50000);
        internal static readonly List<Vector2> cashInItemPositions = new(50000);
        internal static List<Vector2> PointItems => GetPointItemPositions();
        internal static List<Vector2> ChargedPointItems => GetCashInItemPositions();
        private static List<Vector2> GetPointItemPositions()
        {
            pointItemPositions.Clear();
            for (int i = 0; i < items.Count; i++)
            {
                pointItemPositions.Add(items[i].Position);
            }
            return pointItemPositions;
        }
        private static List<Vector2> GetCashInItemPositions()
        {
            cashInItemPositions.Clear();
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (!item.scoringCashIn)
                    continue;
                cashInItemPositions.Add(item.Position);
            }
            return cashInItemPositions;
        }
        [rinCore.Initialize(0)]
        internal static void ResetPointItems() => items.Clear();
        private void Awake() => items.Clear();
        float focusReleaseTime = 0f;
        bool focusWasHeld;
        void WhenPickup(PointItem item)
        {
            GameSession.TryAddScoreRaw(item.scoreValue, "Score Pickup");
            if (ShmupSession.CurrentAs(out ShmupSession s))
            {
                s.ChangeFloat(ShmupSession.keys.HitCounter, item.scoringCashIn ? -50f : 10f, 0f, 99999f);
            }
            ProjectileRenderer.SpawnCosmeticLootParticle(item.Position);
        }
        private void Update()
        {
            bool playerAlive = ShmupPlayer.PlayerAs(out ShmupPlayer player) && player.IsAlive;
            float maxFocusTime = 1.15f;
            float minDelay = 0.35f;
            float basePickupRadius = 1.5f;
            if (!ShmupInput.Focus)
            {
                if (focusWasHeld)
                    focusReleaseTime = Time.time;

                float elapsed = (Time.time - focusReleaseTime) - minDelay;
                float focusLerp = elapsed.Clamp(0f, maxFocusTime - minDelay) / (maxFocusTime - minDelay);
                focusLerp = focusLerp.Clamp(0f, 1f);
                basePickupRadius = focusLerp.MapFrom01(2.5f, 20f);
            }

            for (int i = items.Count - 1; i >= 0; i--)
            {
                var item = items[i];
                if (item == null || !item.UpdatePosition(Time.deltaTime))
                {
                    items.RemoveAt(i);
                    continue;
                }
                if (Time.time < item.minimumPickupTime)
                {
                    continue;
                }
                float finalPickupRadius = basePickupRadius;
                if (ShmupInput.FocusReleasedLongerThan(minDelay))
                {
                    float itemAge = Time.time - (item.minimumPickupTime - 0.5f);
                    float itemPickupLerp = itemAge.Clamp(0f, 1.5f) / 1.5f;

                    itemPickupLerp = itemPickupLerp.Clamp(0f, 1f);
                    itemPickupLerp *= itemPickupLerp;

                    float additionalRadius = itemPickupLerp.MapFrom01(0f, 6f);
                    finalPickupRadius += additionalRadius;
                }
                if (playerAlive && item.Position.SquareDistanceToLessThan(player.CurrentPosition, finalPickupRadius))
                {
                    WhenPickup(item);
                    items.RemoveAt(i);
                    continue;
                }
            }
            itemParticle.RenderAnimatedPoints(PointItems, 0.3f, false);
            cashInRenderer.RenderAnimatedPoints(ChargedPointItems, 0.3f, false);
            focusWasHeld = ShmupInput.Focus;
        }
        private static void Create(Vector2 worldPosition, bool scoringCashIn, double scoreValue)
        {
            items.Add(new(worldPosition)
            {
                scoringCashIn = scoringCashIn,
                scoreValue = scoreValue
            });
        }
        public static void RestartPointItems()
        {
            items.Clear();
            pointItemPositions.Clear();
        }
    }
    public class PointItem
    {
        private static readonly float GravityStrength = -5.5f;
        private static readonly float MaxFallSpeed = -5.5f;
        public bool scoringCashIn;
        public double scoreValue;
        public float minimumPickupTime;
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; private set; }
        public PointItem(Vector2 position)
        {
            this.Position = position;
            this.Velocity = new Vector2(10f.RandomPositiveNegativeRange(), 2.5f.Spread(45f));
            this.Velocity = new(0.3f * Velocity.x, Velocity.y);
            this.scoringCashIn = false;
            this.scoreValue = 1000d;
            this.minimumPickupTime = Time.time + 0.35f.Spread(20f);
        }
        public void ApplyGravity(float deltaTime)
        {
            float x = Velocity.x.LerpUnclamped(0f, deltaTime * 6f);
            Velocity += new Vector2(0f, GravityStrength) * deltaTime;
            Velocity = new(x, Velocity.y);
            if (Velocity.y < MaxFallSpeed)
                Velocity = new Vector2(Velocity.x, MaxFallSpeed);
        }
        public bool UpdatePosition(float deltaTime)
        {
            ApplyGravity(deltaTime);
            Position += Velocity * deltaTime;
            if (Position.y <= ShmupWorldspace.BiggerWorldSpace.min.y)
                return false;
            return true;
        }
    }
}