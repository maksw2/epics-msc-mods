using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HutongGames.PlayMaker;
using JetMod;
using MSCLoader;
using UnityEngine;

namespace JetMod
{
    public class BuyHook : FsmStateAction
    {
        public ShopManager shop;

        public override void OnEnter()
        {
            shop.ItemsPurchased();
            Finish();
        }
    }

    public class ResetHook : FsmStateAction
    {
        public ShopManager shop;

        public override void OnEnter()
        {
            shop.ResetItems();
            Finish();
        }
    }

    public class ShopManager : MonoBehaviour
    {
        FsmBool buy;
        FsmString item;
        FsmBool open;
        FsmFloat total;
        Transform deskspawnpoint;
        PlayMakerFSM register;
        Camera camera;
        bool updatemoney;

        // setup in editor
        public GameObject letter;
        public MeshRenderer[] items;
        public GameObject[] disableme;

        public class Shelfitem
        {
            public Part partData;
            public OASIS.BasePart partInfo;
            public int Price;
            public string Name;
            public bool BeingBought;
            public bool Purchased;
            public GameObject ItemObj;
            public GameObject ShelfObj;
        }

        public List<Shelfitem> shelfitems = new List<Shelfitem>
    {
        new Shelfitem
        {
            Price = 25000,
            Name = "jet",
            BeingBought = false,
            Purchased = false
        },
        new Shelfitem
        {
            Price = 800,
            Name = "bracket",
            BeingBought = false,
            Purchased = false
        },
        new Shelfitem
        {
            Price = 750,
            Name = "waterpump",
            BeingBought = false,
            Purchased = false
        },
        new Shelfitem
        {
            Price = 5000,
            Name = "controller",
            BeingBought = false,
            Purchased = false
        },
        new Shelfitem
        {
            Price = 50,
            Name = "waterpump line in",
            BeingBought = false,
            Purchased = false
        },
        new Shelfitem
        {
            Price = 50,
            Name = "waterpump line out",
            BeingBought = false,
            Purchased = false
        },
        new Shelfitem
        {
            Price = 100,
            Name = "waterpump wiring",
            BeingBought = false,
            Purchased = false
        }
    };

        public void ResetItems()
        {
            for (int i = 0; i < items.Length; i++)
            {
                MeshRenderer coll = items[i];
                Shelfitem shelfitem = shelfitems.Where((Shelfitem p) => p.Name == coll.gameObject.name).SingleOrDefault();

                shelfitem.BeingBought = false;
                coll.enabled = true;
                GameObject child = disableme[i];
                if (child) child.SetActive(true);
            }
        }

        IEnumerator SpawnItems()
        {
            for (int i = 0; i < shelfitems.Count; i++)
            {
                Shelfitem shelfitem = shelfitems[i];
                if (shelfitem.BeingBought)
                {
                    shelfitem.BeingBought = false;
                    // Refund and dont do anything if this part was attached to the car already
                    if (shelfitem.partInfo.attachedTo != -1)
                    {
                        VariableSystem.PlayerMoney.Value = VariableSystem.PlayerMoney.Value + shelfitem.Price;
                        continue;
                    }
                    shelfitem.Purchased = true;
                    shelfitem.partData.purchased = true;
                    shelfitem.ShelfObj.SetActive(false);
                    shelfitem.ItemObj.SetActive(true);
                    shelfitem.ItemObj.transform.position = deskspawnpoint.position;
                    // Letter spawn
                    if (shelfitem.Name == "jet")
                    {
                        letter.SetActive(true);
                        letter.transform.position = deskspawnpoint.position;
                    }
                }
                yield return new WaitForSeconds(0.2f);
            }
            ResetItems();
        }

        public void ItemsPurchased()
        {
            StartCoroutine(SpawnItems());
        }

        void Start()
        {
            deskspawnpoint = GameObject.Find("REPAIRSHOP").transform.Find("LOD/Store/PartSpawn");
            register = GameObject.Find("REPAIRSHOP").transform.Find("LOD/Store/ShopCashRegister/Register").GetPlayMaker("Data");
            total = register.FsmVariables.GetFsmFloat("Total");
            register.GetState("Purchase").InsertAction(0, new BuyHook
            {
                shop = this
            });
            register.GetState("Reset order").InsertAction(0, new ResetHook
            {
                shop = this
            });
            open = GameObject.Find("REPAIRSHOP").GetPlayMaker("OpeningHours").FsmVariables.GetFsmBool("OpenRepairShop");
            buy = PlayMakerGlobals.Instance.Variables.GetFsmBool("GUIbuy");
            item = PlayMakerGlobals.Instance.Variables.GetFsmString("GUIinteraction");
            camera = GameObject.Find("PLAYER").transform.FindChild("Pivot/AnimPivot/Camera/FPSCamera/FPSCamera").GetComponent<Camera>();
        }

        void Update()
        {
            if (updatemoney) register.SendEvent("PURCHASE");
            updatemoney = false;
            for (int i = 0; i < items.Length; i++)
            {
                MeshRenderer coll = items[i];
                Ray ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

                if (!Physics.Raycast(ray, out var hitInfo) || !open.Value) continue;
                GameObject gameObject = hitInfo.collider.gameObject;
                float distance = hitInfo.distance;

                if (!(distance <= 0.5f) || !(gameObject.gameObject == coll.gameObject)) continue;
                buy.Value = true;
                Shelfitem shelfitem = shelfitems.Where((Shelfitem p) => p.Name == coll.gameObject.name).SingleOrDefault();
                item.Value = shelfitem.Name + " " + shelfitem.Price + " MK";

                if (Input.GetMouseButtonDown(0) && !shelfitem.BeingBought)
                {
                    total.Value += shelfitem.Price;
                    shelfitem.BeingBought = true;
                    coll.enabled = false;
                    GameObject child = disableme[i];
                    if (child) child.SetActive(false);
                    updatemoney = true;
                }

                if (Input.GetMouseButtonDown(1) && shelfitem.BeingBought)
                {
                    total.Value -= shelfitem.Price;
                    shelfitem.BeingBought = false;
                    coll.enabled = true;
                    GameObject child = disableme[i];
                    if (child) child.SetActive(true);
                    updatemoney = true;
                }
            }
        }
    }
}