using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;
using MatthiasToolbox.Writer.Enumerations;

namespace MatthiasToolbox.Writer.DataModel
{
    public class TextBlock : Fragment
    {
        #region ctor

        /// <summary>
        /// default ctor
        /// </summary>
        public TextBlock() { this.FragmentType = FragmentType.TextBlock; }

        /// <summary>
        /// full ctor
        /// </summary>
        /// <param name="name"></param>
        /// <param name="comment"></param>
        public TextBlock(string text)
        {
            this.Text = text;
        }

        #endregion
        #region data

        [Column]
        public string Text { get; set; }

        #endregion
    }
}
