using System;
using System.Collections;
namespace MatthiasToolbox.GraphDesigner.Interfaces
{
    public interface IPalette
    {
        IEnumerable ItemsSource { get; set; }
        /// <summary>
        /// Replaces the ItemsSource with a new collection of items.
        /// </summary>
        /// <param name="allItems">New collection ofitems.</param>
        void UpdateItems(IEnumerable allItems);
        string Title { get; set; }
    }
}
