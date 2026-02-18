using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using MatthiasToolbox.Basics.Datastructures.Trees;

namespace MatthiasToolbox.Presentation.Controls
{
    public class GenericTreeViewModel
    {
        private readonly ReadOnlyCollection<GenericItemViewModel> _items;

        public ReadOnlyCollection<GenericItemViewModel> Items
        {
            get { return _items; }
        }

        public GenericTreeViewModel(IEnumerable<ITreeItem> items)
        {
            _items = new ReadOnlyCollection<GenericItemViewModel>(
                (from i in items
                 select new GenericItemViewModel(null, i))
                .ToList());
        }
    }
}
