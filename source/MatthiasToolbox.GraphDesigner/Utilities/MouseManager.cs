using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using MatthiasToolbox.GraphDesigner;
using MatthiasToolbox.GraphDesigner.Interfaces;
using MatthiasToolbox.Presentation.Controls;
using MatthiasToolbox.Presentation.Events;
using MatthiasToolbox.Presentation.Interfaces;

namespace MatthiasToolbox.GraphDesigner.Utilities
{
    internal class MouseManager
    {
        #region cvar

        private GraphControl _graphControl;

        private IMouseHandler _currentMouseHandler;

        /// <summary>
        /// The last mouse handler is the last multi event handler
        /// </summary>
        private IMouseHandler _lastMouseHandler;

        private bool _isSingleEvent;

        #endregion
        #region evnt

        ///delegate declaration for changes of the mouse handler
        public delegate void MouseManagerChangedHandler(object sender, MouseManagerEventArgs eventArgs);

        public event MouseManagerChangedHandler HandlerChanged;

        #endregion
        #region ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="MouseManager"/> class.
        /// </summary>
        /// <param name="graphControl">The graph control.</param>
        /// <param name="initialMouseHandler">The initial mouse handler.</param>
        internal MouseManager(GraphControl graphControl, IMouseHandler initialMouseHandler)
        {
            this._graphControl = graphControl;
            SetEventHandler(initialMouseHandler, false);
        }

        #endregion

        #region prop

        private IMouseHandler CurrentMouseHandler
        {
            get { return this._currentMouseHandler; }
            set
            {
                IMouseHandler oldHandler = this._currentMouseHandler;
                this._currentMouseHandler = value;

                //throw event that the handler has changed.
                OnCurrentHandlerChanged(oldHandler, this._currentMouseHandler);
            }
        }

        #endregion
        #region impl

        /// <summary>
        /// Sets the event handler.
        /// </summary>
        /// <param name="graphCursor">The graph cursor.</param>
        /// <param name="isSingleEvent">if set to <c>true</c> [is single mouse button event].</param>
        internal void SetEventHandler(IMouseHandler graphCursor, bool isSingleEvent)
        {
            SetLastMouseHandler();

            this._isSingleEvent = isSingleEvent;
            this.CurrentMouseHandler = graphCursor;
        }

        /// <summary>
        /// only multiple event mouse handlers will can be used as last mouse handler
        /// </summary>
        private void SetLastMouseHandler()
        {
            if (!this._isSingleEvent)
                this._lastMouseHandler = this.CurrentMouseHandler;
        }

        /// <summary>
        /// Ends the single mouse event and changes the mouse handler to the last use mouse handler
        /// </summary>
        internal void EndSingleMouseEvent()
        {
            if (this._isSingleEvent)
            {
                this._isSingleEvent = false;
                this.CurrentMouseHandler = this._lastMouseHandler;
            }
        }

        protected internal virtual void OnCurrentHandlerChanged(IMouseHandler oldHandler, IMouseHandler newHandler)
        {
            MouseManagerEventArgs e = new MouseManagerEventArgs(oldHandler, newHandler);
            if (this.HandlerChanged != null) this.HandlerChanged.Invoke(this, e);
        }

        #endregion
        #region catched events

        internal void MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            this.CurrentMouseHandler.MouseDoubleClick(sender, e);

            EndSingleMouseEvent();
        }

        public void MouseUp(object sender, MouseButtonEventArgs e)
        {
            this.CurrentMouseHandler.MouseUp(sender, e);

            EndSingleMouseEvent();
        }

        internal void MouseMove(object sender, MouseEventArgs e)
        {
            this.CurrentMouseHandler.MouseMove(sender, e);
        }

        internal void MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.CurrentMouseHandler.MouseDown(sender, e);
        }

        #endregion
    }
}
