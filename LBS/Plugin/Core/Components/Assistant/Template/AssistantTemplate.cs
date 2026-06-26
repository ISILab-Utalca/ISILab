using ISILab.LBS.Assistants;
using ISILab.LBS.Plugin.Components.Behaviours;
using ISILab.LBS.Plugin.Components.Data;
using ISILab.LBS.Plugin.Components.Data.Tessellation.TileMap;
using ISILab.LBS.Plugin.Core.AI.Assistant;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Xml.Linq;
using UnityEngine;

public class /*#*/SCRPITNAME/*#*/ : LBSAssistant, IAssistantThreaded
{
    public /*#*/SCRPITNAME/*#*/(string IconGuid, string name, Color colorTint) : base(IconGuid, name, colorTint)
    {

    }

    public void RunAsync(string insideStyle, string outsideStyle, Action<float> onProgress = null, CancellationToken token = default)
    {
        // Cancel Check
        if (((IAssistantThreaded)this).CheckPendingCancel(this, token))
            return;

        // Update progress bar
        onProgress?.Invoke(1);
    }

    public override object Clone()
    {
        return new /*#*/SCRPITNAME/*#*/(IconGuid, Name, ColorTint);
    }

    public override void OnGUI() { }

    public void OnTaskCancelled() { }
}
