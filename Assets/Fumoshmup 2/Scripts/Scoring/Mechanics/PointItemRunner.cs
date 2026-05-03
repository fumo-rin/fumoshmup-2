namespace FumoShmup
{
    using FumoShmup2;
    using rinCore;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;

    public class PointItemRunner : MonoBehaviour
    {
        internal static readonly List<PointItem> items = new();
        [SerializeField] ParticleSystem itemParticle;
        internal static readonly List<Vector2> pointItemPositions = new(50000);
        internal static List<Vector2> PointItems => GetPointItemPositions();
        private static List<Vector2> GetPointItemPositions()
        {
            pointItemPositions.Clear();
            for (int i = 0; i < items.Count; i++)
            {
                pointItemPositions.Add(items[i].Position);
            }
            return pointItemPositions;
        }
        [rinCore.Initialize(0)]
        internal static void ResetPointItems() => items.Clear();
        private void Awake() => items.Clear();
        float focusReleaseTime = 0f;
        bool focusWasHeld;
        private void Update()
        {
            void WhenPickup(Vector2 position)
            {
                PlayerScoring.AddPickupScore(1234d);
            }
            bool playerAlive = ShmupPlayer.PlayerAs(out ShmupPlayer player) && player.IsAlive;
            float maxFocusTime = 1.15f;
            float minDelay = 0.35f;
            for (int i = items.Count - 1; i >= 0; i--)
            {
                var item = items[i];
                if (item == null || !item.UpdatePosition(Time.deltaTime))
                {
                    items.RemoveAt(i);
                    continue;
                }

                float pickupRadius = 2.5f;
                ShmupWorldspace.MapToWorldspaceUnclamped(0.5f, 0.7f, out Vector2 topOfScreen);
                if (playerAlive && topOfScreen.y <= player.CurrentPosition.y)
                {
                    WhenPickup(item.Position);
                    items.RemoveAt(i);
                    continue;
                }
                if (!ShmupInput.Focus)
                {
                    if (focusWasHeld) focusReleaseTime = Time.time;
                    float elapsed = (Time.time - focusReleaseTime) - minDelay;
                    float focusLerp = elapsed.Clamp(0f, maxFocusTime - minDelay) / (maxFocusTime - minDelay);
                    focusLerp = focusLerp.Clamp(0f, 1f);
                    pickupRadius = focusLerp.MapFrom01(2.5f, 20f);
                }
                if (playerAlive && item.Position.SquareDistanceToLessThan(player.CurrentPosition, pickupRadius))
                {
                    WhenPickup(item.Position);
                    items.RemoveAt(i);
                    continue;
                }
            }
            itemParticle.RenderAnimatedPoints(PointItems, Time.time);
            focusWasHeld = ShmupInput.Focus;
        }
        public static void Create(Vector2 worldPosition)
        {
            items.Add(new(worldPosition));
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
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; private set; }
        public PointItem(Vector2 position)
        {
            this.Position = position;
            this.Velocity = new Vector2(10f.RandomPositiveNegativeRange(), 3.5f.Spread(45f));
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
