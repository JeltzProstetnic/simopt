using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MatthiasToolbox.Presentation.Controls
{
    /// <summary>
    /// Interaktionslogik für ThreeStateButton.xaml
    /// </summary>
    public partial class ThreeStateButton : Button
    {
        public ImageSource ImageSourceEnabled
        {
            get { return (ImageSource)GetValue(ImageSourceEnabledProperty); }
            set { SetValue(ImageSourceEnabledProperty, value); }
        }

        public static readonly DependencyProperty ImageSourceEnabledProperty =
            DependencyProperty.Register("ImageSourceEnabled", typeof(ImageSource), typeof(ThreeStateButton), new UIPropertyMetadata(null));

        public ImageSource ImageSourcePressed
        {
            get { return (ImageSource)GetValue(ImageSourcePressedProperty); }
            set { SetValue(ImageSourcePressedProperty, value); }
        }

        public static readonly DependencyProperty ImageSourcePressedProperty =
            DependencyProperty.Register("ImageSourcePressed", typeof(ImageSource), typeof(ThreeStateButton), new UIPropertyMetadata(null));

        public ImageSource ImageSourceDisabled
        {
            get { return (ImageSource)GetValue(ImageSourceDisabledProperty); }
            set { SetValue(ImageSourceDisabledProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ImageDisabled.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ImageSourceDisabledProperty =
            DependencyProperty.Register("ImageSourceDisabled", typeof(ImageSource), typeof(ThreeStateButton), new UIPropertyMetadata(null));

        public ThreeStateButton()
        {
            InitializeComponent();
            DataContext = this;
        }
    }
}
