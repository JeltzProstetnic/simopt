using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Xml;
using MatthiasToolbox.Basics.Datastructures.Graph;
using MatthiasToolbox.Basics.Interfaces;
using MatthiasToolbox.GraphDesigner.Factory;
using MatthiasToolbox.GraphDesigner.Interfaces;
using MatthiasToolbox.GraphDesigner.Utilities;
using MatthiasToolbox.GraphDesigner.Events;
using MatthiasToolbox.GraphDesigner.Enumerations;

namespace MatthiasToolbox.GraphDesigner.DiagramDesigner
{
    /// <summary>
    /// DesignerCanvas displays DesignerItem nodes and Connections
    /// </summary>
    public partial class DesignerCanvas : Canvas
    {
        #region over

        protected override void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);

            DragObject dragObject = e.Data.GetData(typeof(DragObject)) as DragObject;

            if (dragObject == null) return;

            Point position = e.GetPosition(this);
            DesignerItem newItem = null;

            if (dragObject.DataContext != null)
            {
                IVertex<Point> vertex = dragObject.DataContext as IVertex<Point>;
                newItem = CreateNode(vertex, position, dragObject.DesiredSize);
            }
            else if (!String.IsNullOrEmpty(dragObject.Xaml))
            {
                newItem = CreateDesignerItem(dragObject);
            }

            if (newItem == null) return;

            if (dragObject.DesiredSize.HasValue)
            {
                Size desiredSize = dragObject.DesiredSize.Value;

                newItem.Width = desiredSize.Width;
                newItem.Height = desiredSize.Height;

                DesignerCanvas.SetLeft(newItem, Math.Max(0, position.X - newItem.Width / 2));
                DesignerCanvas.SetTop(newItem, Math.Max(0, position.Y - newItem.Height / 2));
            }
            else
            {
                DesignerCanvas.SetLeft(newItem, Math.Max(0, position.X));
                DesignerCanvas.SetTop(newItem, Math.Max(0, position.Y));
            }

            AddDesignerItem(newItem);

