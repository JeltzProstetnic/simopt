using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;
using System.IO;

namespace MatthiasToolbox.Player.Data
{
    public partial class Clip
    {
        private bool hasData = false;

        private FileInfo file;

        public FileInfo FileInfo
        {
            get
            {
                if (!hasData) GetData(); 
                return file;
            }
        }

        public override string ToString()
        {
            if (SkipCount > 0)
            {
                return Label.Replace(".mp3", "") + " Skipped " + SkipCount.ToString() + " time(s)";
            }
            else
            {
                return Label.Replace(".mp3", "");
            }
        }

        private void GetData()
        {
            file = new FileInfo(File);
            hasData = true;
        }
    }
}
