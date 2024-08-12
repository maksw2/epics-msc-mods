using UnityEngine;
using HutongGames.PlayMaker;
using Steamworks;
using System.Collections;

namespace JetMod
{
    public class IndicatorGUI : MonoBehaviour
    {
        Transform fluidBar;
        FuelTrigger fuelTrigger;

        public void Setup(FuelTrigger trigger)
        {
            fuelTrigger = trigger;

            transform.parent = GameObject.Find("GUI").transform.Find("Indicators/Fluids");
            transform.localPosition = Vector3.zero;
            transform.localEulerAngles = Vector3.zero;
            transform.localScale = new Vector3(2f, 2f, 2f);

            PlayMakerFSM.DestroyImmediate(transform.Find("bar").GetComponent<PlayMakerFSM>());

            fluidBar = transform.Find("bar");
            fluidBar.localScale = new Vector3(fuelTrigger.fluidLevel / fuelTrigger.maxCapacity, 1f, 1f);
        }

        void Update()
        {
            if (fuelTrigger != null) fluidBar.localScale = new Vector3(fuelTrigger.fluidLevel / fuelTrigger.maxCapacity, 1f, 1f);
        }
    }

    public class AudioManager : MonoBehaviour
    {
        Transform audioParent;

        Transform waterSound;
        Transform oilSound;
        Transform jerrycanSound;

        void Start()
        {
            audioParent = GameObject.Find("Systems").transform.Find("DisabledAudioParent");
            waterSound = audioParent.Find("WaterPouringSound");
            oilSound = audioParent.Find("BrakeFluidPouringSound");
            jerrycanSound = audioParent.Find("FuelPouringSound");
        }

        public void Reset()
        {
            waterSound.SetParent(audioParent,false);
            oilSound.SetParent(audioParent, false);
            jerrycanSound.SetParent(audioParent, false);
        }

        public void Pour(Transform transform, int id)
        {
            switch(id)
            {
                case 0:
                    oilSound.SetParent(transform, false);
                    AudioSource source0 = oilSound.GetComponent<AudioSource>();
                    source0.volume = 0.2f;
                    source0.pitch = 0.85f;
                    break;
                case 1:
                    waterSound.SetParent(transform, false);
                    AudioSource source1 = waterSound.GetComponent<AudioSource>();
                    source1.volume = 0.5f;
                    source1.pitch = 1f;
                    break;
                case 2:
                    oilSound.SetParent(transform, false);
                    AudioSource source2 = oilSound.GetComponent<AudioSource>();
                    source2.volume = 0.2f;
                    source2.pitch = 0.95f;
                    break;
                case 3:
                    waterSound.SetParent(transform, false);
                    AudioSource source3 = waterSound.GetComponent<AudioSource>();
                    source3.volume = 0.8f;
                    source3.pitch = 1f;
                    break;
                case 4:
                    jerrycanSound.SetParent(transform, false);
                    break;
            }
        }
    }

    public class FuelTrigger : MonoBehaviour
    {
        public enum FluidType { TwoStrokeFuel = 0, Gasoline = 1, DieselFuelOil = 2 };
        public FluidType fluidType;

        public Rigidbody tankRigidBody;

        public float fluidLevel;
        public float maxCapacity = 5f;
        
        [HideInInspector]
        public float dieselAmount = 0f;
        [HideInInspector]
        public float fuelOilAmount = 0f;

        private FsmBool pouring;
        private FsmFloat level;
        private FsmFloat levelPÖ;

        private int fuelMode = 0;

        public AudioManager audioManager;
        public PlayMakerFSM fluidTrigger;
        public Collider trigger;
        private GameObject barGUI;

        void Start()
        {
            transform.gameObject.layer = 11;

            transform.tag = "Trigger";

            if (fluidType == FluidType.DieselFuelOil && dieselAmount == 0f && fuelOilAmount != 0f)
            {
                dieselAmount = fluidLevel;
            }

            SetupGUI();
        }

        void SetupGUI()
        {
            barGUI = GameObject.Instantiate(GameObject.Find("GUI").transform.Find("Indicators/Fluids/GasolineCar").gameObject);
            // ENSURE it shows up
            foreach (Transform child in barGUI.transform) child.gameObject.SetActive(true);
            IndicatorGUI indicator = barGUI.AddComponent<IndicatorGUI>();
            indicator.Setup(this);
        }

