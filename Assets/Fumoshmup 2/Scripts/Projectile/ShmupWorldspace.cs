using UnityEngine;
using rinCore;

namespace FumoShmup2
{
    public struct Vector2Shmup
    {
        public float x, y;
        public Vector2Shmup(float x, float y)
        {
            this.x = x;
            this.y = y;
        }
        public Vector2 Vector2Now
        {
            get
            {
                return ShmupWorldspace.MapToWorldspaceUnclamped(x, y, out _);
            }
        }
        public static implicit operator Vector2(Vector2Shmup v)
        {
            return v.Vector2Now;
        }
    }
    [DefaultExecutionOrder(-5)]
    [RequireComponent(typeof(Camera))]
    public class ShmupWorldspace : MonoBehaviour
    {
        private static ShmupWorldspace instance;
        public static Rect WorldSpace => GetWorldspace(ref space);
        public static Rect BiggerWorldSpace => GetWorldspace(ref bigSpace);
        public static Rect WideWorldspace => GetWorldspace(ref wideSpace);
        static Rect space;
        static Rect wideSpace;
        static Rect bigSpace;
        static Rect smallSpace;
        static Vector2 storedPosition;
        static float topOfScreenY = 0.75f;
        public static bool IsTopOfScreen(FumoUnit unit)
        {
            if (unit != null && unit.IsAlive)
            {
                ShmupWorldspace.MapWorldspaceToNormalized(unit.CurrentPosition, out Vector2 n, true);
                return n.y > topOfScreenY;
            }
            return false;

        }
        private static Camera _cachedCamera;
        private static Camera MainCam
        {
            get
            {
                if (_cachedCamera == null)
                    _cachedCamera = Camera.main;
                return _cachedCamera;
            }
            set
            {
                _cachedCamera = value;
            }
        }
        [SerializeField] Camera assignedCameraOverride;
        [Initialize(-100)]
        private static void Reinitialize()
        {
            space = default;
            wideSpace = default;
            bigSpace = default;
            _cachedCamera = null;
            storedPosition = new(-999f, 9999f);
            instance = null;
        }
        private static Rect GetWorldspace(ref Rect space)
        {
            RecalculateSpace();
            return space;
        }
        static bool Recalculate => RunRecalculate();
        private static bool RunRecalculate()
        {
            if (MainCam == null)
            {
                return true;
            }
            if (storedPosition == (Vector2)MainCam.transform.position)
            {
                return false;
            }
            storedPosition = MainCam.transform.position;
            return true;
        }
        private static void SetSpace(ref Rect space, Vector2 center, Vector2 size)
        {
            Vector2 actualcenter = center - size * 0.5f;
            space = new Rect(actualcenter, size);
            RecalculateSpace();
        }
        public static bool IsWithinSpace(Vector2 worldPosition)
        {
            RecalculateSpace();
            return WorldSpace.Contains(worldPosition);
        }
        public static float GetMappedX(float xSpace)
        {
            return space.xMin.LerpUnclamped(space.xMax, xSpace);
        }
        public static Vector2 MapToWorldspaceUnclamped(float xSpace, float ySpace, out Vector2 worldspace)
        {
            worldspace = Vector2.zero;
            if (space == null)
            {
                worldspace = MainCam == null ? Vector2.zero : MainCam.transform.position;
                return worldspace;
            }
            RecalculateSpace();
            worldspace = new Vector2(
                space.xMin.LerpUnclamped(space.xMax, xSpace),
                space.yMin.LerpUnclamped(space.yMax, ySpace)
            );
            return worldspace;
        }
        public static void MapWorldspaceToNormalized(Vector2 worldPosition, out Vector2 normalized, bool clamp = true)
        {
            RecalculateSpace();
            normalized.x = (worldPosition.x - space.x) / space.width;
            normalized.y = (worldPosition.y - space.y) / space.height;
            if (clamp)
            {
                normalized.x = Mathf.Clamp01(normalized.x);
                normalized.y = Mathf.Clamp01(normalized.y);
            }
        }
        public static void ClampToNormalizedSpace(Vector2 worldPosition, Rect normalizedClamp, out Vector2 clamped, Rect space)
        {
            RecalculateSpace();
            clamped = new Vector2(
                Mathf.Clamp(worldPosition.x, space.x + normalizedClamp.xMin * space.width, space.x + normalizedClamp.xMax * space.width),
                Mathf.Clamp(worldPosition.y, space.y + normalizedClamp.yMin * space.height, space.y + normalizedClamp.yMax * space.height)
            );
        }
        private void Awake()
        {
            instance = this;
            RecalculateSpace();
            if (assignedCameraOverride != null) MainCam = assignedCameraOverride;
        }
        private static void RecalculateSpace()
        {
            if (!Recalculate)
            {
                return;
            }
            if (MainCam == null)
            {
                return;
            }
            float height = MainCam.orthographicSize * 2f;
            float w = MainCam.aspect * height;
            SetSpace(ref space, (Vector2)MainCam.transform.position, new(w, height));
            SetSpace(ref wideSpace, (Vector2)MainCam.transform.position, new(w * 1.5f, height));
            SetSpace(ref bigSpace, (Vector2)MainCam.transform.position, new(w * 1.15f, height * 1.15f));
        }
    }
}
