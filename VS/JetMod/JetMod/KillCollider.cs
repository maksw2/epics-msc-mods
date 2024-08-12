using MSCLoader;
using System.Collections;
using UnityEngine;

namespace JetMod
{
    public class KillCollider : MonoBehaviour
    {
        public bool killingEnabled = true;
        private bool Killed = false;
        private Rigidbody player;
        private FixedJoint playerJoint;
        private GameObject MoveAwayFrom;
        private GameObject death;
        private GameObject FPSCamera;

        // Use this for initialization
        void Start()
        {
            player = VariableSystem.Player.transform.Find("Pivot/AnimPivot/Camera/FPSCamera/DeadBody").GetComponent<Rigidbody>();
            playerJoint = VariableSystem.Player.transform.Find("Pivot/AnimPivot/Camera/FPSCamera/DeadBody").GetComponent<FixedJoint>();
            FPSCamera = VariableSystem.Player.transform.Find("Pivot/AnimPivot/Camera/FPSCamera/FPSCamera").gameObject;
            MoveAwayFrom = gameObject.transform.parent.gameObject;
            death = GameObject.Find("Systems").transform.Find("Death").gameObject;
            gameObject.layer = 23;
        }

        IEnumerator AwaitPlayer()
        {
            while (true)
            {
                if (player.gameObject.activeInHierarchy)
                {
                    player.AddForceAtPosition((player.gameObject.transform.position - MoveAwayFrom.transform.position).normalized * 7000, MoveAwayFrom.transform.position);
                    break;
                }
                yield return new WaitForEndOfFrame();
            }
        }

        void OnTriggerStay(Collider obj)
        {
            if (obj.gameObject != VariableSystem.Player || Killed || !killingEnabled) return;
            var blocked = Physics.Linecast(MoveAwayFrom.transform.position, FPSCamera.transform.position, ~(1 << 20 | 1 << 23));
            if (blocked) return;
            Killed = true;
            VariableSystem.TriggerAchievement("jetmod_Burnt");
            Component.Destroy(playerJoint);
            death.SetActive(true);
            Destroy(VariableSystem.Player.GetComponent<SimpleSmoothMouseLook>());
            Destroy(VariableSystem.Player.GetComponent<MouseLook>());
            Destroy(VariableSystem.Player.GetComponent<CharacterController>());
            Destroy(VariableSystem.Player.GetComponent<CharacterMotor>());
            Destroy(VariableSystem.Player.GetComponent<FPSInputController>());
            Destroy(FPSCamera.GetComponent<SimpleSmoothMouseLook>());
            Destroy(FPSCamera.GetComponent<MouseLook>());

            // deathsfx
            Transform array = GameObject.Find("MasterAudio/Death").GetComponentInChildren<Transform>();
            int pickedsound = Random.Range(0, array.childCount);
            var a = GameObject.Find("MasterAudio/Death").transform.GetChild(pickedsound).GetComponent<AudioSource>();
            var burn = GameObject.Find("MasterAudio/HouseFoley/grill_addmeat").GetComponent<AudioSource>();
            burn.spatialBlend = 0f;
            burn.volume = 10f;
            burn.Play();
            a.spatialBlend = 0f;
            a.Play();

            // change text
            death.transform.Find("GameOverScreen/Paper/Fatigue/TextEN").GetComponent<TextMesh>().text = "Young male\ndies from\nsevere burns";
            death.transform.Find("GameOverScreen/Paper/Fatigue/TextFI").GetComponent<TextMesh>().text = "Nuori mies kuoli\nvakaviin palovammoihin";

            StartCoroutine(AwaitPlayer());
            // funny fling
        }
    }
}