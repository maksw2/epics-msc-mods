using System.Collections;
using HutongGames.PlayMaker;
using MSCLoader;
using UnityEngine;

namespace JetMod
{
    public class WaterPumpSim : MonoBehaviour
    {
        private AudioSource button;
        public AudioSource loop;
        public AudioSource turnon;
        public AudioSource turnoff;
        public GameObject pivot;
        private FsmFloat fueltank;
        private FsmFloat caprot;
        private GameObject cap;
        private GameObject fuelnozzle;
        private GameObject fueltankpipe;
        private GameObject fueltankpiv;
        public float fuel;
        public float draw;
        public bool on = false;
        public bool canTurnOn = true;
        public bool inpipe = false;
        public bool outpipe = false;
        public bool wired = false;
        private PlayMakerFSM electrics;
        public GameObject inletcoll;
        public GameObject outletcoll;
        public GameObject wiringcoll;
        public GameObject inletAlt;
        public GameObject inlet;
        public GameObject outlet;
        public GameObject wiring;
        private FsmBool wiringok;
        private FsmBool inwater;
        private FsmFloat volts;
        private FsmFloat charge;
        private GameObject battery;
        public GameObject leak;
        public GameObject powerSwitch;
        private GameObject leaksfx;
        public OASIS.JointedPart thisPart;
        public GameObject fakeBolt;
        private Coroutine starter;
        public Tank60L fuelTank;
        public bool usingFuelTank;
        private int maskLayer;
        private Vector3 switchOff = new Vector3(0f, -9f, 0f);
        private Vector3 switchOn = new Vector3(0f, 9f, 0f);

        void Start()
        {
            maskLayer = ~(1 << LayerMask.NameToLayer("Player") | 1 << LayerMask.NameToLayer("DontCollide") | 1 << LayerMask.NameToLayer("PlayerOnlyColl") | 1 << LayerMask.NameToLayer("Parts") | 1 << LayerMask.NameToLayer("TriggerOnly") | 1 << LayerMask.NameToLayer("Collider"));
            button = GameObject.Find("MasterAudio/CarFoley/dash_button").GetComponent<AudioSource>();
            leaksfx = Object.Instantiate(VariableSystem.Player.transform.Find("PeeSound").gameObject);
            leaksfx.transform.parent = gameObject.transform;
            leaksfx.transform.localPosition = Vector3.zero;
            electrics = VariableSystem.Satsuma.transform.Find("CarSimulation/Car/Electrics").GetPlayMaker("Electrics");
            wiringok = electrics.FsmVariables.FindFsmBool("ElectricsOK");
            inwater = electrics.FsmVariables.FindFsmBool("InWater");
            volts = electrics.FsmVariables.FindFsmFloat("Volts");
            battery = electrics.FsmVariables.FindFsmGameObject("db_Battery").Value;
            charge = battery.GetPlayMaker("Data").FsmVariables.FindFsmFloat("Charge");
            fueltank = GameObject.Find("Database/DatabaseMechanics/FuelTank").GetPlayMaker("Data").FsmVariables.GetFsmFloat("FuelLevel");
            caprot = VariableSystem.Satsuma.transform.Find("MiscParts/fuel tank pipe(xxxxx)/FuelFiller/OpenCap").GetPlayMaker("Screw").FsmVariables.GetFsmFloat("Rot");
            cap = VariableSystem.Satsuma.transform.Find("MiscParts/fuel tank pipe(xxxxx)/FuelFiller/OpenCap/fuel_filler_cap").gameObject;
            fuelnozzle = VariableSystem.Satsuma.transform.Find("MiscParts/fuel tank pipe(xxxxx)/FuelFiller/OpenCap/CapTrigger_FuelSatsuma").gameObject;
            fueltankpipe = VariableSystem.Satsuma.transform.Find("MiscParts/fuel tank pipe(xxxxx)").gameObject;
            fueltankpiv = VariableSystem.Satsuma.transform.Find("MiscParts/pivot_fuel_tank").gameObject;
            leaksfx.GetComponent<AudioSource>().spatialBlend = 1f;
        }

        IEnumerator AwaitStart()
        {
            turnon.Play();
            while (turnon.isPlaying)
            {
                yield return new WaitForEndOfFrame();
            }
            loop.Play();
            if (inpipe && !outpipe)
            {
                leak.SetActive(true);
                leaksfx.SetActive(true);
            }
            starter = null;
        }

