using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Controls;
using System.Windows.Media;
using System.Globalization;

namespace MatthiasToolbox.Presentation.Converters
{
    public class StarsConverter : IValueConverter
    {
        private static Dictionary<float, string> ratingToFile;
        private static Dictionary<string, float> fileToRating;
        protected static Dictionary<float, Image> ratingToImage;

        public static string star0Path = "";
        public static string star0_5Path = "";
        public static string star1Path = "";
        public static string star1_5Path = "";
        public static string star2Path = "";
        public static string star2_5Path = "";
        public static string star3Path = "";
        public static string star3_5Path = "";
        public static string star4Path = "";
        public static string star4_5Path = "";
        public static string star5Path = "";
        public static string star5_5Path = "";
        public static string star6Path = "";

        static StarsConverter()
        {
            ratingToFile = new Dictionary<float, string>();
            fileToRating = new Dictionary<string, float>();
            ratingToImage = new Dictionary<float, Image>();

            star0Path = "pack://application:,,,/MatthiasToolbox.Presentation;component/Resources/0star.png";
            star0_5Path = "pack://application:,,,/MatthiasToolbox.Presentation;component/Resources/0,5star.png";
            star1Path = "pack://application:,,,/MatthiasToolbox.Presentation;component/Resources/1star.png";
            star1_5Path = "pack://application:,,,/MatthiasToolbox.Presentation;component/Resources/1,5star.png";
            star2Path = "pack://application:,,,/MatthiasToolbox.Presentation;component/Resources/2star.png";
            star2_5Path = "pack://application:,,,/MatthiasToolbox.Presentation;component/Resources/2,5star.png";
            star3Path = "pack://application:,,,/MatthiasToolbox.Presentation;component/Resources/3star.png";
            star3_5Path = "pack://application:,,,/MatthiasToolbox.Presentation;component/Resources/3,5star.png";
            star4Path = "pack://application:,,,/MatthiasToolbox.Presentation;component/Resources/4star.png";
            star4_5Path = "pack://application:,,,/MatthiasToolbox.Presentation;component/Resources/4,5star.png";
            star5Path = "pack://application:,,,/MatthiasToolbox.Presentation;component/Resources/5star.png";
            star5_5Path = "pack://application:,,,/MatthiasToolbox.Presentation;component/Resources/5,5star.png";
            star6Path = "pack://application:,,,/MatthiasToolbox.Presentation;component/Resources/6star.png";

            ratingToFile[0f] = star0Path;
            ratingToFile[0.5f] = star0_5Path;
            ratingToFile[1f] = star1Path;
            ratingToFile[1.5f] = star1_5Path;
            ratingToFile[2f] = star2Path;
            ratingToFile[2.5f] = star2_5Path;
            ratingToFile[3f] = star3Path;
            ratingToFile[3.5f] = star3_5Path;
            ratingToFile[4f] = star4Path;
            ratingToFile[4.5f] = star4_5Path;
            ratingToFile[5f] = star5Path;
            ratingToFile[5.5f] = star5_5Path;
            ratingToFile[6f] = star6Path;

            fileToRating[star0Path] = 0f;
            fileToRating[star0_5Path] = 0.5f;
            fileToRating[star1Path] = 1f;
            fileToRating[star1_5Path] = 1.5f;
            fileToRating[star2Path] = 2f;
            fileToRating[star2_5Path] = 2.5f;
            fileToRating[star3Path] = 3f;
            fileToRating[star3_5Path] = 3.5f;
            fileToRating[star4Path] = 4f;
            fileToRating[star4_5Path] = 4.5f;
            fileToRating[star5Path] = 5f;
            fileToRating[star5_5Path] = 5.5f;
            fileToRating[star6Path] = 6f;


            foreach (KeyValuePair<float, string> kvp in ratingToFile)
            {
                Image img = new Image();
                img.Source = new ImageSourceConverter().ConvertFromString(kvp.Value) as ImageSource;
                ratingToImage[kvp.Key] = img;
            }
        }

        public object Convert(object value,
                           Type targetType,
                           object parameter,
                           CultureInfo culture)
        {
            if (value.GetType() != typeof(float)) throw new ArgumentException("Argument must be of type float.", "value");
            float r = (float)value;
            float rating = 0;

            if (r < 0.25f) rating = 0f;
            else if (r < 0.75f) rating = 0.5f;
            else if (r < 1.25f) rating = 1f;
            else if (r < 1.75f) rating = 1.5f;
            else if (r < 2.25f) rating = 2f;
            else if (r < 2.75f) rating = 2.5f;
            else if (r < 3.25f) rating = 3f;
            else if (r < 3.75f) rating = 3.5f;
            else if (r < 4.25f) rating = 4f;
            else if (r < 4.75f) rating = 4.5f;
            else if (r < 5.25f) rating = 5f;
            else if (r < 5.75f) rating = 5.5f;
            else rating = 6f;

            return ratingToImage[rating].Source;
        }

        public object ConvertBack(object value,
                                  Type targetType,
                                  object parameter,
                                  CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}