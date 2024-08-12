using HutongGames.PlayMaker;
using MSCLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace JetMod
{
    class VariableSystem : MonoBehaviour
    {
        public static GameObject Satsuma;
        public static GameObject Player;
        public static AxisCarController satsumaAxisCarController;
        public static Transform satsumaMiscParts;
        public static PlayMakerFSM fuelCap;
        public static FsmBool GUIuse;
        public static FsmBool PlayerInMenu;
        public static FsmFloat PlayerMoney;
        public static FsmString GUIinteraction;
        #if DEBUG
        public static FsmString GUIsubtitle;
        #endif
        public static GameObject rightTailLight;
        public static Camera mainCamera;
        private static bool achievementCorePresent = false;

        public void Init()
        {
            Satsuma = GameObject.Find("SATSUMA(557kg, 248)");
            satsumaAxisCarController = Satsuma.GetComponent<AxisCarController>();
            satsumaMiscParts = Satsuma.transform.Find("MiscParts");
            fuelCap = Satsuma.transform.Find("MiscParts/fuel tank pipe(xxxxx)/FuelFiller/OpenCap").GetPlayMaker("Screw");
            Player = GameObject.Find("PLAYER");
            GUIuse = PlayMakerGlobals.Instance.Variables.GetFsmBool("GUIuse");
            PlayerInMenu = PlayMakerGlobals.Instance.Variables.GetFsmBool("PlayerInMenu");
            GUIinteraction = PlayMakerGlobals.Instance.Variables.GetFsmString("GUIinteraction");
            PlayerMoney = PlayMakerGlobals.Instance.Variables.GetFsmFloat("PlayerMoney");
            rightTailLight = Satsuma.transform.Find("MiscParts/rearlight(right)").gameObject;
            achievementCorePresent = ModLoader.IsModPresent("AchievementCore");
            mainCamera = Camera.main;
            #if DEBUG
            GUIsubtitle = PlayMakerGlobals.Instance.Variables.GetFsmString("GUIsubtitle");
            #endif
    }

    // Achievement core broken with release configuration. Temporarily disabled till a fix is found.
    public static void TriggerAchievement(string achievementId)
        {
            // if (achievementCorePresent) altTriggerAchievement(achievementId);
        }

        internal static void altTriggerAchievement(string id)
        {
            // Achievement.TriggerAchievement(id);
        }
    }
}
