using System;
using System.Collections.Generic;
using Microsoft.LiveLabs.Html;
using Microsoft.LiveLabs.JavaScript;
using Microsoft.LiveLabs.JavaScript.Interop;
using Ribbon.Controls;

using MenuType = Ribbon.Menu;

namespace Ribbon
{
    [Import(MemberNameCasing = Casing.Exact)]
    public class MenuLauncherControlProperties : ControlProperties
    {
        extern public MenuLauncherControlProperties();
        extern public string CacheMenuVersions { get; }
        extern public string CommandMenuOpen { get; set;  }
        extern public string CommandMenuClose { get; set; }
        extern public string CommandValueId { get; }
        extern public string PopulateDynamically { get; }
        extern public string PopulateOnlyOnce { get; }
        extern public string PopulateQueryCommand { get; }
    }

    /// <summary>
    /// Abstract class that all CUI Controls that need to launch Menus should subclass from.  Provides some common functionality like listening to the Menu events, positioning the Menu etc.
    /// </summary>
    internal abstract class MenuLauncher : Control, IModalController
    {
        public const string PopulationXML = "PopulationXML";
        public const string PopulationJSON = "PopulationJSON";
        public const string PollAgainInterval = "PollAgainInterval";
        public const string PopulationVersion = "PopulationVersion";

        /// <summary>
        /// MenuLauncher contructor.
        /// </summary>
        /// <param name="root">The Root that this MenuLauncher was created by and is part of.</param>
        /// <param name="id">The Component id of this MenuLauncher.</param>
        /// <param name="properties">Dictionary of Control parameters</param>
        /// <param name="menu">The Menu that this MenuLauncher should launch.</param>
        public MenuLauncher(Root root, string id, ControlProperties properties, MenuType menu)
            : base(root, id, properties)
        {
            _menu = menu;
        }

        bool _menuLaunched = false;
        protected ISelectableControl _selectedControl;

        /// <summary>
        /// Whether this MenuLauncher's Menu currently launched
        /// </summary>
        public bool MenuLaunched
        {
            get
            {
                return _menuLaunched;
            }
        }

        MenuType _menu;
        /// <summary>
        /// The Menu that this MenuLauncher will launch.
        /// </summary>
        internal MenuType Menu
        {
            get
            {
                return _menu;
            }
        }

        bool _launchedByKeyboard = false;
        protected bool LaunchedByKeyboard
        {
            get
            {
                return _launchedByKeyboard;
            }
            set
            {
                _launchedByKeyboard = value;
            }
        }

        HtmlElement _elmHadFocus = null;
        protected HtmlElement ElmHadFocus
        {
            get
            {
                return _elmHadFocus;
            }
            set
            {
                _elmHadFocus = value;
            }
        }

        IFrame _elmBackFrame;
        private IFrame BackFrame
        {
            get
            {
                if (CUIUtility.IsNullOrUndefined(_elmBackFrame))
                {
                    _elmBackFrame = Utility.CreateHiddenIframeElement();

                }
                return _elmBackFrame;
            }
        }

        /// <summary>
        /// Launch this MenuLauncher's Menu.
        /// </summary>
        /// <param name="evt">The DomEvent that triggered the launch(usually a mouse click)</param>
        protected bool LaunchMenu(HtmlElement elmHadFocus)
        {
            return LaunchMenu(elmHadFocus, null);
        }

