using System.Collections;
using UnityEngine;

namespace JetMod
{
    public class ControllerSim : MonoBehaviour
    {
        public JetSim jetscript;
        public bool canstart = true;
        public bool afterburnerEnabled = false;
        public OASIS.Part thisPart;
        public OASIS.JointedPart jetpart;
        public GameObject selector;
        public MeshRenderer start;
        public MeshRenderer stop;
        public MeshRenderer manual;
        public GameObject menu;
        public GameObject statscreen;
        public TextMesh activity;
        public TextMesh info;
        public TextMesh RPM;
        public TextMesh startBtn;
        public TextMesh stopBtn;
        public TextMesh manualBtn;
        public TextMesh aftBtn;
        public GameObject abIndicator;
        public Color selected;
        public Color deselected;
        private GameObject wiredetached;
        private GameObject wireattached;
        public GameObject canvas;
        private GameObject buttons;
        private int selection = 0;
        private float time = Time.time;
        private int maskLayer;

        private IEnumerator EngineStartingPhase()
        {
            jetscript.start.Stop();
            StartCoroutine(jetscript.StartEngine());
            menu.SetActive(false);
            statscreen.SetActive(true);
            activity.text = "STARTING";
            activity.fontSize = 26;
            bool started = false;
            while (true)
            {
                if (jetscript.interruption)
                {
                    jetscript.interruption = false;
                    yield break;
                }
                if (!jetscript.decelling && !started)
                {
                    info.text = "STARTING.";
                    yield return new WaitForSeconds(0.33f);
                    info.text = "STARTING..";
                    yield return new WaitForSeconds(0.33f);
                    info.text = "STARTING...";
                    yield return new WaitForSeconds(0.33f);
                }
                else if (!started)
                {
                    info.text = "STARTING\nSTARTED\nFUEL ON";
                    started = true;
                    yield return new WaitForSeconds(0.5f);
                }
                if (started && !jetscript.started)
                {
                    yield return new WaitForSeconds(0.33f);
                    info.text = "STARTING\nSTARTED\nFUEL ON\nRPM\nLOWERING.";
                    yield return new WaitForSeconds(0.33f);
                    info.text = "STARTING\nSTARTED\nFUEL ON\nRPM\nLOWERING..";
                    yield return new WaitForSeconds(0.33f);
                    info.text = "STARTING\nSTARTED\nFUEL ON\nRPM\nLOWERING...";
                }
                else if (jetscript.started)
                {
                    break;
                }
                yield return new WaitForEndOfFrame();
            }
            info.text = "STARTING\nSTARTED\nFUEL ON\nRPM\nLOWERING\nREADY";
            yield return new WaitForSeconds(1f);
            menu.SetActive(true);
            statscreen.SetActive(false);
        }

        public IEnumerator EngineStopPhase(bool FuelLoss)
        {
            jetscript.interruption = true;
            StartCoroutine(jetscript.StopEngine());
            menu.SetActive(false);
            statscreen.SetActive(true);
            jetscript.throtmode = true;
            activity.text = "STOPPING";
            activity.fontSize = 26;
            if (FuelLoss)
            {
                activity.text = "STOPPING\nNO FUEL";
                activity.fontSize = 15;
            }
            bool off = false;
            while (true)
            {
                if (jetscript.decelling && !off)
                {
                    info.text = "FUEL OFF\nSTOPPING.";
                    yield return new WaitForSeconds(0.33f);
                    info.text = "FUEL OFF\nSTOPPING..";
                    yield return new WaitForSeconds(0.33f);
                    info.text = "FUEL OFF\nSTOPPING...";
                    yield return new WaitForSeconds(0.33f);
                }
                else if (!off)
                {
                    info.text = "FUEL OFF\nSTOPPING\nSTOPPED";
                    off = true;
                    yield return new WaitForSeconds(0.5f);
                }
                if (off && !jetscript.decelling)
                {
                    break;
                }
                yield return new WaitForEndOfFrame();
            }
            info.text = "FUEL OFF\nSTOPPING\nSTOPPED\nCOOLING.";
            yield return new WaitForSeconds(0.33f);
            info.text = "FUEL OFF\nSTOPPING\nSTOPPED\nCOOLING..";
            yield return new WaitForSeconds(0.33f);
            info.text = "FUEL OFF\nSTOPPING\nSTOPPED\nCOOLING...";
            yield return new WaitForSeconds(0.33f);
            info.text = "FUEL OFF\nSTOPPING\nSTOPPED\nCOOLING\nREADY";
            yield return new WaitForSeconds(1f);
            menu.SetActive(true);
            statscreen.SetActive(false);
            jetscript.interruption = false;
        }

        private IEnumerator CantStart()
        {
            menu.SetActive(false);
            statscreen.SetActive(true);
            activity.text = "FAIL";
            activity.fontSize = 26;
            info.text = "ERR 191\nENGINE NO\nFUEL";
            yield return new WaitForSeconds(1f);
            info.text = "ERR 191\nENGINE NO\nFUEL\nREADY";
            yield return new WaitForSeconds(1f);
            menu.SetActive(true);
            statscreen.SetActive(false);
        }

