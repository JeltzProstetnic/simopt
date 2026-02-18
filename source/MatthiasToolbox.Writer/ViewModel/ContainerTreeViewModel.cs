using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Writer.DataModel;

namespace MatthiasToolbox.Writer.ViewModel
{
    public class ContainerTreeViewModel : DocumentTreeViewModel
    {
        readonly Container container;

        public ContainerTreeViewModel(Container container, DocumentTreeViewModel parent = null)
            : base(parent, container, true)
        {
            this.container = container;
        }

        protected override void LoadChildren()
        {
            foreach(Container container in this.container.SubContainers)
                base.Children.Add(new ContainerTreeViewModel(container, this));
            base.LoadChildren();
        }
    }
}
