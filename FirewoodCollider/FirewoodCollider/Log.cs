using HutongGames.PlayMaker;
using System.Collections;
using UnityEngine;

namespace FirewoodCollider
{
    public class Log : MonoBehaviour
    {
        internal FsmInt logschopped;
        internal FsmFloat stress;
        internal static Collider[] plrcollider;
        internal Collider logcollider;
        internal Collider logcollider2;

        void ignoreColliders(Collider[] colliders, bool ignore)
        {
            for (var i = 0; i < colliders.Length; i++)
            {
                var collider = colliders[i];
                Physics.IgnoreCollision(logcollider, collider, ignore);
                Physics.IgnoreCollision(logcollider2, collider, ignore);
            }
        }

        private void Start()
        {
            // References
            if (plrcollider == null)
            plrcollider = new Collider[] {
            GameObject.Find("YARD").transform.Find("MachineHall/Logging/Pölkky/PlayerOnlyColl").GetComponent<Collider>(),
            GameObject.Find("COTTAGE").transform.Find("Logging/Pölkky/PlayerOnlyColl").GetComponent<Collider>(),
            GameObject.Find("CABIN").transform.Find("LOD/Logging/Pölkky/PlayerOnlyColl").GetComponent<Collider>() };

            logcollider = gameObject.transform.GetComponent<Collider>();
            logcollider2 = gameObject.transform.parent.GetComponent<Collider>();
            logschopped = GameObject.Find("Systems").transform.GetChild(12).GetComponent<PlayMakerFSM>().FsmVariables.GetFsmInt("LogsChopped");
            stress = PlayMakerGlobals.Instance.Variables.GetFsmFloat("PlayerStress");

            // Setup
            ignoreColliders(plrcollider, true);

            // Ignored collision, so now safe to set to breakable.
            gameObject.GetComponent<FixedJoint>().breakForce = 2000f;
        }

        private void OnJointBreak()
        {
            // Stats and refs
            logschopped.Value += 1;
            stress.Value -= 1f;
            GameObject half = gameObject.transform.parent.gameObject;
            Transform masterAudio = MasterAudio.Instance.gameObject.transform.Find("WoodChop");
            int index = Random.Range(0, masterAudio.GetComponentsInChildren<Transform>().Length - 1);
            Transform chopSFX = masterAudio.GetChild(index);

            // Play chop sfx
            chopSFX.transform.position = gameObject.transform.position;
            chopSFX.transform.GetComponent<AudioSource>().Play();

            // Separation
            gameObject.transform.parent = null;
            half.transform.parent = null;
            gameObject.name = "firewood(Clone)";
            half.name = "firewood(Clone)";
            gameObject.tag = "PART";
            half.tag = "PART";

            // Enable collisions
            ignoreColliders(plrcollider, false);

            // Delete script
            GameObject.Destroy(this);
        }
    }
}