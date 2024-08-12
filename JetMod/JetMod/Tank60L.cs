using MSCLoader;
using UnityEngine;

namespace JetMod
{
    public class Tank60L : MonoBehaviour
    {
        public OASIS.JointedPart thisPart;
        public SphereCollider fuelTrigger;
        public GameObject cap;
        public GameObject handlePivot;
        public FuelTrigger fuel;
        private Vector3 capPosClosed = new Vector3(0, 0.1614f, 0.0081f);
        private Vector3 capPosOpen = new Vector3(0.1077f, 0.1672f, -0.01939f);
        private Vector3 handleRotClosed = new Vector3(-90, 0, -360);
        private Vector3 handleRotOpen = new Vector3(-25, 180, -180);
        int maskLayer;
        bool open = false;
        private AudioSource capNoise;

        // Use this for initialization
        void Start()
        {
            maskLayer = ~(1 << LayerMask.NameToLayer("Player") | 1 << LayerMask.NameToLayer("DontCollide") | 1 << LayerMask.NameToLayer("PlayerOnlyColl") | 1 << LayerMask.NameToLayer("Parts") | 1 << LayerMask.NameToLayer("TriggerOnly") | 1 << LayerMask.NameToLayer("Collider"));
            capNoise = GameObject.Find("MasterAudio").transform.Find("HouseFoley/jerry_can_cap").gameObject.GetComponent<AudioSource>();
        }

        // Update is called once per frame
        void Update()
        {
            // Cap behavior
            if (Physics.Raycast(VariableSystem.mainCamera.ScreenPointToRay(Input.mousePosition), out var hitInfo, 1f, maskLayer) && hitInfo.collider.gameObject == cap)
            {
                VariableSystem.GUIuse.Value = true;

                if (!cInput.GetKeyDown("Use")) return;

                open = !open;

                capNoise.transform.position = transform.position;
                capNoise.Play();

                cap.transform.localPosition = open ? capPosOpen : capPosClosed;
                handlePivot.transform.localEulerAngles = open ? handleRotOpen : handleRotClosed;
                fuelTrigger.enabled = open;
            }
        }
    }
}