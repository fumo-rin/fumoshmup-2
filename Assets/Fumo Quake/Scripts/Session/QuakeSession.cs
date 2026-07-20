using rinCore;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FumoQuake
{
    [System.Serializable]
    public class QuakeSession : GameSession
    {
        [SerializeField] public List<ScenePairSO> levelSequence = new();
        Dictionary<QuakeKeyItems, bool> levelItems = new();
        public static bool HasItem(QuakeKeyItems item) =>
            CurrentAs(out QuakeSession ses) &&
            ses.levelItems != null &&
            ses.levelItems.TryGetValue(item, out bool v) &&
            v;
        public static bool AwardItem(QuakeKeyItems item) =>
            CurrentAs(out QuakeSession ses) &&
            !ses.levelItems.TryGetValue(item, out _) &&
            ses.levelItems.TryAdd(item, true);

        static Queue<ScenePairSO> levelQueue;
        public bool submitScore;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            levelQueue = null;
        }
        protected override void WhenEndSession()
        {

        }
        protected override void WhenStartSession()
        {
            QuakeController.StoredHealth = null;
            PlayerWeaponsController.ResetWeaponState();
            Queue<ScenePairSO> levels = new();
            foreach (var item in levelSequence)
            {
                levels.Enqueue(item);
            }
            foreach (ScenePairSO level in levels)
            {
                Debug.Log("Session Levels : " + level.name);
            }
            levelQueue = levels;
            NextLevelOrMenu();
        }
        public static void NextLevelOrMenu()
        {
            ScenePairSO next = levelQueue.Count > 0 ? levelQueue.Dequeue() : null;
            if (next != null)
            {
                SceneLoader.LoadScenePair(next, null);
                if (CurrentAs(out QuakeSession ses))
                {
                    ses.levelItems = new();
                }
            }
            else
            {
                EndSession(new()
                {
                    SubmitScore = QuakeSession.CurrentAs(out QuakeSession ses) && ses.submitScore
                });
                SceneLoader.MainMenu();
            }
        }
    }
}