        /// <summary>
        /// Launch this MenuLauncher's Menu.
        /// </summary>
        /// <param name="evt">The DomEvent that triggered the launch(usually a mouse click)</param>
        protected bool LaunchMenu(HtmlElement elmHadFocus, Action onDelayedLoadSucceeded)
        {
            // If the menu is already launched, don't launch it twice
            // This happens when the menu is launched using the enter
            // key or the space bar since the browser sends two events:
            // one for the keypress and one for the associated click
            // when enter or space is pressed on an anchor element.
            if (MenuLaunched)
                return false;

            _elmHadFocus = elmHadFocus;

            // If there is no Menu, then we check if this menu polls to get initialized.
            // If so, we poll for the menu XML, otherwise we exit.
            if (Utility.IsTrue(Properties.PopulateDynamically))
                PollForDynamicMenu(true, onDelayedLoadSucceeded);

            // If there is no Menu to launch then we can't launch the Menu
            // This is not necessarily a bug.  There is an asynchronous callback
            // available to create menus that are populated dynamically.  This
            // is handled in PollForDynamicMenu() above.
            if (CUIUtility.IsNullOrUndefined(_menu))
                return false;

            // If we got this far and the member delayed load variable is not null, this was a delayed
            // load that succeeded, so we need to run the callback and clear the variable
            if (!CUIUtility.IsNullOrUndefined(_onDelayLoadSucceeded))
            {
                _onDelayLoadSucceeded();
                _onDelayLoadSucceeded = null;
            }

            _menu.EnsureRefreshed();

            if (!_menu.HasItems())
                return false;

            ControlComponent comp = DisplayedComponent;
            comp.EnsureChildren();
            comp.IgnoreDirtyingEvents = true;
            comp.AddChild(_menu);
            comp.IgnoreDirtyingEvents = false;

            _menu.PollIfRootPolledSinceLastPoll();
            _menu.InvalidatePositionAndSizeData();

#if !CUI_NORIBBON
            // Adding the menu may cause window resize events which then immediately close the menu.
            // To fix this, we remove the onresize handler on Window until the menu is done launching.
            bool isInRibbon = Root is SPRibbon;
            SPRibbon ribbon = null;
            bool oldWindowResizedHandlerEnabled = false;
            if (isInRibbon)
            {
                ribbon = (SPRibbon)Root;
                oldWindowResizedHandlerEnabled = ribbon.WindowResizedHandlerEnabled;
                ribbon.WindowResizedHandlerEnabled = false;
            }
#endif

            HtmlElement menuElement = _menu.ElementInternal;

            // Hide menu while positioning
            menuElement.Style.Visibility = "hidden";
            menuElement.Style.Position = "absolute";
            menuElement.Style.Top = "0px";
            menuElement.Style.Left = "0px";

            // Place menu directly on top of modal div (z-index:1000)
            menuElement.Style.ZIndex = 1001;

            // Add menu to DOM
            Browser.Document.Body.AppendChild(menuElement);

            if (BrowserUtility.InternetExplorer7 &&  Root.TextDirection == Direction.RTL)
            {
                int menuWidth = menuElement.OffsetWidth;

                // The menu items have 12px of padding, 2px of borders, and 2px of margins,
                // and the menu itself has 2px of borders too, so we need to remove that first
                menuWidth = menuWidth >= 18 ? menuWidth - 18 : 0;
                string strMenuWidth = menuWidth + "px";

                List<Component> sections = _menu.Children;
                foreach (MenuSection section in sections)
                {
                    List<Component> items = section.Children;
                    foreach (Component c in items)
                    {
                        if (c is MenuItem)
                        {
                            c.ElementInternal.Style.Width = strMenuWidth;
                        }
                    }
                }
            }

            PositionMenu(menuElement, comp.ElementInternal);

            // For IE, we need a backframe IFrame in order for the menu to show up over 
            // ActiveX controls
            if (BrowserUtility.InternetExplorer)
            {
                AddAndPositionBackFrame();
            }

            // Show menu once it is positioned
            Root.BeginModal(this, _elmHadFocus);
            Root.AddMenuLauncherToStack(this);
            menuElement.Style.Visibility = "visible";

            _menuLaunched = true;
            _menu.Launched = true;

            FocusOnAppropriateMenuItem(null);

#if !CUI_NORIBBON
            if (isInRibbon)
            {
                ribbon.WindowResizedHandlerEnabled = oldWindowResizedHandlerEnabled;
            }
#endif

            return true;
        }

