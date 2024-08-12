using HutongGames.PlayMaker;
using MSCLoader;
using OASIS;
using System.Collections.Generic;
using UnityEngine;
using static JetMod.ShopManager;

namespace JetMod
{
    public class JetMod : Mod
    {
        public override string ID => "JetTurbine";

        public override string Name => "JetTurbine";

        public override string Author => "epicduck410";

        public override string Version => "1.0.1";

        public override void ModSetup()
        {
            SetupFunction(Setup.OnNewGame, Mod_OnNewGame);
            SetupFunction(Setup.OnMenuLoad, Mod_OnMenuLoad);
            SetupFunction(Setup.OnLoad, Mod_OnLoad);
            SetupFunction(Setup.OnSave, Mod_OnSave);
            #if DEBUG
            SetupFunction(Setup.Update, Mod_Update);
            #endif
        }

        bool achievementcorePresent;

        // im using this only for achievments so yeah it goes here
        private void Mod_OnMenuLoad()
        {
            // Anti-piracy entry point
            SaveSystem.Init();
            if (!ModLoader.IsReferencePresent("OASIS"))
            {
                ModConsole.Error("JetMod: WARNING! OASIS is not installed. Please install it before attempting to use this mod. Please get it here: https://github.com/Horsey4/OASIS");
                return;
            }
            achievementcorePresent = false; //ModLoader.IsModPresent("AchievementCore");
            /*
            if (!achievementcorePresent)
            {
                ModConsole.Log("JetMod: Achievments will not be obtained as you use the mod. If you want to, you can download AchievementCore here: https://www.nexusmods.com/mysummercar/mods/3518");
                return;
            }

            // required else MSCLoader sees Achievement class and freaks out since it doesn't exist and breaks the mod.
            setupAchievements();
            */
        }

        /*
        Sprite startup;
        Sprite burnt;
        Sprite detach;
        private void setupAchievements()
        {
            // prepare ab
            AssetBundle ab = LoadAssets.LoadBundle(Properties.Resources.jet_icons);

            // generate icons
            startup = ab.LoadAsset<Sprite>("jetmod_Startup");
            burnt = ab.LoadAsset<Sprite>("jetmod_Burnt");
            detach = ab.LoadAsset<Sprite>("jetmod_Detach");
            ab.Unload(false);
            Achievement.CreateAchievement("jetmod_Startup", "JetMod", "First Startup", "Did you know that the fuelpump is using a brushed motor?", startup, false);
            Achievement.CreateAchievement("jetmod_Burnt", "JetMod", "To a crisp", "This is the easiest way to die", burnt, false);
            Achievement.CreateAchievement("jetmod_Detach", "JetMod", "Well that happened", "Perhaps you should make sure it's bolted down next time?", detach, false);
        }
        */

        public override void ModSettings()
        {

        }

        private GameObject shelf;
        private GameObject jet;
        private JetSim jetsim;
        private GameObject bracket;
        private JointedPart bracketPart;
        private GameObject bracketBolt;
        private GameObject controller;
        private ControllerSim controllersim;
        private FixedJoint carmass;

        // mass
        float jetMass;
        float bracketMass;
        Rigidbody jetBody;
        Rigidbody bracketBody;
        Rigidbody carMassRigidbody;

        // waterpump related
        private GameObject waterpump;
        private WaterPumpSim watersim;
        private GameObject waterpump_linein;
        private OASIS.Part lineinPart;
        private GameObject waterpump_lineout;
        private OASIS.Part lineoutPart;
        private GameObject waterpump_wiring;
        private OASIS.Part wiringPart;
        private GameObject tank60L;
        private Letter letter;
        private Tank60L tanksim;
        private GameObject attachPoints;
        private MeshRenderer inletMesh;
        private MeshRenderer outletMesh;
        private MeshRenderer wiringMesh;
        BasePart[] allParts;

