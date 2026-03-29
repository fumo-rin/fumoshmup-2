using UnityEngine;
using rinCore;
using UnityEngine.Search;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine.Serialization;
using System.Collections.Generic;

namespace FumoShmup2
{

    #region Collider Shapes

    [System.Serializable]
    public abstract class ProjectileColliderShape
    {
        public abstract float HalfLength { get; }
        public abstract string ShapeName { get; }
        public abstract bool OverlapsPoint(Vector2 point, float radius, Vector2 position, float angleDeg);
        public abstract void DrawGizmo(Vector2 position, float angleDeg, Color color);
        public abstract IEnumerable<int> GetOccupiedPixels(Vector2 position, float angleDeg);
        #region Helper Rect Finder
        protected IEnumerable<int> GetPixelsInRotatedRect(Vector2 position, float angleDeg, Vector2 halfSize)
        {
            Rect space = ShmupWorldspace.WorldSpace;
            int width = ProjectileRunner.COLLISION_BITMAP_SIZE_XY.Item1;
            int height = ProjectileRunner.COLLISION_BITMAP_SIZE_XY.Item2;

            Quaternion rot = Quaternion.Euler(0, 0, angleDeg);
            Vector2 c1 = (Vector2)(rot * new Vector2(-halfSize.x, -halfSize.y)) + position;
            Vector2 c2 = (Vector2)(rot * new Vector2(halfSize.x, -halfSize.y)) + position;
            Vector2 c3 = (Vector2)(rot * new Vector2(halfSize.x, halfSize.y)) + position;
            Vector2 c4 = (Vector2)(rot * new Vector2(-halfSize.x, halfSize.y)) + position;

            float minX = Mathf.Min(c1.x, Mathf.Min(c2.x, Mathf.Min(c3.x, c4.x)));
            float maxX = Mathf.Max(c1.x, Mathf.Max(c2.x, Mathf.Max(c3.x, c4.x)));
            float minY = Mathf.Min(c1.y, Mathf.Min(c2.y, Mathf.Min(c3.y, c4.y)));
            float maxY = Mathf.Max(c1.y, Mathf.Max(c2.y, Mathf.Max(c3.y, c4.y)));

            int xStart = Mathf.Clamp(Mathf.FloorToInt(((minX - space.xMin) / space.width) * width), 0, width - 1);
            int xEnd = Mathf.Clamp(Mathf.FloorToInt(((maxX - space.xMin) / space.width) * width), 0, width - 1);
            int yStart = Mathf.Clamp(Mathf.FloorToInt(((minY - space.yMin) / space.height) * height), 0, height - 1);
            int yEnd = Mathf.Clamp(Mathf.FloorToInt(((maxY - space.yMin) / space.height) * height), 0, height - 1);

            float cellW = space.width / width;
            float cellH = space.height / height;

            for (int y = yStart; y <= yEnd; y++)
            {
                for (int x = xStart; x <= xEnd; x++)
                {
                    Vector2 cellCenter = new Vector2(
                        space.xMin + (x * cellW) + (cellW * 0.5f),
                        space.yMin + (y * cellH) + (cellH * 0.5f)
                    );

                    if (OverlapsPoint(cellCenter, 0.1f, position, angleDeg))
                    {
                        yield return y * width + x;
                    }
                }
            }
        }
        #endregion
    }

    #region Drawer
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(ProjectileColliderShape), true)]
    public class ProjectileColliderShapeDrawer : PropertyDrawer
    {
        private static Type[] _shapeTypes;
        private static string[] _shapeNames;

        static ProjectileColliderShapeDrawer()
        {
            _shapeTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsSubclassOf(typeof(ProjectileColliderShape)) && !t.IsAbstract)
                .ToArray();

            _shapeNames = _shapeTypes.Select(t => t.Name.Replace("Shape", "")).ToArray();
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            Rect labelRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(labelRect, label);

            var currentShape = property.managedReferenceValue;
            int currentIndex = 0;

            if (currentShape != null)
            {
                var currentType = currentShape.GetType();
                currentIndex = Array.IndexOf(_shapeTypes, currentType);
                if (currentIndex < 0) currentIndex = 0;
            }

            Rect popupRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + 2f, position.width, EditorGUIUtility.singleLineHeight);
            int newIndex = EditorGUI.Popup(popupRect, "Shape Type", currentIndex, _shapeNames);

            if (newIndex != currentIndex || currentShape == null)
            {
                property.managedReferenceValue = Activator.CreateInstance(_shapeTypes[newIndex]);
                property.serializedObject.ApplyModifiedProperties();
                property.serializedObject.Update();
                currentShape = property.managedReferenceValue;
            }

            if (currentShape != null)
            {
                SerializedProperty iterator = property.Copy();
                SerializedProperty end = iterator.GetEndProperty();
                iterator.NextVisible(true);
                EditorGUI.indentLevel++;

                float y = popupRect.yMax + 4f;
                while (!SerializedProperty.EqualContents(iterator, end))
                {
                    float h = EditorGUI.GetPropertyHeight(iterator, true);
                    Rect r = new Rect(position.x, y, position.width, h);
                    EditorGUI.PropertyField(r, iterator, true);
                    y += h + 2f;

                    if (!iterator.NextVisible(false))
                        break;
                }
                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight * 2 + 4f;
            if (property.managedReferenceValue == null)
                return height;

            SerializedProperty iterator = property.Copy();
            SerializedProperty end = iterator.GetEndProperty();
            iterator.NextVisible(true);

            while (!SerializedProperty.EqualContents(iterator, end))
            {
                height += EditorGUI.GetPropertyHeight(iterator, true) + 2f;
                if (!iterator.NextVisible(false))
                    break;
            }

            return height;
        }
    }