        // this is here cause the mod class does not have startcoroutine lol
        public void detachLater(OASIS.Part part)
        {
            StartCoroutine(LateDetach(part));
        }
        // OASIS.Bolt doesn't like when you detach the same frame it's part attaches
        IEnumerator LateDetach(OASIS.Part part)
        {
            yield return null;
            yield return new WaitForSeconds(0.15f);
            yield return new WaitForEndOfFrame();
            part.detach();
        }

        public void Deactivate()
        {
            if (on)
            {
                turnon.Stop();
                loop.Stop();
                turnoff.Play();
                leak.SetActive(false);
                leaksfx.SetActive(false);
                if (starter != null)
                {
                    StopCoroutine(starter);
                    starter = null;
                }
            }
        }

        void Update()
        {
            int attachedTo = thisPart.attachedTo;

            // Waterpump fuel behavior
            float pitch = Mathf.Clamp(charge.Value / 120f, 0.4f, 1f);
            turnon.pitch = pitch;
            turnoff.pitch = pitch;
            loop.pitch = pitch;

            bool canTurnOn = !inwater.Value && volts.Value > 10f && wiringok.Value && wired && attachedTo != -1;


            bool fuelSystemSetup = (usingFuelTank ? fuelTank.fuel.fluidLevel > 0f : fueltank.Value > 0f) && inpipe && outpipe && (usingFuelTank ? true : fueltankpipe.activeInHierarchy && fueltankpiv.transform.childCount != 0);


            if (canTurnOn)
            {
                if (on && fuelSystemSetup) fuel = Mathf.Clamp01(fuel + 50f * Time.deltaTime);
                if (on && !loop.isPlaying && !turnon.isPlaying)
                {
                    if (starter != null) StopCoroutine(starter);
                    starter = StartCoroutine(AwaitStart());
                }
            }
            else
            {
                // Ensure it turns off when it loses electricity or detaches
                if (loop.isPlaying || starter != null || turnon.isPlaying)
                {
                    if (starter != null) StopCoroutine(starter);
                    starter = null;
                    leak.SetActive(false);
                    leaksfx.SetActive(false);
                    turnon.Stop();
                    loop.Stop();
                    turnoff.Play();
                }
            }

            // Make sure the fuel cap is not visible if code forcefully attaches it or when inpipe is on
            VariableSystem.fuelCap.enabled = !inpipe || usingFuelTank;
            if (inpipe && !usingFuelTank) fuelnozzle.SetActive(false);
            if (inpipe && !usingFuelTank) cap.SetActive(false);

            // Allows the pump to draw from fuel cell, if being used
            if (usingFuelTank) fuelTank.fuel.fluidLevel -= draw;
            else fueltank.Value -= draw;
            fuel = Mathf.Clamp01(fuel - 1f * Time.deltaTime);

            // Battery drain and leak
            bool isLeaking = loop.isPlaying && wired && inpipe && !outpipe && (usingFuelTank ? fuelTank.fuel.fluidLevel > 0f : fueltank.Value > 0f);

            if (loop.isPlaying) charge.Value -= 0.04f * Time.deltaTime;
            if (isLeaking)
            {
                if (usingFuelTank) fuelTank.fuel.fluidLevel -= 0.4f * Time.deltaTime;
                else fueltank.Value -= 0.4f * Time.deltaTime;
            }
            leaksfx.SetActive(isLeaking);
            leak.SetActive(isLeaking);

            // Part handling

            inletcoll.SetActive((attachedTo != -1 && caprot.Value == 1f) || inpipe || fuelTank.thisPart.attachedTo != -1);
            outletcoll.SetActive(attachedTo != -1);
            wiringcoll.SetActive(attachedTo != -1);

            // Switch behavior
            if (Physics.Raycast(VariableSystem.mainCamera.ScreenPointToRay(Input.mousePosition), out var hitInfo, 1f, maskLayer) && hitInfo.collider.gameObject == powerSwitch)
            {
                VariableSystem.GUIuse.Value = true;

                if (cInput.GetButtonDown("Use") || Input.GetMouseButtonDown(0))
                {
                    // Play sound
                    button.transform.position = gameObject.transform.position;
                    button.Play();

                    on = !on;

                    // flip switch
                    pivot.transform.localEulerAngles = on ? switchOn : switchOff;

                    if (starter != null && !on)
                    {
                        StopCoroutine(starter);
                        starter = null;
                    }

                    if (!on && canTurnOn)
                    {
                        turnon.Stop();
                        loop.Stop();
                        turnoff.Play();
                        leak.SetActive(false);
                        leaksfx.SetActive(false);
                    }
                }
            }
        }
    }
}
