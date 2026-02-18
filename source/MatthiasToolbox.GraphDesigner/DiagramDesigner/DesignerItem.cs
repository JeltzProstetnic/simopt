using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using MatthiasToolbox.Basics.Interfaces;
using MatthiasToolbox.GraphDesigner.Controls;
using MatthiasToolbox.GraphDesigner.Interfaces;
using MatthiasToolbox.Presentation.Interfaces;
using System.Windows.Input;
using System.Windows.Media;
using MatthiasToolbox.Basics.Datastructures.Graph;

namespace MatthiasToolbox.GraphDesigner.DiagramDesigner
{
    //These attributes identify the types of the named parts that are used for templating
    [TemplatePart(Name = "PART_ResizeDecorator", Type = typeof(Control))]
    [TemplatePart(Name = "PART_ContentPresenter", Type = typeof(ContentPresenter))]
    public class DesignerItem : ContentControl, ISelectable, IGroupable, IPosition<Point>
    {
        #region cvar

        private readonly List<Connection> _connections;

        #endregion
        #region ID

        private readonly Guid _id;

        public Guid ID
        {
            get { return _id; }
        }

        #endregion
        #region ParentID

        public Guid ParentID
        {
            get { return (Guid)GetValue(ParentIDProperty); }
            set { SetValue(ParentIDProperty, value); }
        }

        public static readonly DependencyProperty ParentIDProperty = DependencyProperty.Register("ParentID", typeof(Guid), typeof(DesignerItem));

        #endregion
        #region IsGroup

        public bool IsGroup
        {
            get { return (bool)GetValue(IsGroupProperty); }
            set { SetValue(IsGroupProperty, value); }
        }

        public static readonly DependencyProperty IsGroupProperty = DependencyProperty.Register("IsGroup", typeof(bool), typeof(DesignerItem));

        #endregion
        #region IsSelected

        public bool IsSelected
        {
            get { return (bool)GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }
        public static readonly DependencyProperty IsSelectedProperty =
          DependencyProperty.Register("IsSelected",
                                       typeof(bool),
                                       typeof(DesignerItem),
                                       new FrameworkPropertyMetadata(false));

        #endregion
        #region ConnectorDecoratorTemplate

        // can be used to replace the default template for the ConnectorDecorator
        public static readonly DependencyProperty ConnectorDecoratorTemplateProperty =
            DependencyProperty.RegisterAttached("ConnectorDecoratorTemplate", typeof(ControlTemplate), typeof(DesignerItem));

        public static ControlTemplate GetConnectorDecoratorTemplate(UIElement element)
        {
            return (ControlTemplate)element.GetValue(ConnectorDecoratorTemplateProperty);
        }

        public static void SetConnectorDecoratorTemplate(UIElement element, ControlTemplate value)
        {
            element.SetValue(ConnectorDecoratorTemplateProperty, value);
        }

        #endregion
        #region IsDragConnectionOver

        // while drag connection procedure is ongoing and the mouse moves over 
        // this item this value is true; if true the ConnectorDecorator is triggered
        // to be visible, see template
        public bool IsDragConnectionOver
        {
            get { return (bool)GetValue(IsDragConnectionOverProperty); }
            set { SetValue(IsDragConnectionOverProperty, value); }
        }

        public static readonly DependencyProperty IsDragConnectionOverProperty =
            DependencyProperty.Register("IsDragConnectionOver",
                                         typeof(bool),
                                         typeof(DesignerItem),
                                         new FrameworkPropertyMetadata(false));



        #endregion
        #region prop

        public IVertex<Point> Vertex { get; internal set; }

        public IEnumerable<Connection> Connections { get { return _connections; } }

        /// <summary>
        /// Position of the DesignerItem on the Canvas.
        /// </summary>
        public Point Position
        {
            get { return new Point(Canvas.GetLeft(this), Canvas.GetTop(this)); }
            set
            {
                Canvas.SetLeft(this, value.X);
                Canvas.SetTop(this, value.Y);
            }
        }

        /// <summary>
        /// Gets or sets the actual rendered size.
        /// </summary>
        /// <value>The actual size.</value>
        public Size ActualSize
        {
            get { return new Size(ActualWidth, ActualHeight); }
            set
            {
                Width = value.Width;
                Height = value.Height;
            }
        }

        #endregion
        #region ctor

        static DesignerItem()
        {
            // set the key to reference the style for this control
            FrameworkElement.DefaultStyleKeyProperty.OverrideMetadata(typeof(DesignerItem), new FrameworkPropertyMetadata(typeof(DesignerItem)));
        }

        public DesignerItem(Guid id)
        {
            this._id = id;
            this._connections = new List<Connection>();
            this.Loaded += new RoutedEventHandler(DesignerItem_Loaded);
        }

        public DesignerItem()
            : this(Guid.NewGuid())
        {
        }

        #endregion
        #region impl
        #region evnt
        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);
            DesignerCanvas designer = VisualTreeHelper.GetParent(this) as DesignerCanvas;

            // update selection
            if (designer != null)
            {
                if (Keyboard.Modifiers == ModifierKeys.Shift) //don't focus when user holds shift
                {
                    e.Handled = false;
                    return;
                }
                else if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.None)
                {
                    if (this.IsSelected)
                    {
                        designer.SelectionService.RemoveFromSelection(this);
                    }
                    else
                    {
                        designer.SelectionService.AddToSelection(this);
                    }
                }
                else if (!this.IsSelected)
                {
                    designer.SelectionService.SelectItem(this);
                }
                Focus();
            }

            e.Handled = false;
        }

        void DesignerItem_Loaded(object sender, RoutedEventArgs e)
        {
            if (base.Template == null)
                return;

            ContentPresenter contentPresenter = this.Template.FindName("PART_ContentPresenter", this) as ContentPresenter;
            if (contentPresenter == null)
                return;

            UIElement contentVisual = VisualTreeHelper.GetChild(contentPresenter, 0) as UIElement;
            if (contentVisual == null)
                return;
        }
        #endregion
        #region connections

        public void AddConnection(Connection connection)
        {
            this._connections.Add(connection);
        }

        public void RemoveConnection(Connection connection)
        {
            this._connections.Remove(connection);
        }

        #endregion
        #region vertex

        /// <summary>
        /// Updates the Vertex position.
        /// </summary>
        public virtual void UpdateVertexPosition()
        {
            Vertex.Position = Position;
        }

        #endregion
        #endregion
    }
}