        public void Mod_OnLoad()
        {
            if (!ModLoader.IsReferencePresent("OASIS"))
            {
                ModConsole.Error("JetMod: WARNING! OASIS is not installed. Please install it before using this mod. Please get it here: https://github.com/Horsey4/OASIS/releases");
                return;
            }

            GameObject.Find("Systems").AddComponent<VariableSystem>().Init(); // prepare variables for use

            // prepare assets
            AssetBundle ab = LoadAssets.LoadBundle(Properties.Resources.jet);

            shelf = GameObject.Instantiate(ab.LoadAsset<GameObject>("Shelf.prefab"));
            Transform parts = GameObject.Instantiate(ab.LoadAsset<GameObject>("Parts.prefab")).transform;
            attachPoints = parts.Find("AttachPoints").gameObject;
            attachPoints.transform.SetParent(null);
            jet = parts.Find("jet (epicd)").gameObject;
            jet.transform.SetParent(null);
            bracket = parts.Find("bracket (epicd)").gameObject;
            bracket.transform.SetParent(null);
            controller = parts.Find("controller (epicd)").gameObject;
            controller.transform.SetParent(null);
            waterpump = parts.Find("waterpump (epicd)").gameObject;
            waterpump.transform.SetParent(null);
            waterpump_linein = parts.Find("waterpump line in (epicd)").gameObject;
            waterpump_linein.transform.SetParent(null);
            waterpump_lineout = parts.Find("waterpump line out (epicd)").gameObject;
            waterpump_lineout.transform.SetParent(null);
            waterpump_wiring = parts.Find("waterpump wiring (epicd)").gameObject;
            waterpump_wiring.transform.SetParent(null);
            tank60L = parts.Find("60L Tank (epicd)").gameObject;
            tank60L.transform.SetParent(null);
            letter = parts.Find("Note (epicd)").gameObject.GetComponent<Letter>();
            letter.transform.SetParent(null);

            // grab parts/sims
            jetsim = jet.GetComponent<JetSim>();
            bracketPart = bracket.GetComponent<JointedPart>();
            controllersim = controller.GetComponent<ControllerSim>();
            watersim = waterpump.GetComponent<WaterPumpSim>();
            lineinPart = waterpump_linein.GetComponent<OASIS.Part>();
            lineoutPart = waterpump_lineout.GetComponent<OASIS.Part>();
            wiringPart = waterpump_wiring.GetComponent<OASIS.Part>();
            tanksim = tank60L.GetComponent<Tank60L>();
            inletMesh = waterpump_linein.GetComponent<MeshRenderer>();
            outletMesh = waterpump_lineout.GetComponent<MeshRenderer>();
            wiringMesh = waterpump_wiring.GetComponent<MeshRenderer>();
            bracketBolt = bracket.transform.Find("Bolts").gameObject;

            jet.MakePickable();
            bracket.MakePickable();
            controller.MakePickable();
            waterpump.MakePickable();
            waterpump_linein.MakePickable();
            waterpump_lineout.MakePickable();
            waterpump_wiring.MakePickable();
            tank60L.MakePickable();
            letter.gameObject.MakePickable();

            // prepare shelf
            Texture mainTexture = ab.LoadAsset<Texture>("Shelf.png");
            GameObject shop = GameObject.Find("REPAIRSHOP").transform.Find("LOD/Store/Shelfs").gameObject;
            Material material = shop.transform.Find("garage_shelf_bars").GetComponent<MeshRenderer>().materials[0];
            shelf.transform.parent = shop.transform;
            shelf.transform.localPosition = new Vector3(-0.2f, 0.5f, -2.7f);
            shelf.transform.localEulerAngles = new Vector3(0f, 90f, 0f);
            MeshRenderer component = shelf.GetComponent<MeshRenderer>();
            MeshRenderer component2 = shelf.transform.GetChild(0).GetComponent<MeshRenderer>();
            int deobfuscator = 1 / (SaveSystem.gameIsLoaded ? 1 : 0);
            component.material = material;
            component2.material = material;
            component.material.mainTexture = mainTexture;
            component2.material.mainTexture = mainTexture;

            ab.Unload(false);

            // TODO: DEBUG 
            // VariableSystem.Player.transform.position = shelf.transform.position;

            // setup attachpoint transform
            foreach (Transform obj in attachPoints.GetComponentsInChildren<Transform>())
            {
                obj.gameObject.layer = 11; // set layer per OASIS requirement

                switch (obj.gameObject.name)
                {
                    case "controllerAttach":
                        obj.transform.parent = VariableSystem.satsumaMiscParts;
                        obj.localPosition = new Vector3(-0.05802268f, 0.6935728f, -0.5507776f);
                        obj.localEulerAngles = new Vector3(270f, 0f, 0f);
                        break;

                    case "bracketAttach":
                        obj.parent = VariableSystem.Satsuma.transform;
                        obj.localPosition = new Vector3(0f, 1.16f, -0.6f);
                        obj.localEulerAngles = new Vector3(0f, 90f, 270f);
                        break;

                    case "jetAttach":
                        obj.parent = bracket.transform;
                        obj.localPosition = new Vector3(0f, -0.07f, 0f);
                        obj.localRotation = Quaternion.Euler(180f, 0f, 0f);
                        break;

                    case "waterpumpAttach":
                        obj.transform.parent = VariableSystem.satsumaMiscParts;
                        obj.localPosition = new Vector3(-0.7389999f, 1f, 1f);
                        obj.localEulerAngles = new Vector3(295f, 89f, 180f);
                        break;

                    case "waterinletAttach":
                        obj.parent = waterpump.transform;
                        obj.localPosition = new Vector3(0.25f, 0f, 0.0009999871f);
                        obj.localEulerAngles = new Vector3(0f, 295f, 270f);
                        break;

                    case "wateroutletAttach":
                        obj.parent = waterpump.transform;
                        obj.localPosition = new Vector3(-0.16f, -0.1f, 0.08999999f);
                        obj.localEulerAngles = new Vector3(0f, 295f, 270f);
                        break;

                    case "waterwiringAttach":
                        obj.parent = waterpump.transform;
                        obj.localPosition = new Vector3(-0.6999997f, 0.3499999f, -0.6599998f);
                        obj.localEulerAngles = new Vector3(359.9902f, 269.5511f, 270f);
                        break;

                    case "60LTankAttach":
                        obj.parent = VariableSystem.satsumaMiscParts;
                        obj.localPosition = new Vector3(0, 0.6612715f, 1.52842f);
                        obj.localEulerAngles = new Vector3(0, 0, 0);
                        break;

                    case "CarMass":
                        obj.parent = VariableSystem.Satsuma.transform;
                        obj.localPosition = new Vector3(0f, 0.65f, -0.6f);
                        obj.localEulerAngles = new Vector3(0f, 0f, 0f);
                        carmass = obj.gameObject.GetComponent<FixedJoint>();
                        carMassRigidbody = obj.gameObject.GetComponent<Rigidbody>();
                        carmass.connectedBody = VariableSystem.Satsuma.GetComponent<Rigidbody>();
                        break;
                }
            }

            GameObject.Destroy(attachPoints);

            // cache mass
            jetBody = jet.GetComponent<Rigidbody>();
            jetMass = jetBody.mass;
            bracketBody = bracket.GetComponent<Rigidbody>();
            bracketMass = bracketBody.mass;

            jetsim.targetRigidbody = jetBody;

            // listen to events
            jetsim.thisPart.onAttach += Jetscript_OnAttach;
            jetsim.thisPart.onDetach += Jetscript_OnDetach;
            jetsim.thisPart.onBreak += Jetscript_OnBreak;
            jetsim.JetDetached += Jetscript_OnDetach;

            bracketPart.onAttach += Bracketscript_OnAttach;
            bracketPart.onDetach += Bracketscript_OnDetach;
            bracketPart.onBreak += Bracketscript_OnBreak;
            jetsim.BracketDetached += Bracketscript_OnDetach;

            watersim.thisPart.onAttach += Waterscript_OnAttach;
            watersim.thisPart.onDetach += Waterscript_OnDetach;
            watersim.thisPart.onBreak += Waterscript_OnBreak;

            lineinPart.onAttach += Inscript_OnAttach;
            lineinPart.onDetach += Inscript_OnDetach;

            lineoutPart.onAttach += Outscript_OnAttach;
            lineoutPart.onDetach += Outscript_OnDetach;

            wiringPart.onAttach += Wiringscript_OnAttach;
            wiringPart.onDetach += Wiringscript_OnDetach;

            // currently useless
            // tanksim.thisPart.onAttach += TankScript_OnAttach;
            tanksim.thisPart.onDetach += TankScript_OnDetach;
            tanksim.thisPart.onBreak += TankScript_OnBreak;

            // The mod will save and load parts in this order
            allParts = new BasePart[] 
            {   
                jetsim.thisPart, 
                bracketPart, 
                controllersim.thisPart, 
                watersim.thisPart, 
                lineinPart, 
                lineoutPart, 
                wiringPart, 
                tanksim.thisPart 
            };
            
            var doesBelong = SaveSystem.doesSaveBelongToUser(FsmVariables.GlobalVariables.FindFsmFloat("PlayerID").Value);
            
            if (SaveSystem.doesSaveExist() && doesBelong && deobfuscator - 1 == 0)
            {
                // Loads file into class for use
                SaveSystem.Load(FsmVariables.GlobalVariables.FindFsmFloat("PlayerID").Value);
            }
            else
            {
                if (!doesBelong)
                    ModConsole.LogError("The current JetMod.dat was not created with the current savefile, and cannot be used.");

                // Setup the savefile class instead

                // Initialize savedata for the rest of the mod to use
                SaveData.partsList = new List<Part>();
                foreach (BasePart part in allParts)
                {
                    // Generate and add part
                    SaveData.partsList.Add(new Part
                    {
                        partName = part.gameObject.name,
                        attached = false,
                        purchased = false,
                        position = Vector3.zero,
                        rotation = Quaternion.identity,
                        tightness = SaveData.grabTightness(part)
                    });
                }
            }

            letter.onLoad();

            // Variables
            tanksim.fuel.tankRigidBody.mass = SaveData.fuelCellFluidLevel + 5;
            tanksim.fuel.fluidLevel = SaveData.fuelCellFluidLevel;
            letter.gameObject.transform.position = SaveData.letterPos;
            letter.gameObject.transform.rotation = SaveData.letterRot;

            // Utilize savedata

            for (int i = 0; i < SaveData.partsList.Count; i++)
            {
                BasePart part = allParts[i];
                Part partData = SaveData.partsList[i];
                // Do nothing if it isn't purchased
                part.gameObject.SetActive(partData.purchased);
                if (!partData.purchased) continue;
                if (!partData.attached)
                {
                    part.gameObject.transform.position = partData.position;
                    part.gameObject.transform.rotation = partData.rotation;
                    continue;
                }
                part.attach(0);
                part.onAttach?.Invoke(0);
                for (int b = 0; b < part.bolts.Length; b++)
                {
                    Bolt bolt = part.bolts[b];
                    bolt.tightness = partData.tightness[b];
                }
            }

            // Activate letter?
            letter.gameObject.SetActive(jet.activeInHierarchy);

            // Connect Shop to SaveData and instantiated objects + part info
            GameObject[] items = new GameObject[7] { jet, bracket, waterpump, controller, waterpump_linein, waterpump_lineout, waterpump_wiring };
            BasePart[] itemInfo = new BasePart[7] { jetsim.thisPart, bracketPart, watersim.thisPart, controllersim.thisPart, lineinPart, lineoutPart, wiringPart };
            ShopManager partshop = shelf.GetComponent<ShopManager>();
            partshop.letter = letter.gameObject;
            Transform transform = partshop.gameObject.transform.Find("Items");
            for (int i = 0; i < partshop.shelfitems.Count; i++)
            {
                Shelfitem shelfitem = partshop.shelfitems[i];
                shelfitem.ItemObj = items[i];
                shelfitem.partInfo = itemInfo[i];
                shelfitem.ShelfObj = transform.Find(shelfitem.Name).gameObject;
                shelfitem.partData = SaveData.findPart(shelfitem.Name);
            }
        }
        
