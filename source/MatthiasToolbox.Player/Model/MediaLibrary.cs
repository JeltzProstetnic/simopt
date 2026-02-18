using System.Collections.ObjectModel;
using MatthiasToolbox.Player.Data;

namespace MatthiasToolbox.Player.Model
{
    public class MediaLibrary
    {
        #region cvar

        private readonly ObservableCollection<Clip> _clips;

        #endregion
        #region prop

        public ObservableCollection<Clip> Clips { get { return this._clips; } }

        #endregion

        public MediaLibrary()
        {
            this._clips = new ObservableCollection<Clip>();
            InitializeMedia();
        }

        /// <summary>
        /// Reads all clips from the database
        /// </summary>
        private void InitializeMedia()
        {
            foreach (Clip clip in Database.GetAllClips(Database.OpenInstance))
            {
                this._clips.Add(clip);
            }
        }
    }
}