        private void FocusOnAppropriateMenuItem(HtmlEvent evt)
        {
            if (CUIUtility.IsNullOrUndefined(_menu.SelectedMenuItem) && !CUIUtility.IsNullOrUndefined(_selectedControl))
            {
                Control ctl = (Control)_selectedControl;
                ControlComponent dispComp = ctl.DisplayedComponent;
                if (dispComp is MenuItem)
                    _menu.SelectedMenuItem = (MenuItem)dispComp;
            }

            // Let focus remain on triggering element if using jaws
            // LaunchedByKeyboard true for onkeydown, not jaws onclick
            if (LaunchedByKeyboard)
            {
                _menu.FocusOnFirstItem(evt);
            }
            else
            {
                // If nothing has been selected before then we auto-select the first item in some cases
                MenuItem selectedItem = _menu.SelectedMenuItem;
                if (!CUIUtility.IsNullOrUndefined(selectedItem))
                {
                    // This auto selection only happens for ToggleButtons in DropDowns where one of the 
                    // menu items represents the "currently selected item" in the DropDown.  
                    // Currently selected font in a font dropdown for example.
                    Control selectedItemControl = selectedItem.Control;
                    if (selectedItemControl is ToggleButton && selectedItemControl is ISelectableControl)
                    {
                        ISelectableControl isc = (ISelectableControl)selectedItemControl;
                        if (!_menu.FocusOnItemById(isc.GetMenuItemId()))
                            _menu.FocusOnFirstItem(evt);
                    }
                }
            }
        }

        int _pendingBackFrameTimeoutId = -1;
        protected void AddAndPositionBackFrame()
        {
            if (_pendingBackFrameTimeoutId != -1)
                Browser.Window.ClearTimeout(_pendingBackFrameTimeoutId);
            _pendingBackFrameTimeoutId = Browser.Window.SetTimeout(new Action(AddAndPositionBackFrameInternal), 50);
        }

        protected void AddAndPositionBackFrameInternal()
        {
#if PERF_METRICS
            PMetrics.PerfMark(PMarker.perfCUIAddAndPositionBackFrameStart);
#endif
            Browser.Document.Body.AppendChild(BackFrame);
            Utility.PositionBackFrame(BackFrame, _menu.ElementInternal);
#if PERF_METRICS
            PMetrics.PerfMark(PMarker.perfCUIAddAndPositionBackFrameEnd);
#endif
        }

        #region Event Handlers
        public virtual void OnModalBodyClick(HtmlEvent evt)
        {
            // Firefox specific fix
            Utility.CancelEventUtility(evt, false, true);
            LaunchedByKeyboard = false; // remember we're using the mouse
            Root.CloseMenuStack(this);
        }

        public virtual void OnModalBodyMouseOver(HtmlEvent args)
        {
        }

        public virtual void OnModalBodyMouseOut(HtmlEvent args)
        {
        }

        public virtual void OnModalContextMenu(HtmlEvent evt)
        {
            // The goal is to prevent the browser's right click context menu from appearing while
            // also sending the contextmenu event to the element underneath the modal div.
            // At the moment this works correctly in Firefox3 but not IE7.
            // Unfortunately, each browser seems to behave differently with regards to event
            // propagation, and at the moment only Firefox achieves the ideal behavior.
            //
            // In Safari 3, calling PreventDefault() is necessary to block the browser context menu.
            // No combination of PD() and StopPropagation() will pass the event along.
            //
            // In IE7 and IE8, calling either PD() or SP() will block the browser context menu.
            // No combination of PD() and StopPropagation() will pass the event along.
            //
            // In Firefox 3, calling either PD() or SP() will block the browser context menu.
            // If you call SP() but not PD(), the event will get passed along to the element, but
            // if you call PD() it will not be passed along.
            Utility.CancelEventUtility(evt, false, true);

            LaunchedByKeyboard = false; // remember we're using the mouse
            Root.CloseMenuStack(this);
        }

