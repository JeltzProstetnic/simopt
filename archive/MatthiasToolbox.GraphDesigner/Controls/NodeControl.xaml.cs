using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MatthiasToolbox.GraphDesigner.Interfaces;
using MatthiasToolbox.GraphDesigner.Utilities;
using MatthiasToolbox.Basics.Datastructures.Graph;
using System.ComponentModel;

namespace MatthiasToolbox.GraphDesigner.Controls
{
    /// <summary>
    /// Interaktionslogik für NodeControl.xaml
    /// </summary>
    public partial class NodeControl : UserControl, IConnectable
    {
        private Brush _background = new SolidColorBrush(Colors.Black);
        private Thickness _borderThickness = new Thickness(10);
        private CornerRadius _cornerRadius = new CornerRadius(0);


        public IVertex<Point> Vertex { get; private set; }

        public NodeControl()
        {
            InitializeComponent();
        }

        public NodeControl(IVertex<Point> vertex)
        {
            InitializeComponent();
            //if (vertex is INamedElement) Title = (vertex as INamedElement).Name;

            this.DataContext = vertex;
            this.Vertex = vertex;
        }

        #region INodeControl

        public Brush Background
        {
            get
            {
                return _background;
            }
            set
            {
                _background = value;
                OnNotifyPropertyChanged("Background");
            }
        }


        private void OnNotifyPropertyChanged(string p)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(p));
            }
        }

        //public String Title
        //{
        //    get { return textBoxTitle.Text; }
        //    set 
        //    { 
        //        textBoxTitle.Text = value;
        //        if (string.IsNullOrEmpty(textBoxTitle.Text))
        //        {
        //            textBoxTitle.Visibility = Visibility.Collapsed;
        //            spTitle.Visibility = Visibility.Collapsed;
        //        }
        //        else
        //        {
        //            textBoxTitle.Visibility = Visibility.Visible;
        //            spTitle.Visibility = Visibility.Visible;
        //        }
        //    }
        //}

        public Thickness BorderThickness
        {
            get { return _borderThickness; }
            set
            {
                _borderThickness = value;
                OnNotifyPropertyChanged("BorderThickness");
            }
        }

        public CornerRadius CornerRadius
        {
            get { return _cornerRadius; }
            set
            {
                _cornerRadius = value;
                OnNotifyPropertyChanged("CornerRadius");
            }
        }

        //public string Intro
        //{
        //    get { return textBoxIntro.Text; }
        //    set 
        //    { 
        //        textBoxIntro.Text = value;
        //        if (string.IsNullOrEmpty(textBoxIntro.Text))
        //            textBoxIntro.Visibility = Visibility.Collapsed;
        //        else
        //            textBoxIntro.Visibility = Visibility.Visible;
        //    }
        //}

        //public string Text
        //{
        //    get { return textBoxText.Text; }
        //    set 
        //    { 
        //        textBoxText.Text = value;
        //        if (string.IsNullOrEmpty(textBoxText.Text))
        //            textBoxText.Visibility = Visibility.Collapsed;
        //        else
        //            textBoxText.Visibility = Visibility.Visible;
        //    }
        //}

        //public Visibility SplitterVisibility
        //{
        //    get
        //    {
        //        if (string.IsNullOrEmpty(textBoxIntro.Text) || string.IsNullOrEmpty(textBoxText.Text))
        //            return Visibility.Collapsed;
        //        else return Visibility.Visible;
        //    }
        //}

        //public Visibility TextVisibility
        //{
        //    get
        //    {
        //        if (string.IsNullOrEmpty(textBoxText.Text))
        //            return Visibility.Collapsed;
        //        else return Visibility.Visible;
        //    }
        //}

        //public Visibility IntroVisibility
        //{
        //    get
        //    {
        //        if (string.IsNullOrEmpty(textBoxIntro.Text))
        //            return Visibility.Collapsed;
        //        else return Visibility.Visible;
        //    }
        //}

        //public Visibility SeparatorVisibility
        //{
        //    get
        //    {
        //        if (textBoxIntro == null || (string.IsNullOrEmpty(textBoxIntro.Text) && string.IsNullOrEmpty(textBoxText.Text)))
        //            return Visibility.Collapsed;
        //        else return Visibility.Visible;
        //    }
        //}

        #endregion
        #region IConnectable

        public ConnectionInfo GetInfo()
        {
            return new ConnectionInfo();
        }

        public List<object> Connections
        {
            get { return null; }
        }

        #endregion
        #region INotifyPropertyChanged

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;


        #endregion
    }
}