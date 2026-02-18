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
using System.Windows.Threading;
using MatthiasToolbox.Presentation;

namespace MatthiasToolbox.Geography.Controls
{
    /// <summary>
    /// Interaction logic for WorldMap.xaml
    /// </summary>
    public partial class WorldMap : UserControl
    {
        #region cvar

        private MacroRegion macroRegionUnderMouse;
        private SubRegion subRegionUnderMouse;
        private Country countryUnderMouse;

        private bool foundSomething;
        private bool doubleClickOnCountry = false;

        private Dictionary<BitmapSource, Image> countryShapeImageSources;
        private Dictionary<Country, BitmapSource> countryShapeCountries;
        private Dictionary<BitmapSource, MacroRegion> macroRegionShapeImageSources;
        private Dictionary<SubRegion, BitmapSource> subRegionShapeImageSources;

        private Image lastShapeImage;
        private Image currentShapeImage;

        private Image tmpImg;
        private BitmapSource tmpSrc;

        private byte[] tmpPixels = new byte[1];

        private DispatcherTimer shapeRefreshTimer;

        #endregion

        public WorldMap()
        {
            InitializeComponent();

            countryShapeImageSources = new Dictionary<BitmapSource, Image>();
            countryShapeCountries = new Dictionary<Country, BitmapSource>();
            macroRegionShapeImageSources = new Dictionary<BitmapSource, MacroRegion>();
            subRegionShapeImageSources = new Dictionary<SubRegion, BitmapSource>();
            shapeRefreshTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(100), DispatcherPriority.ApplicationIdle, shapeRefreshTimer_Elapsed, Dispatcher.CurrentDispatcher);

            LoadWorldMaps();
            LoadShapeImages();
        }

        

        /// <summary>
        /// worldmap: if the mouse enters the highlight-shape of a country,
        /// the shapeRefreshTimer will be enabled
        /// </summary>
        private void Shape_MouseEnter(object sender, MouseEventArgs e)
        {
            shapeRefreshTimer.IsEnabled = true;
        }

        /// <summary>
        /// worldmap: if the mouse leaves the highlight-shape of a country,
        /// the shapeRefreshTimer will be disabled
        /// </summary>
        private void Shape_MouseLeave(object sender, MouseEventArgs e)
        {
            shapeRefreshTimer.IsEnabled = false;
        }

        /// <summary>
        /// find a shape image under the mouse, where the pixel under the mouse is not transparent
        /// </summary>
        private void shapeRefreshTimer_Elapsed(object sender, EventArgs e)
        {
            if ((int)tmpImg.ActualWidth == 0) return;

            shapeRefreshTimer.IsEnabled = false;
            foundSomething = false;

            int x = ((int)(Mouse.GetPosition(tmpImg).X * (double)tmpSrc.PixelWidth) / (int)tmpImg.ActualWidth); // + 8; doesn't work either, only the mouse pointer is concerned
            int y = ((int)(Mouse.GetPosition(tmpImg).Y * (double)tmpSrc.PixelHeight) / (int)tmpImg.ActualHeight);

            foreach (KeyValuePair<BitmapSource, MacroRegion> kvp in macroRegionShapeImageSources)
            {
                if (!kvp.Key.IsPixelTransparent(x, y))
                {
                    macroRegionUnderMouse = kvp.Value;
                    break;
                }
            }
            if (macroRegionUnderMouse == null) return; // TODO: remove, set a dummy macro city instead

            foreach (SubRegion sr in macroRegionUnderMouse.SubRegions.Values)
            {
                if (!subRegionShapeImageSources[sr].IsPixelTransparent(x, y))
                {
                    subRegionUnderMouse = sr;
                    break;
                }
            }
            if (subRegionUnderMouse == null) return;

            foreach (Country co in subRegionUnderMouse.Countries.Values)
            {
                if (countryShapeCountries.ContainsKey(co))
                {
                    BitmapSource src = countryShapeCountries[co];
                    if (!src.IsPixelTransparent(x, y))
                    {
                        currentShapeImage = countryShapeImageSources[src];
                        if (currentShapeImage != lastShapeImage)
                        {
                            lastShapeImage.Opacity = 0;
                            currentShapeImage.Opacity = 0.5;
                            lastShapeImage = currentShapeImage;
                        }
                        else
                        {
                            currentShapeImage.Opacity = 0.5;
                            lastShapeImage = currentShapeImage;
                        }
                        foundSomething = true;
                        countryUnderMouse = co;
                        break;
                    }
                }
            }
            if (!foundSomething)
            {
                lastShapeImage.Opacity = 0;
                countryUnderMouse = null;
            }

            shapeRefreshTimer.IsEnabled = true;
        }

