using rinCore;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.UI;

namespace FumoQuake
{
    public class QU_SpawnPoint : MonoBehaviour
    {
        static HashSet<QU_SpawnPoint> randomSpawns = new();
        private void OnEnable()
        {
            Debug.Log("Starting : " + transform.name);
            randomSpawns.Add(this);
        }
        private void OnDisable()
        {
            randomSpawns.Remove(this);
        }
        public static bool LoadSpawnpoint(out QU_SpawnPoint randomSpawn)
        {
            randomSpawn = randomSpawns.OrderByRandom().FirstOrDefault();
            return randomSpawn != null;
        }
    }
}