        public virtual void OnModalKeyPress(HtmlEvent evt)
        {
            // To make OACR happy
            if (evt != null)
            {
                // Close Menu when Escape is pressed
                if (evt.KeyCode == (int)Key.Esc)
                {
                    // Stop this event from bubbling so it doesn't close dialogs (O14:208597)
                    Utility.CancelEventUtility(evt, false, true);
                    
                    // Close all menus within this one
                    LaunchedByKeyboard = true;
                    Root.CloseMenuStack(this);
                }

                // Catch tab and loop from first and last items
                if (evt.KeyCode == (int)Key.Tab)
                {
                    if (evt.ShiftKey)
                    {
                        if (!_menu.FocusPrevious(evt))
                            _menu.FocusOnLastItem(evt);
                        Utility.CancelEventUtility(evt, false, true);
                    }
                    else
                    {
                        if (!_menu.FocusNext(evt))
                            _menu.FocusOnFirstItem(evt);
                        Utility.CancelEventUtility(evt, false, true);
                    }
                }

                if (evt.KeyCode == (int)Key.Down)
                {
                    if (!_menu.FocusNext(evt))
                        _menu.FocusOnFirstItem(evt);

                    Utility.CancelEventUtility(evt, true, true);
                }

                if (evt.KeyCode == (int)Key.Up)
                {
                    if (!_menu.FocusPrevious(evt))
                        _menu.FocusOnLastItem(evt);

                    Utility.CancelEventUtility(evt, true, true);
                }

                // If this is an FlyoutAnchor, use its right/left key events instead
                if (this is FlyoutAnchor)
                {
                    if ((evt.KeyCode == (int)Key.Right && Root.TextDirection == Direction.LTR) ||
                        (evt.KeyCode == (int)Key.Left && Root.TextDirection == Direction.RTL))
                    {
                        if (!_menu.FocusNext(evt))
                            _menu.FocusOnFirstItem(evt);

                        Utility.CancelEventUtility(evt, true, true);
                    }
                    if ((evt.KeyCode == (int)Key.Left && Root.TextDirection == Direction.LTR) ||
                        (evt.KeyCode == (int)Key.Right && Root.TextDirection == Direction.RTL))
                    {
                        if (!_menu.FocusPrevious(evt))
                            _menu.FocusOnLastItem(evt);

                        Utility.CancelEventUtility(evt, true, true);
                    }
                }
            }
        }
        #endregion

        #region Menu Positioning
        protected virtual void PositionMenu(HtmlElement menu, HtmlElement launcher)
        {
            Root.PositionFlyOut(menu, launcher);
        }

        /// <summary>
        /// Get all pertinent element dimensions for positioning the menu
        /// </summary>
        /// <param name="menu">The menu to be launched. If null, then only get launcher dimensions</param>
        /// <param name="launcher">The element that is launching the menu</param>
        /// <returns>A dictionary with all the necessary dimensional data to position the menu</returns>
        protected Dictionary<string, int> GetAllElementDimensions(HtmlElement menu, HtmlElement launcher)
        {
            return Root.GetAllElementDimensions(menu, launcher);
        }
        #endregion

