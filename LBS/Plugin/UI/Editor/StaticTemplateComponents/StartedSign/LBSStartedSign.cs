using ISILab.DevTools.Macros;
using ISILab.LBS.VisualElements;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Plugin.VisualElements.Editor.StaticTemplateComponents.StartedSign
{
    [UxmlElement]
    public partial class LBSStartedSign : VisualElement
    {
        private const string URL = "https://isilab-utalca.github.io/ISILab/en/overview/overview.en.html";

        private VisualTreeAsset signPopup;

        public LBSStartedSign()
        {
            signPopup = AssetMacro.LoadAssetByGuid<VisualTreeAsset>("2019cc78f8952b649a6004d15c450b71");
            signPopup.CloneTree(this);

            Label linkLabel = this.Query<Label>("Link");
            linkLabel.RegisterCallback<PointerUpEvent>(onHyperlinkClicked);


            Label nameAndVersion = this.Q<Label>("Label");
            var version = ReadVersion();
            if (version is not null)
            {
                nameAndVersion.text = $"Level Building Sidekick v{version}";
            }
        }

        private void onHyperlinkClicked(PointerUpEvent _evt)
        {
            Application.OpenURL(URL);
        }

        private string ReadVersion()
        {
            string version;
            try
            {
                var json = AssetMacro.LoadAssetByGuid<TextAsset>("0123ec6f222a9234db1dc45e77a47651");

                var aux = JObject.Parse(json.text);

                version = (string)aux["version"];
            }
            catch (Exception args)
            {
                Debug.Log("[LBSStartedSIgn]: " + args.Message);
                version = null;
            }

            return version;
        }

    }
}

