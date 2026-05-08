using rinCore;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace FumoShmup2
{
    public abstract class StageRunablePrefab : MonoBehaviour
    {
        protected abstract IEnumerator RunablePayload();
        public IEnumerator RunItem(bool RunSeperately)
        {
            IEnumerator RunWithCancellation(IEnumerator payload, System.Func<bool> cancellation)
            {
                while (!cancellation() && payload.MoveNext())
                    yield return payload.Current;
            }
            Vector2 center = new Vector2Shmup(0.5f, 0.5f).Vector2Now;
            StageRunablePrefab spawned = Instantiate(this, center, Quaternion.identity);
            if (!RunSeperately)
            {
                yield return RunWithCancellation(spawned.RunablePayload(), () => ShmupStage.RanStageThisFrame);
                if (spawned.gameObject != null)
                    Destroy(spawned.gameObject);
            }
            else
            {
                IEnumerator CO_Secondary()
                {
                    yield return RunWithCancellation(spawned.RunablePayload(), () => ShmupStage.RanStageThisFrame);
                    if (spawned.gameObject != null)
                        Destroy(spawned.gameObject);
                }
                CO_Secondary().RunRoutine("Secondary Stage Runable", false);
                yield break;
            }
        }
        void LateUpdate()
        {
            if (ShmupStage.RanStageThisFrame)
            {
                Destroy(gameObject);
            }
        }
    }
}