        private void TankScript_OnBreak(int bruh1, float bruh2)
        {
            if (!watersim.usingFuelTank) return;
            // waterpump inlet pipe will still be attached to the tank, so this removes it
            lineinPart.detach();
            watersim.inletAlt.SetActive(false);
            watersim.inpipe = false;
            inletMesh.enabled = true;
        }

        private void TankScript_OnDetach(int bruh)
        {
            if (!watersim.usingFuelTank) return;
            // waterpump inlet pipe will still be attached to the tank, so this removes it
            lineinPart.detach();
            watersim.inletAlt.SetActive(false);
            watersim.inpipe = false;
            inletMesh.enabled = true;
        }

        private void Waterscript_OnDetach(int bruh)
        {
            lineinPart.detach();
            lineoutPart.detach();
            wiringPart.detach();
            watersim.inletcoll.SetActive(false);
            watersim.outletcoll.SetActive(false);
            watersim.wiringcoll.SetActive(false);
            watersim.inlet.SetActive(false);
            watersim.outlet.SetActive(false);
            watersim.wiring.SetActive(false);
            watersim.inletAlt.SetActive(false);
            inletMesh.enabled = true;
            outletMesh.enabled = true;
            wiringMesh.enabled = true;
            watersim.inpipe = false;
            watersim.outpipe = false;
            watersim.wired = false;
            watersim.fakeBolt.SetActive(false);
            watersim.Deactivate();
        }