        void OnTriggerEnter(Collider other)
        {
            if (fluidType.GetHashCode() == 0 && other.name == "TwoStrokeTrigger")
            {
                fluidTrigger = other.GetComponent<PlayMakerFSM>();
                pouring = fluidTrigger.FsmVariables.GetFsmBool("Pouring");
                level = fluidTrigger.FsmVariables.GetFsmFloat("Fluid");
            }

            if (fluidType.GetHashCode() == 1)
            {
                if (other.name == "FluidTrigger" && other.transform.parent.name == "gasoline(itemx)")
                {
                    fluidTrigger = other.GetComponent<PlayMakerFSM>();
                    pouring = fluidTrigger.FsmVariables.GetFsmBool("Pouring");
                    level = fluidTrigger.FsmVariables.GetFsmFloat("Fluid");
                    fuelMode = 1;
                }
                else if(other.name == "FuelNozzle" && other.transform.parent.name == "Pistol 98")
                {
                    fluidTrigger = other.GetComponent<PlayMakerFSM>();
                    pouring = fluidTrigger.FsmVariables.GetFsmBool("Pour");
                    level = fluidTrigger.FsmVariables.GetFsmFloat("FuelFlow");
                    fuelMode = 2;
                }
            }

            if (fluidType.GetHashCode() == 2)
            {
                if (other.name == "FluidTrigger" && other.transform.parent.name == "diesel(itemx)")
                {
                    fluidTrigger = other.GetComponent<PlayMakerFSM>();
                    pouring = fluidTrigger.FsmVariables.GetFsmBool("Pouring");
                    level = fluidTrigger.FsmVariables.GetFsmFloat("Fluid");
                    levelPÖ = fluidTrigger.FsmVariables.GetFsmFloat("FluidFuelOil");
                    fuelMode = 1;
                }
                else if (other.name == "FuelNozzle" && other.transform.parent.name == "Pistol D")
                {
                    fluidTrigger = other.GetComponent<PlayMakerFSM>();
                    pouring = fluidTrigger.FsmVariables.GetFsmBool("Pour");
                    level = fluidTrigger.FsmVariables.GetFsmFloat("FuelFlow");
                    fuelMode = 2;
                }
                else if (other.name == "FuelNozzle" && other.transform.parent.name == "Pistol PÖ")
                {
                    fluidTrigger = other.GetComponent<PlayMakerFSM>();
                    pouring = fluidTrigger.FsmVariables.GetFsmBool("Pour");
                    level = fluidTrigger.FsmVariables.GetFsmFloat("FuelFlow");
                    fuelMode = 3;
                }
            }
        }

        void OnTriggerExit()
        {
            if (fluidTrigger != null) fluidTrigger = null;
            if (fuelMode > 1) pouring.Value = false;
            pouring = new FsmBool(false);
            level = new FsmFloat(0f);
            levelPÖ = new FsmFloat(0f);
            if (fuelMode != 0) fuelMode = 0;
            if (barGUI.activeInHierarchy) barGUI.SetActive(false);
            audioManager.Reset();
        }

        void FixedUpdate()
        {
            if (!trigger.enabled && barGUI.activeInHierarchy) OnTriggerExit();

            if (fluidType == FluidType.DieselFuelOil) fluidLevel = dieselAmount + fuelOilAmount;

            if (pouring == null) return;

            barGUI.SetActive(pouring.Value && fluidLevel != maxCapacity);

            if (fluidTrigger != null)
            {
                if (fluidLevel >= maxCapacity || fuelMode == 1 && (fluidType != FluidType.DieselFuelOil && level.Value == 0f || fluidType == FluidType.DieselFuelOil && (level.Value == 0f & levelPÖ.Value == 0f))) audioManager.Reset();

                switch (fluidType.GetHashCode())
                {
                    case 0:
                        if (level.Value != 0f && fluidLevel != maxCapacity && pouring.Value)
                        {
                            audioManager.Pour(transform, 3);
                            fluidLevel = Mathf.Clamp(fluidLevel += 0.1f * Time.deltaTime, 0f, maxCapacity);
                            level.Value = Mathf.Clamp(level.Value -= 0.1f * Time.deltaTime, 0f, Mathf.Infinity);
                        }
                        break;
                    case 1:
                        if (fuelMode == 1 && pouring.Value && level.Value != 0f && fluidLevel != maxCapacity)
                        {
                            audioManager.Pour(transform, 4);
                            fluidLevel = Mathf.Clamp(fluidLevel += 0.7f * Time.deltaTime, 0f, maxCapacity);
                            level.Value = Mathf.Clamp(level.Value -= 0.7f * Time.deltaTime, 0f, Mathf.Infinity);
                        }
                        else if (fuelMode == 2)
                        {
                            pouring.Value = fluidLevel != maxCapacity;
                            if (pouring.Value) fluidLevel = Mathf.Clamp(fluidLevel += level.Value * Time.deltaTime, 0f, maxCapacity);
                        }
                        break;
                    case 2:
                        if (fuelMode == 1 && pouring.Value && dieselAmount + fuelOilAmount < maxCapacity)
                        {
                            if(level.Value != 0f)
                            {
                                audioManager.Pour(transform, 4);
                                dieselAmount = Mathf.Clamp(dieselAmount += 0.7f * Time.deltaTime, 0f, maxCapacity);
                                level.Value = Mathf.Clamp(level.Value -= 0.7f * Time.deltaTime, 0f, Mathf.Infinity);
                            }
                            if (levelPÖ.Value != 0f)
                            {
                                audioManager.Pour(transform, 4);
                                fuelOilAmount = Mathf.Clamp(fuelOilAmount += 0.7f * Time.deltaTime, 0f, maxCapacity);
                                levelPÖ.Value = Mathf.Clamp(levelPÖ.Value -= 0.7f * Time.deltaTime, 0f, Mathf.Infinity);
                            }
                        }
                        else if (fuelMode == 2)
                        {
                            pouring.Value = dieselAmount + fuelOilAmount < maxCapacity;
                            if (pouring.Value) dieselAmount = Mathf.Clamp(dieselAmount += level.Value * Time.deltaTime, 0f, maxCapacity);
                        }
                        else if (fuelMode == 3)
                        {
                            pouring.Value = dieselAmount + fuelOilAmount < maxCapacity;
                            if (pouring.Value) fuelOilAmount = Mathf.Clamp(fuelOilAmount += level.Value * Time.deltaTime, 0f, maxCapacity);
                        }
                        break;
                }
            }

            if(tankRigidBody != null)
            {
                tankRigidBody.mass = fluidLevel + 5f;
            }
        }
    }
}