            e.Handled = true;
        }

        protected override Size MeasureOverride(Size constraint)
        {
            Size size = new Size();

            foreach (UIElement element in this.InternalChildren)
            {
                double left = Canvas.GetLeft(element);
                double top = Canvas.GetTop(element);
                left = double.IsNaN(left) ? 0 : left;
                top = double.IsNaN(top) ? 0 : top;

                //measure desired size for each child
                element.Measure(constraint);

                Size desiredSize = element.DesiredSize;
                if (!double.IsNaN(desiredSize.Width) && !double.IsNaN(desiredSize.Height))
                {
                    size.Width = Math.Max(size.Width, left + desiredSize.Width);
                    size.Height = Math.Max(size.Height, top + desiredSize.Height);
                }
            }
            // add margin 
            size.Width += 10;
            size.Height += 10;
            return size;
        }

        #endregion
        #region cvar

        #region const

        /// <summary>
        /// Minimum gap left of a DesignerItem
        /// </summary>
        public const double GAP_X = 20;
        /// <summary>
        /// Minimum gap top of a DesignerItem
        /// </summary>
        public const double GAP_Y = 20;

        #endregion

        private INodeFactory _nodeFactory;
        private IEdgeFactory _edgeFactory;

        private readonly INodeFactory _defaultNodeFactory = new DefaultNodeFactory();
        private readonly IEdgeFactory _defaultEdgeFactory = new DefaultEdgeFactory();

        private SelectionService _selectionService;

        #endregion
        #region evnt

        public delegate void DesignerItemCreatedHandler(object sender, DesignerItemEventArgs e);
        public event DesignerItemCreatedHandler DesignerItemCreated;

        public delegate void EdgeCreatedHandler(object sender, EdgeEventArgs e);
        public event EdgeCreatedHandler EdgeCreated;

        public delegate void DesignerItemRemovedHandler(object sender, DesignerItemEventArgs e);
        public event DesignerItemRemovedHandler DesignerItemRemoved;

        public delegate void EdgeRemovedHandler(object sender, EdgeEventArgs e);
        public event EdgeRemovedHandler EdgeRemoved;

        #endregion
        #region prop

        internal SelectionService SelectionService
        {
            get { return _selectionService ?? (_selectionService = new SelectionService(this)); }
        }

        public INodeFactory NodeFactory 
        { 
            get 
            {
                if (_nodeFactory == null) return _defaultNodeFactory;
                else return _nodeFactory;
            } 
            set 
            {
                _nodeFactory = value;
            } 
        }

        public IEdgeFactory EdgeFactory
        {
            get
            {
                if (_edgeFactory == null) return _defaultEdgeFactory;
                else return _edgeFactory;
            }
            set 
            {
                _edgeFactory = value;
            }
        }

        #endregion
        #region impl

        /// <summary>
        /// Updates the canvas after adding a designer item.
        /// </summary>
        /// <param name="newItem">The new designer item.</param>
        internal void AddDesignerItem(DesignerItem newItem)
        {
            Canvas.SetZIndex(newItem, this.Children.Count);
            this.Children.Add(newItem);
            newItem.ApplyTemplate();

            //update selection
            this.SelectionService.SelectItem(newItem);
            newItem.Focus();
        }

        /// <summary>
        /// On dragging an object/IVertex into the designer.
        /// </summary>
        /// <param name="template">The template.</param>
        /// <param name="position">The position.</param>
        /// <param name="size">The size.</param>
        /// <returns></returns>
        internal DesignerItem CreateNode(IVertex<Point> template, Point position, Size? size)
        {
            if (template == null) return null;

            //create the designeritem 
            DesignerItem newItem = new DesignerItem();
            IVertex<Point> vertex = NodeFactory.CreateVertex(template, position, size);
            newItem.Content = NodeFactory.CreateUIElement(vertex, template);
            newItem.Vertex = vertex;

            OnDesignerItemCreated(new DesignerItemEventArgs() { Item = newItem });

            return newItem;
        }

        /// <summary>
        /// On dragging a shape into the designer.
        /// </summary>
        /// <param name="dragObject">The drag object or null if the dragObject is empty.</param>
        private DesignerItem CreateDesignerItem(DragObject dragObject)
        {
            DesignerItem newItem = null;
            Object content = XamlReader.Load(XmlReader.Create(new StringReader(dragObject.Xaml)));

            if (content != null)
            {
                newItem = new DesignerItem();
                newItem.Content = content;
            }

            OnDesignerItemCreated(new DesignerItemEventArgs() { Item = newItem });

            return newItem;
        }

        public Connection AddConnection(Connection con, PathRouting pathRouting = PathRouting.Direct, bool doThrowEvent = true)
        {
            Children.Add(con);
            if(doThrowEvent)
                OnEdgeCreated(new EdgeEventArgs() { Edge = con.Edge });
            return con;
        }

        public DesignerItem Add(UIElement o, IVertex<Point> vertex)
        {
            DesignerItem newItem = new DesignerItem();
            newItem.Content = o;
            newItem.Vertex = vertex;

            if (vertex is ISize)
            {
                newItem.Width = ((ISize)vertex).Width;
                newItem.Height = ((ISize)vertex).Height;
            }
            else if (vertex is ISize<Size>)
            {
                newItem.Width = ((ISize<Size>) vertex).Size.Width;
                newItem.Height = ((ISize<Size>) vertex).Size.Height;
            }

            DesignerCanvas.SetLeft(newItem, vertex.Position.X);
            DesignerCanvas.SetTop(newItem, vertex.Position.Y);

            Canvas.SetZIndex(newItem, this.Children.Count);
            this.Children.Add(newItem);
            newItem.ApplyTemplate();

            //update selection
            this.SelectionService.SelectItem(newItem);
            newItem.Focus();

            return newItem;
        }

        /// <summary>
        /// Expands the canvas size to the left and the top. All items will be translated to new positions.
        /// </summary>
        /// <param name="deltaX">The length resized on the left.</param>
        /// <param name="deltaY">The length resized on the top.</param>
        public void ExpandCanvas(double deltaX, double deltaY)
        {
            //translate items
            foreach (DesignerItem designerItem in Children.OfType<DesignerItem>())
            {
                double left = DesignerCanvas.GetLeft(designerItem) + deltaX;
                double top = DesignerCanvas.GetTop(designerItem) + deltaY;
                
                //set new position
                DesignerCanvas.SetLeft(designerItem, left);
                DesignerCanvas.SetTop(designerItem, top);
            }

            this.InvalidateMeasure();
        }

        #region event invokers

        protected void OnDesignerItemCreated(DesignerItemEventArgs e)
        {
            if (this.DesignerItemCreated != null) this.DesignerItemCreated.Invoke(this, e);
        }

        protected void OnDesigneritemRemoved(DesignerItemEventArgs e)
        {
            if (this.DesignerItemRemoved != null) this.DesignerItemRemoved.Invoke(this, e);
        }

        protected void OnEdgeCreated(EdgeEventArgs e)
        {
            if (this.EdgeCreated != null) this.EdgeCreated.Invoke(this, e);
        }

        protected void OnEdgeRemoved(EdgeEventArgs e)
        {
            if(this.EdgeRemoved != null) this.EdgeCreated.Invoke(this, e);
        }

        #endregion
        #endregion

    }
}