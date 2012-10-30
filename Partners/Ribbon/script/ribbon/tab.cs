using System;
using System.Collections.Generic;
using Microsoft.LiveLabs.Html;

namespace Ribbon
{
    /// <summary>
    /// A class represeting a Ribbon Tab.
    /// </summary>
    internal class Tab : RibbonComponent
    {
        bool _selected;
        ListItem _elmTitle;
        Anchor _elmTitleA;
        Span _elmTitleSpan;
        Span _elmATText;
        int _lastScalingIndex;
        string _cssClass;

        bool _contextual;
        string _contextualGroupId;

        /// <summary>
        /// Tab constructor
        /// </summary>
        /// <param name="ribbon">the Ribbon that created this Tab and that it is a part of</param>
        /// <param name="id">Component id of the Tab</param>
        /// <param name="title">Title of the Tab</param>
        /// <param name="description">Description of the Tab</param>
        /// <param name="contextual">whether this Tab is a contextual Tab or not</param>
        /// <param name="command">the command for this Tab.  Used in enabling or disabling the tab.</param>
        /// <param name="contextualGroupId">the id of the ContextualGroup that this Tab is a part of.</param>
        internal Tab(SPRibbon ribbon,
                     string id,
                     string title,
                     string description,
                     string command,
                     bool contextual,
                     string contextualGroupId,
                     string cssClass)
            : base(ribbon, id, title, description)
        {
            _lastScalingIndex = -1;
            _scalingInfo = new Scaling();
            _contextual = contextual;
            _contextualGroupId = CUIUtility.SafeString(contextualGroupId);
            _command = CUIUtility.SafeString(command);
            _cssClass = CUIUtility.SafeString(cssClass);

            // Contextual tabs are invisible by default
            if (contextual)
                VisibleInternal = false;
#if DEBUG
            if (command == "DEBUG_ALWAYS_ENABLED")
                VisibleInternal = true;
#endif
        }

        internal override void RefreshInternal()
        {
            EnsureDOMElementAndEmpty();
            EnsureTitleRefreshed();
            AppendChildrenToElement(ElementInternal);
            base.RefreshInternal();
        }

        internal void AttachTitle()
        {
            _elmTitle = (ListItem)Browser.Document.GetById(Id + "-title");
            _elmTitleA = (Anchor)_elmTitle.ChildNodes[0];
            _elmTitleSpan = (Span)_elmTitleA.ChildNodes[0];
            _elmATText = (Span)_elmTitleA.ChildNodes[1];
        }

        internal void AttachTitleEvents()
        {
            _elmTitleA.Click += OnTitleClick;
            _elmTitleA.KeyPress += OnTitleKeyPress;
            //O14:438510 FF doesn't cancel both keypress and keydown. After moving focus, keypress gets fired. We'll just use keypress as it fires after keypress and handle it.
            _elmTitleA.DblClick += OnTitleDblClick;
        }

        internal override void AttachDOMElements()
        {
            base.AttachDOMElements();
            AttachTitle();
        }

        internal override void AttachEvents()
        {
            base.AttachEvents();
            AttachTitleEvents();
        }

        internal override void EnsureDOMElement()
        {
            if (!CUIUtility.IsNullOrUndefined(ElementInternal))
                return;

            base.EnsureDOMElement();

            // aria supoort for tabs
            ElementInternal.SetAttribute("role", "tabpanel");
            ElementInternal.SetAttribute("aria-labelledby", Id + "-title");
        }

        protected override string DOMElementTagName
        {
            get 
            { 
                return "ul"; 
            }
        }

        internal HtmlElement TitleDOMElement
        {
            get 
            { 
                return this._elmTitle; 
            }
        }

        internal void EnsureTitleDOMElement()
        {
            if (CUIUtility.IsNullOrUndefined(_elmTitle))
                _elmTitle = new ListItem();
        }

        internal void EnsureHiddenATDOMElement()
        {
            if (CUIUtility.IsNullOrUndefined(_elmATText))
                _elmATText = new Span();
        }

