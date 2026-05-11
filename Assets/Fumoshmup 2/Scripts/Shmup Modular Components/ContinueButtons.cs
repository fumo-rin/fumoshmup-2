using rinCore;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
namespace FumoShmup2
{
    public class ContinueButtons : MonoBehaviour
    {
        static ContinueButtons instance;
        [SerializeField] GameObject visibilityNest;
        [SerializeField] Button yesB, noB;
        bool visible;
        static bool IsVisible => instance != null && instance.visible;
        private void Awake()
        {
            instance = this;
            Hide();
        }
        public static bool Show(out WaitUntil waitForInput, Action yes, Action no)
        {
            if (instance == null || instance.gameObject == null)
            {
                Debug.LogError("Missing Continue tings");
                waitForInput = null;
                return false;
            }
            instance.visible = true;
            instance.visibilityNest.SetActive(true);
            instance.yesB.gameObject.Select_WithEventSystem();
            waitForInput = new WaitUntil(() => !IsVisible);

            yes += Hide;
            no += Hide;

            instance.yesB.BindSingleAction(yes);
            instance.noB.BindSingleAction(no);
            return true;
        }
        private static void Hide()
        {
            if (RinHelper.ValidGameObjects(instance.visibilityNest))
            {
                instance.visibilityNest.SetActive(false);
                instance.visible = false;
            }
        }
    }

}
