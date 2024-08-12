using HutongGames.PlayMaker;
using MSCLoader;
using OASIS;
using Steamworks;
using System.Collections;
using System.Deployment.Internal;
using System.Reflection;
using UnityEngine;

namespace JetMod
{
    public class LetterHook : FsmStateAction
    {
        public Letter letterHandler;
        public GameObject letter;
        public AudioSource mail_insert;
        public FsmGameObject envelope;

        public override void OnEnter()
        {
            if (envelope.Value == letter)
            {
                letterHandler.orderEntered();
                letter.SetActive(false);
                mail_insert.gameObject.transform.position = letter.transform.position;
                mail_insert.Play();
            }
            Finish();
        }
    }

    public class PostBuyHook : FsmStateAction
    {
        public Letter letter;
        public override void OnEnter()
        {
            letter.onOrderPaid();
            Finish();
        }
    }

    public class PostSlip : Interactable
    {
        public Letter postalSystem;
        AudioSource[] swears;
        AudioSource cashRegister;
        bool disabled;

        IEnumerator cooldown()
        {
            yield return new WaitForSeconds(3);
            disabled = false;
        }

        public void Start()
        {
            swears = GameObject.Find("MasterAudio").transform.Find("Swearing").GetComponentsInChildren<AudioSource>();
            cashRegister = GameObject.Find("MasterAudio").transform.Find("Store/cash_register_2").GetComponent<AudioSource>();
        }

        public override void mouseExit() { CursorGUI.buy = false; }

        public override void mouseOver()
        {
            // Cooldown
            if (disabled) return;

            CursorGUI.buy = true;
            VariableSystem.GUIinteraction.Value = "PAY POST ORDER";
            if (Input.GetMouseButtonDown(0))
            {
                if (VariableSystem.PlayerMoney.Value < 1750)
                {
                    // Not enough money
                    disabled = true;
                    StartCoroutine(cooldown());
                    VariableSystem.GUIinteraction.Value = "NOT ENOUGH MONEY";
                    CursorGUI.buy = false;
                    // Play random swear
                    var sfx = swears[Random.Range(0, swears.Length)];
                    sfx.gameObject.transform.position = VariableSystem.Player.transform.position;
                    sfx.Play();
                    return;
                }
                // When bought
                cashRegister.gameObject.transform.position = VariableSystem.Player.transform.position;
                cashRegister.Play();
                VariableSystem.PlayerMoney.Value = VariableSystem.PlayerMoney.Value - 1750;
                postalSystem.onOrderPaid();
            }
        }
    }

    public class Letter : Interactable
    {
        public GameObject half;
        public TextMesh Name;
        public GameObject Signed;
        public GameObject UI;
        public Camera camera;
        public Collider letter;
        public Collider signButton;
        public Tank60L part;
        public GameObject box;
        GameObject envelope;
        GameObject options;
        GameObject postSlip;
        GameObject currBox;
        AudioSource sign;
        AudioSource open;
        bool isOrderPlaced;
        public int timeLeft = 3600;
        bool slipAvailable = false;

        internal void setupBox()
        {
            currBox = GameObject.Instantiate(box);
            currBox.name = "60L Tank (epicd)";
            currBox.GetComponent<Package>().postSystem = this;
            part.fuel.fluidLevel = 0f;
        }

        internal void setupPostSlip()
        {
            if (postSlip != null) return;
            postSlip = GameObject.Instantiate(GameObject.Find("STORE").transform.Find("LOD/ActivateStore/PostOffice/PostOrderBuy").gameObject);
            postSlip.transform.SetParent(GameObject.Find("STORE").transform.Find("LOD/ActivateStore/PostOffice").transform);
            postSlip.transform.localPosition = new Vector3(0.106f, 0.849f, 0.983f);
            postSlip.transform.localEulerAngles = new Vector3(-8.368706E-07f, 7.845429f, -4.125681E-07f);
            // ew playmaker vaporize it!!!
            Component.Destroy(postSlip.GetPlayMaker("Use"));
            // replace with good
            var postscript = postSlip.gameObject.AddComponent<PostSlip>();
            postscript.postalSystem = this;
            postSlip.transform.Find("Price").GetComponent<TextMesh>().text = "1,750";
            postSlip.name = "PostOrderBuyFuelCell";
        }

        public void Start()
        {
            // setup references
            options = GameObject.Find("Systems").transform.Find("Options").gameObject;

            var masterAudio = GameObject.Find("MasterAudio");
            sign = masterAudio.transform.Find("GUI/buy").GetComponent<AudioSource>();
            open = masterAudio.transform.Find("HouseFoley/mail_envelope_open").GetComponent<AudioSource>();

            // setup UI
            UI.transform.SetParent(GameObject.Find("Sheets").transform, false);
            UI.transform.position = Vector3.zero;
            Name.text = PlayMakerGlobals.Instance.Variables.GetFsmString("PlayerName").Value;

            // setup new envelope & action
            envelope = GameObject.Instantiate(GameObject.Find("ITEMS/parts magazine(itemx)/EnvelopeSpawn").transform.Find("envelope(xxxxx)").gameObject);
            envelope.name = "envelope(epicd)";

            setupPostSlip();

            var postboxFSM = GameObject.Find("STORE").transform.Find("LOD/Post Box/OrderTrigger").GetPlayMaker("Open");
            postboxFSM.InitializeFSM();
            postboxFSM.GetState("State 2").AddAction(new LetterHook
            {
                letterHandler = this,
                letter = envelope,
                mail_insert = GameObject.Find("MasterAudio").transform.Find("HouseFoley/mail_insert").GetComponent<AudioSource>(),
                envelope = postboxFSM.FsmVariables.FindFsmGameObject("Envelope")
            });
        }