        // This needs to be Ensured separately from the body of the tab
        // so that we can refresh the body of the tab on demand.
        /// <summary>
        /// Ensures that the title of this tab is refreshed based on the current properties of the Tab.
        /// </summary>
        internal void EnsureTitleRefreshed()
        {
            string ctxTabClasses = " ";

            // If the anchor element hasn't been instantiated, then it is possible
            // that the title element has only been shallowly instantiated and all its
            // parts still need to be created and the event handlers set.
            if (CUIUtility.IsNullOrUndefined(_elmTitleA))
            {
                EnsureTitleDOMElement();

                _elmTitleA = new Anchor();
                Utility.NoOpLink(_elmTitleA);
                _elmTitleA.ClassName = "ms-cui-tt-a";

                _elmTitleSpan = new Span();
                _elmTitleSpan.ClassName = "ms-cui-tt-span";
                _elmTitle.AppendChild(_elmTitleA);
                _elmTitleA.AppendChild(_elmTitleSpan);

                AttachEvents();
            }
            else
            {
                ctxTabClasses += _elmTitle.ClassName.IndexOf("ms-cui-ct-first") > -1 ? "ms-cui-ct-first " : "";
                ctxTabClasses += _elmTitle.ClassName.IndexOf("ms-cui-ct-last") > -1 ? "ms-cui-ct-last" : "";
                ctxTabClasses = ctxTabClasses.TrimEnd();
            }

            _elmTitle.ClassName = GetTitleCSSClassName() + ctxTabClasses;
            _elmTitle.Id = Id + "-title";
            _elmTitle.SetAttribute("role", "tab");
            _elmTitle.SetAttribute("aria-selected", _selected.ToString());

            // Refresh the title, text and description
            UIUtility.SetInnerText(_elmTitleSpan, Title);
            _elmTitle.Title = Title;

            if (!string.IsNullOrEmpty(Description))
            {
                _elmTitleA.SetAttribute("title", Description);
            }
            else
            {
                _elmTitleA.SetAttribute("title", Title);
            }
        }

        internal void SetContextualText(string positionText, string contextualText, string groupName, int tabPos, int totalTabs)
        {
            if (CUIUtility.IsNullOrUndefined(_elmATText))
                EnsureHiddenATDOMElement();

            if (Contextual)
            {
                contextualText = String.Format(contextualText, groupName, tabPos, totalTabs);
            }
            else
            {
                contextualText = String.Format(positionText, tabPos, totalTabs);

            }
            UIUtility.SetInnerText(_elmATText, contextualText);
            Utility.EnsureCSSClassOnElement(_elmATText, "ms-cui-hidden");
            _elmTitleA.AppendChild(_elmATText);
        }

        /// <summary>
        /// Revert the title element to its default CSS class(es)
        /// </summary>
        internal void ResetTitleCSSClasses()
        {
            _elmTitle.ClassName = GetTitleCSSClassName();
        }

        protected override string CssClass
        {
            get 
            { 
                return GetBodyCSSClassName(); 
            }
        }

        /// <summary>
        /// Whether this Tab is the selected Tab in the Ribbon.
        /// </summary>
        public bool Selected
        {
            get 
            { 
                return _selected; 
            }
            set
            {
                // Only allow visible and enabled tabs to be selected like this
                if (!Visible)
                    new InvalidOperationException("Tabs must be visible and enabled in order to be selected.");

                if (value)
                {
                    // If this Tab is using lazy initialization, then initialize it
                    if (NeedsDelayIniting)
                    {
                        DoDelayedInit();
                        return;
                    }

                    // First make the state changes to the ribbon
                    Ribbon.MakeTabSelectedInternal(this);
                    Ribbon.MinimizedInternal = false;

                    Ribbon.RefreshInternal();

                    // Scale the tab if the window size has changed since the last time that
                    // it scaled and was visible.  There is also a possibility that polling 
                    // will cause the Tab to need to scale but this will be handled through the
                    // polling mechanism and the "Ribbon.NeedsScaling" property.
                    if (CUIUtility.SafeString(LastScaleWidthHeight) != Ribbon.LastWindowResizeWidthHeight)
                        Ribbon.Scale();

                    // TODO(josefl): merge this with next perf fix and we should not have
                    // to call RefreshInternal and Poll() separately here.
                    PollIfRootPolledSinceLastPoll();
                    _launchedByKeyboard = false;
                }
                else
                {
                    throw new ArgumentOutOfRangeException("Selected cannot be set to false explicitly.\n" +
                        "Selecting another Tab will do this implicitly.");
                }
            }
        }

