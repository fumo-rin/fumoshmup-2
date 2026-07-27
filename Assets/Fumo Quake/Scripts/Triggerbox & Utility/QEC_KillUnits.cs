using rinCore;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FumoQuake
{
    [RequireComponent(typeof(BoxCollider))]
    public class QEC_KillUnits : MonoBehaviour
    {
        [Tooltip("Percentage of Max Health dealt per second (e.g., 20 = 20%/sec)")]
        public float percentHealthDPS = 20f;
        public float tickInterval = 0.5f;

        [Tooltip("If true, deals damage immediately when a unit enters before starting their tick timer.")]
        public bool hitOnEnter = true;

        private readonly Dictionary<IFumoUnit, float> unitTickTimers = new();
        private BoxCollider box;

        private void Awake()
        {
            box = GetComponent<BoxCollider>();
            box.isTrigger = true;
        }

        private void FixedUpdate()
        {
            var keysToRemove = unitTickTimers.Keys
                .Where(unit => unit == null || !unit.IsAlive)
                .ToList();

            foreach (var deadUnit in keysToRemove)
            {
                unitTickTimers.Remove(deadUnit);
            }

            float currentTime = Time.fixedTime;
            foreach (var unit in unitTickTimers.Keys.ToList())
            {
                if (currentTime >= unitTickTimers[unit])
                {
                    unitTickTimers[unit] = currentTime + tickInterval;
                    Hit(unit);
                }
            }
        }

        private bool Hit(IFumoUnit unit)
        {
            if (unit == null || !unit.IsAlive) return false;
            GameObject go = unit.unitGameObject as GameObject;
            if (go == null) return false;

            if (go.TryGetComponent(out IQuakeHitable hitable))
            {
                float damage = 9999f;
                if (go.TryGetComponent(out IHealthState hs))
                {
                    damage = hs.HealthState_OfMaxHealth(percentHealthDPS * tickInterval);
                }
                hitable.Hit(new IQuakeHitable.HitPacket(null)
                {
                    Damage = damage,
                    HitPoint = hitable.Pivot,
                });
                return true;
            }
            return false;
        }

        private IFumoUnit GetFumoUnit(Collider other)
        {
            if (other.TryGetComponent(out IFumoUnit unit))
                return unit;

            return other.GetComponentInParent<IFumoUnit>();
        }

        private void OnTriggerEnter(Collider other)
        {
            IFumoUnit unit = GetFumoUnit(other);
            if (unit != null && unit.IsAlive && !unitTickTimers.ContainsKey(unit))
            {
                if (hitOnEnter)
                {
                    Hit(unit);
                    unitTickTimers[unit] = Time.fixedTime + tickInterval;
                }
                else
                {
                    unitTickTimers[unit] = Time.fixedTime + tickInterval;
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            IFumoUnit unit = GetFumoUnit(other);
            if (unit != null)
            {
                unitTickTimers.Remove(unit);
            }
        }
    }
}