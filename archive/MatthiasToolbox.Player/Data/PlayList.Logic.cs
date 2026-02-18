using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using MatthiasToolbox.Utilities;

namespace MatthiasToolbox.Player.Data
{
    public partial class PlayList
    {
        private readonly List<String> allowedExtensions = new List<string> { ".mp3", ".wma" };

        public void Initialize()
        {
            items.Clear();
            var p = Database.OpenInstance.GetClips(ID);
            foreach (Clip clip in p)
            {
                //if(clip.FileInfo.Exists) //dwi checking fileinfo before playbay starts
                    items.Add(clip);
            }
        }

        public void RefreshSorting()
        {
            Initialize();
        }

        public void Add(IEnumerable<FileInfo> files)
        {
            foreach (FileInfo fi in files)
            {
                if (allowedExtensions.Contains(fi.Extension.ToLower()))
                {
                    Clip c = Database.OpenInstance.GetClip(fi);

                    if (c != null)
                    {
                        items.Add(c);
                    }
                    else
                    {
                        c = new Clip();
                        c.Label = fi.Name;
                        c.File = fi.FullName;
                        c.Checksum = fi.GetCRC32();
                        Database.OpenInstance.ClipTable.InsertOnSubmit(c);
                        Database.OpenInstance.SubmitChanges();
                        PlayListEntry e = new PlayListEntry(this, c);
                        Database.OpenInstance.PlayListEntryTable.InsertOnSubmit(e);
                        items.Add(c);
                    }
                }
            }
            Database.OpenInstance.SubmitChanges();
        }

        public IEnumerable<Clip> Clips { get { return items; } }
        public IEnumerable<PlayListEntry> Entries { get { return (from row in Database.OpenInstance.PlayListEntryTable where row.PlayListID == id select row); } }

        public void Remove(Clip clip)
        {
            if (clip == null)
                return;
            items.Remove(clip);
        }
    }
}
