using System;
using System.Collections.Generic;
using System.Linq;
using ISILab.LBS.Modules;
using UnityEngine;

namespace ISILab.LBS.Modules
{
    [Serializable]
    public class NoteModule : LBSModule
    {
        [SerializeField, SerializeReference]
        private List<LBSNote> notes = new();

        public List<LBSNote> Notes => notes;

        public NoteModule() : base() { }

        public NoteModule(List<LBSNote> notes) : base()
        {
            this.notes = notes;
        }

        public void AddNote(LBSNote note)
        {
            if (!notes.Contains(note))
                notes.Add(note);
        }

        public bool RemoveNote(LBSNote note)
        {
            return notes.Remove(note);
        }

        public override void Clear()
        {
            notes.Clear();
        }

        public override object Clone()
        {
            return new NoteModule(notes.Select(t => (LBSNote)t.Clone()).ToList());
        }

        public override bool IsEmpty()
        {
            return notes.Count == 0;
        }
    }
}
