using System;
using System.Collections.Generic;
using Microsoft.LiveLabs.Html;
using Microsoft.LiveLabs.JavaScript;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace Ribbon
{
    [Import(MemberNameCasing = Casing.Exact)]
    public class RibbonProperties : RootProperties
    {
        extern public RibbonProperties();
        extern public string Image32by32GroupPopupDefault { get; }
        extern public string Image32by32GroupPopupDefaultClass { get; }
        extern public string Image32by32GroupPopupDefaultTop { get; }
        extern public string Image32by32GroupPopupDefaultLeft { get; }
        extern public string TabSwitchCommand { get; }
        extern public string ScaleCommand { get; }
        extern public string ShortcutKeyJumpToRibbon_Ctrl { get; }
        extern public string ShortcutKeyJumpToRibbon_Alt { get; }
        extern public string ShortcutKeyJumpToRibbon_Shift { get; }
        extern public string ShortcutKeyJumpToRibbon_AccessKey { get; }
        extern public string ShortcutKeyJumpToFirstControl_Ctrl { get; }
        extern public string ShortcutKeyJumpToFirstControl_Alt { get; }
        extern public string ShortcutKeyJumpToFirstControl_Shift { get; }
        extern public string ShortcutKeyJumpToFirstControl_AccessKey { get; }
        extern public string NavigationHelpText { get; }
        extern public string ATContextualTabText { get; }
        extern public string ATTabPositionText { get; }
    }

    /// <summary>
    /// The in memory structure of the Ribbon is a hierarchy.  Each member of this Ribbon hierarchy is a subclass of Component.  This hierarchy represents the current state of the Ribbon.  The Ribbon State can be manipulated by calling methods on the Components in the Ribbon Hierarchy.  There are some methods like addChild(), removeChild() etc that are common to all Ribbon Components and there are other methods like Group.selectLayout() that are particular to a particular subclass of Ribbon Component.  These methods change the Ribbon State.
    /// Changing the Ribbon State through method calls on Components in the Ribbon Component Hierarchy dirties the Ribbon.  When the Ribbon is dirty, it simply means that the Ribbon State is not accurately reflected in the Ribbon's DOM ElementInternal.  In order for the Ribbon State to be reflected in the Ribbon's DOM Element, Ribbon.Refresh() has to be called.  When this is called, the Ribbon Hierarchy does a recursive RefreshInternal() on every Component in the Hierarchy that is dirty.  Calling Ribbon.Refresh() causes the Ribbon to enter a clean state, meaning that the Ribbon DOM Element accurately reflects the Ribbon State.
    ///<para>
    ///This class is the top level container of all ribbon components.  
    ///So I will use this place to write general documentation about this 
    ///Ribbon Framework.
    ///</para>
    ///<para>, 
    ///CSS STYLING
    ///-------------------------------------------------------------------------------
    ///All CSS Styles used in the Ribbon framework begin with: "ms-cui".  The
    ///Component type comes next.  So, for example, the class used for the outer DOMElement
    ///of the Section Component would be "ms-cui-section".  Every Control has a 
    ///four letter abbreviation.  For example Button(fsea), FSLabel(fslb) etc.
    ///These should prefix any Control specific CSS classes.  So, for example, a class
    ///that is used for the down arrow in the DropDown(fscb) class would be called 
    ///something like: "ms-cui-cb-arrow".  "ctl" is used for classes that are 
    ///generic to all controls.  For example, a class that would be applied to all controls
    ///when they get disabled would be called: "ms-cui-ctl-disabled".
    ///</para>
    ///
    ///Here is a list of Control abbreviations
    ///
    ///ToggleButton - tbtn
    ///DropDown - dd
    ///Button - btn
    ///SplitButton - sbtn
    ///FlyoutAnchor - fa
    ///FSLabel - lbl
    ///
    ///
    ///CONTROL PARAMETERS:
    ///-------------------------------------------------------------------------------
    ///There will be many controls that will be implemented.  They all take a property
    ///bag of parameters.  Common parameter names that are used for basically the same
    ///thing in multiple controls should have standardized names and abbriviations.
    ///
    ///QUESTIONS:
    ///-------------------------------------------------------------------------------
    ///If you have any questions of feedback regarding this framework you can email JosefL.
    /// </summary>
    public class SPRibbon : Root
    {
        Tab _selectedTab;
        Tab _previousTab;
        Div _elmRibbonTopBars;            // will hold the two top bar containers
        Div _elmTopBar1;                  // QAT (if applicable) & Center & Right peripheral content
        Div _elmTopBar2;                  // the tab titles container & left & right peripheral content
        UnorderedList _elmTabTitles;      // Will hold tab titles
        HtmlElement _elmScrollCurtain;    // appears when the ribbon docks to hide scrolling content above the ribbon
        Div _elmJewelPlaceholder;         // will hold the jewel button
        Span _elmQATPlaceholder;          // will hold the QAT row of buttons
        Div _elmTabContainer;             // Will hold the body of the selected tab
        Span _elmNavigationInstructions;
        string _oldDOMElementDisplayValue;

        // Peripheral content containers
        Div _elmTabRowLeft;
        Div _elmTabRowRight;
        Div _elmQATRowCenter;
        Div _elmQATRowRight;

        bool _peripheralContentsLoaded = false;

        // Used for ribbon accessibility shortcuts
        HtmlElement _elmStoredFocus;
        string _initialTabTitle;
        string _jumpToLastFocusedKeys;
        string _jumpToRibbonTabKeys;

        bool _windowResizedHandlerEnabled = false;
        bool _eventHandlerAttached = false;
        Dictionary<string, ContextualGroup> _contextualGroups;

        /// <summary>
        /// Creates a Ribbon.
        /// </summary>
        /// <param name="id">The Component id for the Ribbon.</param>
        internal SPRibbon(string id, RibbonProperties properties)
            : base(id, properties)
        {
            _contextualGroups = new Dictionary<string, ContextualGroup>();

            _jumpToRibbonTabKeys = properties.ShortcutKeyJumpToRibbon_Ctrl +
                    properties.ShortcutKeyJumpToRibbon_Alt +
                    properties.ShortcutKeyJumpToRibbon_Shift +
                    properties.ShortcutKeyJumpToRibbon_AccessKey;
            _jumpToLastFocusedKeys = properties.ShortcutKeyJumpToFirstControl_Ctrl +
                    properties.ShortcutKeyJumpToFirstControl_Alt +
                    properties.ShortcutKeyJumpToFirstControl_Shift +
                    properties.ShortcutKeyJumpToFirstControl_AccessKey;

            _viewPortWidth = Utility.GetViewPortWidth();
            _viewPortHeight = Utility.GetViewPortHeight();
            _lastWindowResizeWidthHeight = GetWindowWidthHeightString();
        }

        /// <summary>
        /// The DOM Element which had focus before calling the ribbon shortcut
        /// </summary>
        public HtmlElement StoredFocus
        {
            get 
            { 
                return _elmStoredFocus; 
            }
            set 
            { 
                _elmStoredFocus = value; 
            }
        }

        private bool ShouldStoreFocus(HtmlElement elm)
        {
            if (CUIUtility.IsNullOrUndefined(elm))
                return false;
            else if (CUIUtility.SafeString(elm.Id) == "Ribbon")
                return false;
            else if (elm.TagName.ToLower() == "body")
                return true;
            else
                return ShouldStoreFocus((HtmlElement)elm.ParentNode);
        }


        #region Component Overrides
        /// <summary>
        /// Cause the in memory state of the Ribbon Component hierarchy to be reflected in the Ribbon's DOMElementInternal.
        /// </summary>
        public override void Refresh()
        {
            // REVIEW(josefl): review how this is done here.
            RefreshInternal();
            base.Refresh();
            Scale();
        }

        internal override void RefreshInternal()
        {
            // First we ensure that the top level DOM elements have been created
            base.RefreshInternal();
            EnsureDOMElement();
            HandlePeripheralsAndTabTitles();

            // Now go through all tab titles and add them to the tab container div
            Tab selTab = null;
            Tab firstVisibleTab = null, lastVisibleTab = null;
            string currentCtxId = null;
            ContextualGroup currentCtxGroup = null;

            // PERFREVIEW: I'm using the slow method here because I think it will be faster
            // than storing the old _elmTabTitles, calling .RemoveChild and then inserting the new
            // one after.  Tab titles should usually have three children.  It also saves
            // code
            Utility.RemoveChildNodesSlow(_elmTabTitles);

            foreach (ContextualGroup cg in _contextualGroups.Values)
            {
                cg.EnsureTabTitlesCleared();
            }

            int count = 0;
            List<Tab> visibleTabs = new List<Tab>();
            foreach (Tab tab in Children)
            {
                if (tab.Visible)
                {
                    visibleTabs.Add(tab);
                    count++;
                }
            }

            int tabPosition = 0;
            int tabCount = visibleTabs.Count;
            bool accessibilityTextSet = !(string.IsNullOrEmpty(RibbonProperties.ATTabPositionText) || string.IsNullOrEmpty(RibbonProperties.ATContextualTabText));

            foreach (Tab tab in visibleTabs)
            {
                // Make sure that the Tab's DOM Element is up to date before we add it
                tab.EnsureTitleRefreshed();

                if (CUIUtility.IsNullOrUndefined(firstVisibleTab))
                {
                    firstVisibleTab = tab;
                    _initialTabTitle = firstVisibleTab.Id;
                }

                // Revert the tab's CSS classes to default since this might not
                // be the first refresh call (O14:371478)
                tab.ResetTitleCSSClasses();

                if (tab.Contextual)
                {
                    if (currentCtxId == null || tab.ContextualGroupId != currentCtxId)
                    {
                        if (tab.ContextualGroupId != currentCtxId)
                        {
                            if (!CUIUtility.IsNullOrUndefined(lastVisibleTab) && lastVisibleTab.Contextual)
                                Utility.EnsureCSSClassOnElement(lastVisibleTab.TitleDOMElement, "ms-cui-ct-last");
                        }

                        currentCtxId = tab.ContextualGroupId;
                        currentCtxGroup = GetContextualGroup(tab.ContextualGroupId);
                        Utility.RemoveCSSClassFromElement(currentCtxGroup.ElementInternal, "ms-cui-cg-s");
                        SetContextualColor(ContextualColor.None);

                        _elmTabTitles.AppendChild(currentCtxGroup.ElementInternal);
                        Utility.EnsureCSSClassOnElement(tab.TitleDOMElement, "ms-cui-ct-first");
                    }
                    currentCtxGroup.AddTabTitleDOMElement(tab.TitleDOMElement);
                }
                else
                {
                    if (!string.IsNullOrEmpty(currentCtxId))
                    {
                        // No need to check if lastVisibleTab is valid here because this path is only reached if currentCtxId
                        // is not null which implies that there was a previous tab processed that was contextual
                        Utility.EnsureCSSClassOnElement(lastVisibleTab.TitleDOMElement, "ms-cui-ct-last");
                        currentCtxId = "";
                        currentCtxGroup = null;
                    }
                    if (accessibilityTextSet)
                    {
                        tabPosition++;
                        tab.SetContextualText(RibbonProperties.ATTabPositionText, "", "", tabPosition, tabCount);
                    }
                    _elmTabTitles.AppendChild(tab.TitleDOMElement);
                }
                // If the selected tab is still visible, then we want to have it
                // selected after we refresh
                if (tab == _selectedTab)
                    selTab = tab;

                lastVisibleTab = tab;
            }
            if (currentCtxId != null)
            {
                // No need to check if lastVisibleTab is valid here because this path is only reached if currentCtxId
                // is not null which implies that there was a previous tab processed that was contextual
                Utility.EnsureCSSClassOnElement(lastVisibleTab.TitleDOMElement, "ms-cui-ct-last");
                lastVisibleTab = null;
                currentCtxId = null;
                currentCtxGroup = null;
            }

            int ctxlTabPosition = 1;

            // Add JAWS text
            if (accessibilityTextSet)
            {
                for (int i = 0; i < count; i++)
                {
                    Tab tab = (Tab)visibleTabs[i];
                    if (tab.Contextual)
                    {
                        if (tab.ContextualGroupId != currentCtxId)
                        {
                            currentCtxId = tab.ContextualGroupId;
                            currentCtxGroup = GetContextualGroup(tab.ContextualGroupId);
                            ctxlTabPosition = 1;
                        }
                        tab.EnsureHiddenATDOMElement();
                        tab.SetContextualText(RibbonProperties.ATTabPositionText, RibbonProperties.ATContextualTabText, currentCtxGroup.Title, ctxlTabPosition, currentCtxGroup.Count);
                        ctxlTabPosition++;
                    }
                }
            }

            if (!CUIUtility.IsNullOrUndefined(selTab))
            {
                if (selTab.Contextual)
                {
                    ContextualGroup cg = GetContextualGroup(selTab.ContextualGroupId);
                    Utility.EnsureCSSClassOnElement(cg.ElementInternal, "ms-cui-cg-s");
                    SetContextualColor(cg.Color);
                }
            }

            // If the old selected tab is not visible any more and the ribbon is not minimized, then
            // just select the last selected non contextual tab or first tab 
            // that is visible in the ribbon.            
            if (CUIUtility.IsNullOrUndefined(selTab) && !_minimized)
            {
                selTab = !CUIUtility.IsNullOrUndefined(_lastSelectedNonContextualTab) ?
                _lastSelectedNonContextualTab : firstVisibleTab;
                MakeTabSelectedInternal(selTab);
            }

            // When the ribbon is minimized, no tabs are selected
            if (_minimized)
                selTab = null;

            UpdateDOMForSelectedTab();

            if (!CUIUtility.IsNullOrUndefined(selTab))
            {
                // We want to make sure that all controls render disabled first if
                // this tab needs to poll for its state.
                if (selTab.RootPolledSinceLastPoll)
                    Utility.DisableElement(_elmTabContainer);

                bool disabled = _elmTabContainer.ClassName.IndexOf("ms-cui-disabled") != -1;
                _elmTabContainer.ClassName = selTab.GetContainerCSSClassName() + (disabled ? " ms-cui-disabled" : "");
            }

            AttachEvents();
            Dirty = false;
            if (_minimizedChanged && !string.IsNullOrEmpty(RibbonProperties.RootEventCommand))
            {
                Dictionary<string, string> props = new Dictionary<string, string>();
                props["RootId"] = Id;
                props["RootType"] = "Ribbon";
                props["Minimized"] = Minimized.ToString();
                props["Maximized"] = (!Minimized).ToString();
                RaiseCommandEvent(RibbonProperties.RootEventCommand,
                                  CommandType.RootEvent,
                                  props);
                _minimizedChanged = false;
            }
        }

        /// <summary>
        /// Attach this Component to an already existing DOM Element.
        /// The default behavior is to attach to Document.GetElementById(this.Id)
        /// </summary>
        internal override void AttachInternal(bool recursive)
        {
            AttachDOMElements();
            AttachEvents();
            Dirty = false;

            if (recursive)
            {
                if (!CUIUtility.IsNullOrUndefined(Children))
                {
                    foreach (Tab tab in Children)
                    {
                        // Don't try to attach to tabs that are not visible.
                        // since they will not have DOM elements.
                        if (!tab.Visible)
                            continue;

                        if (!CUIUtility.IsNullOrUndefined(_selectedTab) && _selectedTab == tab)
                        {
                            // Only one tab body can be shown at a time in the DOM so we only 
                            // need to attach to the selected tab if there is one.
                            _selectedTab.AttachInternal(recursive);
                        }
                        else
                        {
                            // Attache the DOM titles of the non shown tabs
                            tab.AttachTitle();
                            tab.AttachTitleEvents();
                        }
                    }
                }
            }

            if (!CUIUtility.IsNullOrUndefined(_contextualGroups))
            {
                foreach (ContextualGroup cg in _contextualGroups.Values)
                {
                    cg.AttemptAttachDOMElements();
                }
            }
        }

        internal override void AttachDOMElements()
        {
            // Attach the outer Ribbon Element
            base.AttachDOMElements();
            DomNodeCollection childElms = ElementInternal.ChildNodes;
            _elmScrollCurtain = Browser.Document.GetById("cui-" + Id + "-scrollCurtain");
            _elmNavigationInstructions = (Span)childElms[0];
            _elmRibbonTopBars = (Div)childElms[1];
            _elmTopBar1 = (Div)_elmRibbonTopBars.ChildNodes[0];
            _elmTopBar2 = (Div)_elmRibbonTopBars.ChildNodes[1];
            _elmJewelPlaceholder = (Div)_elmTopBar2.ChildNodes[0];
            if (childElms.Length > 2)
                _elmTabContainer = (Div)childElms[2];

            _elmQATPlaceholder = (Span)Utility.GetFirstChildElementByClassName(_elmTopBar1, "ms-cui-qat-container");
            _elmTabTitles = (UnorderedList)Utility.GetFirstChildElementByClassName(_elmTopBar2, "ms-cui-tts");
            if (CUIUtility.IsNullOrUndefined(_elmTabTitles))
                _elmTabTitles = (UnorderedList)Utility.GetFirstChildElementByClassName(_elmTopBar2, "ms-cui-tts-scale-1");
            if (CUIUtility.IsNullOrUndefined(_elmTabTitles))
                _elmTabTitles = (UnorderedList)Utility.GetFirstChildElementByClassName(_elmTopBar2, "ms-cui-tts-scale-2");
        }

        internal override void AttachEvents()
        {
            // Add a window.onresize event handler so that we can scale when the window is resized
            WindowResizedHandlerEnabled = true;

            // Restrict the Esc and shift-arrow bahaviors to the ribbon
            ElementInternal.KeyDown += OnRibbonEscKeyPressed;
            if (!_eventHandlerAttached)
            {
                ElementInternal.KeyDown += OnKeydownGroupShortcuts;
                Browser.Document.KeyDown += OnKeydownRibbonShortcuts;
                _eventHandlerAttached = true;
            }
            base.AttachEvents();
        }

        /// <summary>
        /// Gets or sets whether the onresize handler in Window is set up. Changing this value
        /// will cause the event handler to be added or removed from the Window object.
        /// </summary>
        internal bool WindowResizedHandlerEnabled
        {
            get 
            { 
                return _windowResizedHandlerEnabled; 
            }
            set
            {
                if (value == _windowResizedHandlerEnabled)
                    return;

                if (value)
                {
                    Browser.Window.Resize += OnWindowResize;
                }
                else
                {
                    try
                    {
                        Browser.Window.Resize -= OnWindowResize;
                    }
                    catch
                    {
                        // Do nothing -- this occurs if the handler was not added using addHandler, so we can ignore it.
                    }
                }

                _windowResizedHandlerEnabled = value;
            }
        }

        /// <summary>
        /// This is a key listener that will respond to special shortcuts or hotkeys. When Ctrl + [ is pressed,
        /// when in the document, the focus will move to the first tab title which is store during refreshinternal.
        /// The logic for jumping to the initial tab is rendered from the server as the ribbon script may not be loaded initially.
        /// Ctrl + ] will jump to the last focused command if one is stored.
        /// </summary>
        internal void OnKeydownRibbonShortcuts(HtmlEvent args)
        {
            if (!CUIUtility.IsNullOrUndefined(args))
            {
                int keycode = args.KeyCode;

                string key = args.CtrlKey ? "t" : "f";
                key += args.AltKey ? "t" : "f";
                key += args.ShiftKey ? "t" : "f";

                try
                {
                    key += ProcessKeyCode(keycode).ToString();
                }
                catch
                {
                    return;
                }

                HtmlElement elm = args.TargetElement;

                if (key == _jumpToLastFocusedKeys)
                {
                    ClearControlSelection();
                    JumpToLastFocused(elm);
                }
                else if (key == _jumpToRibbonTabKeys)
                {
                    ClearControlSelection();
                    JumpToRibbonTab(elm);
                }
            }
        }

        Dictionary<int, int> _cuiKeyHash;
        private int ProcessKeyCode(int keyCode)
        {
            if (CUIUtility.IsNullOrUndefined(_cuiKeyHash))
                InitKeyCodes();

            return _cuiKeyHash.ContainsKey(keyCode) ? _cuiKeyHash[keyCode] : keyCode;
        }

        private void InitKeyCodes()
        {
            _cuiKeyHash = new Dictionary<int, int>();
            _cuiKeyHash[219] = 91;
            _cuiKeyHash[221] = 93;
            _cuiKeyHash[51] = 35;
            _cuiKeyHash[186] = 59;
            _cuiKeyHash[187] = 61;
            _cuiKeyHash[188] = 44;
            _cuiKeyHash[189] = 45;
            _cuiKeyHash[190] = 46;
            _cuiKeyHash[191] = 47;
            _cuiKeyHash[222] = 39;
        }

        private void ClearControlSelection()
        {
            if (!CUIUtility.IsNullOrUndefined(Browser.Document.Selection) && Browser.Document.Selection.Type == "Control")
            {
                TextRange r = Browser.Document.Selection.CreateRangeCollection();

                for (int len = r.Text.Length; len > 0; len--)
                {
                    r.MoveEnd("character", 1);
                }
                r.PerformSelect();
            }
        }

        /// <summary>
        /// Focuses on the last focused control.
        /// </summary>
        /// <param name="currentElement">the currently focused element</param>
        public void JumpToLastFocused(HtmlElement currentElement)
        {
            if (InModalMode)
                Root.CloseAllMenus();

            if (ShouldStoreFocus(currentElement))
                StoredFocus = currentElement;

            if (!CUIUtility.IsNullOrUndefined(LastFocusedControl))
            {
                try
                {
                    SetFocus();
                }
                catch
                {
                    // TODO (cschle): log error
                }
                
                return;
            }
            SetFocusOnRibbon();
        }

        /// <summary>
        /// Focuses on the initial tab
        /// </summary>
        /// <param name="currentElement">the currently focused element</param>
        public void JumpToRibbonTab(HtmlElement currentElement)
        {
            if (ShouldStoreFocus(currentElement))
                StoredFocus = currentElement;
            if (InModalMode)
                Root.CloseAllMenus();

            if (!string.IsNullOrEmpty(_initialTabTitle))
            {
                HtmlElement elmTitle = Browser.Document.GetById(_initialTabTitle + "-title");
                if (!CUIUtility.IsNullOrUndefined(elmTitle))
                    ((HtmlElement)elmTitle.FirstChild).PerformFocus();
            }
        }


        /// <summary>
        /// Sets the focus on the title of the selected tab
        /// </summary>
        public void SetFocusOnTabTitle()
        {
            if (!string.IsNullOrEmpty(_initialTabTitle))
            {
                HtmlElement elmTitle = Browser.Document.GetById(_initialTabTitle + "-title");
                if (!CUIUtility.IsNullOrUndefined(elmTitle))
                    ((HtmlElement)elmTitle.FirstChild).PerformFocus();
            }
        }

        internal void OnKeydownGroupShortcuts(HtmlEvent args)
        {
            if (!CUIUtility.IsNullOrUndefined(args))
            {
                int key = args.KeyCode;
                if (((args.CtrlKey || args.ShiftKey) && (key == (int)Key.Right && Root.TextDirection == Direction.LTR)
                            || (key == (int)Key.Left && Root.TextDirection == Direction.RTL)))
                {
                    Utility.CancelEventUtility(args, true, true);
                    _selectedTab.MoveGroupFocus(true);
                }
                else if (((args.CtrlKey || args.ShiftKey) && (key == (int)Key.Left && Root.TextDirection == Direction.LTR)
                            || (key == (int)Key.Right && Root.TextDirection == Direction.RTL)))
                {
                    Utility.CancelEventUtility(args, true, true);
                    _selectedTab.MoveGroupFocus(false);
                }
            }
        }

        /// <summary>
        /// This method will recursively call down the tab body and set focus on the first enabled, focusable control.
        /// </summary>
        public void SetFocusOnRibbon()
        {
            if (_minimized)
                SetFocusOnTabTitle();
            else
                _selectedTab.SetRefreshFocus();
        }

        /// <summary>
        /// Sets the focus on the title of the selected tab or, if there is no selected tab, defaults
        /// to the normal set focus behavior.
        /// </summary>
        public void SetFocusOnCurrentTab()
        {
            if (!CUIUtility.IsNullOrUndefined(_selectedTab))
                _selectedTab.SetFocusOnTitle();
            else
                SetFocusOnRibbon();
        }

        /// <summary>
        /// Sets the browser focus to the last user focused control in the ribbon. If the ribbon is minimized, focus is set on the tab title.
        /// </summary>
        public override bool SetFocus()
        {
            if (_minimized || !base.SetFocus())
                SetFocusOnTabTitle();
            return true;
        }

        /// <summary>
        /// A key listener that will listen for Esc, and return the focus to element where the user was before jumping
        /// to the ribbon.
        /// </summary>
        internal void OnRibbonEscKeyPressed(HtmlEvent args)
        {
            if (!CUIUtility.IsNullOrUndefined(args) && !InModalMode)
            {
                // If there is no stored element, let event propogate (for dialogs)
                if (args.KeyCode == (int)Key.Esc && !CUIUtility.IsNullOrUndefined(StoredFocus))
                {
                    Utility.CancelEventUtility(args, true, true);

                    try 
                    { 
                        StoredFocus.PerformFocus(); 
                    }
                    catch {};

                    StoredFocus = null;
                }
            }
        }


        /// <summary>
        /// Adds a ContextualGroup to this ribbon
        /// </summary>
        /// <param name="id">The id of the ContextualGroup.</param>
        /// <param name="title">The Title of the ContextualGroup.</param>
        /// <param name="color">The color of the ContextualGroup.</param>
        public void AddContextualGroup(string id, string title, ContextualColor color, string command)
        {
            ContextualGroup cg = GetContextualGroup(id);
            if (!CUIUtility.IsNullOrUndefined(cg))
            {
                throw new ArgumentException("A contextual group with id: " + id +
                                       " has already been added to this ribbon.");
            }

            cg = new ContextualGroup(id, title, color, command);
            _contextualGroups[id] = cg;
        }

        /// <summary>
        /// Returns an ArrayList with the ids of the ContextualGroups that are currently in this ribbon.
        /// </summary>
        public List<string> ContextualGroupIds
        {
            get
            {
                List<string> contextualGroupKeys = new List<string>();
                foreach (string entry in _contextualGroups.Keys)
                    contextualGroupKeys.Add(entry);
                return contextualGroupKeys;
            }
        }

        /// <summary>
        /// Removes a ContextualGroup from the Ribbon.
        /// </summary>
        /// <param name="id">The id of the ContextualGroup that is to be removed.</param>
        public void RemoveContextualGroup(string id)
        {
            if (!CUIUtility.IsNullOrUndefined(GetContextualGroup(id)))
            {
                foreach (Tab tab in Children)
                {
                    if (tab.Contextual && tab.ContextualGroupId == id)
                        throw new InvalidOperationException("You cannot remove a contextual group when there are Tabs that refer to it.");
                }
                _contextualGroups.Remove(id);
            }
        }

        /// <summary>
        /// Cause the Tabs that are part of a ContextualGroup to be displayed along with the ContextualGroup header above them.
        /// </summary>
        /// <param name="id"></param>
        public void ShowContextualGroup(string id)
        {
            SetVisibilityForContextualGroup(id, true);
        }

        /// <summary>
        /// Hide a ContextualGroup header and the Tabs associated with the group.
        /// </summary>
        /// <param name="id">The id of the ContextualGroup to that should be hidden.</param>
        public void HideContextualGroup(string id)
        {
            SetVisibilityForContextualGroup(id, false);
        }

        private void SetVisibilityForContextualGroup(string id, bool visibility)
        {
            ContextualGroup cg = GetContextualGroup(id);
            if (CUIUtility.IsNullOrUndefined(cg))
                throw new ArgumentOutOfRangeException("This ribbon does not contain a contextual group with id: " + id);

            bool changed = false;
            foreach (Tab tab in Children)
            {
                if (tab.ContextualGroupId == id)
                {
                    if (tab.Visible != visibility)
                        changed = true;
                    tab.VisibleInternal = visibility;
                }
            }

            // If any of the tab visibilities were changed, then we need to dirty the ribbon
            if (changed)
                OnDirtyingChange();
        }

        /// <summary>
        /// Retrieves a Contextual Group.
        /// </summary>
        /// <param name="id">The id of the ContextualGroup that is to be returned.</param>
        /// <returns></returns>
        internal ContextualGroup GetContextualGroup(string id)
        {
            if (!_contextualGroups.ContainsKey(id))
                return null;

            return _contextualGroups[id];
        }

        public override void AddChildAtIndex(Component child, int index)
        {
            EnsureCorrectChildType(child);
            Tab tab = (Tab)child;
            if (CUIUtility.IsNullOrUndefined(tab))
                throw new ArgumentNullException("child must not be null or undefined.");

            // If this is a contextual tab, then the ContextualGroup 
            // must exist in this ribbon.
            if (tab.Contextual)
            {
                ContextualGroup cg = GetContextualGroup(tab.ContextualGroupId);
                if (CUIUtility.IsNullOrUndefined(cg))
                {
                    throw new ArgumentException("A contextual tab with contextual group id: " +
                                           tab.ContextualGroupId + " cannot be added because " +
                                           " the ribbon does not have a contextual group with this id.");
                }
            }
            base.AddChildAtIndex(child, index);
        }
        protected override void EnsureCorrectChildType(Component child)
        {
            if (!typeof(Tab).IsInstanceOfType(child))
                throw new InvalidCastException("Only children of type Tab can be added to a Ribbon");
        }

        protected override string CssClass
        {
            get 
            { 
                return "ms-cui-ribbon"; 
            }
        }

        protected override string RootType
        {
            get 
            { 
                return "Ribbon"; 
            }
        }
        #endregion

        #region Jewel & QAT Attachment
        private void EnsureJewelPlaceholder()
        {
            if (CUIUtility.IsNullOrUndefined(_elmJewelPlaceholder))
            {
                _elmJewelPlaceholder = new Div();
                _elmJewelPlaceholder.Id = "jewelcontainer";
                _elmJewelPlaceholder.ClassName = "ms-cui-jewel-container";
                _elmJewelPlaceholder.Style.Display = "none";
                _elmTopBar2.AppendChild(_elmJewelPlaceholder);
            }
        }

        private void EnsureQATPlaceholder()
        {
            if (CUIUtility.IsNullOrUndefined(_elmQATPlaceholder))
            {
                _elmQATPlaceholder = new Span();
                _elmQATPlaceholder.ClassName = "ms-cui-qat-container";
                _elmTopBar1.AppendChild(_elmQATPlaceholder);
            }
        }

        internal void BuildAndSetQAT(string qatId, bool attachToDOM, DataSource ds)
        {
            // Build QAT
            QATBuildOptions options = new QATBuildOptions();
            options.AttachToDOM = attachToDOM;
            options.TrimmedIds = RibbonBuilder.Options.TrimmedIds;
            QATBuilder builder = new QATBuilder(options,
                                                _elmQATPlaceholder,
                                                RibbonBuilder.BuildClient);
            builder.DataSource = ds;
            if (!builder.BuildQAT(qatId))
                throw new InvalidOperationException("QAT could not be built");

            QAT = builder.QAT;
            _elmTopBar1.Style.Display = "block";
        }

        internal void BuildAndSetJewel(string jewelId, bool attachToDOM, DataSource ds)
        {
            _elmJewelPlaceholder.Style.Display = "block";

            // Build Jewel
            JewelBuildOptions options = new JewelBuildOptions();
            options.AttachToDOM = attachToDOM;
            options.TrimmedIds = RibbonBuilder.Options.TrimmedIds;
            JewelBuilder builder = new JewelBuilder(options,
                                                    _elmJewelPlaceholder,
                                                    RibbonBuilder.BuildClient);
            builder.DataSource = ds;
            if (!builder.BuildJewel(jewelId))
                throw new InvalidOperationException("Jewel could not be built");

            Jewel = builder.Jewel;
        }

        protected QAT _qat = null;
        protected QAT QAT
        {
            get 
            { 
                return _qat; 
            }
            set 
            { 
                _qat = value; 
            }
        }

        protected Jewel _jewel = null;
        protected Jewel Jewel
        {
            get 
            { 
                return _jewel; 
            }
            set 
            { 
                _jewel = value; 
            }
        }

        #endregion

        #region Component Factory
        /// <summary>
        /// Create a Tab.
        /// </summary>
        /// <param name="id">The Component id of the Tab.</param>
        /// <param name="command">The Tab's command</param>
        /// <param name="title">The Title of the Tab.</param>
        /// <param name="description">The Description of the Tab.</param>
        /// <param name="cssClass">The CSS class to apply to the Tab header in addition to the standard ones</param>
        /// <returns>The created Tab</returns>
        internal Tab CreateTab(string id, string title, string description, string command, string cssClass)
        {
            return new Tab(this, id, title, description, command, false, null, cssClass);
        }

        /// <summary>
        /// Create a Contextual Tab.
        /// </summary>
        /// <param name="id">The Component id of the Tab.</param>
        /// <param name="title">The Title of the Tab.</param>
        /// <param name="description">The Description of the Tab.</param>
        /// <param name="contextualGroupId">The id of the ContextualGroup that this Tab belongs to.</param>
        /// <returns>The created Tab</returns>
        internal Tab CreateContextualTab(string id,
                                         string title,
                                         string description,
                                         string command,
                                         string contextualGroupId,
                                         string cssClass)
        {
            return new Tab(this, id, title, description, command, true, contextualGroupId, cssClass);
        }

        /// <summary>
        /// Creates a Group.
        /// </summary>
        /// <param name="id">Component id of the Group.</param>
        /// <param name="title">Title of the Group.</param>
        /// <param name="description">Description of the Group.</param>
        /// <returns>The created Group.</returns>
        internal Group CreateGroup(string id, GroupProperties properties, string title, string description, string command)
        {
            return new Group(this, id, title, description, command, properties);
        }

        internal GroupPopup CreateGroupPopup(string id, Group group)
        {
            return new GroupPopup(this, id, group);
        }

        internal GroupPopupLayout CreateGroupPopupLayout(string id, Group group)
        {
            return new GroupPopupLayout(this, id, group);
        }

        /// <summary>
        /// Creates a Layout.
        /// </summary>
        /// <param name="id">Component id of the Layout</param>
        /// <param name="title">Title of the Layout</param>
        /// <returns>The created Layout</returns>
        internal Layout CreateLayout(string id, string title)
        {
            return new Layout(this, id, title);
        }

        /// <summary>
        /// Creates a Section
        /// </summary>
        /// <param name="id">Component id of the Section</param>
        /// <param name="type">The type of Section to create</param>
        /// <returns>the created Section</returns>
        internal Section CreateSection(string id, SectionType type, SectionAlignment alignment)
        {
            return new Section(this, id, type, alignment);
        }

        /// <summary>
        /// Creates a Pane
        /// </summary>
        /// <param name="id">Component id of the Pane</param>
        /// <returns>the created Pane</returns>
        internal Strip CreateStrip(string id)
        {
            return new Strip(this, id);
        }
        #endregion

        #region Tab Manipulation
        Tab _lastSelectedNonContextualTab = null;

        internal void UpdateDOMForSelectedTab()
        {
            // Make sure that the tab has its top level DOMElement
            if (!CUIUtility.IsNullOrUndefined(_selectedTab))
                _selectedTab.EnsureDOMElement();

            // Keep the last selected non-contextual tab so that we can revert to it
            // if a selected contextual tab becomes hidden.  This code assumes
            // that non-contextual tabs are always visible and never hidden during
            // the runtime of the ribbon.
            if (!CUIUtility.IsNullOrUndefined(_selectedTab) && !_selectedTab.Contextual)
                _lastSelectedNonContextualTab = _selectedTab;

            // If this tab is empty or null then remove the body of the ribbon
            if (CUIUtility.IsNullOrUndefined(_selectedTab) || _selectedTab.Children.Count == 0)
            {
                // Remove the tab container div
                if (!CUIUtility.IsNullOrUndefined(ElementInternal) &&
                    !CUIUtility.IsNullOrUndefined(_elmTabContainer) &&
                     ElementInternal.LastChild == _elmTabContainer)
                {
                    ElementInternal.RemoveChild(_elmTabContainer);
                }
            }
            else if (!CUIUtility.IsNullOrUndefined(_elmTabContainer))
            {
                // If _selectedTab is not empty, then we need to make sure that
                // _elmTabContainer is attached to the ribbon
                ElementInternal.AppendChild(_elmTabContainer);
            }

            // REVIEW(josefl): Will the controls handle this on their own?
            // _selectedTab.PollIfRootPolledSinceLastPoll();

            // At this point there is nothing left to do if the selected tab was set to null
            if (CUIUtility.IsNullOrUndefined(_selectedTab))
                return;

            // Now make sure that the selected tab's DOMElement is up to date
            bool wasDirty = _selectedTab.Dirty;
            _selectedTab.EnsureRefreshed();

            // If the tab is empty, we don't append the DOM Element so that the Ribbon
            // tab section will get minimized.
            // _elmTabContainer may be null if the ribbon has never been refreshed
            if (_selectedTab.Children.Count > 0 && !CUIUtility.IsNullOrUndefined(_elmTabContainer))
            {
                // We do not want to remove the tab body from the DOM if it is already there.
                bool alreadyThere = false; ;
                if (_elmTabContainer.HasChildNodes())
                {
                    alreadyThere = _elmTabContainer.FirstChild == _selectedTab.ElementInternal;
                    if (!alreadyThere)
                        _elmTabContainer.RemoveChild(_elmTabContainer.FirstChild);
                }

                if (!alreadyThere)
                    _elmTabContainer.AppendChild(_selectedTab.ElementInternal);
            }

            if (!string.IsNullOrEmpty(this.RibbonProperties.TabSwitchCommand) &&
                (_previousTab != _selectedTab))
            {
                Dictionary<string, string> props = new Dictionary<string, string>();
                if (!CUIUtility.IsNullOrUndefined(_previousTab) && _previousTab != _selectedTab)
                {
                    props["OldContextId"] = _previousTab.Id;
                    props["OldContextCommand"] = _previousTab.Command;
                }
                else
                {
                    props["OldContextId"] = "";
                    props["OldContextCommand"] = "";
                }
                if (!CUIUtility.IsNullOrUndefined(_selectedTab))
                {
                    props["NewContextId"] = _selectedTab.Id;
                    props["NewContextCommand"] = _selectedTab.Command;
                    props["ChangedByUser"] = _selectedTab.SelectedByUser.ToString();
                }

                RaiseCommandEvent(this.RibbonProperties.TabSwitchCommand,
                                  CommandType.TabSelection,
                                  props);

                UpdatePreviousSelectedTab(_selectedTab);
            }

            // Reset this to the default value since we explicitly set it to true
            // if the tab changed as the result of a click.
            if (!CUIUtility.IsNullOrUndefined(_selectedTab))
            {
                _selectedTab.SelectedByUser = false;
                if (_selectedTab.LaunchedByKeyboard)
                    _selectedTab.SetRefreshFocus();
            }
        }

        internal void MakeTabSelectedInternal(Tab tab)
        {
            if (!CUIUtility.IsNullOrUndefined(tab))
            {
                OnDirtyingChange();
                tab.SetSelectedInternal(true, false);
                if (!tab.Contextual)
                    SetContextualColor(ContextualColor.None);

                if (!CUIUtility.IsNullOrUndefined(_selectedTab) && _selectedTab != tab)
                    _selectedTab.SetSelectedInternal(false, false);
                _selectedTab = tab;
                MinimizedInternal = false;
            }

            // Need to make sure that we clear the last focused control when tabs are switched
            // O14:673517
            LastFocusedControl = null;
        }

        private void UpdatePreviousSelectedTab(Tab newlySelectedTab)
        {
            _previousTab = newlySelectedTab;
        }

        public bool SelectTabById(string tabId)
        {
            Tab tab = (Tab)GetChild(tabId);
            if (!CUIUtility.IsNullOrUndefined(tab))
            {
                if (tab.Selected && tab.Visible)
                {
                    return true;
                }

                if (tab.Contextual && !tab.Visible)
                    this.ShowContextualGroup(tab.ContextualGroupId);

                if (tab.Visible)
                {
                    tab.Selected = true;
                    return true;
                }
            }
            return false;
        }

        public bool SelectTabByCommand(string tabCommand)
        {
            // This tab is already selected
            if (SelectedTabCommand == tabCommand)
                return true;

            foreach (Tab tab in Children)
            {
                if (tab.Command == tabCommand)
                    return SelectTabById(tab.Id);
            }
            return false;
        }

        // Called by Tab to set the color of the top border of the tab
        internal void SetContextualColor(ContextualColor color)
        {
            string cssColor = ContextualGroup.GetColorNameForContextualTabColor(color);
            if (string.IsNullOrEmpty(cssColor))
            {
                Utility.RemoveCSSClassFromElement(_elmTopBar2, _currentCtxCss);
                _currentCtxCss = null;
            }
            else
            {
                if (_currentCtxCss == null)
                {
                    Utility.RemoveCSSClassFromElement(_elmTopBar2, _currentCtxCss);

                }
                _currentCtxCss = "ms-cui-ct-topBar-" + cssColor;
                Utility.EnsureCSSClassOnElement(_elmTopBar2, _currentCtxCss);
            }
        }
        private string _currentCtxCss = null;

        // Used for debugging
        internal Tab SelectedTab
        {
            get 
            { 
                return _selectedTab; 
            }
        }

        // Adding this back in for merge TODO(josefl): should remove later 
        internal void SelectTab(Tab tab)
        {
            if (tab.Contextual && !tab.Visible)
                ShowContextualGroup(tab.ContextualGroupId);

            _selectedTab = tab;
            tab.SetSelectedInternal(true, true);
            OnDirtyingChange();
        }

        public string SelectedTabCommand
        {
            get 
            { 
                return _selectedTab != null ? _selectedTab.Command : null; 
            }
        }

        public string SelectedTabId
        {
            get 
            { 
                return _selectedTab != null ? _selectedTab.Id : null; 
            }
        }
        #endregion

        #region Scaling & Minimizing
        private int GetVerticalScaleRoom()
        {
            if (CUIUtility.IsNullOrUndefined(_selectedTab))
                return 0;
            return _elmTabContainer.OffsetHeight -
                   _selectedTab.ElementInternal.OffsetHeight;
        }

        private int GetHorizontalScaleRoom()
        {
            if (CUIUtility.IsNullOrUndefined(_selectedTab))
                return 0;
            return _elmTabContainer.OffsetWidth -
                   _selectedTab.GetNeededWidth();
        }

        private int GetWidth()
        {
            return ElementInternal.OffsetWidth;
        }

        private int GetMinimumWidth()
        {
            // TODO(josefl): get the right value for this number
            return 100;
        }

        private void Hide()
        {
            _oldDOMElementDisplayValue = ElementInternal.Style.Display;
            ElementInternal.Style.Display = "none";
            return;
        }

        private void Show()
        {
            // If display is set to "none", then we need to display the ribbon
            // so that its size will get refreshed.  Then we can see if the 
            // space is still to small for it.            
            ElementInternal.Style.Display = _oldDOMElementDisplayValue;
            if (GetWidth() < GetMinimumWidth())
            {
                ElementInternal.Style.Display = "none";
                return;
            }
        }

        private string GetWindowWidthHeightString()
        {
            return Utility.GetViewPortWidth().ToString() +
                   Utility.GetViewPortHeight().ToString();
        }

        private const int _maxScaleTries = 25;
        internal bool ScaleInternal(bool hideIfTooSmall)
        {
            int horizontalScaleRoom = 20;
            if (CUIUtility.IsNullOrUndefined(ElementInternal))
                return false;

            // Only scale by cookie the very first time that a tab is scaled.
            if (!CUIUtility.IsNullOrUndefined(_selectedTab) && !_selectedTab.ScaledByCookie)
            {
                string cookie = null;
                if (UseDataCookie)
                    cookie = GetDataCookieValue(_selectedTab.Id);

                string widthHeight = _lastWindowResizeWidthHeight;

                // If the saved scale for this tab is for this window height and width
                if (!string.IsNullOrEmpty(cookie) && 
                    cookie.StartsWith(widthHeight))
                {
                    string[] parts = cookie.Split( new char[]{'|'} );
                    // The format of the cookie should be "widthheight-scalingindex-horizontalscaleroom"
                    if (!CUIUtility.IsNullOrUndefined(parts) && parts.Length == 4)
                    {
                        // Only try to use the index from the cookie if we have a valid scaling number
                        int index = Int32.Parse(parts[1]);
                        int hsr = Int32.Parse(parts[2]);
                        string scalingHint = parts[3];
                        horizontalScaleRoom = hsr > horizontalScaleRoom ? hsr : horizontalScaleRoom;

                        if (!Double.IsNaN(index) && index >= 0)
                        {
                            // If the scaling index is not available, then we don't use it
                            // This can happen i cases where the same tab like "Ribbon.Track"
                            // Has different customizations on one page than another and thus potentially
                            // different scale steps.
                            if (index >= 0 && index < _selectedTab.Scaling.StepsInternal.Count)
                            {
                                _selectedTab.ScaleIndex(index);
                                _selectedTab.ScaledByCookie = true;
                                RefreshInternal();

                                // If the scaling hint has not changed, then we know that we are safe to exit
                                // because we are in the exact same situation (in regards to ribbon data) as the
                                // last time that we scaled this tab to the size that we just scaled it.
                                // O14:653582
                                // We also need to set the LastScaleWidthHeight to avoid an unnecessary scale in the third click
                                if (scalingHint == ((RibbonBuilder)Builder).RibbonBuildOptions.ScalingHint)
                                {
                                    _selectedTab.LastScaleWidthHeight = _lastWindowResizeWidthHeight;
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            // If there is no selected tab then we do not need to scale
            if (CUIUtility.IsNullOrUndefined(_selectedTab))
                return false;

            // To save performance, we call Scale() before we call RefreshInternal() so that
            // we avoid unecessary work.  So, if we were not able to scale based on the scaling cookie
            // above, then we need to call RefreshInternal() so that we have an initial ribbon size to start
            // scaling from.
            if (_selectedTab.Dirty)
                RefreshInternal();

            // If GetWidth() == 0, it is usually because the outer DOM element has not been
            // added to the DOM yet.  In this case we do not want to hide the ribbon.
            if (hideIfTooSmall && GetWidth() > 0)
            {
                if (!CUIUtility.IsNullOrUndefined(ElementInternal))
                {
                    // Hide the ribbon if it is too short
                    if (ElementInternal.Style.Display != "none" &&
                        GetWidth() < GetMinimumWidth())
                    {
                        Hide();
                    }
                    else if (ElementInternal.Style.Display == "none")
                    {
                        Show();
                    }
                }
            }

            int timesDown = 0;
            bool noroom = false;
            while ((GetHorizontalScaleRoom() < 0 || 
                   GetVerticalScaleRoom() < 0 || _selectedTab.Overflowing) && timesDown < _maxScaleTries)
            {
                // If the tab did not scale down then it is at its smallest width
                if (!_selectedTab.ScaleDown())
                {
                    noroom = true;
                    break;
                }

                RefreshInternal();
                timesDown++;
            }

            if (noroom)
            {
                // Comment this out for now so that the ribbon will atleast not be hidden in some 
                // quirks mode/browsers
                // TODO(josefl): revisit this.
                //Hide();
            }

            int timesUp = 0;
            int timesDownRevert = 0;
            // Only try to scale up if we have not just scaled down
            if (timesDown <= 0)
            {
                while (GetHorizontalScaleRoom() > horizontalScaleRoom && timesUp < _maxScaleTries)
                {
                    // If the tab failed to scale up then it is already at its maximum size
                    if (!_selectedTab.ScaleUp())
                        break;

                    RefreshInternal();
                    timesUp++;

                    // If we just scaled up to far and overran the borders,
                    // then we need to undo this last scale up and then exit
                    // since we have found the maximum size that doesn't overflow.
                    if (GetHorizontalScaleRoom() <= 0 || GetVerticalScaleRoom() < 0 || _selectedTab.Overflowing)
                    //if (_selectedTab.Overflowing || GetVerticalScaleRoom() < 0)
                    {
                        _selectedTab.ScaleDown();
                        RefreshInternal();
                        timesDownRevert++;
                        break;
                    }
                }
            }

            // Only store the cookie if the current tab actually has a scaling index
            // and is set to one.
            if (UseDataCookie && _selectedTab.CurrentScalingIndex >= -1)
                StoreTabScaleCookie();

            _selectedTab.LastScaleWidthHeight = _lastWindowResizeWidthHeight;

            // Scale the tab title header if needed.
            ScaleHeader();

            // If timesDown is greater than zero it means that we scaled down that many steps
            // timesUp - timesDownRevert will give us how many scale steps up we actually went
            // The timesDownRevert tells us how many times we scaled down after scaling up.
            // This happens if we are scaling up and we make it too big.  Then we have to go down a step again.
            bool bScaled = (timesDown > 0 || timesUp - timesDownRevert > 0);
            if (bScaled)
            {
                // Send notification
                if (!string.IsNullOrEmpty(this.RibbonProperties.ScaleCommand))
                {
                    RaiseCommandEvent(this.RibbonProperties.ScaleCommand,
                        CommandType.General, null);
                }
            }

            return bScaled;
        }

        private bool ShouldFixRtlHeader()
        {
            return ((Root.TextDirection == Direction.RTL) && BrowserUtility.InternetExplorer7);
        }

        private void SetTabHeaderWidth()
        {
            _elmTabTitles.Style.Width = "auto";
            int width = CalculateWidth(_elmTabTitles);
            _elmTabTitles.Style.Width = width + "px";
            _elmTabTitles.SetAttribute("_widthAdded", "true");
        }

        private int CalculateWidth(HtmlElement elmTabHeaders)
        {
            int width = 0;
            HtmlElementCollection tabs = elmTabHeaders.Children;
            int length = tabs.Length;
            for (int i = 0; i < length; i++)
            {
                HtmlElement elm = tabs[i];
                if (!CUIUtility.IsNullOrUndefined(elm) && 
                    elm.NodeName == "LI" && 
                    elm.OffsetWidth > 0)
                {
                    HtmlElement ctxlGroup = (HtmlElement)elm.ChildNodes[1];
                    if (!CUIUtility.IsNullOrUndefined(ctxlGroup) && ctxlGroup.NodeName == "UL")
                    {
                        ctxlGroup.Style.Width = "auto";
                        int groupWidth = CalculateWidth(ctxlGroup);
                        ctxlGroup.Style.Width = groupWidth + "px";
                        width = width + groupWidth + 4;
                    }
                    else
                    {
                        width = width + elm.OffsetWidth + 2;
                    }
                }
            }
            return width;
        }

        private void ScaleHeader()
        {
            if (ShouldFixRtlHeader())
            {
                SetTabHeaderWidth();
            }

            // Call the ribbon header scaling code if it is present
            NativeUtility.RibbonScaleHeader(_elmRibbonTopBars, Root.TextDirection == Direction.RTL);
        }

        /// <summary>
        /// Cause the Ribbon to Scale.
        /// </summary>
        internal bool Scale()
        {
            // Don't do any scaling on a minimized ribbon
            if (_minimized)
            {
                // We still need to scale the ribbon header even if the ribbon is minimized.
                ScaleHeader();
                return false;
            }

            bool scaled = ScaleInternal(false);

            _lastScaleTime = DateTime.Now;
            // REVIEW(josefl): we should not need this here because the controls themselves
            // should now be able to build their DOM elements with the correct enabled state.
            /*            if (!ScriptUtility.IsNullOrUndefined(_selectedTab))
                        {
                            if (!_selectedTab.Enabled)
                                _selectedTab.SetEnabledRecursively(false);
                        }
            */
            return scaled;
        }

        internal void ScaleIndex(int index)
        {
            if (!CUIUtility.IsNullOrUndefined(_selectedTab))
                _selectedTab.ScaleIndexInternal(index);
        }

        bool _autoScale = true;
        /// <summary>
        /// Whether the Ribbon automaically Scales when the page is resized.
        /// </summary>
        internal bool AutoScale
        {
            get 
            { 
                return _autoScale; 
            }
            set 
            { 
                _autoScale = value; 
            }
        }

        DateTime _lastScaleTime = DateTime.Now;
        internal DateTime LastScaleTime
        {
            get 
            {
                return _lastScaleTime;
            }
        }

        private void StoreTabScaleCookie()
        {
            // Cookie anatomy (keep in sync with %WCUI%\server\CommandUI\Ribbon.cs
            // Key - Tab.Id
            // Four Parts:
            // WidthHeight-ScalingIndex-HorizontalScaleRoom-ScalingHint
            StoreDataCookie(_selectedTab.Id,
                            Utility.GetViewPortWidth().ToString() +
                            Utility.GetViewPortHeight().ToString() + "|" +
                            _selectedTab.CurrentScalingIndex.ToString() + "|" +
                            GetHorizontalScaleRoom().ToString() + "|" +
                            ((RibbonBuilder)Builder).RibbonBuildOptions.ScalingHint.ToString());
        }

        bool _minimized = false;
        bool _minimizedPreviousValue = false;
        bool _minimizedChanged = false;
        /// <summary>
        /// Whether the Ribbon is minimized or not.
        /// </summary>
        public bool Minimized
        {
            get 
            { 
                return _minimized; 
            }
            set
            {
                if (_minimized != value)
                {
                    MinimizedInternal = value;
                    // if we are unminimizing the ribbon, then we need to poll for state
                    // so that the controls in the shown tab will get their states refreshed
                    if (!value && PollForState)
                        PollForStateAndUpdate();
                }
            }
        }

        /// <summary>
        /// This should only be used by carefully crafted internal code
        /// because we are updating the state variable but not the actual 
        /// state of any of the runtime or DOM datastructures.
        /// It should only be used when we know that we will be updating
        /// these datastructures very soon and before the user application 
        /// could do anything with the public Minimized attribute.
        /// </summary>
        bool _minimizedEverSet = false;
        internal bool MinimizedInternal
        {
            get 
            { 
                return _minimized; 
            }
            set
            {
                if (_minimized != value || !_minimizedEverSet)
                {
                    OnDirtyingChange();
                    _minimizedPreviousValue = !value;
                    _minimizedChanged = true;
                    _minimized = value;
                    _minimizedEverSet = true;
                    if (value && !CUIUtility.IsNullOrUndefined(_selectedTab))
                    {
                        _selectedTab.SetSelectedInternal(false, false);
                        _selectedTab = null;
                    }
                }
            }
        }

        internal override void PollForStateAndUpdateInternal()
        {
            // Need to do this here since we are not calling base method.  See comment at bottom of function.
            // Also, this needs to be done before the children are called so that it will not appear that 
            // the root (this ribbon) has been polled since they have been.  This would trigger unnecessary polling.
            LastPollTime = DateTime.Now;

            // So we can tell if any of the polling
            // needs the ribbon to be re-scaled.
            NeedScaling = false;

            // Go through and find out which contextual groups should be enable
            // and store this information.
            // REVIEW(josefl): A possible performance improvement here is to get these all at once
            // with one command.
            Dictionary<string, bool> enabledGroups = new Dictionary<string, bool>();
            foreach (string entry in _contextualGroups.Keys)
            {
                ContextualGroup group = GetContextualGroup(entry);
                bool enabled = false;
                if (!string.IsNullOrEmpty(group.Command))
                {
                    enabled = RootUser.IsRootCommandEnabled(group.Command, this);
#if DEBUG
                    enabled = enabled || group.Command == "DEBUG_ALWAYS_ENABLED";
#endif
                }

                if (enabled)
                    enabledGroups[entry] = true;
                SetVisibilityForContextualGroup(entry, enabled);
            }

            if (QAT != null)
            {
                QAT.PollForStateAndUpdate();
            }
            if (Jewel != null)
            {
                Jewel.PollForStateAndUpdate();
            }

            if (Dirty)
            {
                RefreshInternal();
                // We do a targeted scaling of the header here since the contextual
                // groups that are displayed may have changed after polling.
                ScaleHeader();
            }

            // This needs to come after the potential call to RefreshInternal()
            // above becuase if the ribbon is being maximized, then we need the call to
            // RefreshInternal() above to set the selected tab again (which is null
            // when the ribbon is minimized).  O14:594201
            if (!CUIUtility.IsNullOrUndefined(_selectedTab))
                _selectedTab.PollForStateAndUpdateInternal();

            // REVIEW(josefl): Should we just use Dirty for NeedsScaling or do they need to be
            // separate.  Also, is refresh getting called twice here because of the call to
            // ScaleInternal() and RefreshInternal()?
            if (NeedScaling)
            {
                Scale();
                NeedScaling = false;
            }

            // Here we do not want to call the base method because it will trigger a poll of 
            // the tab children (ie, Component.PollForStateAndUpdate()) which is unnecessary and
            // performance intensive.  So we just do what Root.PollForStateAndUpdate() does minus
            // the base.PollForStateAndUpdate().
            EnsureGlobalDisablingRemoved();
        }

        internal override bool OnPreBubbleCommand(CommandEventArgs command)
        {
            // Root events are from the ribbon as a whole and not from a particular location
            if (command.Type != CommandType.RootEvent)
            {
                // If the command is a tab selection command then it comes from the upper ribbon
                command.CommandInfo.RootLocation = command.Type ==
                    CommandType.TabSelection ? "UpperRibbon" : "LowerRibbon";

                // If the command is a tabswitch command, then we want to put the tab id in 
                // the commandinfo property bag.
                if (command.Type == CommandType.TabSelection)
                {
                    command.CommandInfo.TabId = command.Properties.ContainsKey("NewContextId") ?
                        command.Properties["NewContextId"] : string.Empty;
                }
            }

            return base.OnPreBubbleCommand(command);
        }

        /// <summary>
        /// Stop the context menu from opening
        /// </summary>
        /// <param name="evt"></param>
        private void OnContextMenu(HtmlEvent args)
        {
            // In most browsers, PreventDefault() will stop the context menu from opening;
            // however, in Firefox 3, we have to also StopPropagation().
            Utility.CancelEventUtility(args, true, true);
        }

        /// <summary>
        ///  The "Jewel" element that appears in the top left corner of the Ribbon.
        /// </summary>
        public Div JewelElement
        {
            get 
            { 
                return _elmJewelPlaceholder; 
            }
            set 
            { 
                _elmJewelPlaceholder = value; 
            }
        }

        bool _handlingResize = false;

        string _lastWindowResizeWidthHeight = null;
        internal string LastWindowResizeWidthHeight
        {
            get 
            {
                return CUIUtility.SafeString(_lastWindowResizeWidthHeight);
            }
        }

        private void OnWindowResize(HtmlEvent args)
        {
            string newResizeWidthHeight = GetWindowWidthHeightString();
            // Sometimes in IE, onwindow resize is called twice.  We do not want to handle it twice so
            // we ignore this if the actual size of the window has not changed.
            if (_lastWindowResizeWidthHeight == newResizeWidthHeight)
                return;

            _lastWindowResizeWidthHeight = newResizeWidthHeight;

            //TEMP: try this so that the DOM has a chance to get initialized so that we can scale
            //return;
            // Sometimes apparently doing certain operations in the DOM like ElementInternal.offsetWidth 
            // can trigger a windor.resize event.  We want to avoid infinite recursion.
            // Also, close all open menus before scaling (O14:23711)
            // If the viewport didn't actually change size, then this resize was probably caused by the page
            // content growing from a Live Preview, so do not close Menus (O14:119661)
            if (!_handlingResize && _autoScale && ViewPortSizeChanged())
            {
                ResetCachedViewPortSizes();

                _handlingResize = true;

                // Invalidate width & height data
                _componentWidth = _componentHeight = -1;

                CloseAllMenus();
                CloseOpenTootips();

                bool scaled = Scale();

                // If the ribbon actually scaled, then we need to pollforstate because
                // it is possible that some controls are getting their first manifestations
                // in the UI in a particular Layout.  So, they need to be able to poll for their state.
                // This could potentially be optimized in the future.
                if (scaled)
                {
                    PollForStateAndUpdate();
                }

                _handlingResize = false;
            }
        }

        int _viewPortWidth;
        int _viewPortHeight;
        private bool ViewPortSizeChanged()
        {
            return (_viewPortWidth != Utility.GetViewPortWidth()) || (_viewPortHeight != Utility.GetViewPortHeight());
        }

        private void ResetCachedViewPortSizes()
        {
            _viewPortWidth = Utility.GetViewPortWidth();
            _viewPortHeight = Utility.GetViewPortHeight();
        }
        #endregion

        public RibbonProperties RibbonProperties
        {
            get 
            { 
                return (RibbonProperties)Properties; 
            }
        }

        internal RibbonBuilder RibbonBuilder
        {
            get 
            { 
                return (RibbonBuilder)Builder; 
            }
            set 
            { 
                Builder = value; 
            }
        }

        /// <summary>
        /// The DOMElement type of this Component (div, span...etc).
        /// </summary>
        /// <seealso cref="EnsureDOMElement"/>
        protected override string DOMElementTagName
        {
            get 
            { 
                return "div"; 
            }
        }

        internal override void EnsureDOMElement()
        {
            base.EnsureDOMElement();
            ElementInternal.SetAttribute("aria-describedby", "ribboninstructions");
            ElementInternal.SetAttribute("role", "toolbar");

            // Here we ensure the minimum elements needed for the ribbon outer element
            // to be able to be placed into the DOM and also for the QAT and Jewel
            // to be able to be placed into the ribbon DOM.
            EnsureTopBars();
            EnsureTopBars1And2();
            EnsureQATPlaceholder();
            EnsureJewelPlaceholder();
            EnsureTabContainer();
            EnsurePeripheralPlaceholders();
            EnsureJewelPlaceholder();
        }

        private void EnsureTopBars1And2()
        {
            if (CUIUtility.IsNullOrUndefined(_elmTopBar1))
            {
                _elmTopBar1 = new Div();
                _elmTopBar1.ClassName = "ms-cui-topBar1";
                _elmTopBar1.Style.Display = "none";
                _elmRibbonTopBars.AppendChild(_elmTopBar1);
            }

            if (CUIUtility.IsNullOrUndefined(_elmTopBar2))
            {
                _elmTopBar2 = new Div();
                _elmTopBar2.ClassName = "ms-cui-topBar2";
                _elmRibbonTopBars.AppendChild(_elmTopBar2);
            }
        }

        private void EnsureTopBars()
        {
            if (CUIUtility.IsNullOrUndefined(_elmNavigationInstructions))
            {
                _elmNavigationInstructions = new Span();
                _elmNavigationInstructions.ClassName = "ms-cui-hidden";
                _elmNavigationInstructions.Id = "ribboninstruction";
                UIUtility.SetInnerText(_elmNavigationInstructions, ((RibbonProperties)Properties).NavigationHelpText);
            }

            if (CUIUtility.IsNullOrUndefined(_elmRibbonTopBars))
            {
                _elmRibbonTopBars = new Div();
                _elmRibbonTopBars.ClassName = "ms-cui-ribbonTopBars";
                ElementInternal.AppendChild(_elmNavigationInstructions);
                ElementInternal.AppendChild(_elmRibbonTopBars);
            }
        }

        private void EnsureTabContainer()
        {
            // Do this if this is the first time that the Ribbon is being refreshed
            if (CUIUtility.IsNullOrUndefined(_elmTabContainer))
            {
                _elmTabContainer = new Div();
                Utility.DisableElement(_elmTabContainer);
            }
        }
        private void EnsurePeripheralPlaceholders()
        {
            // Create peripheral content placeholders as necessary
            if (CUIUtility.IsNullOrUndefined(_elmQATRowCenter))
                _elmQATRowCenter = (Div)Browser.Document.GetById(ClientID + "-" + RibbonPeripheralSection.QATRowCenter);

            if (CUIUtility.IsNullOrUndefined(_elmQATRowRight))
                _elmQATRowRight = (Div)Browser.Document.GetById(ClientID + "-" + RibbonPeripheralSection.QATRowRight);

            if (CUIUtility.IsNullOrUndefined(_elmTabRowLeft))
                _elmTabRowLeft = (Div)Browser.Document.GetById(ClientID + "-" + RibbonPeripheralSection.TabRowLeft);

            if (CUIUtility.IsNullOrUndefined(_elmTabRowRight))
                _elmTabRowRight = (Div)Browser.Document.GetById(ClientID + "-" + RibbonPeripheralSection.TabRowRight);
        }

        private void HandlePeripheralsAndTabTitles()
        {
            if (!_peripheralContentsLoaded &&
                !CUIUtility.IsNullOrUndefined(_elmQATRowCenter) &&
                _elmQATRowCenter.ParentNode != _elmTopBar1)
            {
                if (_elmQATRowCenter.ParentNode != null)
                    _elmQATRowCenter.ParentNode.RemoveChild(_elmQATRowCenter);

                _elmTopBar1.AppendChild(_elmQATRowCenter);
                _elmQATRowCenter.Style.Display = "inline-block";
                _elmTopBar1.Style.Display = "block";
                Utility.SetUnselectable(_elmQATRowCenter, true, false);
            }

            if (!_peripheralContentsLoaded &&
                !CUIUtility.IsNullOrUndefined(_elmQATRowRight) &&
                _elmQATRowRight.ParentNode != _elmTopBar1)
            {
                if (_elmQATRowRight.ParentNode != null)
                    _elmQATRowRight.ParentNode.RemoveChild(_elmQATRowRight);

                _elmTopBar1.AppendChild(_elmQATRowRight);
                _elmQATRowRight.Style.Display = "inline-block";
                _elmTopBar1.Style.Display = "block";
                Utility.SetUnselectable(_elmQATRowRight, true, false);
            }

            if (!_peripheralContentsLoaded &&
                !CUIUtility.IsNullOrUndefined(_elmTabRowLeft) &&
                _elmTabRowLeft.ParentNode != _elmTopBar2)
            {
                if (_elmTabRowLeft.ParentNode != null)
                    _elmTabRowLeft.ParentNode.RemoveChild(_elmTabRowLeft);

                _elmTopBar2.AppendChild(_elmTabRowLeft);
                _elmTabRowLeft.Style.Display = "block";
                Utility.SetUnselectable(_elmTabRowLeft, true, false);
            }

            if (CUIUtility.IsNullOrUndefined(_elmTabTitles))
            {
                _elmTabTitles = new UnorderedList();
                _elmTabTitles.SetAttribute("role", "tablist");

                // We purposely don't match the server rendering here by setting "ms-cui-disabled"
                // because we don't wait it to flash disabled/enabled when the first tab is expanded.
                _elmTabTitles.ClassName = "ms-cui-tts";
                _elmTopBar2.AppendChild(_elmTabTitles);
            }

            if (!_peripheralContentsLoaded &&
                !CUIUtility.IsNullOrUndefined(_elmTabRowRight) &&
                _elmTabRowRight.ParentNode != _elmTopBar2)
            {
                if (_elmTabRowRight.ParentNode != null)
                    _elmTabRowRight.ParentNode.RemoveChild(_elmTabRowRight);
                _elmTopBar2.AppendChild(_elmTabRowRight);
                _elmTabRowRight.Style.Display = "block";
                Utility.SetUnselectable(_elmTabRowRight, true, false);
            }

            // All peripheral contents should be loaded by now if they are set
            _peripheralContentsLoaded = true;
        }

        protected internal override void EnsureGlobalDisablingRemoved()
        {
            Utility.EnableElement(_elmTabTitles);
            EnsureTabContainerGlobalDisablingRemoved();

            if (Jewel != null)
                Jewel.Enabled = true;
            if (QAT != null)
                QAT.PollForStateAndUpdate();
        }

        internal void EnsureTabContainerGlobalDisablingRemoved()
        {
            Utility.EnableElement(_elmTabContainer);
        }

        public override RootUser RootUser
        {
            get 
            { 
                return base.RootUser; 
            }
            set
            {
                base.RootUser = value;
                if (!CUIUtility.IsNullOrUndefined(_qat))
                    _qat.RootUser = value;
                if (!CUIUtility.IsNullOrUndefined(_jewel))
                    _jewel.RootUser = value;
            }
        }

        public override void Dispose()
        {
            Disposed = true;

            Root root = this.Root;
            if (!CUIUtility.IsNullOrUndefined(root))
            {
                int timer = root.TooltipLauncherTimer;
                if (!CUIUtility.IsNullOrUndefined(timer))
                {
                    Browser.Window.ClearTimeout(timer);
                }

                root.CloseOpenTootips();
            }

            // This property set will remove the event handler
            WindowResizedHandlerEnabled = false;

            ElementInternal.KeyDown -= OnRibbonEscKeyPressed;
            if (_eventHandlerAttached)
            {
                ElementInternal.KeyDown -= OnKeydownGroupShortcuts;
                Browser.Document.KeyDown -= OnKeydownRibbonShortcuts;
            }

            base.Dispose();
            _previousTab = null;
            _selectedTab = null;
            _elmRibbonTopBars = null;
            _elmTabTitles = null;
            _elmJewelPlaceholder = null;
            _elmTabContainer = null;
            _elmTabRowLeft = null;
            _elmTabRowRight = null;
            _elmQATPlaceholder = null;
            _elmQATRowCenter = null;
            _elmQATRowRight = null;
            _elmTopBar1 = null;
            _elmTopBar2 = null;

            foreach (ContextualGroup cg in _contextualGroups.Values)
            {
                cg.Dispose();

            }
            _contextualGroups.Clear();
            _contextualGroups = null;
        }
    }

    /// <summary>
    /// This is a major hack which we may not have time to get rid of (in O14).
    /// Please don't use this as a model for how things are done in the ribbon.
    /// </summary>
    public class RibbonCommand
    {
        private static HtmlElement GetServerButtonElement(string srcId)
        {
            JSObject dict = NativeUtility.GetSPButton();
            if (dict != null)
            {
                string ctrlId = dict.GetField<string>(srcId);
                if (ctrlId != null)
                {
                    HtmlElement elm = Browser.Document.GetById(ctrlId);
                    return elm;
                }
            }
            return null;
        }
        public static void ServerButton(string srcId, string menuItemId)
        {
            HtmlElement elm = GetServerButtonElement(srcId);
            if (elm != null)
                elm.PerformClick();
        }
        public static bool ServerQueryButton(string srcId)
        {
            HtmlElement elm = GetServerButtonElement(srcId);
            return elm != null;
        }
        public static string ServerControlLabel(string ribbonCommand)
        {
            JSObject dict = NativeUtility.GetSPButton();
            if (dict != null)
            {
                string ctrlId = dict.GetField<string>(ribbonCommand);
                if (ctrlId != null)
                {
                    HtmlElement elm = Browser.Document.GetById(ctrlId);
                    return elm.GetAttribute("value");
                }
            }
            return null;
        }
    }
}