        private void RememberActiveTabId()
        {
            if (!CUIUtility.IsNullOrUndefined(this.Ribbon.RibbonBuilder) &&
                !CUIUtility.IsNullOrUndefined(this.Ribbon.RibbonBuilder.RibbonBuildOptions) &&
                !string.IsNullOrEmpty(this.Ribbon.RibbonBuilder.RibbonBuildOptions.ClientID))
            {
                Input elem = (Input)Browser.Document.GetById(this.Ribbon.RibbonBuilder.RibbonBuildOptions.ClientID + "_activeTabId");
                if (elem != null)
                {
                    elem.Value = this.Id;
                }
            }
        }

        internal void SetSelectedInternal(bool selected, bool refresh)
        {
            if (selected)
            {
                RememberActiveTabId();
            }

            _selected = selected;
            OnDirtyingChange();

            // Sometimes we do not want to refresh and dirty.  
            // When attaching to existing DOM elements for example.
            if (refresh)
            {
                EnsureTitleRefreshed();
            }
        }

        internal override void OnDelayedInitFinished(bool success)
        {
            base.OnDelayedInitFinished(success);
            Ribbon.Scale();
            Ribbon.PollForStateAndUpdate();
        }

        protected override void EnsureCorrectChildType(Component child)
        {
            if (!typeof(Group).IsInstanceOfType(child))
                throw new InvalidCastException("Only children of type Group can be added to Tab Components");
        }


        #region Scaling
        Scaling _scalingInfo;
        public Scaling Scaling
        {
            get
            {
                if (CUIUtility.IsNullOrUndefined(_scalingInfo))
                    _scalingInfo = new Scaling();
                return _scalingInfo;
            }
        }

        public int CurrentScalingIndex
        {
            get 
            { 
                return _lastScalingIndex; 
            }
        }

        string _lastScaleWidthHeight;
        internal string LastScaleWidthHeight
        {
            get 
            { 
                return _lastScaleWidthHeight; 
            }
            set 
            { 
                _lastScaleWidthHeight = value; 
            }
        }

        /// <summary>
        /// Use the largest scale for all the Groupus in this Tab.
        /// </summary>
        /// <returns>Whether the scale for this Tab actually changed</returns>
        public void ScaleMax()
        {
            ScaleMaxInternal();
            OnDirtyingChange();
        }

        internal void ScaleMaxInternal()
        {
            foreach (Group group in Children)
            {
                string layoutName = _scalingInfo.GetGroupMaxSize(group.Id);
                if (!string.IsNullOrEmpty(layoutName))
                    group.SelectLayout(layoutName, null);
            }
            _lastScalingIndex = -1;
        }

        /// <summary>
        /// Scale to a particular scaling index.
        /// </summary>
        /// <param name="index"></param>
        public void ScaleIndex(int index)
        {
            ScaleIndexInternal(index);
            OnDirtyingChange();
        }

        internal void ScaleIndexInternal(int index)
        {
            ScaleMaxInternal();
            while (index > _lastScalingIndex)
            {
                // We scale down and if no scale was changed, then we are at the lowest scale
                // possible
                if (!ScaleDownInternal())
                    break;
            }
        }

        internal bool ScaleUpInternal()
        {
            // If there is no previous scale index, we start at the zeroth index
            if (_lastScalingIndex == -2)
            {
                ScaleMaxInternal();
                return true;
            }

            // We are already at the Max Scale so there is nothing to do
            if (_lastScalingIndex == -1)
                return false;

            List<ScalingStep> steps = _scalingInfo.StepsInternal;

            ScalingStep step = (ScalingStep)steps[_lastScalingIndex];
            Group group = (Group)GetChild(step.GroupId);

            // We pass "null" for the PopupSize because we are scaling to the previous layout
            // This implies that it is not the smallest one and therefore hopefully not a popup.
            // If it is a popup, then passing in null like this will simply cause us to use
            // the default popup size for this scaling template.
            // The scenario that we are punting on here then is default popup sizes for popup
            // scale steps that are not the last one in the scaling sequence for a group.
            // This will practially never happen and even if it does, we'll just fall back
            // the the default behavior.  O14:611435           
            group.SelectLayout(step.PreviousLayoutName, null);

            // Update the step that we are on
            _lastScalingIndex--;
            return true;
        }

