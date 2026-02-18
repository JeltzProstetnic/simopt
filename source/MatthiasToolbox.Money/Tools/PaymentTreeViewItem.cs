using System.Collections.Generic;
using System.Linq;
using MatthiasToolbox.Money.Data;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace MatthiasToolbox.Money.Tools
{
    public class PaymentTreeViewItem : ListBox, INotifyPropertyChanged
    {
        #region cvar

        private static int instanceCounter = 0;
        private List<PaymentTreeViewItem> children;

        #endregion
        #region prop

        public int ID { get; private set; }
        public int TreeOrderID { get; private set; }
        public bool IsParent { get; private set; }

        public IEnumerable<PaymentTreeViewItem> Children { get { return children; } }
        public new bool HasItems { get { return children.Count > 0; } }

        #region propdp

        public static readonly DependencyProperty ContentProperty =
            DependencyProperty.Register("Content", typeof(object), typeof(PaymentTreeViewItem), new UIPropertyMetadata("empty"));
        public object Content
        {
            get { return (object)GetValue(ContentProperty); }
            set { SetValue(ContentProperty, value); }
        }

        // public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register("IsSelected", typeof(bool), typeof(PaymentTreeViewItem));
        [Bindable(true)]
        public bool IsSelected
        {
            get { return (bool)GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }

        public static readonly DependencyProperty IsExpandedProperty =
            DependencyProperty.Register("IsExpanded", typeof(bool?), typeof(PaymentTreeViewItem), new UIPropertyMetadata(false));
        public bool? IsExpanded
        {
            get { return (bool?)GetValue(IsExpandedProperty); }
            set { SetValue(IsExpandedProperty, value); }
        }

        public static readonly DependencyProperty HasHeaderProperty = DependencyProperty.Register("HasHeader", typeof(bool), typeof(PaymentTreeViewItem));
        [Bindable(false), Browsable(false)]
        public bool HasHeader
        {
            get { return (bool)GetValue(HasHeaderProperty); }
        }

        public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register("Header", typeof(object), typeof(PaymentTreeViewItem));
        [Bindable(true)]
        public object Header
        {
            get { return GetValue(HeaderProperty); }
            set { SetValue(HeaderProperty, value); }
        }

        public static readonly DependencyProperty HeaderStringFormatProperty = DependencyProperty.Register("HeaderStringFormat", typeof(string), typeof(PaymentTreeViewItem));
        [Bindable(true)]
        public string HeaderStringFormat
        {
            get { return (string)GetValue(HeaderStringFormatProperty); }
            set { SetValue(HeaderStringFormatProperty, value); }
        }

        public static readonly DependencyProperty HeaderTemplateProperty = DependencyProperty.Register("HeaderTemplate", typeof(DataTemplate), typeof(PaymentTreeViewItem));
        [Bindable(true)]
        public DataTemplate HeaderTemplate
        {
            get { return (DataTemplate)GetValue(HeaderTemplateProperty); }
            set { SetValue(HeaderTemplateProperty, value); }
        }

        public static readonly DependencyProperty HeaderTemplateSelectorProperty = DependencyProperty.Register("HeaderTemplateSelector", typeof(DataTemplateSelector), typeof(PaymentTreeViewItem));
        [Bindable(true)]
        public DataTemplateSelector HeaderTemplateSelector
        {
            get { return (DataTemplateSelector)GetValue(HeaderTemplateSelectorProperty); }
            set { SetValue(HeaderTemplateSelectorProperty, value); }
        }

        #region unused

        //public static readonly DependencyProperty FocusVisualStyleProperty = DependencyProperty.Register("FocusVisualStyle", typeof(Style), typeof(PaymentTreeViewItem));
        //[Bindable(true)]
        //public Style FocusVisualStyle
        //{
        //    get { return (Style)GetValue(FocusVisualStyleProperty); }
        //    set { SetValue(FocusVisualStyleProperty, value); }
        //}

        //public static readonly DependencyProperty TemplateProperty = DependencyProperty.Register("Template", typeof(Style), typeof(PaymentTreeViewItem));
        //[Bindable(true)]
        //public ControlTemplate Template
        //{
        //    get { return (ControlTemplate)GetValue(TemplateProperty); }
        //    set { SetValue(TemplateProperty, value); }
        //}

        //public static readonly DependencyProperty BackgroundProperty = DependencyProperty.Register("Background", typeof(Brush), typeof(PaymentTreeViewItem));
        //[Bindable(true)]
        //public Brush Background
        //{
        //    get { return (Brush)GetValue(BackgroundProperty); }
        //    set { SetValue(BackgroundProperty, value); }
        //}

        //public static readonly DependencyProperty ForegroundProperty = DependencyProperty.Register("Foreground", typeof(Brush), typeof(PaymentTreeViewItem));
        //[Bindable(true)]
        //public Brush Foreground
        //{
        //    get { return (Brush)GetValue(ForegroundProperty); }
        //    set { SetValue(ForegroundProperty, value); }
        //}

        //public static readonly DependencyProperty BorderBrushProperty = DependencyProperty.Register("BorderBrush", typeof(Brush), typeof(PaymentTreeViewItem));
        //[Bindable(true)]
        //public Brush BorderBrush
        //{
        //    get { return (Brush)GetValue(BorderBrushProperty); }
        //    set { SetValue(BorderBrushProperty, value); }
        //}

        //public static readonly DependencyProperty BorderThicknessProperty = DependencyProperty.Register("BorderThickness", typeof(Thickness), typeof(PaymentTreeViewItem));
        //[Bindable(true)]
        //public Thickness BorderThickness
        //{
        //    get { return (Thickness)GetValue(BorderThicknessProperty); }
        //    set { SetValue(BorderThicknessProperty, value); }
        //}
        
        #endregion

        #endregion

        #endregion
        #region evnt

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
        #region ctor

        static PaymentTreeViewItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PaymentTreeViewItem), new FrameworkPropertyMetadata(typeof(PaymentTreeViewItem)));
        }

        private PaymentTreeViewItem(int id, string name)
        {
            children = new List<PaymentTreeViewItem>();
            ID = id;
            TreeOrderID = instanceCounter++;
            Name = name;
            Header = name;
            base.SelectionMode = System.Windows.Controls.SelectionMode.Multiple;
            Content = Name;
        }

        public PaymentTreeViewItem(PaymentGroup pg)
            : this(pg.ID, pg.Name)
        {
            foreach(PaymentSubGroup psg in pg.SubGroupsByID.Values)
                children.Add(new PaymentTreeViewItem(psg));
            IsParent = true;
            IsExpanded = true;
        }

        public PaymentTreeViewItem(PaymentSubGroup pg)
            : this(pg.ID, pg.Name)
        {
            IsParent = false;
            IsExpanded = false;
        }

        #endregion
        #region impl

        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}