        private void Waterscript_OnBreak(int bruh, float bruh2) { Waterscript_OnDetach(0); }

        private void Waterscript_OnAttach(int bruh)
        {
            watersim.outletcoll.SetActive(true);
            watersim.wiringcoll.SetActive(true);
            watersim.fakeBolt.SetActive(true);
        }

        private void Wiringscript_OnDetach(int bruh)
        {
            wiringMesh.enabled = true;
            watersim.wiring.SetActive(false);
            watersim.wired = false;
        }

        private void Wiringscript_OnAttach(int bruh)
        {
            wiringMesh.enabled = false;
            watersim.wiring.SetActive(true);
            watersim.wired = true;
        }

        private void Outscript_OnDetach(int bruh)
        {
            outletMesh.enabled = true;
            watersim.outlet.SetActive(false);
            watersim.outpipe = false;
        }

        private void Outscript_OnAttach(int bruh)
        {
            outletMesh.enabled = false;
            watersim.outlet.SetActive(true);
            watersim.outpipe = true;
        }

        private void Inscript_OnDetach(int bruh)
        {
            inletMesh.enabled = true;
            watersim.inlet.SetActive(false);
            watersim.inletAlt.SetActive(false);
            watersim.inpipe = false;
            VariableSystem.fuelCap.enabled = true;
        }