        IEnumerator Timer()
        {
            while (timeLeft > 0)
            {
                yield return new WaitForSeconds(1);
                timeLeft--;
            }
            // Slip should be available now
            SaveData.slipAvailable = true;
            postSlip.SetActive(true);
        }

        public void onSave()
        {
            SaveData.isOrderPlaced = isOrderPlaced;
            SaveData.slipAvailable = slipAvailable;
            SaveData.timeLeft = timeLeft;
            // If there is no box, dont save it
            if (currBox == null) return;
            SaveData.boxSpawned = true;
            SaveData.boxPos = currBox.transform.position;
            SaveData.boxRot = currBox.transform.rotation;
        }

        public void onLoad()
        {
            // Ensures this slip exists
            setupPostSlip();

            // Load if ordered & if in box
            isOrderPlaced = SaveData.isOrderPlaced;
            slipAvailable = SaveData.slipAvailable;
            timeLeft = SaveData.timeLeft;
            postSlip.SetActive(slipAvailable);
            if (isOrderPlaced && !slipAvailable)
            {
                // Start timer
                StartCoroutine(Timer());
            }
            postSlip.gameObject.SetActive(timeLeft == 0 && isOrderPlaced && !SaveData.boxSpawned);
            if (!SaveData.boxSpawned) return;
            // Make a new box
            setupBox();
            currBox.transform.position = SaveData.boxPos;
            currBox.transform.rotation = SaveData.boxRot;
            currBox.SetActive(true);
        }

        internal void packageOpened(Transform transform)
        {
            // Enable and place fuel cell at position of box
            part.gameObject.SetActive(true);
            part.gameObject.transform.position = transform.position;
            part.gameObject.transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, transform.eulerAngles.z -90); // Adjustments to fit boxes shape
            SaveData.boxSpawned = false;
            SaveData.findPart("60L Tank (epicd)").purchased = true;
        }

        public void orderEntered()
        {
            // If order already in, just ignore it.
            if (isOrderPlaced) return;

            // Reset timer
            timeLeft = 3600;

            // Don't do anything if its already attached to the car
            if (part.thisPart.attachedTo != -1) return;

            // Start timer
            StartCoroutine(Timer());
            isOrderPlaced = true;
        }

        public void onOrderPaid()
        {
            // Check if the order was placed
            if (!isOrderPlaced) return;
            if (part.thisPart.attachedTo != -1)
            {
                // The part is already attached to the car, so refund the user
                VariableSystem.GUIinteraction.Value = "ALREADY ATTACHED TO CAR";
                VariableSystem.PlayerMoney.Value = VariableSystem.PlayerMoney.Value + 1750;
                return;
            }
            else
            {
                // Position the box at teimos
                if (currBox != null) GameObject.Destroy(currBox); // Ensures a old box doesnt already exist
                setupBox();
                currBox.transform.position = new Vector3(-1554.5f, 4.1f, 1184f);
                currBox.transform.eulerAngles = new Vector3(270, -32, 0);
                part.gameObject.SetActive(false);
                currBox.SetActive(true);
                currBox.name = "60L Tank (epicd)";
            }
            // Reset values
            postSlip.SetActive(false);
            slipAvailable = false;
            isOrderPlaced = false;
            timeLeft = 3600;
        }

        IEnumerator WaitAfterSign()
        {
            sign.transform.position = VariableSystem.Player.transform.position;
            sign.Play();
            yield return new WaitForSeconds(1);
            envelope.SetActive(true);
            envelope.transform.position = VariableSystem.Player.transform.position;
            envelope.transform.eulerAngles = Vector3.up;
            UI.SetActive(false);
            options.SetActive(true);
            Signed.SetActive(false);
            VariableSystem.PlayerInMenu.Value = false;
        }

        public override void mouseOver()
        {
            VariableSystem.GUIuse.Value = true;
            if (cInput.GetKeyDown("Use"))
            {
                VariableSystem.PlayerInMenu.Value = true;
                UI.SetActive(true);
                options.SetActive(false);
                open.transform.position = VariableSystem.Player.transform.position;
                open.Play();

            }
        }

        public new void Update()
        {
            if ((Input.GetKeyDown(KeyCode.Escape) || cInput.GetKeyDown("Use")) && UI.activeInHierarchy)
            {
                VariableSystem.PlayerInMenu.Value = false;
                UI.SetActive(false);
                options.SetActive(true);
                Signed.SetActive(false);
            }

            if (Input.GetMouseButtonDown(0))
            {
                Ray UIray = camera.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(UIray, out hit, 19, 1 << 14) && hit.collider == signButton)
                {
                    Signed.SetActive(true);
                    StartCoroutine(WaitAfterSign());
                }
            }

            // Makes sure the Update from inherited class still runs
            base.Update();
        }
    }
}