        /// <summary>
        /// Close this MenuLauncher's Menu.
        /// </summary>
        internal virtual void CloseMenu()
        {
            // There are cases where the Menu was closed by some other function, so this may get called more than once on one menu
            if (!_menuLaunched)
                return;

            // Remove the menu floating div from the DOM
            UIUtility.RemoveNode(_menu.ElementInternal);
            if (!CUIUtility.IsNullOrUndefined(_elmBackFrame))
                UIUtility.RemoveNode(_elmBackFrame);

            _menu.OnMenuClosed();

            // Now remove the Menu Component from the Ribbon Component Hierarchy
            // TODO: should we remove this or leave it in the hierarchy?
            // Perhaps remove it so that it could be added to a scaled other version of this control
            Component parent = _menu.Parent;
            parent.IgnoreDirtyingEvents = true;
            parent.RemoveChild(_menu.Id);
            parent.IgnoreDirtyingEvents = false;
            _menuLaunched = false;
            _menu.Launched = false;

            Root.EndModal(this);
            // Only return focus to ribbon if user closed menu using keyboard
            if (!CUIUtility.IsNullOrUndefined(_elmHadFocus) && LaunchedByKeyboard)
            {
                _elmHadFocus.PerformFocus();
            }

            _elmHadFocus = null;
            LaunchedByKeyboard = false;
            OnLaunchedMenuClosed();
        }

        public override void OnMenuClosed()
        {
            // TODO: should things go here?
        }

        // Called when the menu is closed
        protected virtual void OnLaunchedMenuClosed()
        {
            _menu.ResetFocusedIndex();
        }

        // This is the default behavior for MenuLaunchers
        // This can of course be overriden to change what types of child Components are allowed
        public override void EnsureCorrectChildType(Component child)
        {
            // allow both menus and tooltips            
            if ((!typeof(MenuType).IsInstanceOfType(child))
                 && (!(typeof(ToolTip).IsInstanceOfType(child)))
                )
            {
                throw new ArgumentException("This Component can only have Menu and ToolTip Components as children.");
            }
        }

        internal override bool OnPreBubbleCommand(CommandEventArgs command)
        {
            if (_menuLaunched && command.Type != CommandType.MenuCreation &&
                command.Type != CommandType.Preview &&
                command.Type != CommandType.PreviewRevert &&
                command.Type != CommandType.OptionPreview &&
                command.Type != CommandType.OptionPreviewRevert &&
                command.Type != CommandType.IgnoredByMenu &&
                command.Type != CommandType.MenuClose)
            {
                if (!CUIUtility.IsNullOrUndefined(command.SourceControl))
                {
                    MenuItem selectedItem = (MenuItem)command.SourceControl.DisplayedComponent;
                    _menu.SelectedMenuItem = selectedItem;
                }
                Root.CloseMenuStack(this);
            }
            return true;
        }

        bool _polledOnce = false;
        Action _onDelayLoadSucceeded;
        protected void PollForDynamicMenu(bool launchMenu)
        {
            PollForDynamicMenu(launchMenu, null);
        }