        private void Inscript_OnAttach(int bruh)
        {
            bool fuelCell = tanksim.thisPart.attachedTo != -1;

            // Add behavior to use fuel cell
            if (fuelCell && VariableSystem.rightTailLight.activeInHierarchy)
            {
                watersim.detachLater(lineinPart);
                VariableSystem.GUIinteraction.Value = "DETACH RIGHT TAILLIGHT";
                return;
            }
            watersim.usingFuelTank = fuelCell;
            inletMesh.enabled = false;
            watersim.inlet.SetActive(!fuelCell);
            watersim.inletAlt.SetActive(fuelCell);
            watersim.inpipe = true;
            VariableSystem.fuelCap.enabled = fuelCell;
        }

        private void Bracketscript_OnDetach(int bruh)
        {
            jetsim.RecalculateBracketSum(0);
            // Fix bugginess when colliding with stuff
            if (jetsim.thisPart.attachedTo != -1)
            {
                jet.transform.SetParent(jetsim.thisPart.triggers[0].transform, true);
            }
            jetsim.targetRigidbody = jetBody;
            bracketBody.mass = jetsim.thisPart.attachedTo != -1 ? bracketMass + jetMass : bracketMass;
            carMassRigidbody.mass = 1;
            bracketBolt.SetActive(false);
        }
        private void Bracketscript_OnBreak(int bruh, float bruh2) { Bracketscript_OnDetach(0); }

        private void Bracketscript_OnAttach(int bruh)
        {
            jetsim.RecalculateBracketSum(0);
            // Fix bugginess when colliding with stuff
            if (jetsim.thisPart.attachedTo != -1)
            {
                jet.transform.SetParent(VariableSystem.satsumaMiscParts, true);
            }
            bracket.transform.SetParent(VariableSystem.satsumaMiscParts, true);
            jetsim.targetRigidbody = jetsim.thisPart.attachedTo != -1 ? carMassRigidbody : jetBody;
            carMassRigidbody.mass = jetsim.thisPart.attachedTo != -1 ? bracketMass + jetMass : bracketMass;
            bracketBody.mass = 1;
            bracketBolt.SetActive(true);
        }

        private void Jetscript_OnDetach(int bruh)
        {
            jetsim.RecalculateJetSum(0);
            jetsim.targetRigidbody = jetBody;
            if (bracketPart.attachedTo != -1)
            {
                carMassRigidbody.mass = bracketPart.attachedTo != -1 ? bracketMass : 1;
                jetBody.mass = jetMass;
            }
            else
            {
                bracketBody.mass -= jetMass;
                jetBody.mass = jetMass;
            }
            if (jetsim.started || jetsim.idle.isPlaying)
            {
                jetsim.throtmode = true;
                jetsim.interruption = true;
                controllersim.ExternalCoroutineStart();
            }
            controllersim.canvas.SetActive(false);
            jetsim.fakeBolt.SetActive(false);
        }
        private void Jetscript_OnBreak(int bruh, float bruh2) { Jetscript_OnDetach(0); }

