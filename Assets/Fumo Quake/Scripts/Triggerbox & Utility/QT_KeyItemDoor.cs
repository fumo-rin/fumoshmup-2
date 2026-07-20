using rinCore;
using System.Collections.Generic;
using UnityEngine;

namespace FumoQuake
{
    public class QT_KeyItemDoor : QT_Base
    {
        enum Mode
        {
            DisableDoorItems,
            EnableDoorItems
        }
        [SerializeField] QuakeKeyItems RequiredItem = QuakeKeyItems.SilverKeyOfDestiny;
        [SerializeField] List<GameObject> doorItems = new();
        [SerializeField] bool explosion;
        [SerializeField] Mode DoorOpeningMode = Mode.DisableDoorItems;
        protected override void WhenAwake()
        {

        }
        protected override bool WhenTriggerEnter(Collider other, IFumoUnit unit)
        {
            bool success = QuakeSession.HasItem(RequiredItem);
            if (success)
            {
                bool active = false;
                switch (DoorOpeningMode)
                {
                    case Mode.DisableDoorItems:
                        active = false;
                        break;
                    case Mode.EnableDoorItems:
                        active = true;
                        break;
                    default:
                        active = false;
                        break;
                }

                foreach (var item in doorItems)
                {
                    if (item == null)
                        continue;
                    item.SetActive(active);
                    if (explosion)
                    {
                        GeneralManager.FunnyExplosion(new()
                        {
                            is3d = true,
                            playSound = true,
                            position = item.transform.position,
                            scale = 3f
                        });
                    }
                }
            }
            return success;
        }
    }
}