        /// <summary>
        /// Go to the next largest scale for this Tab.
        /// </summary>
        /// <returns>whether any of the Group layouts actually changed</returns>
        /// <seealso cref="CUI.Tab.SetScale"/>
        /// <seealso cref="SetScalingForGroup"/>
        /// <seealso cref="ScaleDown"/>
        public bool ScaleUp()
        {
            bool scaled = ScaleUpInternal();
            if (scaled)
                OnDirtyingChange();
            return scaled;
        }

        internal bool ScaleDownInternal()
        {
            // If there is no previous scale index, we start at the zeroth index
            if (_lastScalingIndex == -2)
            {
                ScaleMax();
                return true;
            }

            List<ScalingStep> steps = _scalingInfo.StepsInternal;
            // If we are already at the smallest scale, then return.
            if (steps.Count <= _lastScalingIndex + 1)
                return false;

            // Go down one scale smaller
            _lastScalingIndex++;
            ScalingStep step = (ScalingStep)steps[_lastScalingIndex];
            Group group = (Group)GetChild(step.GroupId);
#if DEBUG
            // Hack to disable these alert in Ja-Jp Pseudo builds - O14:628489
            if (!(Title.StartsWith("[") && Title.EndsWith("]")))
            {
                if (step.HasScaleWarning)
                {
                    Browser.Window.Alert("Tab Scale Warning hit for:\nTab: " + Id +
                        "\nGroup: " + group.Id +
                        "\nSize: " + step.LayoutName +
                        "\nMessage: " + step.ScaleWarningMessage);
                }
            }
#endif
            group.SelectLayout(step.LayoutName, step.PopupSize);

            return true;
        }

        /// <summary>
        /// Go to the next smallest scale for this Tab.
        /// </summary>
        /// <returns>whether any of this Tab's Group's layouts actually changed</returns>
        /// <seealso cref="SetScale"/>
        /// <seealso cref="SetScalingForGroup"/>
        /// <seealso cref="ScaleUp"/>
        public bool ScaleDown()
        {
            bool scaled = ScaleDownInternal();
            if (scaled)
                OnDirtyingChange();
            return scaled;
        }

        // It would be better if this was just set by ScaleIndex() or something like this
        // but this may not catch every case and it is late in the release so I'm using this
        // prop so that it can be set in the exact place where the ribbon scales by cookie.
        bool _scaledByCookie = false;
        internal bool ScaledByCookie
        {
            get 
            { 
                return _scaledByCookie; 
            }
            set 
            { 
                _scaledByCookie = value; 
            }
        }

        // Get the sum of the widths of the groups in this tab.
        // Used for scaling the Ribbon
        internal int GetNeededWidth()
        {
            int sum = 0;
            DomNodeCollection nodes = ElementInternal.ChildNodes;
            for (int i = 0; i < nodes.Length; i++)
                sum += ((HtmlElement)nodes[i]).OffsetWidth;
            return sum;
        }

        #endregion

        /// <summary>
        /// Whether this Tab is a contextual Tab or not
        /// </summary>
        /// <seealso cref="ContextualGroupId"/>
        public bool Contextual
        {
            get 
            { 
                return _contextual; 
            }
        }

        /// <summary>
        /// The id for the ContextualGroup that this Tab belongs to
        /// </summary>
        /// <seealso cref="Contextual"/>
        public string ContextualGroupId
        {
            get 
            { 
                return _contextualGroupId; 
            }
        }

        /// <summary>
        /// Used internally to remember if this tab was set by a user click.
        /// </summary>
        /// <param name="evt"></param>
        bool _selectedByUser = false;
        internal bool SelectedByUser
        {
            get 
            { 
                return _selectedByUser; 
            }
            set 
            { 
                _selectedByUser = value; 
            }
        }

