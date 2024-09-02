using HutongGames.PlayMaker.Actions;
using MSCLoader;
using UnityEngine;

namespace FirewoodCollider
{
    public class FirewoodCollider : Mod
    {
        public override string ID => "FirewoodCollider"; // Your (unique) mod ID 
        public override string Name => "FirewoodCollider"; // Your mod name
        public override string Author => "epicduck410"; // Name of the Author (your name)
        public override string Version => "1.21"; // Version
        public override bool UseAssetsFolder => false;

        public override void ModSetup()
        {
            SetupFunction(Setup.OnLoad, Mod_OnLoad);
        }

        private void Mod_OnLoad()
        {
            // Logging vars
            Transform loggingHome = GameObject.Find("YARD").transform.Find("MachineHall/Logging");

            // Setup logging sites
            loggingHome.Find("Pölkky/PlayerOnlyColl").gameObject.layer = LayerMask.NameToLayer("Collider2");
            GameObject.Find("COTTAGE").transform.Find("Logging/Pölkky/PlayerOnlyColl").gameObject.layer = LayerMask.NameToLayer("Collider2");
            GameObject.Find("CABIN").transform.Find("LOD/Logging/Pölkky/PlayerOnlyColl").gameObject.layer = LayerMask.NameToLayer("Collider2");

            // Grab prefab from playmaker
            PlayMakerFSM loggingwall = loggingHome.Find("Logwall").GetPlayMaker("Use");
            loggingwall.InitializeFSM();
            GameObject logPrefab = loggingwall.GetState("Create log").GetAction<CreateObject>(3).gameObject.Value;

            // Check if log already has FSM removed? If so, then mod was already loaded and don't do anything.
            var half = logPrefab.transform.GetChild(0);
            if (!half.GetComponent<PlayMakerFSM>()) return;

            // Delete stupid FSM
            Component.Destroy(half.GetComponent<PlayMakerFSM>());

            // Add script and set to unbreakable
            half.GetComponent<FixedJoint>().breakForce = float.PositiveInfinity;
            half.gameObject.AddComponent<Log>();
        }
    }
}
