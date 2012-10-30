using System.Collections.Generic;
using Microsoft.LiveLabs.Html;

using MenuType = Ribbon.Menu;

namespace Ribbon
{
    /// <summary>
    /// Abstract class to launch ContextMenus.  
    /// Provides some common functionality like listening to the Menu events, positioning the Menu etc.
    /// </summary>
    internal abstract class ContextMenuLauncher : MenuLauncher
    {
        private Bounds _launchPosition;
        private HtmlElement _targetElement;

        private int _x;
        private int _y;

        #region Constructor
        /// <summary>
        /// ContextMenuLauncher constructor
        /// </summary>
        /// <param name="root">The Root that this MenuLauncher was created by and is part of.</param>
        /// <param name="id">The Component id of this MenuLauncher.</param>
        /// <param name="properties">Dictionary of Control properties</param>
        /// <param name="menu">The Menu that this MenuLauncher should launch.</param>
        internal ContextMenuLauncher(Root root, string id, ControlProperties properties, MenuType menu)
            : base(root, id, properties, menu)
        {
            //base class sets the Menu property on menu
        }
        #endregion Constructor

        #region ContextMenuLauncher methods
        /// <summary>
        /// Obsolete API will be removed soon
        /// </summary>
        /// <param name="evt">The DomEvent that triggered the launch(usually a mouse click)</param>
        internal bool LaunchContextMenu(HtmlElement targetElement, HtmlEvent evt)
        {
            _targetElement = targetElement;
            _launchPosition = GetMenuPosition(evt);
            LaunchMenu(null);
            Menu.FocusOnFirstItem(evt);
            return true;
        }

        /// <summary>
        /// Launch this MenuLauncher's Menu at the given position
        /// </summary>
        public bool LaunchContextMenuAt(HtmlElement targetElement, int x, int y)
        {
            this._x = x;
            this._y = y;
            _targetElement = targetElement;

            LaunchMenu(null);
            Menu.FocusOnFirstItem(null);
            return true;
        }
        #endregion ContextMenuLauncher methods

        /// <summary>
        /// Override to make sure the context menu gets launched in the correct location.
        /// </summary>
        protected override void PositionMenu(HtmlElement menu, HtmlElement launcher)
        {
            // This should be cleaned up when LaunchContextMenu is removed
            if (_launchPosition != null)
            {
                Menu.ElementInternal.Style.Top = _launchPosition.Y + "px";
                Menu.ElementInternal.Style.Left = _launchPosition.X + "px";
                Menu.ElementInternal.Style.Position = "absolute";
            }
            else
            {
                menu.Style.Top = "0px";
                menu.Style.Left = "0px";
                Dictionary<string, int> dimensions = GetAllElementDimensions(menu, _targetElement);
                // set dimensions of the launcher
                dimensions["launcherLeft"] = _x;
                dimensions["launcherTop"] = _y;
                dimensions["launcherWidth"] = 0;
                dimensions["launcherHeight"] = 0;
                Root.SetFlyOutCoordinates(menu, dimensions, false);
            }
        }

        /// <summary>
        /// By default this places the menu where mouse was clicked
        /// </summary>
        /// <param name="comp"></param>
        protected virtual Bounds GetMenuPosition(HtmlEvent evt)
        {
            Bounds b = new Bounds(0, 0, 0, 0);

            b.Y = evt.ClientY;
            b.X = evt.ClientX;
            return b;
        }
    }
}
