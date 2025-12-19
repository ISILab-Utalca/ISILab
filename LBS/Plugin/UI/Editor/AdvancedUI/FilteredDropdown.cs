using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ISILab.LBS.VisualElements
{
    public class FilteredDropdown : ClassDropDown
    {
        List<Func<object, bool>> filters = new List<Func<object, bool>>();


        public List<Func<object, bool>> Filters
        {
            get => filters;
            private set => filters = value;
        }

        public FilteredDropdown(Func<object, bool>[] filters) : base()
        {
            this.filters = new List<Func<object, bool>>(filters);
        }

        public override void UpdateOptions()
        {
            base.UpdateOptions();
            types = types.Where(t => Filters.TrueForAll(p => p(t))).ToList();
            choices = types.Select(t => t.Name).ToList();
        }
    }
}

