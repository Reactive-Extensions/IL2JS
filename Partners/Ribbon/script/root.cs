using System;
using System.Collections.Generic;
using Microsoft.LiveLabs.Html;
using Microsoft.LiveLabs.JavaScript;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace Ribbon
{
    /// <summary>
    /// The Root Command UI object communicates with the outside world through.
    /// </summary>
    public abstract class RootUser
    {
        /// <summary>
        /// A command was executed inside the Root.
        /// </summary>
        /// <param name="commandId">the id of the command</param>
        /// <param name="properties">the properties of the command</param>
        /// <param name="root">the Root in which the command was executed</param>
        /// <returns>true if the command was executed successfully</returns>
        public abstract bool ExecuteRootCommand(string commandId, Dictionary<string, string> properties, CommandInformation commandInfo, Root root);

        /// <summary>
        /// Queries whether a particular command is currently enabled.
        /// </summary>
        /// <param name="commandId">the id of the command</param>
        /// <param name="root">the Root that is making the request</param>
        /// <returns>true if the command is enabled</returns>
        public abstract bool IsRootCommandEnabled(string commandId, Root root);

        /// <summary>
        /// Called to let the IRootUser know that the Root has been refreshed(through polling).
        /// </summary>
        /// <param name="root">the Root that has been refreshed</param>
        public abstract void OnRootRefreshed(Root root);
    }

    public sealed class CommandInformation
    {
        public string CommandId;
        public string RootId;
        public string TabId;
        public string RootType;
        public string RootLocation;
    }

    /// <summary>
    /// An object that can cause the Ribbon to enter and exit ModalMode and that can handle user interactions with the page when in Modal mode.
    /// </summary>
    internal interface IModalController
    {
        void OnModalBodyClick(HtmlEvent args);

        void OnModalBodyMouseOver(HtmlEvent args);

        void OnModalBodyMouseOut(HtmlEvent args);

        void OnModalContextMenu(HtmlEvent args);

        void OnModalKeyPress(HtmlEvent args);
    }

    /// <summary>
    /// A type to describe the direction of text in a Root.
    /// </summary>
    public enum Direction
    {
        /// <summary>
        /// Left to Right
        /// </summary>
        LTR = 0,
        /// <summary>
        /// Right to Left
        /// </summary>
        RTL = 1
    };

    [Import(MemberNameCasing = Casing.Exact)]
    [Interop(State = InstanceState.JavaScriptOnly)]
    public class RootProperties
    {
        extern public RootProperties();
        extern public string RootEventCommand { get; }
        extern public string ImageDownArrow { get; }
        extern public string ImageDownArrowClass { get; }
        extern public string ImageDownArrowTop { get; }
        extern public string ImageDownArrowLeft { get; }
        extern public string ImageSideArrow { get; }
        extern public string ImageSideArrowClass { get; }
        extern public string ImageSideArrowTop { get; }
        extern public string ImageSideArrowLeft { get; }
        extern public string ImageUpArrow { get; }
        extern public string ImageUpArrowClass { get; }
        extern public string ImageUpArrowTop { get; }
        extern public string ImageUpArrowLeft { get; }
        extern public string TextDirection { get; }
        extern public string ToolTipFooterText { get; }
        extern public string ToolTipFooterImage16by16 { get; }
        extern public string ToolTipFooterImage16by16Class { get; }
        extern public string ToolTipFooterImage16by16Top { get; }
        extern public string ToolTipFooterImage16by16Left { get; }
        extern public string ToolTipDisabledCommandImage16by16 { get; }
        extern public string ToolTipDisabledCommandImage16by16Class { get; }
        extern public string ToolTipDisabledCommandImage16by16Top { get; }
        extern public string ToolTipDisabledCommandImage16by16Left { get; }
        extern public string ToolTipDisabledCommandDescription { get; }
        extern public string ToolTipDisabledCommandTitle { get; }
        extern public string ToolTipDisabledCommandHelpKey { get; }
        extern public string ToolTipHelpCommand { get; }
        extern public string ToolTipSelectedItemTitlePrefix { get; }
    }

    public abstract class Root : Component, IDisposable
    {
        // This will hold all the components that are added to the ribbon
        // The components are hashed by their ids.
        Dictionary<string, Component> _components;

        Dictionary<string, Control> _controls;
        int _commandSequence;
        int _unique;
        bool _rootScrollEventsInitialized = false;

        private Control _lastFocusedControl;
        private Control _lastCommittedControl;


        // Number of milliseconds to wait before setting all the ribbon elements to unselectable
        internal const int CompleteConstructionInterval = 200;
        protected Direction _textDirection;

        string _clientID;
        /// <summary>
        /// The client ID of the outer container of the Root
        /// </summary>
        internal string ClientID
        {
            get
            {
                return _clientID;
            }
            set
            {
                _clientID = value;
            }
        }

        /// <summary>
        /// This is a way of knowing whether the root has ever been refreshed
        /// The root's Initizlizing is set to true until Root.Refresh() is called
        /// </summary>
        bool _initializing = true;
        public bool Initializing
        {
            get
            {
                return _initializing;
            }
        }

        internal Control LastFocusedControl
        {
            get
            {
                return _lastFocusedControl;
            }
            set
            {
                if (!InModalMode)
                {
                    _lastFocusedControl = value;
                }
            }
        }

        internal Control LastCommittedControl
        {
            get
            {
                return _lastCommittedControl;
            }
            set
            {
                if (!InModalMode)
                {
                    _lastCommittedControl = value;
                    _lastFocusedControl = value;
                }
            }
        }

        internal void EnsureCurrentControlStateCommitted()
        {
            // Make sure that the control that currently has focus commits its state
            // before we switch tabs.  Otherwise we can have dataloss: O14:435161
            if (!CUIUtility.IsNullOrUndefined(LastFocusedControl))
            {
                LastFocusedControl.CommitCurrentStateToApplication();
            }
        }


        internal Root(string id, RootProperties properties)
            : base(null, id, null, null)
        {
            InitRootMember(this);

            _properties = properties;
            _components = new Dictionary<string, Component>();
            _controls = new Dictionary<string, Control>();
            _commandSequence = 0;
            _unique = 0;

            if (!string.IsNullOrEmpty(properties.TextDirection))
                _textDirection = properties.TextDirection.ToLower() == "rtl" ? Direction.RTL : Direction.LTR;

            // Add an event so that we can listen for keystrokes when in modal mode
            Browser.Document.KeyDown += OnModalKeyPress;
            Browser.Window.Unload += OnPageUnload;
        }

        /// <summary>
        /// Register control.
        /// </summary>
        /// <param name="control"></param>
        internal void RegisterControl(Control control)
        {
            _controls[control.Id] = control;
        }

        /// <summary>
        /// It's a temporary solution. Please do not use it.
        /// TODO(shaozhu): Should remove it after dynamic menu is supported.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        internal Control GetControlById(string id)
        {
            if (!_controls.ContainsKey(id))
                return null;
            return _controls[id];
        }

        internal override void RefreshInternal()
        {
            base.RefreshInternal();
            _initializing = false;
            if (!_rootScrollEventsInitialized)
            {
                Browser.Window.Scroll += OnWindowScroll;
                _rootScrollEventsInitialized = true;
            }
        }

        internal override void EnsureDOMElement()
        {
            base.EnsureDOMElement();
            if (TextDirection == Direction.RTL)
                Utility.EnsureCSSClassOnElement(ElementInternal, "ms-cui-rtl");
        }

        /// <summary>
        /// Sets the browsers focus on to the last control that had keyboard focus
        /// </summary>
        /// <returns>Whether focus was successfully set on the control.</returns>
        public virtual bool SetFocus()
        {
            Control ctrl = LastFocusedControl;
            if (CUIUtility.IsNullOrUndefined(ctrl))
                return false;

            return ctrl.SetFocusOnControl();
        }

        /// <summary>
        /// Sets the browser focus on to the control that commited the last command.
        /// </summary>
        /// <returns>Whether focus was successfully set on the control.</returns>
        public virtual bool RestoreFocus()
        {
            Control ctrl = LastCommittedControl;
            if (CUIUtility.IsNullOrUndefined(ctrl))
                return false;

            return ctrl.SetFocusOnControl();
        }

        bool _addedContextMenuHandler = false;

        /// <summary>
        /// This method is used for doing things that can be put off until after the visuals of the
        /// command ui are drawn.  For example, attaching event handlers or setting "unselectable='on'"
        /// </summary>
        internal void CompleteConstruction()
        {
            if (!_disposed)
            {
                ElementInternal.ContextMenu += Utility.ReturnFalse;
                _addedContextMenuHandler = true;
            }
#if PERF_METRICS
            PMetrics.PerfMark(PMarker.perfCUIRibbonCompleteConstruction);
#endif
        }

        public virtual void Refresh()
        {
            if (!CUIUtility.IsNullOrUndefined(RootUser))
                RootUser.OnRootRefreshed(this);
        }

        // REVIEW(josefl):  Do we need a public Attach?  Seems like it will be done by the builder.
        public virtual void Attach()
        {
            AttachInternal(true);
        }

        bool _bNeedScaling = false;
        public bool NeedScaling
        {
            get
            {
                return _bNeedScaling;
            }
            set
            {
                _bNeedScaling = value;
            }
        }


        RootUser _rootUser;
        public virtual RootUser RootUser
        {
            get
            {
                return _rootUser;
            }
            set
            {
                _rootUser = value;
            }
        }

        Builder _builder;
        internal virtual Builder Builder
        {
            get
            {
                return _builder;
            }
            set
            {
                _builder = value;
            }
        }

        internal bool PollForState
        {
            get
            {
                return !CUIUtility.IsNullOrUndefined(RootUser);
            }
        }

        internal bool PollForCommandState(string commandId,
                                          string queryCommandId,
                                          Dictionary<string, string> properties)
        {
            return (PollForCommandStateCore(commandId,
                                           queryCommandId,
                                           properties,
                                           false) & 1) > 0;
        }

        internal int PollForCommandStateCore(string commandId,
                                             string queryCommandId,
                                             Dictionary<string, string> properties,
                                             bool alwaysExecuteQueryCommand)
        {
            int result = 0;

            bool commandEnabled = _rootUser.IsRootCommandEnabled(commandId, this);

            if (commandEnabled)
                result = 1;

            // Used for debugging purposes so that it is possible to have a control be enabled
            // without having a page component on the page to answer "true" to the polling for enabled
#if DEBUG
            if (commandId == "DEBUG_ALWAYS_ENABLED")
                result = 1;
#endif

            // If there is no querycommand or the command is not enabled and 
            // we should not force the query command execution, then we return.
            // Some controls like the CheckBox want to attempt to update their state even when
            // they are not enabled.
            if (string.IsNullOrEmpty(queryCommandId) ||
                    (!alwaysExecuteQueryCommand && !commandEnabled))
            {
                return result;
            }

            // If the query command succeeded, then we set the second bit
            if (_rootUser.ExecuteRootCommand(queryCommandId, properties, null, this))
                result |= 2;

            return result;
        }

        public void PollForStateAndUpdate()
        {
            PollForStateAndUpdateInternal();
        }

        internal override void PollForStateAndUpdateInternal()
        {
            LastPollTime = DateTime.Now;
            base.PollForStateAndUpdateInternal();
            EnsureGlobalDisablingRemoved();
        }

        internal virtual protected void EnsureGlobalDisablingRemoved()
        {
            // We begin with this class on the roots so that everything will look disabled
            // After we poll for state we don't need it any more.
            Utility.EnableElement(ElementInternal);
        }

        public HtmlElement Element
        {
            get
            {
                return ElementInternal;
            }
        }

        protected override string CssClass
        {
            get
            {
                return "ms-cui-disabled";
            }
        }

        /// <summary>
        /// The direction of the text within this Root (LTR or RTL)
        /// </summary>
        public Direction TextDirection
        {
            get
            {
                return _textDirection;
            }
        }

        /// <summary>
        /// Get the next number in a monotonically increasing sequence of numbers.
        /// </summary>
        /// <returns></returns>
        internal int GetUniqueNumber()
        {
            return _unique++;
        }

        #region Component Factory
        /// <summary>
        /// Creates a MenuSecton
        /// </summary>
        /// <param name="id">Component id of the MenuSection</param>
        /// <param name="title">Title of the MenuSection</param>
        /// <param name="description">Description of the MenuSection</param>
        /// <returns>the created MenuSection</returns>
        internal MenuSection CreateMenuSection(string id, string title, string description, bool scrollable, string maxheight, string displayMode)
        {
            return new MenuSection(this, id, title, description, scrollable, maxheight, displayMode);
        }

        /// <summary>
        /// Creates a Menu
        /// </summary>
        /// <param name="id">Component id of the Menu</param>
        /// <param name="title">Title of the Menu</param>
        /// <param name="description">Description of the Menu</param>
        /// <returns>the created Menu</returns>
        internal Menu CreateMenu(string id, string title, string description, string maxWidth)
        {
            return new Menu(this, id, title, description, maxWidth);
        }

        /// <summary>
        /// Creates a Gallery
        /// </summary>
        /// <param name="id">Component id of the Gallery</param>
        /// <param name="title">Title of the Gallery</param>
        /// <param name="description">Description of the Gallery</param>
        /// <param name="properties">A GalleryProperties object with the properties of the Gallery</param>
        /// <returns>the created Gallery</returns>

        internal Gallery CreateGallery(string id, string title, string description, GalleryProperties properties)
        {
            return new Gallery(this, id, title, description, properties);
        }
        /// <summary>
        /// Creates a ControlComponent
        /// </summary>
        /// <param name="id">Component id of the ControlComponent</param>
        /// <param name="displayMode">the display mode of the Control that this ControlComponent represents.  ie "Large", "Medium" etc.</param>
        /// <param name="control">The Control that this ControlComponent belongs to</param>
        /// <returns>the created ControlComponent</returns>
        internal ControlComponent CreateControlComponent(string id,
                                                         string displayMode,
                                                         Control control)
        {
            return new ControlComponent(this, id, displayMode, control);
        }

        /// <summary>
        /// Creates a MenuItem
        /// </summary>
        /// <param name="id">Component id of the MenuItem</param>
        /// <param name="displayMode">display mode of the Control that this MenuItem represents</param>
        /// <param name="control">the control to which this MenuItem belongs</param>
        /// <returns>the created MenuItem</returns>
        internal MenuItem CreateMenuItem(string id,
                                         string displayMode,
                                         Control control)
        {
            return new MenuItem(this, id, displayMode, control);
        }

        #endregion

        #region Command Handling
        /// <summary>
        /// Creates a Command
        /// </summary>
        /// <param name="commandId">id of the Command ("paste" etc)</param>
        /// <param name="type">type of the command</param>
        /// <param name="creator">the Component that created the Command</param>
        /// <param name="parameters">Dictionary of extra parameters that go along with this Command</param>
        /// <returns>the created Command</returns>
        internal CommandEventArgs CreateCommandEventArgs(string commandId,
                                                         CommandType type,
                                                         Component creator,
                                                         Dictionary<string, string> properties)
        {
            return new CommandEventArgs(commandId, type, creator, properties);
        }

        /// <summary>
        /// Cause a command event to be raised in the Root.
        /// </summary>
        /// <param name="command"></param>
        internal void ExecuteCommand(CommandEventArgs command)
        {
            // If the command ID is null, don't bother sending to the root user
            if (string.IsNullOrEmpty(command.Id))
                return;

            // REVIEW(josefl): I don't think that we need to track these internal
            // command sequence numbers any more
            command.SequenceNumber = GetNextCommandSequenceNumber();

            CommandInformation ci = command.CommandInfo;
            ci.CommandId = command.Id;
            ci.RootId = Id;
            ci.RootType = RootType;

            if (!CUIUtility.IsNullOrUndefined(RootUser))
                _rootUser.ExecuteRootCommand(command.Id, command.Properties, ci, this);
        }

        internal override bool OnPreBubbleCommand(CommandEventArgs command)
        {
            ExecuteCommand(command);
            return true;
        }

        protected virtual string RootType
        {
            get
            {
                return "Root";
            }
        }

        /// <summary>
        /// Gets the next Command sequence number in the monotonically increasing sequence.
        /// </summary>
        /// <returns></returns>
        internal int GetNextCommandSequenceNumber()
        {
            return _commandSequence++;
        }
        #endregion

        #region Menu Stack Handling
        List<MenuLauncher> _menuLauncherStack;
        /// <summary>
        /// Adds a menu to the Menu Stack
        /// </summary>
        /// <param name="ml">The MenuLauncher that opened the Menu</param>
        internal void AddMenuLauncherToStack(MenuLauncher ml)
        {
            if (CUIUtility.IsNullOrUndefined(MenuLauncherStack))
                MenuLauncherStack = new List<MenuLauncher>();
            MenuLauncherStack.Add(ml);
        }

        bool _closeInProgress = false;
        /// <summary>
        /// Closes a Menu and all open Menus nested under it
        /// </summary>
        /// <param name="parent">The MenuLauncher that opened the Menu to be closed</param>
        internal void CloseMenuStack(MenuLauncher par)
        {
            // If the menu launcher stack is gone, all menus should be closed already
            if (CUIUtility.IsNullOrUndefined(MenuLauncherStack))
                return;

            _closeInProgress = true;
            int index = MenuLauncherStack.IndexOf(par);

            // For each menu nested in the parent, call CloseMenu
            for (int i = MenuLauncherStack.Count - 1; i >= index; i--)
            {
                ((MenuLauncher)MenuLauncherStack[i]).CloseMenu();
                MenuLauncherStack.RemoveAt(i);
            }
            _closeInProgress = false;
        }

        /// <summary>
        /// Closes all open menus in this Root
        /// </summary>
        internal void CloseAllMenus()
        {
            if (_closeInProgress || CUIUtility.IsNullOrUndefined(MenuLauncherStack))
                return;

            for (int i = MenuLauncherStack.Count - 1; i >= 0; i--)
            {
                ((MenuLauncher)MenuLauncherStack[i]).CloseMenu();
                MenuLauncherStack.RemoveAt(i);
            }
        }

        /// <summary>
        /// The current stack of MenuLaunchers with open Menus
        /// </summary>
        internal List<MenuLauncher> MenuLauncherStack
        {
            get
            {
                return _menuLauncherStack;
            }
            set
            {
                _menuLauncherStack = value;
            }
        }

        int _pendingMenuCloseTimeoutId = -1;
        internal int PendingMenuCloseTimeoutId
        {
            get
            {
                return _pendingMenuCloseTimeoutId;
            }
            set
            {
                _pendingMenuCloseTimeoutId = value;
            }
        }

        int _pendingMenuCloseMenuLauncherStackIndex = -1;
        internal int PendingMenuCloseMenuLauncherStackIndex
        {
            get
            {
                return _pendingMenuCloseMenuLauncherStackIndex;
            }
            set
            {
                _pendingMenuCloseMenuLauncherStackIndex = value;
            }
        }
        #endregion

        #region Tooltip Handling
        Control _tooltipLauncher;
        /// <summary>
        /// The control with an open Tooltip.
        /// </summary>
        internal Control TooltipLauncher
        {
            get
            {
                return _tooltipLauncher;
            }
            set
            {
                _tooltipLauncher = value;
            }
        }

        internal void CloseOpenTootips()
        {
            if (!CUIUtility.IsNullOrUndefined(TooltipLauncher))
                TooltipLauncher.CloseToolTip();
        }

        int _toolTipLaunchTimer;
        internal int TooltipLauncherTimer
        {
            get
            {
                return _toolTipLaunchTimer;
            }
            set
            {
                _toolTipLaunchTimer = value;
            }
        }

        IFrame _tooltipBackFrame;
        /// <summary>
        /// IFrame positioned behind tooltips to ensure they render over ActiveX controls in InternetExplorer.
        /// </summary>
        /// <remarks>
        /// The IFrame is defined once and added to the DOM then reused by all tooltip instances. 
        /// Since only one tooltip can render at a time, there is no conflict from doing this.
        /// </remarks>
        internal IFrame TooltipBackFrame
        {
            get
            {
                if (BrowserUtility.InternetExplorer && 
                    CUIUtility.IsNullOrUndefined(_tooltipBackFrame))
                {
                    _tooltipBackFrame = Utility.CreateHiddenIframeElement();
                    _tooltipBackFrame.ClassName = "ms-cui-tooltip-backFrame";
                    _tooltipBackFrame.Style.Visibility = "hidden";
                    Browser.Document.Body.AppendChild(_tooltipBackFrame);
                }
                return _tooltipBackFrame;
            }
        }

        #endregion


        #region Modal Mode Handling
        bool _modal = false;
        Div _elmModalDiv;
        List<IModalController> _modalControllerStack;
        IModalController _currentModalController = null;
        internal bool BeginModal(IModalController controller, HtmlElement _elmTrigger)
        {
            if (CUIUtility.IsNullOrUndefined(_modalControllerStack))
                _modalControllerStack = new List<IModalController>();

            _modalControllerStack.Add(controller);
            _currentModalController = controller;

            if (_modal)
                return false;

            // This div covers the whole window and will essentially block any elements
            // behind it from receiving click/mouseover/mouseout events etc.
            Div modalDiv = ModalDiv;
            modalDiv.Style.Visibility = "hidden";
            Browser.Document.Body.AppendChild(modalDiv);
            modalDiv.Style.Visibility = "visible";
            _modal = true;
            return true;
        }

        /// <summary>
        /// Whether the Ribbon is currently in a modal mode (usually because of a menu/gallery)
        /// </summary>
        internal bool InModalMode
        {
            get
            {
                return _modal;
            }
        }

        internal void EndModal(IModalController controller)
        {
            if (controller != _currentModalController)
                return;

            if (!InModalMode)
                throw new InvalidOperationException("Cannot end modal mode because the Ribbon is not in Modal Mode");

            _modalControllerStack.RemoveAt(_modalControllerStack.Count - 1);
            _currentModalController = null;
            if (_modalControllerStack.Count > 0)
            {
                _currentModalController =
                    (IModalController)_modalControllerStack[_modalControllerStack.Count - 1];
            }

            if (CUIUtility.IsNullOrUndefined(_currentModalController))
                EnsureEndModal();
        }

        private void EnsureEndModal()
        {
            if (!InModalMode)
                return;

            // Don't remove modal div unless all modal controllers are closed
            if (_modalControllerStack.Count == 0)
            {
                Browser.Document.Body.RemoveChild(ModalDiv);
                _modal = false;
                if (BrowserUtility.InternetExplorer)
                {
                    // Office14#39330: Null the div, forcing it to be recreated.
                    // This fixes an IE7-specific WAC bug where the overlay was not getting rendered.
                    ClearModalDivEvents();
                    _elmModalDiv = null;
                }
            }
        }

        private Div ModalDiv
        {
            get
            {
                if (CUIUtility.IsNullOrUndefined(_elmModalDiv))
                {
                    _elmModalDiv = new Div();
                    // Have to use GetField because the compiler compiles this to 
                    // Browser.internetExplorer when it should be "Browser.InternetExplorer"
                    // TODO: fix this
                    if (BrowserUtility.InternetExplorer)
                        _elmModalDiv.ClassName = "ms-cui-modalDiv-ie";
                    else
                        _elmModalDiv.ClassName = "ms-cui-modalDiv-ff";
                    Utility.SetUnselectable(_elmModalDiv, true, false);

                    // Attach functions for the events that we care about when in modal mode
                    _elmModalDiv.Click += OnModalMouseClick;
                    _elmModalDiv.MouseOver += OnModalMouseOver;
                    _elmModalDiv.MouseOut += OnModalMouseOut;
                    _elmModalDiv.ContextMenu += OnModalContextMenu;
                }
                return _elmModalDiv;
            }
        }

        private void OnModalContextMenu(HtmlEvent args)
        {
            _currentModalController.OnModalContextMenu(args);
        }
        private void OnModalMouseClick(HtmlEvent args)
        {
            _currentModalController.OnModalBodyClick(args);
        }
        private void OnModalMouseOver(HtmlEvent args)
        {
            _currentModalController.OnModalBodyMouseOver(args);
        }
        private void OnModalMouseOut(HtmlEvent args)
        {
            _currentModalController.OnModalBodyMouseOut(args);
        }
        private void OnModalKeyPress(HtmlEvent args)
        {
            // If we are not in modal mode, we don't do anything
            if (!_modal)
                return;

            // TODO: rawevent doesn't work in FF I think
            //evt.RawEvent.CancelBubble = true;
            //evt.StopPropagation();
            //return;

            _currentModalController.OnModalKeyPress(args);
        }
        #endregion

        protected virtual void OnWindowScroll(HtmlEvent args)
        {
            CloseAllMenus();
            CloseOpenTootips();
        }

        internal void FlushPendingChanges()
        {
            EnsureCurrentControlStateCommitted();
        }

        private void OnPageUnload(HtmlEvent args)
        {
            Dispose();
        }

        private void ClearModalDivEvents()
        {
            if (!CUIUtility.IsNullOrUndefined(_elmModalDiv))
            {
                _elmModalDiv.Click -= OnModalMouseClick;
                _elmModalDiv.MouseOver -= OnModalMouseOver;
                _elmModalDiv.MouseOut -= OnModalMouseOut;
                _elmModalDiv.ContextMenu -= OnModalContextMenu;
            }
        }

        public override void Dispose()
        {
            _disposed = true;

            ClearModalDivEvents();

            if (_rootScrollEventsInitialized)
                Browser.Window.Scroll -= OnWindowScroll;

            if (!CUIUtility.IsNullOrUndefined(ElementInternal) && _addedContextMenuHandler)
                ElementInternal.ContextMenu -= Utility.ReturnFalse;

            // This is needed because of a bug in the Microsoft.Ajax toolkit.  Please see O14:207300
            try
            {
                Browser.Document.KeyDown -= OnModalKeyPress;
            }
            catch (Exception)
            {
            }

            Browser.Window.Unload -= OnPageUnload;

            _elmModalDiv = null;
            _rootUser = null;
            _builder = null;
            base.Dispose();
        }

        bool _disposed = false;
        internal virtual bool Disposed
        {
            get
            {
                return _disposed;
            }
            set
            {
                _disposed = value;
            }
        }

        #region Position & Size
        internal override int ComponentWidth
        {
            get
            {
                if (_componentWidth == -1 && !CUIUtility.IsNullOrUndefined(ElementInternal))
                {
                    _componentWidth = ElementInternal.OffsetWidth;
                }

                return _componentWidth;
            }
        }

        internal override int ComponentHeight
        {
            get
            {
                if (_componentHeight == -1 && !CUIUtility.IsNullOrUndefined(ElementInternal))
                {
                    _componentHeight = ElementInternal.OffsetHeight;
                }

                return _componentHeight;
            }
        }

        internal override int ComponentTopPosition
        {
            get
            {
                if (_componentTopPosition == -1 && !CUIUtility.IsNullOrUndefined(ElementInternal))
                {
                    _componentTopPosition = UIUtility.CalculateOffsetTop(ElementInternal);

                }
                return _componentTopPosition;
            }
        }

        internal override int ComponentLeftPosition
        {
            get
            {
                if (_componentLeftPosition == -1 && !CUIUtility.IsNullOrUndefined(ElementInternal))
                {
                    _componentLeftPosition = UIUtility.CalculateOffsetLeft(ElementInternal);
                }

                return _componentLeftPosition;
            }
        }
        #endregion

        // This is used for Roots that can store information in cookies
        // The information will be versioned by this version string
        // in such a way that it will get invalidated when there is a
        // new version.
        string _cookieDataVersion = "";
        public string CookieDataVersion
        {
            get
            {
                return _cookieDataVersion;
            }
            set
            {
                _cookieDataVersion = value;
            }
        }

        bool _useDataCookie = false;
        public bool UseDataCookie
        {
            get
            {
                return _useDataCookie;
            }
            set
            {
                _useDataCookie = value;
            }
        }

        protected void StoreDataCookie(string name, string value)
        {
            DateTime date = DateTime.Now;
            // TODO: decide on when to expire this cookie 
            // Make it expire in seven days for now
            date.AddDays(7);
            Browser.Document.Cookie = name + "=" + CookieDataVersion + value +
                "; expires=" + date.ToString() + "; path=/";
        }

        protected string GetDataCookieValue(string name)
        {
            // We prepend the cookie data version to the name because this is 
            // how the cookie was stored.
            name = CookieDataVersion + name;

            string[] values = Browser.Document.Cookie.Split(new char[] { ';' });
            for (int i = 0; i < values.Length; i++)
            {
                string value = values[i].TrimStart();
                if (value.StartsWith(name))
                {
                    // Compare for equal sign separately for possible small perf gain
                    if (value.StartsWith(name + "="))
                        return value.Substring(name.Length + 1);
                }
            }
            return "";
        }

        RootProperties _properties = null;
        internal RootProperties Properties
        {
            get
            {
                return _properties;
            }
        }

        #region positioning flyouts such as menus and tooltips
        private bool _fixedPositioningEnabled;
        internal bool FixedPositioningEnabled
        {
            get
            {
                return _fixedPositioningEnabled;
            }
            set
            {
                _fixedPositioningEnabled = value;
            }
        }

        /// <summary>
        /// Position the flyout control vertically, relative to the launching control and the viewing area on the screen.
        /// </summary>
        /// <param name="flyOut">The flyout to be launched.</param>
        /// <param name="launcher">The element launching the tooltip.</param>
        internal virtual void PositionFlyOut(HtmlElement flyOut, HtmlElement launcher)
        {
            UpdateFlyOutPosition(flyOut, launcher, false);
        }

        /// <summary>
        /// Position the flyout control horizontally, relative to the launching control and the viewing area on the screen.
        /// </summary>
        /// <param name="flyOut">The flyout to be launched.</param>
        /// <param name="launcher">The element launching the tooltip.</param>
        internal virtual void PositionFlyOutHorizontal(HtmlElement flyOut, HtmlElement launcher)
        {
            UpdateFlyOutPosition(flyOut, launcher, true);
        }

        private void UpdateFlyOutPosition(HtmlElement flyOut, HtmlElement launcher, bool horizontal)
        {
            if (CUIUtility.IsNullOrUndefined(flyOut) || CUIUtility.IsNullOrUndefined(launcher))
            {
                return;
            }

            // Set temporary position on flyOut and get dimensions
            flyOut.Style.Top = "0px";
            flyOut.Style.Left = "0px";
            Dictionary<string, int> dimensions = GetAllElementDimensions(flyOut, launcher);

            SetFlyOutCoordinates(flyOut, dimensions, horizontal);
        }

        /// <summary>
        /// Set fyout coordinates given information about the viewing area in the screen, the launching control and the flyout dimensions.
        /// </summary>
        /// <param name="flyOut">The flyout to be launched.</param>
        /// <param name="dimensions">Position information on the screen viewing area, lauching control and flyout.</param>
        internal void SetFlyOutCoordinates(HtmlElement flyOut, Dictionary<string, int> dimensions, bool horizontal)
        {
            // reusable boolean to check whether the dom element must be edited or not
            bool changed = false;
            if (CUIUtility.IsNullOrUndefined(flyOut) || CUIUtility.IsNullOrUndefined(dimensions))
            {
                return;
            }

            // Create default coordinates
            int flyOutLeft;
            int flyOutTop;

            // Cache dimensions for performance
            int launcherLeft = dimensions["launcherLeft"];
            int launcherTop = dimensions["launcherTop"];
            int launcherWidth = dimensions["launcherWidth"];
            int launcherHeight = dimensions["launcherHeight"];

            int flyOutWidth = dimensions["flyOutWidth"];
            int flyOutHeight = dimensions["flyOutHeight"];
            int flyOutRealHeight = dimensions["flyOutRealHeight"];

            int viewportWidth = dimensions["viewportWidth"];
            int viewportHeight = dimensions["viewportHeight"];

            int viewableLeft = dimensions["viewableLeft"];
            int viewableTop = dimensions["viewableTop"];

            bool isLTR = Root.TextDirection == Direction.LTR;
            bool buttedToRightEdge = false, buttedToLeftEdge = false;

            string objScrollable = flyOut.GetAttribute("mscui:scrollable");
            bool wasScrollable =  Utility.IsTrue(objScrollable);

            if (horizontal)
            {
                if (isLTR)
                {
                    // Left-to-right method
                    flyOutLeft = launcherLeft + launcherWidth;
                    flyOutLeft += 2; // styling: make the borders of the menus align
                }
                else // Right-to-left method
                {
                    flyOutLeft = launcherLeft - flyOutWidth;
                }

                flyOutTop = launcherTop;
            }
            else
            {
                if (isLTR)
                {
                    // Left-to-right method
                    flyOutLeft = launcherLeft;
                }
                else // Right-to-left method
                {
                    flyOutLeft = launcherLeft + launcherWidth - flyOutWidth;
                }

                flyOutTop = launcherTop + launcherHeight;
                int minWidth = launcherWidth >= 2 ? launcherWidth - 2 : launcherWidth /* borders */; // O14:389872 - flyout should be at least as wide as the launcher
                if (minWidth > flyOutWidth)
                    flyOutWidth = minWidth;
                flyOut.Style.MinWidth = minWidth + "px";
            }

            // If the root is fixed positioned, then we need to take scroll offset into account (O14:28297)
            if (FixedPositioningEnabled)
            {
                flyOutTop += viewableTop;
                flyOutLeft += viewableLeft;
            }

            flyOut.Style.Top = flyOutTop + "px";
            flyOut.Style.Left = flyOutLeft + "px";

            // Horizonal Positioning
            // If the flyOut can fit in the viewport at all, then try positioning logic
            if (flyOutWidth <= viewportWidth)
            {
                // If the flyOut is too close to the right side of the viewport
                if (flyOutLeft + flyOutWidth > viewableLeft + viewportWidth)
                {
                    // If we're positioning horizontally and the flyout can fit
                    // launching the other way, try that
                    if (horizontal && isLTR && (launcherLeft - flyOutWidth) > viewableLeft)
                    {
                        flyOutLeft = launcherLeft - flyOutWidth;
                    }
                    // Otherwise, just align along the edge of the viewport
                    else
                    {
                        flyOutLeft = viewableLeft + viewportWidth - flyOutWidth - 5;
                        buttedToRightEdge = true;
                    }

                    changed = true;
                }
                // If the flyOut is too close to the left side of the viewport
                else if (flyOutLeft < viewableLeft)
                {
                    // If we're positioning horizontally and the flyout can fit
                    // launching the other way, try that
                    if (horizontal && !isLTR && (launcherLeft + launcherWidth + flyOutWidth) < (viewableLeft + viewportWidth))
                    {
                        flyOutLeft = launcherLeft + launcherWidth;
                    }
                    // Otherwise, just align along the edge of the viewport
                    else
                    {
                        flyOutLeft = viewableLeft + 5;
                        buttedToLeftEdge = true;
                    }

                    changed = true;
                }
                else
                {
                    changed = false;
                }
            }
            // If the flyOut can't fit into the viewport, just align against the appropriate edge
            else
            {
                if (isLTR)
                {
                    flyOutLeft = viewableLeft;
                    changed = true;
                }
                else // RTL
                {
                    flyOutLeft = viewableLeft + viewportWidth - flyOutWidth;
                    changed = true;
                }
            }
            // If changed, set the styles accordingly
            if (changed)
            {
                flyOut.Style.Left = flyOutLeft + "px";
                changed = false;
            }

            // Vertical Positioning (not affected by Text Direction)

            // If the flyOut is too close to the bottom of the viewport, launch the flyOut upwards
            // We work with the real height of the flyout's content here so that even if it is 
            // currently scrollable, we can see if it still needs to be scrollable
            if (flyOutTop + flyOutRealHeight > viewableTop + viewportHeight)
            {
                // store initial value and viewableHeight
                int oldflyOutTop = flyOutTop;
                int oldflyOutViewableHeight = viewableTop + viewportHeight - flyOutTop;

                flyOutTop = launcherTop - flyOutRealHeight;
                if (FixedPositioningEnabled)
                    flyOutTop += viewableTop;

                int newflyOutViewableHeight = launcherTop;
                if (!FixedPositioningEnabled)
                    newflyOutViewableHeight -= viewableTop;
                changed = true;

                // If launching upwards is worse than before, go back to launching downwards
                if (newflyOutViewableHeight < flyOutRealHeight)
                {
                    int flyOutWidthWithScrollbar = flyOutWidth + 22;

                    if (newflyOutViewableHeight < oldflyOutViewableHeight)
                    {
                        flyOutTop = oldflyOutTop;

                        // O14:45827 - Enable scrolling and change the height of the menu to fit in the space available
                        flyOut.Style.MaxHeight = (oldflyOutViewableHeight - 5) + "px";

                        if (!wasScrollable)
                        {
                            flyOut.Style.OverflowY = "scroll";
                            flyOut.Style.Width = flyOutWidthWithScrollbar + "px";
                        }

                        if (buttedToRightEdge && isLTR)
                        {
                            flyOutLeft -= 27; // compensate for the added scrollbar
                            flyOut.Style.Left = flyOutLeft + "px";
                        }
                        else if (buttedToLeftEdge && !isLTR)
                        {
                            flyOutLeft += 27; // compensate for the added scrollbar
                            flyOut.Style.Left = flyOutLeft + "px";
                        }

                        changed = false;
                    }
                    else
                    {
                        // Can't fit in either direction, but launching upwards is better
                        // Still need to resize the menu and enable scrolling
                        flyOut.Style.MaxHeight = (newflyOutViewableHeight - 5) + "px";

                        if (!wasScrollable)
                        {
                            flyOut.Style.OverflowY = "scroll";
                            flyOut.Style.Width = flyOutWidthWithScrollbar + "px";
                        }

                        if (buttedToRightEdge && isLTR)
                        {
                            flyOutLeft -= 27; // compensate for the added scrollbar
                            flyOut.Style.Left = flyOutLeft + "px";
                        }
                        else if (buttedToLeftEdge && !isLTR)
                        {
                            flyOutLeft += 27; // compensate for the added scrollbar
                            flyOut.Style.Left = flyOutLeft + "px";
                        }
                    }

                    if (!wasScrollable)
                        flyOut.SetAttribute("mscui:scrollable", "true");
                }
                else // fits fine when launching upwards
                {
                    // Revert auto-scroll styles to default if not needed
                    if (wasScrollable)
                    {
                        flyOut.Style.MaxHeight = "none";
                        flyOut.Style.OverflowY = "visible";
                        flyOut.Style.Width = "auto";
                        flyOut.SetAttribute("mscui:scrollable", "false");
                    }
                }
            }
            else // fits fine vertically
            {
                // Revert auto-scroll styles to default if not needed
                if (wasScrollable)
                {
                    flyOut.Style.MaxHeight = "none";
                    flyOut.Style.OverflowY = "visible";
                    flyOut.Style.Width = "auto";
                    flyOut.SetAttribute("mscui:scrollable", "false");
                }
                changed = false;
            }

            // If changed, set the styles accordingly
            if (changed)
            {
                flyOut.Style.Top = flyOutTop + "px";
                changed = false;
            }
        }

        /// <summary>
        /// Get all pertinent element dimensions for positioning the tooltip        
        /// </summary>
        /// <param name="flyOut">The flyOut to be launched. If null, then only get launcher dimensions</param>
        /// <param name="launcher">The element that is launching the flyOut</param>
        /// <returns>A dictionary with all the necessary dimensional data to position the flyOut</returns>
        internal Dictionary<string, int> GetAllElementDimensions(HtmlElement flyOut, HtmlElement launcher)
        {
            Dictionary<string, int> d = new Dictionary<string, int>();
            if (CUIUtility.IsNullOrUndefined(flyOut) || CUIUtility.IsNullOrUndefined(launcher))
            {
                return d;
            }

            // Get dimensions of the launcher
            d["launcherWidth"] = launcher.OffsetWidth;
            d["launcherHeight"] = launcher.OffsetHeight;

            // Get position coordinates for the launcher
            int top = launcher.OffsetTop, left = launcher.OffsetLeft;
            if (!CUIUtility.IsNullOrUndefined(launcher.OffsetParent))
            {
                HtmlElement elem = launcher.OffsetParent;
                for (/*top, left*/; !CUIUtility.IsNullOrUndefined(elem); elem = elem.OffsetParent)
                {
                    top += elem.OffsetTop;
                    left += elem.OffsetLeft;
                }
            }
            else
            {
                top = launcher.OffsetTop;
                left = launcher.OffsetLeft;
            }

            // account for scrolling
            if (!CUIUtility.IsNullOrUndefined(launcher.ParentNode))
            {
                int scrollTop = 0;
                int scrollLeft = 0;
                HtmlElement elem2 = (HtmlElement)launcher.ParentNode;
                for (/*scrollTop, scrollLeft*/; !CUIUtility.IsNullOrUndefined(elem2) && elem2.TagName.ToLower() != "html"; elem2 = (HtmlElement)elem2.ParentNode)
                {
                    if (elem2.ScrollTop > 0)
                    {
                        scrollTop += elem2.ScrollTop;
                    }

                    if (elem2.ScrollLeft > 0)
                    {
                        if ((elem2 == Browser.Window.Document.DocumentElement) && BrowserUtility.InternetExplorer7 && Root.TextDirection == Direction.RTL)
                        {
                            // O14: 645034 - IE 7 mis-reports the Document.DocumentElement.ScrollLeft value in RTL layouts
                            scrollLeft += Browser.Document.Body.ScrollLeft;
                        }
                        else
                        {
                            scrollLeft += elem2.ScrollLeft;
                        }
                    }
                }

                if (top >= scrollTop)
                {
                    top -= scrollTop;
                }

                if (left >= scrollLeft)
                {
                    left -= scrollLeft;
                }
            }

            d["launcherTop"] = top;
            d["launcherLeft"] = left;

            if (flyOut != null)
            {
                // Get dimensions of the flyOut
                d["flyOutWidth"] = flyOut.OffsetWidth;
                d["flyOutHeight"] = flyOut.OffsetHeight;
                d["flyOutRealHeight"] = flyOut.ScrollHeight;

                // Get dimensions of viewport
                // IE7, Mozilla, Firefox, Opera, Safari
                d["viewportWidth"] = Utility.GetViewPortWidth();
                d["viewportHeight"] = Utility.GetViewPortHeight();

                // Get viewable coordinates of the viewport - Firefox 3
                d["viewableTop"] = Browser.Document.DocumentElement.ScrollTop;
                d["viewableLeft"] = Browser.Document.DocumentElement.ScrollLeft;

                // IE7, Mozilla, Opera, Safari
                if (CUIUtility.IsNullOrUndefined(d["viewableTop"]))
                {
                    JSObject windowObject = (JSObject)(object)Browser.Window;
                    d["viewableTop"] = windowObject.GetField<int>("pageYOffset");
                    d["viewableLeft"] = windowObject.GetField<int>("pageXOffset");
                }
                if (BrowserUtility.InternetExplorer7 && (Root.TextDirection == Direction.RTL))
                {
                    // O14: 645034 - IE 7 mis-reports the Document.DocumentElement.ScrollLeft value in RTL layouts
                    d["viewableLeft"] = Browser.Document.Body.ScrollLeft;
                }
            }

            return d;
        }
        #endregion
    }
}
