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
//using GMap.NET.WindowsPresentation;
//using GMap.NET;

namespace MatthiasToolbox.Geography.Controls
{
    /// <summary>
    /// Interaktionslogik für LocationPickerWindow.xaml
    /// </summary>
    public partial class LocationPickerWindow : Window
    {
        #region cvar

        // private GMapMarker marker;
        private ILocation selectedLocation;

        #endregion
        #region prop

        public bool HasLocation { get { return SelectedLocation != null; } }

        // public bool HasPosition { get { return SelectedPositionLatLng.HasValue; } }

        // public PointLatLng? SelectedPositionLatLng { get; set; }

        public ILocation SelectedLocation
        {
            get
            {
                return worldTree1.SelectedItem;
            }
            set
            {
                selectedLocation = value;

                if (value == null) worldTree1.UnselectAll();
                else
                {
                    if (value is City)
                    {
                        worldTree1.ExpandCountry((value as City).Country);
                    }
                    else if (value is Location)
                    {
                        worldTree1.ExpandCountry((value as Location).Country);
                    }

                    worldTree1.SelectCityOrLocation(value);
                }
            }
        }

        public string EnteredName { get; set; }

        public int Zoom { get; set; }

        #endregion
        #region ctor

        public LocationPickerWindow()
        {
            InitializeComponent();

            //worldTree1.ItemSelected += worldTree1_ItemSelected;
            //mapPickerMap.MouseDoubleClick += mapPickerMap_MouseDoubleClick;

            //mapPickerMap.DragButton = System.Windows.Input.MouseButton.Left;
            //mapPickerMap.MapType = MapType.GoogleTerrain;

            // marker = new GMapMarker();
            Ellipse mark = new Ellipse();
            mark.Width = 8;
            mark.Height = 8;
            mark.Fill = new SolidColorBrush(Colors.Red);
            // marker.Shape = mark;

            // mapPickerMap.Markers.Add(marker);
        }

        #endregion
        #region init

        private void Initialize()
        {
            InitializeMap();

            if (!string.IsNullOrEmpty(EnteredName)) textBoxEnteredName.Text = EnteredName;
            else if (selectedLocation != null)
            {
                SelectedLocation = selectedLocation;
                textBoxEnteredName.Text = SelectedLocation.Name;
            }
        }

        private void InitializeMap()
        {
            //if (Zoom != 0) mapPickerMap.Zoom = Zoom;
            //else mapPickerMap.Zoom = 1;

            //if (SelectedPositionLatLng.HasValue)
            //{
            //    marker.Position = SelectedPositionLatLng.Value;
            //    mapPickerMap.CurrentPosition = SelectedPositionLatLng.Value;
            //}
            //else if (SelectedLocation != null) SetMarkerToLocation();
        }

        #endregion
        #region hand

        #region window

        private void LocationPickerWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // nothing to do yet
        }

        private void LocationPickerWindow_ContentRendered(object sender, EventArgs e)
        {
            Initialize();
        }

        private void LocationPickerWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // nothing to do yet
        }

        private void LocationPickerWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
            }
        }

        #endregion
        #region buttons

        private void buttonOK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        //private void buttonSearch_Click(object sender, RoutedEventArgs e)
        //{
        //    GeoCoderStatusCode status;

        //    PointLatLng? result = GMaps.Instance.GetLatLngFromGeocoder(textBoxSearch.Text, out status);

        //    if (status == GeoCoderStatusCode.G_GEO_SUCCESS && result != null)
        //    {
        //        marker.Position = result.Value;
        //        mapPickerMap.CurrentPosition = result.Value;
        //        SelectedPositionLatLng = result.Value;
        //        SelectedLocation = null;
        //        textBoxEnteredName.Text = textBoxSearch.Text;
        //        textBoxEnteredName.IsEnabled = true;
        //        buttonOK.IsEnabled = true;
        //    }
        //    else
        //    {
        //        // TODO: notify user
        //    }
        //}

        #endregion
        #region tree

        private void worldTree1_ItemSelected(ILocation selectedItem, RoutedPropertyChangedEventArgs<object> e)
        {
            if (selectedItem is City || selectedItem is Location)
            {
                SelectedLocation = selectedItem;
                // SelectedPositionLatLng = null;
                textBoxEnteredName.Text = selectedItem.Name;
                textBoxEnteredName.IsEnabled = false;
                buttonOK.IsEnabled = true;
                SetMarkerToLocation();
            }
            else
            {
                SelectedLocation = null;
                textBoxEnteredName.Text = "";
                buttonOK.IsEnabled = false;
                SetMarkerToZero();
            }
        }

        #endregion
        #region map

        void mapPickerMap_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            //System.Windows.Point selectedPositionPixels = e.GetPosition(mapPickerMap);
            //SelectedPositionLatLng = mapPickerMap.FromLocalToLatLng((int)selectedPositionPixels.X, (int)selectedPositionPixels.Y);
            //marker.Position = SelectedPositionLatLng.Value;
            //textBoxSearch.Text = "";
            //textBoxEnteredName.IsEnabled = true;
            //textBoxEnteredName.Text = SelectedPositionLatLng.Value.ToString();
            //worldTree1.UnselectAll();
            //buttonOK.IsEnabled = true;
        }

        private void mapPickerMap_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // nothing to do
        }

        #endregion
        #region textboxes

        private void textBoxEnteredName_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            EnteredName = textBoxEnteredName.Text;
        }

        private void textBoxSearch_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // TODO: Suchvorschläge?
        }

        #endregion

        #endregion
        #region impl

        private void SetMarkerToLocation()
        {
            //marker.Position = GetSelectedLocationPosition();
            //mapPickerMap.CurrentPosition = marker.Position;
        }

        private void SetMarkerToZero()
        {
            // marker.Position = new PointLatLng(0, 0);
            // reset zoom?
        }

        #endregion
        #region util

        //private PointLatLng GetSelectedLocationPosition()
        //{
        //    if (SelectedLocation.Latitude == 0 && SelectedLocation.Longitude == 0)
        //    {
        //        GeoCoderStatusCode status;
        //        PointLatLng? result = GMaps.Instance.GetLatLngFromGeocoder(GetSelectedLocationSearchName(), out status);

        //        if (status == GeoCoderStatusCode.G_GEO_SUCCESS && result.HasValue)
        //        {
        //            SelectedLocation.Latitude = result.Value.Lat;
        //            SelectedLocation.Longitude = result.Value.Lng;
        //            GeoDatabase.Instance.SubmitChanges();
        //            return result.Value;
        //        }
        //        else
        //        {
        //            return new PointLatLng(0, 0);
        //        }
        //    }
        //    else return new PointLatLng(SelectedLocation.Latitude, SelectedLocation.Longitude);
        //}

        private string GetSelectedLocationSearchName()
        {
            if (SelectedLocation is City)
            {
                string cityNameOnly = (SelectedLocation as City).Name;

                if (cityNameOnly.Contains(","))
                {
                    int comma = cityNameOnly.IndexOf(',');
                    cityNameOnly = cityNameOnly.Substring(0, comma);
                }

                return cityNameOnly + "," + (SelectedLocation as City).Country.ShortName;
            }
            else if (SelectedLocation is Location)
            {
                return SelectedLocation.Name + "," + (SelectedLocation as Location).Country.ShortName;
            }
            else return "";
        }

        #endregion
    }
}