        protected void PollForDynamicMenu(bool launchMenu, Action onDelayedLoadSucceeded)
        {
            // If this menu should only be populated once and it has been 
            // populated then we have nothing to do.
            if (_polledOnce && Utility.IsTrue(Properties.PopulateOnlyOnce))
                return;

            // If there is no dynamic menu creation command, then we can't create
            // the menu dynamically.
            if (string.IsNullOrEmpty(Properties.PopulateQueryCommand))
                return;
            // REVIEW(josefl):  How should we handle errors here?  Should we just fail 
            // silently or just let them throw like we are now?

            Dictionary<string, string> menuProperties = new Dictionary<string, string>();

            // If this control caches its menu versions, then we send the versions that we have cached
            // out with the property bag.
            Dictionary<string, bool> menuVersions;
            if (Utility.IsTrue(Properties.CacheMenuVersions))
            {
                menuVersions = new Dictionary<string, bool>();
                foreach (string key in _cachedMenuVersions.Keys)
                    menuProperties[key] = "true";
            }

            // Poll for the menu
            bool commandEnabled = Root.PollForCommandState(Properties.PopulateQueryCommand,
                                                           Properties.PopulateQueryCommand,
                                                           menuProperties);

            if (commandEnabled)
            {
                Menu menu = null;
                string data = "";
                // If the poll answerer sent XML then we use it and build a menu from it
                if (menuProperties.ContainsKey(PopulationJSON) && !string.IsNullOrEmpty(menuProperties[PopulationJSON]))
                {
                    data = menuProperties[PopulationJSON];
                }
                else if (menuProperties.ContainsKey(PopulationXML) && !string.IsNullOrEmpty(menuProperties[PopulationXML]))
                {
                    data = Builder.ConvertXMLStringToJSON(menuProperties[PopulationXML]);
                }

                string pVersion = menuProperties.ContainsKey(PopulationVersion) ?
                    menuProperties[PopulationVersion] : string.Empty;
                int pInterval = menuProperties.ContainsKey(PollAgainInterval) ?
                    Int32.Parse(menuProperties[PollAgainInterval]) : -1;
                
                if (!string.IsNullOrEmpty(data))
                {
                    // Build the Menu from the xml and then cache it if we are suppose to
                    menu = Root.Builder.BuildMenu(Browser.Window.Eval(data), new BuildContext(), false);
                    if (!CUIUtility.IsNullOrUndefined(menu))
                    {
                        // Store the fact that we have successfully polled for the menu once
                        _polledOnce = true;

                        // Store the menu in the cache of menus if need if we should
                        if (Utility.IsTrue(Properties.CacheMenuVersions) &&
                            !string.IsNullOrEmpty(pVersion))
                        {
                            CachedMenuVersions[pVersion] = menu;
                        }
                    }
                }
                // If the PopulationVersion was set, then we try to use a stored menu.
                else if (!string.IsNullOrEmpty(pVersion))
                {
                    menu = CachedMenuVersions[pVersion];
                }
                else if (launchMenu && -1 != pInterval)
                {
                    // This is used if the answering component needs to issue an asynchronous
                    // request to get the data needed to construct the menu.  PollAgainInterval
                    // then holds the number of milliseconds until this MenuLauncher should try
                    // polling for the Menu again.

                    _onDelayLoadSucceeded = onDelayedLoadSucceeded;

                    // So, in this case, we set up a timout so that we can try to poll for the menu
                    // again in a specified number of milliseconds.
                    Browser.Window.SetTimeout(new Action(OnTryDelayedDynamicPopulation), pInterval);

                    // We also make sure the the _menu member is null since we are now waiting for a new one
                    _menu = null;
                }

                if (!CUIUtility.IsNullOrUndefined(menu))
                {
                    _menu = menu;

                    OnDynamicMenuPopulated();
                }
            }
        }

        // If a dynamic menu launch was started, we are now checking back to see
        // if the rootuser now has the necessary information to construct the menu.
        private void OnTryDelayedDynamicPopulation()
        {
            // TODO(josefl): This needs some thought and work to make sure that it 
            // works properly for all MenuLaunchers.
            LaunchMenu(_elmHadFocus, _onDelayLoadSucceeded);
        }

        protected virtual void OnDynamicMenuPopulated()
        {
        }



        private MenuLauncherControlProperties Properties
        {
            get
            {
                return (MenuLauncherControlProperties)base.ControlProperties;
            }
        }

        Dictionary<string, Menu> _cachedMenuVersions = null;
        protected Dictionary<string, Menu> CachedMenuVersions
        {
            get
            {
                if (CUIUtility.IsNullOrUndefined(_cachedMenuVersions))
                    _cachedMenuVersions = new Dictionary<string, Menu>();

                return _cachedMenuVersions;
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            if (!CUIUtility.IsNullOrUndefined(_menu))
                _menu.Dispose();

            if (!CUIUtility.IsNullOrUndefined(_cachedMenuVersions))
            {
                // This may have gotten called before if this is the current menu that we are
                // holding but it won't hurt to call it twice.
                foreach (string key in _cachedMenuVersions.Keys)
                    _cachedMenuVersions[key].Dispose();

                _cachedMenuVersions.Clear();
                _cachedMenuVersions = null;
            }

            _selectedControl = null;
            _menu = null;
            _elmBackFrame = null;
        }
    }
}
