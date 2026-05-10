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
        public static void SetCurrent(ShmupGamemode mode)
        {
            cachedCurrent = mode;
        }
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
    }
    #endregion
    #region Shottype & Player
    public partial class ShmupGamemode
    {
        [System.Serializable]
        public class Shottype
        {
            public string ShotName;
            [SerializeField] List<EcoEntry> PlayerEcoOverrides = new();
            public ShmupPlayer PlayerObject;
            public ShmupMovers.PlayerShmupMover UnfocusMover, FocusMover;
            [System.Serializable]
            class EcoEntry
            {
                public string Key;
                public int Value;
            }
            public void ApplyEcoOverrides()
            {
                foreach (var item in PlayerEcoOverrides)
                {

                }
            }
        }
        #region Request Player
        public static void SpawnCurrentPlayer(Vector2Shmup position)
        {
            var gamemode = CurrentMode;
            gamemode.RequestPlayer(gamemode.PlayerShot, position);
        }
        private void RequestPlayer(Shottype shot, Vector2Shmup? positionOverride = null)
        {
            IEnumerator SpawnAfterLoad()
            {
                yield return new WaitUntil(() => !SceneLoader.IsLoading);

                Vector2Shmup position = positionOverride ?? new(0.5f, 0.2f);
                Vector2 playerPos = position.Vector2Now;
                if (ShmupPlayer.PlayerAs(out ShmupPlayer p))
                {
                    yield break;
                }
                var output = shot.PlayerObject.Instantiate2D(playerPos);
                output.shmupMovers = new()
                {
                shot.UnfocusMover,
                shot.FocusMover
                };
            }
            SpawnAfterLoad().RunRoutine("Load Player", false);
        }
        #endregion
    }
    #endregion
    [System.Serializable]
    public partial class ShmupGamemode
    {
        public Shottype PlayerShot;
    }
}