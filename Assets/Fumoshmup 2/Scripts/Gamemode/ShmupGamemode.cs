using rinCore;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FumoShmup2
{
    #region New
    public partial class ShmupGamemode
    {

    }
    #endregion
    #region New
    public partial class ShmupGamemode
    {

    }
    #endregion
    #region Graze Action
    public partial class ShmupGamemode
    {
        public delegate void ShmupGrazeAction(int grazeIncrease, int totalGraze);
        public static event ShmupGrazeAction WhenGraze;
        public double scorePerGraze = 100f;
        public static int TotalGraze { get; private set; }
        public static void TriggerGraze(int grazeIncrease)
        {
            TotalGraze += grazeIncrease;
            WhenGraze?.Invoke(grazeIncrease, TotalGraze);
            if (CurrentMode is ShmupGamemode mode && mode.scorePerGraze > 0d)
            {
                GameSession.TryAddScoreRaw(grazeIncrease * mode.scorePerGraze, "Player Grazing");
            }
        }
    }
    #endregion
    #region Current Mode Select
    public partial class ShmupGamemode
    {
        static ShmupGamemode cachedCurrent;
        public static ShmupGamemode CurrentMode
        {
            get
            {
                if (cachedCurrent == null)
                {
                    Debug.LogError("Called current Gamemode without caching a mode first.");
                    return null;
                }
                return cachedCurrent;
            }
            private set
            {
                cachedCurrent = value;
            }
        }
        public void StartGamemode(int shotIndex, GameSession.sessionData data, params (string key, int value)[] resources)
        {
            ShottypeIndex = shotIndex;
            CurrentMode = this;
            ShmupSession.shmupPlayerResources playerEconomy = new ShmupSession.shmupPlayerResources();
            foreach (var item in resources)
            {
                playerEconomy.SetInt(item.key, item.value);
            }
            if (TryGetCurrentShot(out Shottype shot))
            {
                foreach (var item in shot.ForeachPlayerEco())
                {
                    playerEconomy.SetInt(item.key, item.value);
                }
            }
            GameSession session = new ShmupSession(data, true, playerEconomy);
        }
    }
    #endregion
    #region Shottype & Player
    public partial class ShmupGamemode
    {
        [SerializeField] List<Shottype> ShotCache = new();
        static int cachedShotInt = 0;
        public static int ShottypeIndex
        {
            get
            {
                return cachedShotInt;
            }
            set
            {
                Debug.Log("Set Shot index : " + value);
                cachedShotInt = value;
            }
        }
        public static bool TryGetCurrentShot(out Shottype shot)
        {
            shot = null;
            var current = CurrentMode;
            if (current == null)
            {
                return false;
            }
            if (current.ShotCache == null || current.ShotCache.Count <= 0)
            {
                return false;
            }
            if (!current.ShotCache.TryGetIndex(ShottypeIndex, out shot))
            {
                if (!current.ShotCache.TryGetIndex(0, out shot))
                {
                    Debug.LogError("failed to get fallback shot or shot index.. bad");
                }
            }
            return shot != null;
        }
        [System.Serializable]
        public class Shottype
        {
            public string ShotName;
            [SerializeField] List<EcoEntry> PlayerEco = new();
            public ShmupPlayer PlayerObject;
            public ShmupMovers.PlayerShmupMover UnfocusMover, FocusMover;
            [System.Serializable]
            class EcoEntry
            {
                public string Key;
                public int Value;
            }
            public IEnumerable<(string key, int value)> ForeachPlayerEco()
            {
                foreach (var item in PlayerEco.ToList())
                {
                    yield return new(item.Key, item.Value);
                }
            }
        }
        #region Request Player
        public void RequestPlayer(Shottype shot)
        {
            IEnumerator SpawnAfterLoad()
            {
                yield return new WaitUntil(() => !SceneLoader.IsLoading);

                Vector2 playerPos = new Vector2Shmup(0.5f, 0.2f).Vector2Now;
                if (ShmupPlayer.PlayerAs(out ShmupPlayer p))
                {
                    playerPos = p.CurrentPosition;
                    Destroy(p);
                }
                var output = shot.PlayerObject.Instantiate2D(playerPos);
                output.shmupMovers = new()
                {
                shot.UnfocusMover,
                shot.FocusMover
                };
            }
            SpawnAfterLoad().RunRoutine("Load Player", true);
        }
        #endregion
        #region Start Eco
        public void StartEco(Shottype shot)
        {
        }
        #endregion
    }
    #endregion
    [CreateAssetMenu(menuName = "Fumoshmup 2/New Gamemode")]
    public partial class ShmupGamemode : ScriptableObject
    {

    }
}