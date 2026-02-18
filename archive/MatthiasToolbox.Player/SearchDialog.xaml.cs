using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using MatthiasToolbox.Player.Data;
using MatthiasToolbox.Presentation.Utilities.Collections;

namespace MatthiasToolbox.Player
{
    /// <summary>
    /// Interaction logic for SearchDialog.xaml
    /// </summary>
    public partial class SearchDialog : Window
    {
        #region cvar

        private PlayerPresentationModel _model;
        private Key _keyPressed;
        private ModifierKeys _keyPressedModifieres;

        #endregion cvar
        #region prop

        public ListCollectionView FilteredSongsView { get; set; }

        public Clip SelectedFile { get; set; }

        #endregion prop
        #region ctor 

        public SearchDialog(PlayerPresentationModel model)
        {
            this._model = model;

            this.DataContext = model.CurrentSongs;

            ListCollectionView view = new ListCollectionView(model.CurrentSongs);
            FilteredSongsView = view;

            InitializeComponent();

            new TextSearchFilter(FilteredSongsView, TxtSearch);
        }

        #endregion ctor
        #region impl

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedFile != null)
                DialogResult = true;
        }


        private void SearchDialog_KeyDown(object sender, KeyEventArgs e)
        {
            this._keyPressed = e.Key;
            this._keyPressedModifieres = e.KeyboardDevice.Modifiers;
        }

        private void SearchDialog_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key != this._keyPressed || e.KeyboardDevice.Modifiers != this._keyPressedModifieres) return;

            if (this._keyPressedModifieres == ModifierKeys.Control)
            {
                switch (this._keyPressed)
                {
                    case Key.Q:
                        this._model.EnqueueSongs(this.livFilteredSongs.SelectedItems);
                        break;
                    default:
                        return;
                }
                e.Handled = true;
            }
        }

        private void BtnQueue_Click(object sender, RoutedEventArgs e)
        {
            this._model.EnqueueSongs(this.livFilteredSongs.SelectedItems);
        }

        #endregion impl
    }
}
