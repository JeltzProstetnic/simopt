using System.Windows;
using System.Windows.Controls;
using MatthiasToolbox.Presentation.Controls.DiagramDesigner;

namespace MatthiasToolbox.GraphDesigner.DiagramDesigner
{
    /// <summary>
    /// Implements ItemsControl for ToolboxItems  
    /// </summary>
    public class Toolbox : ItemsControl
    {
        private Size itemSize = new Size(50, 50);

        /// Defines the ItemHeight and ItemWidth properties of
        /// the WrapPanel used for this Toolbox
        public Size ItemSize
        {
            get { return itemSize; }
            set { itemSize = value; }
        }

        /// <summary>
        /// Creates or identifies the element that is used to display the given item. 
        /// </summary>
        /// <returns></returns>       
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new ToolboxItem();
        }

        /// <summary>
        /// Determines if the specified item is (or is eligible to be) its own container.  
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>      
        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return (item is ToolboxItem);
        }
    }
}
