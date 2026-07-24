using NUnit.Framework;
using PlasticGui;
using rinCore;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FumoQuake
{
    #region Item Management
    public partial class QuakeSession
    {
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

            bool success = false;
            switch (item)
            {
                case QuakeKeyItems.SilverKeyOfDestiny:
                    success = !ses.levelItems.TryGetValue(item, out _) && ses.levelItems.TryAdd(item, true);
                    QuakeTextInfoUI.AddText("You Gots that " + item.ToSpacedString().Humanize());
                    break;
                case QuakeKeyItems.GoldenTicketKey:
                    success = !ses.levelItems.TryGetValue(item, out _) && ses.levelItems.TryAdd(item, true);
                    QuakeTextInfoUI.AddText("You Gots that " + item.ToSpacedString().Humanize());
                    break;
                case QuakeKeyItems.QuadDamage:
                    success = true;
                    QuakeTextInfoUI.AddText("Gots Chirumiru! 9x Damage BABYYYYYYYYYYYYYYYYYYYYYY");
                    quadDamageEndTime = Time.time + 15f;
                    break;
                default:
                    success = !ses.levelItems.TryGetValue(item, out _) && ses.levelItems.TryAdd(item, true);
                    QuakeTextInfoUI.AddText("You Gots that " + item.ToSpacedString().Humanize());
                    break;
            }
            return success;
        }
    }
    #endregion
    #region Session Lifecycle
    public partial class QuakeSession
    {
        protected override void WhenStartSession()
        {
            QuakeController.StoredHealth = null;
            PlayerWeaponsController.ResetWeaponState();
            currentLevelIndex = 0;
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

            NextLevelOrMenu();
        }

        protected override void WhenEndSession()
        {
            levelItems?.Clear();
            ItemWarnTime?.Clear();
            currentLevelIndex = 0;
        }
        public bool RestartLevel(SceneLoader.SceneLoadSettings? settings = null)
        {
            if (!CurrentAs(out QuakeSession ses))
            {
                Debug.LogError("[QuakeSession] No active GameSession found! Returning to Main Menu.");
                SceneLoader.MainMenu();
                return false;
            }

            if (!AllowShotgunStarting)
            {
                SceneLoader.MainMenu(new()
                {
                    Delay = 1.75f,
                });
                return false;
            }
            int levelIndex = Mathf.Max(0, ses.currentLevelIndex - 1);
            if (levelIndex >= ses.LevelSequence.Count)
            {
                Debug.LogError("[QuakeSession] Invalid level index! Returning to Main Menu.");
                SceneLoader.MainMenu();
                return false;
            }

            ScenePairSO nextPair = ses.LevelSequence[levelIndex];
            if (nextPair == null)
            {
                Debug.LogError("[QuakeSession] Bad level reference! Returning to Main Menu.");
                SceneLoader.MainMenu();
                return false;
            }

            var loadSettings = settings ?? new SceneLoader.SceneLoadSettings();
            var previousPayload = loadSettings.Payload;

            loadSettings.Payload = () =>
            {
                previousPayload?.Invoke();

                if (CurrentAs(out QuakeSession activeSes))
                {
                    activeSes.currentLevelIndex = levelIndex + 1;
                    activeSes.levelItems = new();
                    activeSes.ItemWarnTime = new();
                    quadDamageEndTime = 0f;
                }
            };

            Debug.Log($"[QuakeSession] Restarting Level [{levelIndex + 1}/{ses.LevelSequence.Count}]: {nextPair.name}");
            SceneLoader.LoadScenePair(nextPair, loadSettings);

            return true;
        }
        public bool NextLevelOrMenu()
        {
            Debug.Log("[QuakeSession] Requesting Next Level...");
            if (!CurrentAs(out QuakeSession ses))
            {
                Debug.LogError("[QuakeSession] No active GameSession found! Returning to Main Menu.");
                SceneLoader.MainMenu();
                return false;
            }

            if (ses.LevelSequence == null || ses.LevelSequence.Count == 0)
            {
                Debug.LogError("[QuakeSession] LevelSequence is empty or destroyed! Returning to Main Menu.");
                SceneLoader.MainMenu();
                return false;
            }

            if (ses.currentLevelIndex < ses.LevelSequence.Count)
            {
                ScenePairSO nextPair = ses.LevelSequence[ses.currentLevelIndex];
                if (nextPair == null)
                {
                    ses.currentLevelIndex++;
                    return NextLevelOrMenu();
                }
                int loadedIndex = ses.currentLevelIndex;
                int nextIndex = ses.currentLevelIndex + 1;
                Debug.Log($"[QuakeSession] Loading Level [{loadedIndex + 1}/{ses.LevelSequence.Count}]: {nextPair.name}");
                SceneLoader.LoadScenePair(nextPair, new SceneLoader.SceneLoadSettings()
                {
                    Payload = () =>
                    {
                        if (CurrentAs(out QuakeSession activeSes))
                        {
                            activeSes.currentLevelIndex = nextIndex;
                            activeSes.levelItems = new();
                            activeSes.ItemWarnTime = new();
                            quadDamageEndTime = 0f;
                        }
                    }
                });
                return true;
            }
            else
            {
                EndSession(new EndSessionSettings()
                {
                    SubmitScore = ses.submitScore
                });
                SceneLoader.MainMenu();
                return true;
            }
        }
    }
    #endregion
    [System.Serializable]
    public partial class QuakeSession : GameSession
    {
        [SerializeField] public List<ScenePairSO> LevelSequence = new();
        [SerializeField] public bool AllowShotgunStarting;
        [SerializeField] public bool submitScore;
        public int currentLevelIndex = 0;
        [NonSerialized] public static float quadDamageEndTime;
        public static bool IsQuadDamage => Time.time < quadDamageEndTime;

        Dictionary<QuakeKeyItems, bool> levelItems = new();
        Dictionary<QuakeKeyItems, float> ItemWarnTime = new();

    }
}