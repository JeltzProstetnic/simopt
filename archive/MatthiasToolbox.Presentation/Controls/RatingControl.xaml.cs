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
using System.Globalization;
using MatthiasToolbox.Presentation.Converters;

namespace MatthiasToolbox.Presentation.Controls
{
    /// <summary>
    /// Interaktionslogik für RatingControl.xaml
    /// </summary>
    public partial class RatingControl : UserControl
    {
        public IValueConverter RatingConverter
        {
            get { return (IValueConverter)GetValue(RatingConverterProperty); }
            set { SetValue(RatingConverterProperty, value); }
        }

        // Using a DependencyProperty as the backing store for RatingConverter.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty RatingConverterProperty =
            DependencyProperty.Register("RatingConverter", typeof(IValueConverter), typeof(RatingControl), new UIPropertyMetadata(new StarsConverter()));

        public float RatingValue
        {
            get { return (float)GetValue(RatingValueProperty); }
            set { SetValue(RatingValueProperty, value); }
        }

        // Using a DependencyProperty as the backing store for RatingValue.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty RatingValueProperty =
            DependencyProperty.Register("RatingValue", typeof(float), typeof(RatingControl), new UIPropertyMetadata(3f));

        public RatingControl()
        {
            InitializeComponent();
        }

        private void UpdateImage()
        {
            imageRating.Source = (ImageSource)RatingConverter.Convert(RatingValue, typeof(ImageSource), null, CultureInfo.CurrentCulture);
        }

        private void imageRating_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            double mouseX = e.GetPosition(imageRating).X;

            RatingValue = (float)((mouseX / imageRating.ActualWidth) * 6);

            UpdateImage();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateImage();
        }
    }
}