using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using MatthiasToolbox.Player.Data;

namespace MatthiasToolbox.Player
{
    /// <summary>
    /// TextSearchFilter adds a text filter to search a ICollectionView of Clip's when the textbox is edited.
    /// </summary>
    internal class TextSearchFilter
    {

        /// <summary>
        /// Constructor adds the filter delegate to the Collectionview and adds a TextChanged event to the textBox.
        /// </summary>
        /// <param name="filteredView">The CollectionView to search.</param>
        /// <param name="textBox">The search text box.</param>
        public TextSearchFilter(ICollectionView filteredView, TextBox textBox)
        {
            string filterText = "";

            filteredView.Filter = delegate(object obj)
                                      {
                                          if (String.IsNullOrEmpty(filterText))
                                              return true;

                                          Clip clip = obj as Clip;
                                          string str = clip.Label;

                                          if (String.IsNullOrEmpty(str))
                                              return false;

                                          int index = str.IndexOf(filterText, 0, StringComparison.InvariantCultureIgnoreCase);

                                          return index > -1;
                                      };

            textBox.TextChanged += delegate
                                       {
                                           filterText = textBox.Text;
                                           filteredView.Refresh();
                                       };
        }
    }
}
