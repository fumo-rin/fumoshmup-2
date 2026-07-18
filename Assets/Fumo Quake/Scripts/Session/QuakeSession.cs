using rinCore;
using System.Collections.Generic;
using UnityEngine;

namespace FumoQuake
{
    public class QuakeSession : GameSession
    {
        [SerializeField] ScenePairSO mainMenu;
        [SerializeField] List<ScenePairSO> levelSequence = new();
        static Queue<ScenePairSO> levelQueue;
        protected override void WhenEndSession()
        {
            SceneLoader.LoadScenePair(mainMenu);
        }
        protected override void WhenStartSession()
        {
            PlayerWeaponsController.ResetWeaponState();
            Queue<ScenePairSO> levels = new(levelSequence);
            levelQueue = levels;
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
                    SubmitScore = true
                });
            }
        }
    }
}