        public void Select(string action)
        {
            switch (action)
            {
                case "UP":
                    if (jetscript.throtmode)
                    {
                        selection--;
                        break;
                    }
                    jetscript.controllerexp += 1f;
                    jetscript.controllerexp = Mathf.Clamp(jetscript.controllerexp, 0f, 70f);
                    info.text = "CONTROLS:\nUP\nDOWN\n\n\n" + (jetscript.controllerexp + 30f) + "%";
                    break;
                case "DOWN":
                    if (jetscript.throtmode)
                    {
                        selection++;
                        break;
                    }
                    jetscript.controllerexp -= 1f;
                    jetscript.controllerexp = Mathf.Clamp(jetscript.controllerexp, 0f, 70f);
                    info.text = "CONTROLS:\nUP\nDOWN\n\n\n" + (jetscript.controllerexp + 30f) + "%";
                    break;
                case "OK":
                    if (menu.activeInHierarchy && jetscript.throtmode)
                    {
                        switch (selection)
                        {
                            case 0:
                                if (!jetscript.started && canstart)
                                {
                                    StartCoroutine(EngineStartingPhase());
                                }
                                else if (!canstart)
                                {
                                    StartCoroutine(CantStart());
                                }
                                break;
                            case 1:
                                if (jetscript.started)
                                {
                                    StartCoroutine(EngineStopPhase(false));
                                }
                                break;
                            case 2:
                                menu.SetActive(false);
                                statscreen.SetActive(true);
                                activity.text = "MANUAL";
                                jetscript.throtmode = false;
                                info.text = "CONTROLS:\nUP\nDOWN\n\n\n" + (jetscript.controllerexp + 30f) + "%";
                                break;
                            case 3:
                                afterburnerEnabled = !afterburnerEnabled;
                                aftBtn.text = "AB " + (afterburnerEnabled ? "ON" : "OFF");
                                break;
                        }
                    }
                    else if (!jetscript.throtmode)
                    {
                        menu.SetActive(true);
                        statscreen.SetActive(false);
                        jetscript.throtmode = true;
                    }
                    return;
            }
            startBtn.color = deselected;
            stopBtn.color = deselected;
            manualBtn.color = deselected;
            aftBtn.color = deselected;
            switch (selection)
            {
                case -1:
                    aftBtn.color = selected;
                    selector.transform.localPosition = new Vector3(selector.transform.localPosition.x, aftBtn.transform.localPosition.y, selector.transform.localPosition.z);
                    selection = 3;
                    break;
                case 0:
                    startBtn.color = selected;
                    selector.transform.localPosition = new Vector3(selector.transform.localPosition.x, start.transform.localPosition.y, selector.transform.localPosition.z);
                    break;
                case 1:
                    selector.transform.localPosition = new Vector3(selector.transform.localPosition.x, stop.transform.localPosition.y, selector.transform.localPosition.z);
                    stopBtn.color = selected;
                    break;
                case 2:
                    manualBtn.color = selected;
                    selector.transform.localPosition = new Vector3(selector.transform.localPosition.x, manual.transform.localPosition.y, selector.transform.localPosition.z);
                    break;
                case 3:
                    aftBtn.color = selected;
                    selector.transform.localPosition = new Vector3(selector.transform.localPosition.x, aftBtn.transform.localPosition.y, selector.transform.localPosition.z);
                    break;
                case 4:
                    startBtn.color = selected;
                    selector.transform.localPosition = new Vector3(selector.transform.localPosition.x, start.transform.localPosition.y, selector.transform.localPosition.z);
                    selection = 0;
                    break;
            }
        }

        private void Start()
        {
            // easier to click button
            maskLayer = ~(1 << LayerMask.NameToLayer("Player") | 1 << LayerMask.NameToLayer("DontCollide") | 1 << LayerMask.NameToLayer("PlayerOnlyColl") | 1 << LayerMask.NameToLayer("Parts") | 1 << LayerMask.NameToLayer("TriggerOnly") | 1 << LayerMask.NameToLayer("Collider"));
            wireattached = gameObject.transform.Find("controller_cable_connect").gameObject;
            wiredetached = gameObject.transform.Find("controller_cable").gameObject;
            buttons = gameObject.transform.Find("Buttons").gameObject;
            RPM.text = "RPM: 0";
            if (thisPart.attachedTo != -1)
            {
                wireattached.SetActive(true);
                wiredetached.SetActive(false);
                if (jetpart.attachedTo != -1)
                {
                    canvas.SetActive(true);
                }
            }
            thisPart.onAttach += Partscript_OnAttach;
            thisPart.onDetach += Partscript_OnDetach;
        }

        public void ExternalCoroutineStart()
        {
            StartCoroutine(EngineStopPhase(false));
        }

        public void Partscript_OnDetach(int bruh)
        {
            if (jetscript.started || jetscript.start.isPlaying)
            {
                jetscript.throtmode = true;
                jetscript.interruption = true;
                StartCoroutine(EngineStopPhase(false));
            }
            wireattached.SetActive(false);
            wiredetached.SetActive(true);
            canvas.SetActive(false);
        }

        public void Partscript_OnAttach(int bruh)
        {
            wireattached.SetActive(true);
            wiredetached.SetActive(false);
            if (jetpart.attachedTo != -1)
            {
                canvas.SetActive(true);
            }
        }

        private void Update()
        {
            if (Time.time - time >= 1f && jetscript.started)
            {
                time = Time.time;
                RPM.text = "RPM: " + jetscript.RPM;
            }
            else if (!jetscript.started)
            {
                RPM.text = "RPM: 0";
            }

            if (Physics.Raycast(VariableSystem.mainCamera.ScreenPointToRay(Input.mousePosition), out var hitInfo, 1f, maskLayer) && hitInfo.collider.transform.parent.gameObject == buttons)
            {
                VariableSystem.GUIuse.Value = true;
                if (cInput.GetButtonDown("Use") || Input.GetMouseButtonDown(0))
                {
                    Select(hitInfo.collider.gameObject.name);
                }
            }
        }
    }
}