        bool _launchedByKeyboard = false;
        internal bool LaunchedByKeyboard
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

        /// <summary>
        /// Handle when a tab title is selected via keyboard.
        /// </summary>
        /// <param name="evt"></param>
        private void OnTitleKeyPress(HtmlEvent args)
        {
            if (!CUIUtility.IsNullOrUndefined(args))
            {
                int key = args.KeyCode;
                if (key == (int)Key.Enter)
                {
                    _launchedByKeyboard = true;
                    Utility.CancelEventUtility(args, false, true);
                    OnTitleClick(args);
                }
            }
        }

        /// <summary>
        /// Handle when a tab title is clicked.  
        /// This causes this tab to become the selected one.
        /// </summary>
        /// <param name="evt"></param>
        private void OnTitleClick(HtmlEvent args)
        {
#if PERF_METRICS
            PMetrics.PerfMark(PMarker.perfCUIRibbonTabSwitchWarmStart);
#endif
            Utility.CancelEventUtility(args, false, true);

            _shouldProcessSingleClick = true;
            // If the tab is selected, then we need to make sure that the user didn't try to double click
            // So, we have to wait a bit to let the the double click event fire.
            // Double clicking only works on the selected tab so if this tab is not selected, then
            // we can process the single click right away.
            if (Selected)
            {
                Browser.Window.SetTimeout(TitleClickCallback, 500);
            }
            else
            {
                TitleClickCallback();
            }
        }

        bool _shouldProcessSingleClick = true;

        private void TitleClickCallback()
        {
            // If a double click has been processed since the single click was
            // then we need to ignore the single click since it was really part of 
            // a double click.
            if (!_shouldProcessSingleClick)
                return;
            _selectedByUser = true;

            Ribbon.EnsureCurrentControlStateCommitted();

            Selected = true;

            // close any open tooltips
            Ribbon.CloseOpenTootips();

            // TODO(josefl): This line should be able to be removed.
            // I'm leaving it because it is late in Office14 and I don't want to risk
            // regressing anything.  It will not hurt to have it here too even though
            // it will be called as a sideeffect of setting Selected to true above. O14:637517
            Ribbon.LastFocusedControl = null;

#if PERF_METRICS
            PMetrics.PerfMark(PMarker.perfCUIRibbonTabSwitchWarmEnd);
#endif
        }

        /// <summary>
        /// Moves focus to next or previous group.  
        /// This is only called while focus is in the tab container..
        /// </summary>
        /// <param name="forward"></param>
        internal void MoveGroupFocus(bool forward)
        {
            HtmlElement elm = Browser.Document.ActiveElement;
            string groupId = FindGroupId(elm);

            int length = Children.Count;
            Group gr;
            int index = 0;
            foreach (Group gr1 in Children)
            {
                if (gr1.Id == groupId)
                    break;
                index++;
            }

            int nextGroup;
            if (forward)
                nextGroup = (index + 1) % length;
            else
                nextGroup = index - 1;

            if (nextGroup < 0)
                nextGroup = length + nextGroup;

            while (nextGroup != index)
            {
                gr = (Group)Children[nextGroup];
                if (gr.SetFocusOnFirstControl())
                    return;

                if (forward)
                    nextGroup = (nextGroup + 1) % length;
                else
                    nextGroup = nextGroup - 1;

                if (nextGroup < 0)
                    nextGroup = length + nextGroup;
            }
        }

        // Travel up DOM tree to find the group id
        private string FindGroupId(HtmlElement elm)
        {
            if (elm.NodeName == "LI")
                return elm.Id;
            else
                return FindGroupId((HtmlElement)elm.ParentNode);
        }

        internal void SetRefreshFocus()
        {
            bool focused = false;
            foreach (Group gr in Children)
            {
                if (gr.SetFocusOnFirstControl())
                {
                    focused = true;
                    return;
                }
            }

            if (!focused)
                _elmTitleA.PerformFocus();
        }

        // Focus on the A tag in the tab title
        internal void SetFocusOnTitle()
        {
            if (!CUIUtility.IsNullOrUndefined(_elmTitleA))
                _elmTitleA.PerformFocus();
        }

