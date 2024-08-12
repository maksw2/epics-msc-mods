using UnityEngine;
using MSCLoader;
using OASIS;
using HutongGames.PlayMaker.Actions;
using System.Deployment.Internal;

namespace JetMod
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent (typeof(BoxCollider))]
    public class Package : Interactable
    {
        [HideInInspector]
        public static GameObject FoldingBoxPrefab;
        internal Letter postSystem;
        // It is preferred to call this method in PreLoad
        // Returns true if succeed
        public static bool FindFoldingBoxPrefab()
        {
            var boxes = GameObject.Find("STORE").transform.Find("Boxes");

            // PLAN A: Find foldingBox prefab by finding unopened packages that reference it
            if (boxes.childCount > 0)
            {
                var firstBox = boxes.GetChild(0).GetComponent<PlayMakerFSM>();
                if (firstBox == null) return false;
                firstBox.InitializeFSM();

                if (firstBox.FsmStates.Length < 3) return false;
                var openState = firstBox.FsmStates[2];

                if (openState.Actions.Length < 9) return false;
                var createObjectAction = openState.Actions[8] as CreateObject;
                if (createObjectAction == null) return false;

                FoldingBoxPrefab = createObjectAction.gameObject.Value;
                return FoldingBoxPrefab != null;
            }

            // PLAN B: If that fails, probably was not called from PreLoad and there are no unopened packages, we have to use Resources
            var resx = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var item in resx)
            {
                if (item.name == "FoldingBox")
                {
                    FoldingBoxPrefab = item;
                    return true;
                }
            }

            return false;
        }

        Transform mesh;

        void Start()
        {
            if (FoldingBoxPrefab == null && !FindFoldingBoxPrefab())
            {
                ModConsole.LogError("Package: Failed to find FoldingBox prefab");
            }

            gameObject.MakePickable();
            layerMask = LayerMask.GetMask("Parts");
            mesh = transform.GetChild(0);
        }

        public override void mouseOver()
        {
            CursorGUI.use = true;

            if (cInput.GetKeyDown("Use"))
            {
                CursorGUI.use = false;

                var opened_box = Instantiate(FoldingBoxPrefab);
                opened_box.transform.position = transform.position;
                opened_box.transform.rotation = transform.rotation;
                opened_box.transform.localScale = mesh.localScale;
                postSystem.packageOpened(transform);

                Destroy(gameObject);
            }
        }

        public override void mouseExit()
        {
            CursorGUI.use = false;
        }
    }
}