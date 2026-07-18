using rinCore;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FumoQuake
{
    [System.Serializable]
    public class QuakeSession : GameSession
    {
        [SerializeField] ScenePairSO mainMenu;
        [SerializeField] public List<ScenePairSO> levelSequence = new();
        static Queue<ScenePairSO> levelQueue;
        public bool submitScore;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            levelQueue = null;
        }
        protected override void WhenEndSession()
        {
            SceneLoader.LoadScenePair(mainMenu);
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
            }
            else
            {
                EndSession(new()
                {
                    SubmitScore = QuakeSession.CurrentAs(out QuakeSession ses) && ses.submitScore
                });
            }
        }
    }
}