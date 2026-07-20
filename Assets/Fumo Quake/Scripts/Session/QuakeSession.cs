using rinCore;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FumoQuake
{
    [System.Serializable]
    public class QuakeSession : GameSession
    {
        [SerializeField] public List<ScenePairSO> LevelSequence = new();
        [SerializeField] public bool submitScore;
        public int currentLevelIndex = 0;

        Dictionary<QuakeKeyItems, bool> levelItems = new();
        Dictionary<QuakeKeyItems, float> ItemWarnTime = new();

        #region Session Lifecycle
        protected override void WhenStartSession()
        {
            QuakeController.StoredHealth = null;
            PlayerWeaponsController.ResetWeaponState();

            currentLevelIndex = 0;

            // Clean up nulls & isolate C# list on the heap
            LevelSequence = LevelSequence != null
                ? LevelSequence.Where(s => s != null).ToList()
                : new List<ScenePairSO>();

            Debug.Log($"[QuakeSession] Starting Session with {LevelSequence.Count} levels:");
            for (int i = 0; i < LevelSequence.Count; i++)
            {
                var item = LevelSequence[i];
                string name = (item != null) ? item.name : "DESTROYED_NULL";
                Debug.Log($"  [{i}] - {name}");
            }

            NextLevelOrMenu(LevelSequence);
        }

        protected override void WhenEndSession()
        {
            levelItems?.Clear();
            ItemWarnTime?.Clear();
            currentLevelIndex = 0;
        }

        public static bool NextLevelOrMenu(List<ScenePairSO> sequence = null)
        {
            Debug.Log("[QuakeSession] Requesting Next Level...");

            if (!CurrentAs(out QuakeSession ses))
            {
                Debug.LogError("[QuakeSession] No active GameSession found! Returning to Main Menu.");
                SceneLoader.MainMenu();
                return false;
            }

            // Prefer explicitly passed list over instance list to avoid scene-unload stripping
            List<ScenePairSO> activeList = (sequence != null && sequence.Count > 0)
                ? sequence
                : ses.LevelSequence;

            // Sanity check: filter out any destroyed Unity C++ objects ("fake nulls")
            if (activeList != null)
            {
                activeList = activeList.Where(s => s != null).ToList();
            }

            if (activeList == null || activeList.Count == 0)
            {
                Debug.LogError("[QuakeSession] LevelSequence is empty or destroyed! Returning to Main Menu.");
                SceneLoader.MainMenu();
                return false;
            }

            // Check if we have remaining levels
            if (ses.currentLevelIndex < activeList.Count)
            {
                ScenePairSO nextPair = activeList[ses.currentLevelIndex];

                // Native Unity object check
                if (nextPair == null)
                {
                    Debug.LogError($"[QuakeSession] Level at index {ses.currentLevelIndex} is NULL/Destroyed! Skipping...");
                    ses.currentLevelIndex++;
                    return NextLevelOrMenu(activeList);
                }

                int loadedIndex = ses.currentLevelIndex;
                int nextIndex = ses.currentLevelIndex + 1;

                Debug.Log($"[QuakeSession] Loading Level [{loadedIndex + 1}/{activeList.Count}]: {nextPair.name}");

                // CRITICAL FIX: Lock 'activeList' into closure memory. 
                // Unloading the previous scene cannot destroy this variable reference.
                List<ScenePairSO> persistentList = activeList;

                SceneLoader.LoadScenePair(nextPair, new SceneLoader.SceneLoadSettings()
                {
                    Payload = () =>
                    {
                        if (CurrentAs(out QuakeSession activeSes))
                        {
                            // Restore list & index into the active session instance in the new scene
                            activeSes.LevelSequence = persistentList;
                            activeSes.currentLevelIndex = nextIndex;
                            activeSes.levelItems = new();
                            activeSes.ItemWarnTime = new();

                            Debug.Log($"[QuakeSession Payload] Restored level list ({activeSes.LevelSequence.Count} items). Next Index: {activeSes.currentLevelIndex}");
                        }
                    }
                });

                return true;
            }
            else
            {
                Debug.Log("[QuakeSession] Sequence complete! Returning to Main Menu...");
                EndSession(new EndSessionSettings()
                {
                    SubmitScore = ses.submitScore
                });

                SceneLoader.MainMenu();
                return true;
            }
        }
        #endregion

        #region Item Management
        public static bool HasItem(QuakeKeyItems item)
        {
            if (!CurrentAs(out QuakeSession ses) || ses.levelItems == null) return false;

            bool hasItem = ses.levelItems.TryGetValue(item, out bool v) && v;
            bool warned = !ses.ItemWarnTime.TryGetValue(item, out float warnTime) || Time.time > warnTime + 3f;

            if (!hasItem && !warned)
            {
                QuakeTextInfoUI.AddText("Needs that " + item.ToSpacedString().Humanize());
                ses.ItemWarnTime[item] = Time.time;
            }
            return hasItem;
        }

        public static bool AwardItem(QuakeKeyItems item)
        {
            if (!CurrentAs(out QuakeSession ses) || ses.levelItems == null) return false;

            bool success = !ses.levelItems.TryGetValue(item, out _) && ses.levelItems.TryAdd(item, true);
            QuakeTextInfoUI.AddText("You Gots that " + item.ToSpacedString().Humanize());

            return success;
        }
        #endregion
    }
}