using rinCore;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FumoShmup2
{
    public class ShmupPlayerObjectDetatcher : MonoBehaviour
    {
        [SerializeField] List<Transform> followObjects = new();
        [SerializeField] bool hiddenWhenPlayerHidden;
        private void Start()
        {
            IEnumerator CO_Run()
            {
                var cleanup = followObjects;
                foreach (var item in cleanup)
                {
                    item.transform.SetParent(null);
                }
                Transform follow = transform;
                while (follow != null && followObjects.Count > 0)
                    foreach (var item in followObjects)
                    {
                        if (hiddenWhenPlayerHidden)
                        {
                            item.gameObject.SetActive(follow.gameObject.activeInHierarchy);
                        }
                        item.position = follow.position;
                        yield return null;
                    }

                if (cleanup != null)
                {
                    foreach (var item in cleanup.ToList())
                    {
                        if (item == null)
                        {
                            continue;
                        }
                        Destroy(item.gameObject);
                    }
                }
            }
            CO_Run().RunRoutine();
        }
    }
}
