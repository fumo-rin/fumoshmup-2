using rinCore;
using System.Collections.Generic;
using UnityEngine;
namespace FumoShmup2
{
    #region Game Actions
    public partial class ShmupSession
    {
        public delegate void GameAction();
        public static event GameAction WhenContinue;
        public static void TriggerContinue()
        {
            WhenContinue?.Invoke();
        }
    }
    #endregion
    public partial class ShmupSession : rinCore.GameSession
    {
        public struct keys
        {
            public static string CurrentBombs => "CurrentBombs";
            public static string StartingBombs => "StartingBombs";
        }
        public class shmupPlayerResources
        {
            Dictionary<string, int> intTable = new Dictionary<string, int>();
            public shmupPlayerResources SetInt(string key, int newValue)
            {
                intTable[key] = newValue;
                return this;
            }
            public int GetInt(string key)
            {
                int value = 0;
                if (intTable == null)
                {
                    Debug.LogError("Bwuz");
                    return 0;
                }
                if (!intTable.TryGetValue(key, out value))
                {
                    intTable[key] = 0;
                    return 0;
                }
                return value;
            }
        }
        public shmupPlayerResources playerResources;
        public int GetResource(string key) => playerResources.GetInt(key);
        public int SetResource(string key, int value) => playerResources.SetInt(key, value).GetInt(key);
        public int ChangeResource(string key, int delta) => playerResources.SetInt(key, playerResources.GetInt(key) + delta).GetInt(key);
        public ShmupSession(sessionData data, bool cancelPrevious, shmupPlayerResources playerResources) : base(data, cancelPrevious)
        {
            this.playerResources = playerResources;
        }
    }
}
