using HutongGames.PlayMaker;
using MSCLoader;
using UnityEngine;

namespace JetMod
{
    public class SuspensionHook : FsmStateAction
    {
        public JetSim sim;

        public override void OnEnter()
        {
            sim.SuspensionBumped();
            Finish();
        }
    }
}