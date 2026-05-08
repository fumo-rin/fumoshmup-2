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
            public static string HitCounter => "Hit";
            public static string CashoutActivation060 => "Cashout";
        }
        public class shmupPlayerResources
        {
            Dictionary<string, int> intTable = new Dictionary<string, int>();
            Dictionary<string, float> floatTable = new();
            public shmupPlayerResources SetInt(string key, int newValue)
            {
                intTable[key] = newValue;
                return this;
            }
            public shmupPlayerResources SetFloat(string key, float newValue)
            {
                floatTable[key] = newValue;
                return this;
            }
            public float GetFloat(string key)
            {
                float value = 0;
                if (floatTable == null)
                {
                    Debug.LogError("Bwuz");
                    return 0f;
                }
                if (!floatTable.TryGetValue(key, out value))
                {
                    floatTable[key] = 0;
                    return 0f;
                }
                return value;
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
        public override bool GameLogicStalled
        {
            get
            {
                if (Time.timeScale <= 0f) return true;
                if (GeneralManager.IsPaused) return true;
                if (Dialogue.IsRunning) return true;
                if (base.GameLogicStalled)
                {
                    return true;
                }
                return false;
            }
        }
        public shmupPlayerResources playerResources;
        #region Int
        public int GetInt(string key) => playerResources.GetInt(key);
        public int SetInt(string key, int value, int min, int max) => playerResources.SetInt(key, value.Clamp(min, max)).GetInt(key);
        public int ChangeInt(string key, int delta, int min, int max) => playerResources.SetInt(
            key,
            (playerResources.GetInt(key) + delta).Clamp(min, max)
            ).GetInt(key);
        #endregion
        #region Float
        public float GetFloat(string key) => playerResources.GetFloat(key);
        public float SetFloat(string key, float value, float min, float max) => playerResources.SetFloat(key, value.Clamp(min, max)).GetFloat(key);
        public float ChangeFloat(string key, float delta, float min, float max)
        {
            float current = playerResources.GetFloat(key);
            current += delta;
            current = current.Clamp(min, max);
            playerResources.SetFloat(key, current);
            return current;
        }
        #endregion
        public ShmupSession(sessionData data, bool cancelPrevious, shmupPlayerResources playerResources) : base(data, cancelPrevious)
        {
            this.playerResources = playerResources;
        }
    }
}