        private void Jetscript_OnAttach(int bruh)
        {
            jetsim.RecalculateJetSum(0);
            jetsim.targetRigidbody = bracketPart.attachedTo != -1 ? carMassRigidbody : jetBody;
            if (bracketPart.attachedTo != -1)
            {
                // Fix bugginess when colliding with stuff
                jet.transform.SetParent(VariableSystem.satsumaMiscParts, true);
                carMassRigidbody.mass += jetMass;
                jetBody.mass = 1;
            }
            else
            {
                bracketBody.mass += jetMass;
                jetBody.mass = 1;
            }
            if (controllersim.thisPart.attachedTo != -1)
            {
                controllersim.canvas.SetActive(true);
            }
            jetsim.fakeBolt.SetActive(true);
        }

#if DEBUG
        public void Mod_Update()
        {
            if (Input.GetKeyDown(KeyCode.PageUp)) // ADD TOTAL ITEMS COST TO PLAYER MONEY
            {
                VariableSystem.GUIsubtitle.Value = "Added 31,750 mk";
                VariableSystem.PlayerMoney.Value = VariableSystem.PlayerMoney.Value + 31750;
            }
            
            if (Input.GetKeyDown(KeyCode.End)) // SKIP POST ORDER WAIT
            {
                VariableSystem.GUIsubtitle.Value = "Post order set to 3 seconds";
                letter.timeLeft = 3;
            }

            if (Input.GetKeyDown(KeyCode.Home)) // FORCE SAVE
            {
                VariableSystem.GUIsubtitle.Value = "JetMod.dat saved...";
                Mod_OnSave();
            }

            if (Input.GetKeyDown(KeyCode.Insert)) // INSTALL ALL PARTS
            {
                VariableSystem.GUIsubtitle.Value = "All purchased parts attached & bolted";
                for (int i = 0; i < allParts.Length; i++)
                {
                    BasePart part = allParts[i];
                    if (part.attachedTo != -1 || !SaveData.partsList[i].purchased) continue;
                    part.attach(0);
                }
                // These loops are separated due to a weird physics bug happening when they are combined
                for (int i = 0; i < allParts.Length; i++)
                {
                    BasePart part = allParts[i];
                    if (part.attachedTo != -1)
                    {
                        part.onAttach?.Invoke(0);
                        foreach (Bolt bolt in part.bolts)
                        {
                            bolt.tightness = bolt.maxTightness;
                        }
                    }
                }
                jetsim.RecalculateJetSum(0);
                jetsim.RecalculateBracketSum(0);
                // Double check for abnormal parenting
                if (jetsim.thisPart.attachedTo != -1 && bracketPart.attachedTo != -1 && (bracketPart.transform.parent != VariableSystem.satsumaMiscParts || jet.transform.parent != VariableSystem.satsumaMiscParts))
                {
                    jet.transform.SetParent(VariableSystem.satsumaMiscParts, true);
                    bracket.transform.SetParent(VariableSystem.satsumaMiscParts, true);
                } else if (bracketPart.attachedTo != -1 && bracket.transform.parent != VariableSystem.satsumaMiscParts)
                {
                    bracket.transform.SetParent(VariableSystem.satsumaMiscParts, true);
                }
            }
        }
#endif

        public void Mod_OnSave()
        {
            // Ensure savefile exists
            if (!SaveSystem.doesSaveExist()) SaveSystem.makeOrOverwriteSave();
            // Update part information
            for (int i = 0; i < SaveData.partsList.Count; i++)
            {
                BasePart part = allParts[i];
                Part partData = SaveData.partsList[i];

                partData.position = part.gameObject.transform.position;
                partData.rotation = part.gameObject.transform.rotation;
                partData.attached = part.attachedTo != -1;
                partData.tightness = SaveData.grabTightness(part);
            }
            SaveData.fuelCellFluidLevel = tanksim.fuel.fluidLevel;
            letter.onSave();
            SaveData.letterPos = letter.gameObject.transform.position;
            SaveData.letterRot = letter.gameObject.transform.rotation;

            SaveSystem.Save(FsmVariables.GlobalVariables.FindFsmFloat("PlayerID").Value);
        }

        private void Mod_OnNewGame()
        {
            SaveSystem.deleteSave();
        }
    }
}
