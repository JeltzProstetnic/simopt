using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.GraphDesigner.Interfaces;
using MatthiasToolbox.Presentation.Interfaces;

namespace MatthiasToolbox.Presentation.Events
{
    public class MouseManagerEventArgs : EventArgs
    {
        private readonly IMouseHandler _oldHandler;
        private readonly IMouseHandler _newHandler;

        public MouseManagerEventArgs(IMouseHandler oldHandler, IMouseHandler newHandler)
        {
            this._oldHandler = oldHandler;
            this._newHandler = newHandler;
        }

        public IMouseHandler OldHandler
        {
            get { return _oldHandler; }
        }

        public IMouseHandler NewHandler
        {
            get { return _newHandler; }
        }
    }
}