#endif
    #endregion

    #region Circle
    [System.Serializable]
    public class CircleShape : ProjectileColliderShape
    {
        public float Radius = 0.5f;
        public override string ShapeName => "Circle";
        public override float HalfLength => Radius;

        public override bool OverlapsPoint(Vector2 point, float radius, Vector2 position, float angleDeg)
        {
            float distSq = (point - position).sqrMagnitude;
            float combined = Radius + radius;
            return distSq <= combined * combined;
        }
        public override void DrawGizmo(Vector2 position, float angleDeg, Color color)
        {
            Gizmos.color = color;
            Gizmos.DrawWireSphere(position, Radius);
        }
        public override IEnumerable<int> GetOccupiedPixels(Vector2 position, float angleDeg)
        {
            return GetPixelsInRotatedRect(position, 0, new Vector2(Radius, Radius));
        }
    }
    #endregion
    #region Box
    [System.Serializable]
    public class BoxShape : ProjectileColliderShape
    {
        public Vector2 Size = new Vector2(1f, 1f);
        public override string ShapeName => "Box";
        public override float HalfLength => Size.x * 0.5f;
        public override bool OverlapsPoint(Vector2 point, float radius, Vector2 position, float angleDeg)
        {
            Vector2 local = Quaternion.Euler(0, 0, -angleDeg) * (point - position);
            Vector2 half = Size * 0.5f;

            float dx = Mathf.Max(0, Mathf.Abs(local.x) - half.x);
            float dy = Mathf.Max(0, Mathf.Abs(local.y) - half.y);

            if (dx == 0 && dy == 0)
                return true;
            return dx * dx + dy * dy <= radius * radius;
        }
        public override void DrawGizmo(Vector2 position, float angleDeg, Color color)
        {
            Gizmos.color = color;
            var rot = Quaternion.Euler(0, 0, angleDeg);
            Vector3 half = new Vector3(Size.x, Size.y) * 0.5f;

            Vector3[] corners = new Vector3[]
            {
            rot * new Vector3(-half.x, -half.y) + (Vector3)position,
            rot * new Vector3(-half.x,  half.y) + (Vector3)position,
            rot * new Vector3( half.x,  half.y) + (Vector3)position,
            rot * new Vector3( half.x, -half.y) + (Vector3)position,
            };

            for (int i = 0; i < 4; i++)
                Gizmos.DrawLine(corners[i], corners[(i + 1) % 4]);
        }
        public override IEnumerable<int> GetOccupiedPixels(Vector2 position, float angleDeg)
        {
            return GetPixelsInRotatedRect(position, angleDeg, Size * 0.5f);
        }
    }
    #endregion
    #region Capsule
    [System.Serializable]
    public class CapsuleShape : ProjectileColliderShape
    {
        public float Radius = 0.5f;
        public float Width = 1.0f;
        public override string ShapeName => "Capsule";
        public override float HalfLength => Width * 0.5f;
        public override bool OverlapsPoint(Vector2 point, float radius, Vector2 position, float angleDeg)
        {
            Vector2 local = Quaternion.Euler(0, 0, -angleDeg) * (point - position);

            float halfLine = Mathf.Max(0, Width * 0.5f - Radius);
            Vector2 linePoint = new Vector2(Mathf.Clamp(local.x, -halfLine, halfLine), 0f);

            float distSq = (local - linePoint).sqrMagnitude;
            float combined = Radius + radius;
            return distSq <= combined * combined;
        }
        public override IEnumerable<int> GetOccupiedPixels(Vector2 position, float angleDeg)
        {
            return GetPixelsInRotatedRect(position, angleDeg, new Vector2(Width * 0.5f, Radius));
        }

        public override void DrawGizmo(Vector2 position, float angleDeg, Color color)
        {
            Gizmos.color = color;
            var rot = Quaternion.Euler(0, 0, angleDeg);

            float halfLine = Mathf.Max(0, Width * 0.5f - Radius);

            Vector2 right = rot * Vector2.right * halfLine;
            Vector2 left = rot * Vector2.left * halfLine;

            Gizmos.DrawLine((Vector3)position + (Vector3)right, (Vector3)position + (Vector3)left);
            Gizmos.DrawWireSphere((Vector3)position + (Vector3)right, Radius);
            Gizmos.DrawWireSphere((Vector3)position + (Vector3)left, Radius);
        }
    }
    #endregion

    #endregion

    [CreateAssetMenu(menuName = "Rincore/Shmup/Projectile Define")]
    public class ProjectileDefineSO : ScriptableObject
    {
        [SerializeReference] public ProjectileColliderShape ColliderShape;
        public float HalfLength => ColliderShape.HalfLength;
        public float Size;
        [Range(0.1f, 10f), Tooltip("Bigger number = behind, Smaller number = front")] public float SizeSortingMultiplier = 1f;
        public Color32 Color;
        public float animationSpeed;
        [Range(0f, 100)] public float animationSpreadPercent = 10f;
        public float spin;
        [SearchContext("prefab:any")] public ParticleSystem particleSystemPrefab;
        [SortingLayer] public string SortingLayer = "Default";
        public bool useFlare;
        public Color32 FlareColor;
    }
}
