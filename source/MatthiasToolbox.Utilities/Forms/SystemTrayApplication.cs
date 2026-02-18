using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace MatthiasToolbox.Utilities.Forms
{
    /// <summary>
    /// Base class for a win32 forms based NotifyIcon application.
    /// Usage: Application.Run(new SystemTrayApplication(...));
    /// </summary>
    public class SystemTrayApplication : Form
    {
        #region cvar

        private NotifyIcon  trayIcon;
        private ContextMenu trayMenu;

        #endregion
        #region ctor

        public SystemTrayApplication(string title, Icon icon = null, ContextMenu trayMenu = null)
        {
            if (trayMenu == null)
            {
                // Create tray menu with exit item.
                this.trayMenu = new ContextMenu();
                trayMenu.MenuItems.Add("Exit", OnExit);
            }
            else
            {
                this.trayMenu = trayMenu;
            }

            // Create NotifyIcon.
            trayIcon      = new NotifyIcon();
            trayIcon.Text = title;

            // Setup bitmap.
            if (icon == null) trayIcon.Icon = new Icon(SystemIcons.Application, 40, 40);
            else trayIcon.Icon = icon;
 
            // Add menu to tray icon and show it.
            trayIcon.ContextMenu = trayMenu;
            trayIcon.Visible     = true;
        }

        #endregion
        #region hand

        protected override void OnLoad(EventArgs e)
        {
            Visible       = false; // hide form window.
            ShowInTaskbar = false; // remove from taskbar.
 
            base.OnLoad(e);
        }
 
        private void OnExit(object sender, EventArgs e)
        {
            Application.Exit();
        }

        #endregion
        #region dtor

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                // release the icon resource.
                trayIcon.Dispose();
            }
 
            base.Dispose(isDisposing);
        }

        #endregion
    }
}