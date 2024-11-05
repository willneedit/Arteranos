/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Core;
using ProtoBuf;
using UnityEngine;

namespace Arteranos.WorldEdit.Components
{

    [ProtoContract]
    public class WOCTeleportButton : WOCBase, IClickable
    {
        [ProtoMember(1)]
        public string location = null;
        [ProtoMember(2)]
        public string targetWorldCid = null;
        [ProtoMember(3)]
        public bool inServer = false;

        public void SetState()
        {
            Dirty = true;
        }
        public override object Clone()
        {
            return MemberwiseClone();
        }

        public override (string name, GameObject gameObject) GetUI()
            => ("Teleport Button", BP.I.WorldEdit.TeleportButtonInspector);

        public override void ReplaceValues(WOCBase wOCBase)
        {
            WOCTeleportButton s = wOCBase as WOCTeleportButton;

            s.location = location;
            s.targetWorldCid = targetWorldCid;
            s.inServer = inServer;
        }

        // ---------------------------------------------------------------
        public void GotClicked()
        {
            if(targetWorldCid != null)
            {
                Debug.Log("Cross-world teleportation is not implemented yet");
                return;
            }    

            GameObject tgt = FindTeleportTarget(location);

            if(!tgt)
            {
                Debug.Log($"{location} not found.");
                return;
            }

            // Initiate teleport, according the user's settings.
            G.XRControl.Teleport(tgt.transform.position, tgt.transform.rotation);

            //if (G.Me != null) G.Me.GotObjectClicked(WorldEditorData.GetPathFromObject(GameObject.transform));
            //else ServerGotClicked();
        }

        // No server action (for now), because positioning is up to the authorized clients.
        public void ServerGotClicked() =>
            throw new System.NotImplementedException();

        private static GameObject FindTeleportTarget(string location, Transform node)
        {
            if (node.TryGetComponent(out WorldObjectComponent woc) 
                && woc.TryGetWOC(out WOCTeleportMarker tm) 
                && tm.location == location) return node.gameObject;

            for (int i = 0; i < node.childCount; ++i)
            {
                GameObject res = FindTeleportTarget(location, node.GetChild(i));
                if (res) return res;
            }

            return null;
        }

        public static GameObject FindTeleportTarget(string location)
            => FindTeleportTarget(location, WorldEditorData.FindObjectByPath(null));
    }
}