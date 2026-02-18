/*
 * Erstellt mit SharpDevelop.
 * Benutzer: Matthias
 * Datum: 16.11.2010
 * Zeit: 17:30
 * 
 * Sie können diese Vorlage unter Extras > Optionen > Codeerstellung > Standardheader ändern.
 */
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using MatthiasToolbox.Logging;
using MatthiasToolbox.Player.Data;
using MatthiasToolbox.Player.Model;
using MatthiasToolbox.Utilities;
using System.IO;
using ListViewItem = System.Windows.Controls.ListViewItem;
using Timer = System.Windows.Forms.Timer;
using MatthiasToolbox.Utilities.Shell;

namespace MatthiasToolbox.Player
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        #region cvar

        Point? _dragStart;
        KeyboardListener _keyboardListener = new KeyboardListener();

        private readonly PlayerPresentationModel _model;
        private readonly Timer _timer;
        private bool _seeking = false;

        #endregion
        #region prop


        public PlayerPresentationModel Model { get { return this._model; } }

        public MediaLibrary Library { get; private set; }

        public double Volume
        {
            get { return (double)GetValue(VolumeProperty); }
            set { SetValue(VolumeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Volume.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty VolumeProperty =
            DependencyProperty.Register("Volume", typeof(double), typeof(Window1), new UIPropertyMetadata(0.5,
                new PropertyChangedCallback(VolumeChanged), VolumeCoerce));

        /// <summary>
        /// Coerce the Volume
        /// </summary>
        /// <param name="d"></param>
        /// <param name="basevalue"></param>
        /// <returns></returns>
        private static object VolumeCoerce(DependencyObject d, object basevalue)
        {
            double value = (double)basevalue;

            if (value < 0)
                value = 0;
            if (value > 1)
                value = 1;

            return value;
        }

        private static void VolumeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Window1 window = d as Window1;
            if (window.currentMedia == null)
                return;

            double value = (double)e.NewValue;
            window.currentMedia.Volume = value;
        }


        public Clip CurrentClip
        {
            get { return (Clip)GetValue(CurrentClipProperty); }
            set { SetValue(CurrentClipProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CurrentClip.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CurrentClipProperty =
            DependencyProperty.Register("CurrentClip", typeof(Clip), typeof(Window1), new UIPropertyMetadata(null));

        public ObservableCollection<Clip> PlayedClips { get { return this.Model.PlayedClips; } }

        public bool IsRandom
        {
            get { return (bool)GetValue(IsRandomProperty); }
            set { SetValue(IsRandomProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsRandom.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsRandomProperty =
            DependencyProperty.Register("IsRandom", typeof(bool), typeof(Window1), new UIPropertyMetadata(false));

        private Key _keyPressed;
        private ModifierKeys _keyPressedModifieres;

        #endregion prop

        public delegate void AddDirectoryDelegate(string directory);

        public Window1()
        {
            this._model = new PlayerPresentationModel(this);
            this.DataContext = this._model;

            this.Library = new MediaLibrary();

            InitializeComponent();

            currentMedia.Opacity = 0;
            currentMedia.LoadedBehavior = MediaState.Manual;
            currentMedia.UnloadedBehavior = MediaState.Stop;
            currentMedia.MediaOpened += MediaOpened;

            _timer = new Timer();
            _timer.Interval = 250;
            _timer.Tick += new EventHandler(t_Tick);
            _timer.Start();

        }

        void t_Tick(object sender, EventArgs e)
        {
            if (_seeking) return;
            if (currentMedia != null)
            {
                timelineSlider.ToolTip = currentMedia.Position;
                timelineSlider.Value = currentMedia.Position.TotalMilliseconds;
                if (currentMedia.NaturalDuration.HasTimeSpan)
                {
                    textBlockDuration.Text = currentMedia.Position.ToString().Substring(0, 8) + " / " + currentMedia.NaturalDuration.TimeSpan.ToString().Substring(0, 8);
                }
            }
        }

        private void SliderOpacity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            this.Opacity = sliderOpacity.Value / 100;
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            Model.ShowAddFolderDialog();
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (Database.OpenInstance.PlayListExists("Default"))
            {
                this.Model.CurrentPlayList = Database.OpenInstance.GetPlayList("Default");
                this.Model.CurrentPlayList.Initialize();
            }
            else
            {
                this.Model.CurrentPlayList = Database.OpenInstance.CreatePlayList("Default");
            }

            this.Model.RefreshPlaylist();

            //hook to global media keys
            this._keyboardListener.KeyDown += new RawKeyEventHandler(_keyboardListener_KeyDown);
        }

        void _keyboardListener_KeyDown(object sender, RawKeyEventArgs args)
        {
            switch (args.Key)
            {
                case Key.MediaNextTrack:
                    Dispatcher.Invoke(DispatcherPriority.Normal, new Action(Skip));
                    break;
                case Key.MediaPlayPause:
                    Dispatcher.Invoke(DispatcherPriority.Normal, new Action(PlayPause));
                    break;
                case Key.MediaPreviousTrack:
                    Dispatcher.Invoke(DispatcherPriority.Normal, new Action(Prev));
                    break;
                case Key.MediaStop:
                    Dispatcher.Invoke(DispatcherPriority.Normal, new Action(Stop));
                    break;
                case Key.VolumeDown:
                    Dispatcher.Invoke(DispatcherPriority.Normal, new Action(VolumeDown));
                    break;
                case Key.VolumeUp:
                    Dispatcher.Invoke(DispatcherPriority.Normal, new Action(VolumeUp));
                    break;
                default:
                    return;
            }
        }

        private void VolumeUp()
        {

            Volume += 0.1;
        }

        private void VolumeDown()
        {
            Volume -= 0.1;
        }

        private void Stop()
        {
            if (currentMedia != null)
                currentMedia.Stop();
        }

        private void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            if (Model.IsPaused)
            {
                Model.IsPaused = false;
                currentMedia.Play();
            }
            else Next();
        }

        private void Next()
        {
            //check if "prev" was used
            if (Model.PlayedClipsIndex < Model.PlayedClips.Count - 1)
            { //get next already played song
                Model.PlayedClipsIndex++;
                CurrentClip = Model.PlayedClips[Model.PlayedClipsIndex];
            }
            else if (this.Model.PlayQueue.Count > 0)
            { //play queued clip
                CurrentClip = this.Model.PlayQueue.Dequeue();

                Model.AddPlayedClip(CurrentClip);
            }
            else
            {
                //get next song
                int index = 0;
                //get index
                if (IsRandom)
                {
                    //randomized playback
                    Random rnd = new Random();
                    index = rnd.Next(Model.CurrentSongs.Count);
                }
                else
                {
                    //normal playback
                    index = Model.CurrentSongs.IndexOf(CurrentClip) + 1;
                    if (Model.CurrentSongs.Count <= index)
                        index = 0;
                }
                CurrentClip = this.Model.CurrentSongs[index];

                Model.AddPlayedClip(CurrentClip);
            }

            PlayCurrentClip();
        }

        internal void PlayCurrentClip()
        {
            if (CurrentClip == null)
                return;

            try
            {
                if (!CurrentClip.FileInfo.Exists)
                {
                    Next();
                    return;
                }
            }
            catch (Exception ex)
            {
                Logger.Log("MatthiasToolbox.Player.Window1", "Error while trying to play current clip " + CurrentClip.ToString(), ex);
            }

            SelectAndFocusClip(CurrentClip);
            CurrentClip.PlayCount++;

            currentMedia.Source = CurrentClip.FileInfo.ToURI();

            currentMedia.Play();
            InitializePropertyValues();
        }

        /// <summary>
        /// Selects, and focus the clip. Scrolls to the location of the clip.
        /// </summary>
        /// <param name="clip">The clip to select.</param>
        private void SelectAndFocusClip(Clip clip)
        {
            livPlayList.SelectedItem = clip;
            livPlayList.ScrollIntoView(livPlayList.SelectedItem);
            ListViewItem item = livPlayList.ItemContainerGenerator.ContainerFromItem(livPlayList.SelectedItem) as ListViewItem;
            item.Focus();
        }


        /// <summary>
        /// Scrolls to clip.
        /// </summary>
        /// <param name="clip">The clip.</param>
        private void ScrollToClip(Clip clip)
        {
            int index = this.Model.CurrentSongs.IndexOf(clip);

            VirtualizingStackPanel vsp = (VirtualizingStackPanel)typeof(ItemsControl).InvokeMember("_itemsHost",
                BindingFlags.Instance | BindingFlags.GetField | BindingFlags.NonPublic, null, livPlayList, null);

            double scrollHeight = vsp.ScrollOwner.ScrollableHeight;
            // itemIndex_ is index of the item which we want to show in the middle of the view
            double offset = scrollHeight * index / Model.CurrentSongs.Count;

            vsp.SetVerticalOffset(offset);
        }

        private void btnSkip_Click(object sender, RoutedEventArgs e)
        {
            Skip();
        }

        /// <summary>
        /// Play the next song and increase the SkipCount.
        /// </summary>
        private void Skip()
        {
            if (CurrentClip != null)
            {
                CurrentClip.SkipCount++;
                //Database.OpenInstance.SubmitChanges(); //TODO: Crashbug while adding new folders, database changes saved on exit
            }
            Next();
        }

        private void btnSkipAndDelete_Click(object sender, RoutedEventArgs e)
        {
            FileInfo fi = new FileInfo(currentMedia.Source.OriginalString);
            Clip clip = CurrentClip;
            Next();
            fi.Delete();
            Model.RemoveSongs(new [] { clip });
        }

        private void btnRandomize_Click(object sender, RoutedEventArgs e)
        {
            Random rnd = new Random();
            foreach (PlayListEntry pe in this.Model.CurrentPlayList.Entries)
                pe.OrderID = rnd.Next();
            Database.OpenInstance.SubmitChanges();
            this.Model.CurrentPlayList.RefreshSorting();

            this.Model.RefreshPlaylist();
        }

        //// Change the volume of the media.
        //private void ChangeMediaVolume(object sender, RoutedPropertyChangedEventArgs<double> args)
        //{
        //    if (currentMedia != null) currentMedia.Volume = volumeSlider.Value;
        //}

        // Change the speed of the media.
        private void ChangeMediaSpeedRatio(object sender, MouseButtonEventArgs args)
        {
            if (currentMedia != null) currentMedia.SpeedRatio = speedRatioSlider.Value;
        }

        // When the media opens, initialize the "Seek To" slider maximum value
        // to the total number of miliseconds in the length of the media clip.
        private void MediaOpened(object sender, EventArgs e)
        {
            timelineSlider.Maximum = currentMedia.NaturalDuration.TimeSpan.TotalMilliseconds;
        }

        void InitializePropertyValues()
        {
            // Set the media's starting Volume and SpeedRatio to the current value of the
            // their respective slider controls.
            currentMedia.Volume = volumeSlider.Value;
            currentMedia.SpeedRatio = speedRatioSlider.Value;

            this.Title = CurrentClip.Label;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = System.Windows.WindowState.Minimized;
        }

        private void timelineSlider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (currentMedia != null)
            {
                int SliderValue = (int)timelineSlider.Value;

                // Overloaded constructor takes the arguments days, hours, minutes, seconds, miniseconds.
                // Create a TimeSpan with miliseconds equal to the slider value.
                TimeSpan ts = new TimeSpan(0, 0, 0, 0, SliderValue);
                currentMedia.Position = ts;
            }
            _seeking = false;
        }

        private void btnSkipNoCount_Click(object sender, RoutedEventArgs e)
        {
            Next();
        }

        private void btnResetSpeed_Click(object sender, RoutedEventArgs e)
        {
            if (currentMedia != null)
            {
                speedRatioSlider.Value = 1;
                currentMedia.SpeedRatio = speedRatioSlider.Value;
            }
        }

        private void timelineSlider_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            _seeking = true;
        }

        private void currentMedia_MediaEnded(object sender, RoutedEventArgs e)
        {
            currentMedia.Stop();
            Next();
        }

        private void btnPause_Click(object sender, RoutedEventArgs e)
        {
            PlayPause();
        }

        private void PlayPause()
        {
            if (CurrentClip == null)
            {
                Next();
                return;
            }

            if (Model.IsPaused)
            {
                currentMedia.Play();
                Model.IsPaused = false;
            }
            else
            {
                currentMedia.Pause();
                Model.IsPaused = true;
            }
        }

        private void ToolBarTray_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void btnPrev_Click(object sender, RoutedEventArgs e)
        {
            Prev();
        }

        private void Prev()
        {
            if (Model.PlayedClipsIndex == -1)
                return;

            this.Model.PlayedClipsIndex--;
            this.CurrentClip = Model.PlayedClips[Model.PlayedClipsIndex];
            PlayCurrentClip();
        }

        private void livPlayList_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            ListViewItem listViewItem = sender as ListViewItem;
            if (listViewItem == null)
                return;
            Clip clip = listViewItem.DataContext as Clip;
            if (clip == null)
                return;

            this.CurrentClip = clip;
            Model.AddPlayedClip(CurrentClip);
            PlayCurrentClip();
        }

        /// <summary>
        /// On closing dispose the keyboardlistener.
        /// </summary>
        /// <param name="e">A <see cref="T:System.ComponentModel.CancelEventArgs"/> that contains the event data.</param>
        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            this._keyboardListener.Dispose();
        }


        private void livPlayList_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.KeyboardDevice.Modifiers != ModifierKeys.None)
                return;

            switch (e.Key)
            {
                case Key.Q:
                    Model.EnqueueSongs(this.livPlayList.SelectedItems);
                    break;
                //case Key.F:
                //    Model.ShowSearchDialog();
                //    break;
                case Key.Delete:
                    Model.RemoveSongs(livPlayList.SelectedItems);
                    break;
                default:
                    return;
            }

            e.Handled = true;
        }



        private void livPlayList_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.KeyboardDevice.Modifiers != ModifierKeys.None)
                return;

            if (e.Key == Key.Q)
                e.Handled = true;
        }

        private void MainWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            this._keyPressed = e.Key;
            this._keyPressedModifieres = e.KeyboardDevice.Modifiers;
            e.Handled = true;
        }

        private void MainWindow_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != this._keyPressed || e.KeyboardDevice.Modifiers != this._keyPressedModifieres) return;

            if (this._keyPressedModifieres == ModifierKeys.None)
            {
                switch (this._keyPressed)
                {
                    case Key.F:
                        Model.ShowSearchDialog();
                        break;
                    default:
                        return;
                }
                e.Handled = true;
            }

        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            Model.ShowSearchDialog();
        }

        private void btnOptions_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}