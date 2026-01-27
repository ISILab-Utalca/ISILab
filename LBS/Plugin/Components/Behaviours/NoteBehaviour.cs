using System.Collections.Generic;
using System.Linq;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Components;
using ISILab.LBS.Modules;
using LBS.Components;
using UnityEngine;

namespace ISILab.LBS.Behaviours
{
    [System.Serializable]
    [RequieredModule(typeof(NoteModule))]
    public class NoteBehaviour : LBSBehaviour
    {
        //ColorTint no se usa, en ningún behaviour aparentemente 14/01/2026

        public NoteBehaviour(string IconGuid, string name, Color colorTint) : base(IconGuid, name, colorTint) { }

        public NoteModule NoteModule => OwnerLayer?.GetModule<NoteModule>();

        public List<LBSNote> Notes => NoteModule?.Notes;

        public void AddNote(LBSNote note)
        {
            NoteModule?.AddNote(note);
            RequestTilePaint(note);
        }

        public bool RemoveNote(LBSNote note)
        {
            if (NoteModule?.RemoveNote(note) == true)
            {
                RequestTileRemove(note);
                return true;
            }
            return false;
        }

        public override object Clone()
        {
            var clone = new NoteBehaviour(IconGuid, Name, ColorTint);

            return clone;
        }

        public override void OnAttachLayer(LBSLayer layer)
        {
            OwnerLayer = layer;
            layer.OnChange += UpdateKeys;
        }

        public override void OnDetachLayer(LBSLayer layer)
        {
            OwnerLayer = null;
            layer.OnChange -= UpdateKeys;
        }

        public override void CheckKeys()
        {
            UpdateKeys(Notes.ToList<object>());
        }

        public void UpdateKeys()
        {
            UpdateKeys(Notes.ToList<object>());
        }

        public override void OnGUI() { }
    }
}