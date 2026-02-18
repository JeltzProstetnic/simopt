using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using MatthiasToolbox.Presentation.Utilities.Collections;
using MatthiasToolbox.Utilities;

namespace MatthiasToolbox.Player.Data
{
    public class PlayerPresentationModel
    {
        #region cvar

        private Queue<Clip> _playQueue;
        private PlayList _currentPlayList;

        private bool _isPaused;
        private BackgroundWorker _worker;

        private readonly Queue<string> _foldersToAdd;

        private int playedClipsIndex = -1;
        private readonly ObservableCollection<Clip> _playedClips;
        private Window1 window1;

        #endregion cvar
        #region ctor

        public PlayerPresentationModel(Window1 window1)
        {
            this.window1 = window1;
            this._playedClips = new ObservableCollection<Clip>();
            this._foldersToAdd = new Queue<string>();
            this.PlayQueue = new Queue<Clip>();
            CurrentSongs = new BindableCollection<Clip>();
        }

        #endregion ctor

        #region prop


        public BindableCollection<Clip> CurrentSongs { get; private set; }

        public ObservableCollection<Clip> PlayedClips
        {
            get { return this._playedClips; }
        }

        
        public PlayList CurrentPlayList
        {
            get { return _currentPlayList; }
            set { _currentPlayList = value; }
        }

        public bool IsPaused { get { return this._isPaused; } set { this._isPaused = value; } }

        public int PlayedClipsIndex
        {
            get { return playedClipsIndex; }
            set { playedClipsIndex = value; }
        }

        public Queue<Clip> PlayQueue
        {
            get { return _playQueue; }
            set { _playQueue = value; }
        }

        #endregion prop
        public void ShowAddFolderDialog()
        {
            // System.Windows.Forms.Folder
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.ShowNewFolderButton = false;
            fbd.SelectedPath = Setting.Get<string>("LastAddPath", @"C:\");
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                lock (this._foldersToAdd)
                {
                    this._foldersToAdd.Enqueue(fbd.SelectedPath);
                }
                // Using a worker threads to add the items.
                if (this._worker == null)
                {
                    this._worker = new BackgroundWorker();
                    this._worker.DoWork += delegate(object s, DoWorkEventArgs args) { AddDirectory(); };
                }
                if (!this._worker.IsBusy)
                    this._worker.RunWorkerAsync();
            }
        }

        private void AddDirectory()
        {
            while (this._foldersToAdd.Count > 0)
            {
                string filePath;
                lock (this._foldersToAdd)
                {
                    filePath = this._foldersToAdd.Dequeue();
                }

                DirectoryInfo di = new DirectoryInfo(filePath);
                this._currentPlayList.Add(di.AllFiles());
                Setting.Set<string>("LastAddPath", filePath);

                RefreshPlaylist();
            }
        }

        /// <summary>
        /// Copies the currentplaylist to the current song list. Does not clear the CurrentSongs list.
        /// Removes all songs not contained in the currentPlayList.
        /// </summary>
        internal void RefreshPlaylist()
        {
            int i = 0;
            IEnumerator<Clip> enumerable = this._currentPlayList.Clips.GetEnumerator();
            while (i < this.CurrentSongs.Count && enumerable.MoveNext())
            {
                Clip current = enumerable.Current;
                if (this.CurrentSongs[i] != current)
                    this.CurrentSongs[i] = current;
                i++;
            }
            while (enumerable.MoveNext())
            {
                this.CurrentSongs.Add(enumerable.Current);
                i++;
            }

            //remove items that are not in the currentplaylist
            while (this.CurrentSongs.Count > i)
            {
                this.CurrentSongs.RemoveAt(this.CurrentSongs.Count - 1);
            }
        }

        /// <summary>
        /// Remove songs from the Playlist.
        /// </summary>
        /// <param name="clips">The IEnumerable clips.</param>
        internal void RemoveSongs(IEnumerable clips)
        {
            foreach (Clip clip in clips)
            {
                CurrentPlayList.Remove(clip);
            }
        }

        /// <summary>
        /// Shows the search dialog and plays the selected file.
        /// </summary>
        internal void ShowSearchDialog()
        {
            SearchDialog searchDialog = new SearchDialog(this);

            bool? result = searchDialog.ShowDialog();
            if (result != true) return;

            Clip clip = searchDialog.SelectedFile;

            if (clip == null) return;

            this.window1.CurrentClip = clip;
            AddPlayedClip(clip);
            this.window1.PlayCurrentClip();
        }

        internal void EnqueueSongs(IList selectedItems)
        {
            foreach (Clip selectedItem in selectedItems)
            {
                if (!this.PlayQueue.Contains(selectedItem))
                    this.PlayQueue.Enqueue(selectedItem);
            }
        }

        internal void AddPlayedClip(Clip clip)
        {
            this.PlayedClips.Add(clip);
            this.PlayedClipsIndex++;
        }
    }
}