        /// <summary>
        /// this function checks if a country on the wordmap is double-clicked;
        /// if that is the case a flag will be set;
        /// </summary>
        void WorldViewCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2 && countryUnderMouse != null)
                doubleClickOnCountry = true;
        }

        /// <summary>
        /// this function is called on releasing the left mouse button over the political map;
        /// it is used to jump to the country tab view if a country has been double-clicked 
        /// </summary>
        private void PoliticalMap_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (doubleClickOnCountry)
            {
                doubleClickOnCountry = false;
                //JumpToCountry(countryUnderMouse);
            }
        }

        private void LoadWorldMaps()
        {
            //string packUri1 = "pack://application:,,,/TravelManager;component/Resources/PoliticalMap.jpg";
            //PoliticalMap.Source = new ImageSourceConverter().ConvertFromString(packUri1) as ImageSource;
            //PoliticalMap.Opacity = 1;

            //string packUri2 = "pack://application:,,,/TravelManager;component/Resources/PoliticalMapColored.jpg";
            //PoliticalMapColored.Source = new ImageSourceConverter().ConvertFromString(packUri2) as ImageSource;
            //PoliticalMapColored.Opacity = 0;

            //string packUri3 = "pack://application:,,,/TravelManager;component/Resources/PoliticalMapFaint.jpg";
            //PoliticalMapFaint.Source = new ImageSourceConverter().ConvertFromString(packUri3) as ImageSource;
            //PoliticalMapFaint.Opacity = 0;
            //// canvas.Children.Add(PoliticalMap);


            //RenderOptions.SetBitmapScalingMode(PoliticalMap, BitmapScalingMode.HighQuality);
            //RenderOptions.SetBitmapScalingMode(PoliticalMapColored, BitmapScalingMode.HighQuality);
            //RenderOptions.SetBitmapScalingMode(PoliticalMapFaint, BitmapScalingMode.HighQuality);
            //RenderOptions.SetBitmapScalingMode(addJourneyIcon, BitmapScalingMode.HighQuality);

            //politicalMapTransform = new TransformGroup();     // erzeugt Transformgroup
            //politicalMapScale = new ScaleTransform();         // verändert den maßstab eines bildes, bzw verzerrt das bild (je nach verhältnis der achsen-werte)
            //politicalMapTranslate = new TranslateTransform(); // verschiebt die pixel eines bildes (dh das ganze bild)
            //politicalMapSource = PoliticalMap.Source as BitmapSource;

            //// order sensitive!
            //politicalMapTransform.Children.Add(politicalMapTranslate);  //added translate zu transform group "politicalMapTransform"
            //politicalMapTransform.Children.Add(politicalMapScale);
            //PoliticalMap.RenderTransform = politicalMapTransform;       //added politicalMapTransformGroup zu PoliticalMap

            //TransformGroup t1 = new TransformGroup();
            //t1.Children.Add(new ScaleTransform(2.155, 2.155));
            //t1.Children.Add(politicalMapTranslate);
            //t1.Children.Add(politicalMapScale);
            //PoliticalMapColored.RenderTransform = t1;

            //TransformGroup t2 = new TransformGroup();
            //t2.Children.Add(new ScaleTransform(3.6633, 3.6633)); // should be 1.33333... but probably this is scaled down before this code is executed
            //t2.Children.Add(politicalMapTranslate);
            //t2.Children.Add(politicalMapScale);
            //PoliticalMapFaint.RenderTransform = t2;

            //canvas.MouseWheel += PoliticalMap_MouseWheel;
            //canvas.PreviewMouseLeftButtonDown += PoliticalMap_MouseLeftButtonDown;
            //canvas.PreviewMouseLeftButtonUp += PoliticalMap_MouseLeftButtonUp;
            //canvas.MouseMove += PoliticalMap_MouseMove;
        }

        private void LoadShapeImages()
        {
            //foreach (MacroRegion mr in GeoDatabase.MacroRegionsByName.Values)
            //{
            //    tmpImg = new Image();
            //    tmpImg.Source = new ImageSourceConverter().ConvertFromString(mr.ShapeFile) as ImageSource;

            //    //FileInfo fi = new FileInfo(mr.ShapeFile);
            //    //tmpImg.Source = new BitmapImage(new Uri(fi.FullName));
            //    TransformGroup t = new TransformGroup();
            //    t.Children.Add(new ScaleTransform(3, 3));
            //    t.Children.Add(politicalMapTranslate);
            //    t.Children.Add(politicalMapScale);
            //    tmpImg.RenderTransform = t;        //added politicalMapTransformGroup zu img
            //    tmpImg.MouseEnter += Shape_MouseEnter;
            //    tmpImg.MouseLeave += Shape_MouseLeave;
            //    tmpImg.Opacity = 0;
            //    canvas.Children.Add(tmpImg);
            //    tmpSrc = tmpImg.Source as BitmapSource;
            //    macroRegionShapeImageSources.Add(tmpSrc, mr);
            //}

            //lastShapeImage = tmpImg; // so that it is not null at the first timer tick

            //foreach (SubRegion sr in GeoDatabase.Instance.SubRegions)
            //{
            //    Image img = new Image();
            //    img.Source = new ImageSourceConverter().ConvertFromString(sr.ShapeFile) as ImageSource;

            //    //FileInfo fi = new FileInfo(sr.ShapeFile);
            //    //img.Source = new BitmapImage(new Uri(fi.FullName));
            //    TransformGroup t = new TransformGroup();
            //    t.Children.Add(new ScaleTransform(3, 3));
            //    t.Children.Add(politicalMapTranslate);
            //    t.Children.Add(politicalMapScale);
            //    img.RenderTransform = t;        //added politicalMapTransformGroup zu img
            //    img.MouseEnter += Shape_MouseEnter;
            //    img.MouseLeave += Shape_MouseLeave;
            //    img.Opacity = 0;
            //    canvas.Children.Add(img);
            //    subRegionShapeImageSources.Add(sr, img.Source as BitmapSource);
            //}

            //foreach (Country c in GeoDatabase.Instance.Countries)
            //{
            //    this.DoEvents();
            //    if (!string.IsNullOrEmpty(c.ShapeFile))
            //    {
            //        Image img = new Image();
            //        img.Source = new ImageSourceConverter().ConvertFromString(c.ShapeFile) as ImageSource;

            //        //FileInfo fi = new FileInfo(c.ShapeFile);
            //        //img.Source = new BitmapImage(new Uri(fi.FullName));

            //        TransformGroup t = new TransformGroup();
            //        t.Children.Add(new ScaleTransform(3, 3));
            //        t.Children.Add(politicalMapTranslate);
            //        t.Children.Add(politicalMapScale);
            //        img.RenderTransform = t; // politicalMapTransform;        //added politicalMapTransformGroup zu img

            //        img.MouseEnter += Shape_MouseEnter;
            //        img.MouseLeave += Shape_MouseLeave;
            //        img.Opacity = 0;
            //        canvas.Children.Add(img);
            //        tmpSrc = img.Source as BitmapSource;
            //        countryShapeImageSources.Add(tmpSrc, img);
            //        countryShapeCountries.Add(c, tmpSrc);
            //    }
            //}

            //canvas.MouseDown += WorldViewCanvas_MouseDown;
        }
    }
}
