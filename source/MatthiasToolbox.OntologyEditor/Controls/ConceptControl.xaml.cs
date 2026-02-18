using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MatthiasToolbox.Basics.Datastructures.Graph;
using MatthiasToolbox.GraphDesigner.Interfaces;
using MatthiasToolbox.GraphDesigner.Utilities;
using MatthiasToolbox.Semantics.Metamodel;

namespace MatthiasToolbox.OntologyEditor.Controls
{
    /// <summary>
    /// Interaction logic for ConceptControl.xaml
    /// </summary>
    public partial class ConceptControl : UserControl, IConnectable
    {
        private Brush _background = new SolidColorBrush(Colors.Black);
        private Thickness _borderThickness = new Thickness(10);
        private CornerRadius _cornerRadius = new CornerRadius(0);


        public IVertex<Point> Vertex { get; private set; }

        public ConceptControl(Concept vertex)
        {
            this.DataContext = vertex;
            this.Vertex = vertex;
        
            InitializeComponent();
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
