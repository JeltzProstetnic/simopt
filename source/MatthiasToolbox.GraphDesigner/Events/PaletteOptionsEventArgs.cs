using System;
using System.Collections.ObjectModel;
using MatthiasToolbox.Basics.Interfaces;
using System.Collections;
using MatthiasToolbox.GraphDesigner.Interfaces;

namespace MatthiasToolbox.GraphDesigner.Events
{
    public class PaletteOptionsEventArgs : EventArgs
    {
        public PaletteOptionsEventArgs(IPalette palette)
        {
            Palette = palette;
        }

        public IPalette Palette { get; private set; }
    }
}
