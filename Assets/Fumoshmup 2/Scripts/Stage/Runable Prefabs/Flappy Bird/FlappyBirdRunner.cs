using rinCore;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FumoShmup2
{
    [System.Serializable]
    public class FlappyBirdRunner : StageRunablePrefab
    {
        [SerializeField] int pillarCount = 12;
        [SerializeField] FlappyBirdPillar pillarSpawner;
        [SerializeField] float delayBetweenPillars = 1.5f;
        [SerializeField] float extraDelay;
        protected override IEnumerator RunablePayload()
        {
            List<FlappyBirdPillar> spawnedList = new();
            pillarSpawner.gameObject.SetActive(false);
            for (int i = 0; i < pillarCount; i++)
            {
                FlappyBirdPillar pillar = Instantiate(pillarSpawner, new Vector2Shmup(1.15f, RNG.FloatRange(0.2f, 0.8f)).Vector2Now, Quaternion.identity);
                pillar.transform.parent = transform;
                pillar.gameObject.SetActive(true);
                spawnedList.Add(pillar);
                yield return delayBetweenPillars.WaitForSeconds();
            }
            yield return extraDelay.WaitForSeconds();
            foreach (var spawned in spawnedList.ToList())
            {
                Destroy(spawned.gameObject);
            }
        }
    }
}