        private void OnTitleDblClick(HtmlEvent args)
        {
            _shouldProcessSingleClick = false;
            Utility.CancelEventUtility(args, false, true);

            // If the ribbon is not minimized, double-clicking on a tab will minimize it
            Ribbon.MinimizedInternal = true;
            Ribbon.RefreshInternal();
        }

        // Whether this tab or any of its group has controls that are overflowing the 
        // availabe space on the screen.  Used for Scaling.
        internal bool Overflowing
        {
            get
            {
                // Check to see if the tab's contents have overflowed horizontally and are beyond the edge
                // of the browser window.
                if (!CUIUtility.IsNullOrUndefined(ElementInternal) &&
                    !CUIUtility.IsNullOrUndefined(ElementInternal.LastChild))
                {
                    Bounds b = Utility.GetElementPosition((HtmlElement)ElementInternal.LastChild);

                    if (Root.TextDirection != Direction.RTL && Utility.GetViewPortWidth() <= b.Width + b.X)
                        return true;
                }

                foreach (Group group in Children)
                {
                    if (group.Overflowing)
                        return true;
                }
                return false;
            }
        }

        // Get the correct CSS for whether this tab is the active/selected tab or not
        private string GetTitleCSSClassName()
        {
            string className = "ms-cui-tt " + _cssClass;

            if (_selected)
            {
                className += " ms-cui-tt-s";
            }

            return className;
        }

        private string GetBodyCSSClassName()
        {
            // Get the correct CSS for whether this tab is contextual or not
            string className = "ms-cui-tabBody";
            if (Contextual)
            {
                className += " ms-cui-tabBody-" + ContextualGroup.GetColorNameForContextualTabColor(
                    Ribbon.GetContextualGroup(ContextualGroupId).Color);
            }

            return className;
        }

        internal string GetContainerCSSClassName()
        {
            // Get the correct CSS for whether this tab is contextual or not
            string className = "ms-cui-tabContainer";
            if (Contextual)
            {
                className += " ms-cui-tabContainer-" + ContextualGroup.GetColorNameForContextualTabColor(
                    Ribbon.GetContextualGroup(ContextualGroupId).Color);
            }

            return className;
        }

        public override bool Visible
        {
            get 
            { 
                return base.Visible; 
            }
            set
            {
                if (Contextual)
                {
                    throw new InvalidOperationException("Visibility of Contextual Tabs cannot be set explicitly.");
                }

                base.Visible = value;
            }
        }

        string _command;
        public string Command
        {
            get 
            { 
                return _command; 
            }
        }

        internal override bool OnPreBubbleCommand(CommandEventArgs command)
        {
            command.CommandInfo.TabId = Id;
            return base.OnPreBubbleCommand(command);
        }

        internal override void PollForStateAndUpdateInternal()
        {
            // Poll for this Tab's Command
            // A Group is automatically enabled if it does not have a command defined
            bool enabled = string.IsNullOrEmpty(Command) ? true :
                Ribbon.PollForCommandState(Command, null, null);
            Enabled = enabled;

            // If this Tab is disabled, then everything underneath it is also disabled
            if (enabled)
            {
                LastPollTime = DateTime.Now;
                base.PollForStateAndUpdateInternal();
            }

            // We do this here because for performance reasons.  When a user is 
            // switching between tabs, and the application has not told the ribbon
            // to poll, we only need to have the tab poll again if it was not polled since
            // the ribbon last was.  However, if we only need the tab to poll, then we don't 
            // need the ribbon to poll (for the titles, contextual groups etc).  So, for this
            // case (where the tab polls but not the ribbon) we need to remove the disabling
            // css class that is over the tabcontainer element since we have not sucessfully
            // polled for this tab.
            Ribbon.EnsureTabContainerGlobalDisablingRemoved();
        }

        public override void Dispose()
        {
            _elmTitleA.Click -= OnTitleClick;
            _elmTitleA.KeyPress -= OnTitleKeyPress;
            _elmTitleA.DblClick -= OnTitleDblClick;

            base.Dispose();
            _elmATText = null;
            _elmTitle = null;
            _elmTitleA = null;
            _elmTitleSpan = null;
        }
    }
}
