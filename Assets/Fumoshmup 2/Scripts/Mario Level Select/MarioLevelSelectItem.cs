using System.Collections.Generic;
using UnityEngine;

namespace FumoShmup2
{
    public class MarioLevelSelectItem : MonoBehaviour
    {
        [SerializeField] int levelIndex;
        [field: SerializeField] public List<ShmupStage> AttachedStages { get; private set; } = new();
        public TextAsset StageInfo;
        static List<MarioLevelSelectItem> selections = new();
        Dictionary<Vector2Int, MarioLevelSelectItem> neighbours = new();
        static int currentSelection;
        static bool mapBuilt;

        public delegate void LevelSelectionEvent(MarioLevelSelectItem selected);
        public static event LevelSelectionEvent WhenLevelSelected;

        private void OnEnable()
        {
            selections.Add(this);
            mapBuilt = false;
        }
        private static void SetCurrentSelection(MarioLevelSelectItem item)
        {
            currentSelection = item.levelIndex;
            WhenLevelSelected?.Invoke(item);
        }
        private void OnDisable()
        {
            selections.Remove(this);
            mapBuilt = false;
        }
        private void LateUpdate()
        {
            if (!mapBuilt)
            {
                BuildAllMaps();
                mapBuilt = true;
            }
        }
        private static void BuildAllMaps()
        {
            foreach (var item in selections)
                item.BuildMap();
        }
        private void BuildMap()
        {
            neighbours.Clear();
            neighbours[Vector2Int.up] = FindNearestDirection(this, Vector3.forward);
            neighbours[Vector2Int.down] = FindNearestDirection(this, -Vector3.forward);
            neighbours[Vector2Int.left] = FindNearestDirection(this, Vector3.left);
            neighbours[Vector2Int.right] = FindNearestDirection(this, Vector3.right);
        }
        private static MarioLevelSelectItem FindNearestDirection(MarioLevelSelectItem origin, Vector3 direction)
        {
            MarioLevelSelectItem best = null;
            float bestDistance = float.MaxValue;

            foreach (var candidate in selections)
            {
                if (candidate == origin)
                    continue;

                Vector3 offset = candidate.transform.position - origin.transform.position;
                float forward = Vector3.Dot(offset.normalized, direction);

                if (forward < 0.5f)
                    continue;

                float distance = offset.sqrMagnitude;

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                }
            }

            return best;
        }
        public static bool LoadStored(out MarioLevelSelectItem result)
        {
            return TryGetFromIndex(currentSelection, out result);
        }
        public static bool TryGetDirection(Vector2 input, out MarioLevelSelectItem result)
        {
            result = null;
            if (!mapBuilt)
                return false;

            if (!TryGetFromIndex(currentSelection, out var current))
                return false;

            Vector2Int dir;
            if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
                dir = input.x > 0 ? Vector2Int.right : Vector2Int.left;
            else
                dir = input.y > 0 ? Vector2Int.up : Vector2Int.down;

            if (!current.neighbours.TryGetValue(dir, out result))
                return false;

            if (result != null)
            {
                SetCurrentSelection(result);
            }
            return result != null;
        }
        public static bool TryGetFromIndex(int index, out MarioLevelSelectItem result)
        {
            foreach (var item in selections)
            {
                if (item.levelIndex == index)
                {
                    result = item;
                    return result != null;
                }
            }
            result = null;
            return false;
        }
    }
}