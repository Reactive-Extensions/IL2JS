using Microsoft.LiveLabs.Html;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace Ribbon.Controls
{
    [Import(MemberNameCasing = Casing.Exact)]
    public class BooleanControlProperties : MenuItemControlProperties
    {
        extern public BooleanControlProperties();
        extern public string Alt { get; }
        extern public string CommandType { get; }
        extern public string CommandPreview { get; }
        extern public string CommandRevert { get; }
        extern public string LabelText { get; }
        extern public string QueryCommand { get; }
    }

    [Import(MemberNameCasing = Casing.Exact)]
    public class ToggleButtonProperties : BooleanControlProperties
    {
        extern public ToggleButtonProperties();
        extern public string Image16by16 { get; }
        extern public string Image16by16Class { get; }
        extern public string Image32by32 { get; }
        extern public string Image32by32Class { get; }
        extern public string ImageArrow { get; }
        extern public string ImageArrowClass { get; }
    }

    public static class ToggleButtonCommandProperties
    {
        public const string On = "On";
        public const string CommandValueId = "CommandValueId";
    }

    /// <summary>
    /// A class representing a control that can be on or off.
    /// Takes the following parameters:
    /// SmallImg - Url to the image displayed in the button
    /// CmdOff - id of the Command that is issued when the button is turned off
    /// CmdOn - id of the Command that is issued when the button is turned on
    /// CmdTpe (optional) - the type of command sent on click (General or OptionSelection)
    /// MenuItemId (only for menu or menu16 display modes) - a unique identifier for the option
    /// CommandValueId (only for menu or menu16 display modes) - the command value to send on selection from a menu
    /// LblTxt - Alt text for image in the button and/or Label of the control
    /// LblStyle (optional) - the css style for the label
    /// </summary>
    internal class ToggleButton : Control, IMenuItem, ISelectableControl
    {
        protected string _id;

        public ToggleButton(Root root, string id, BooleanControlProperties properties)
            : base(root, id, properties)
        {
            _id = id;
            AddDisplayModes();
            StateProperties[ToggleButtonCommandProperties.On] = false.ToString();
        }
        // Large display mode elements
        Anchor _elmLarge;

        string _menuItemId;
        string _commandValueId;

        // Small display mode elements
        Anchor _elmSmall;

        // Medium display mode elements
        Anchor _elmMedium;

        // Menu display mode elements
        Anchor _elmMenu;

        // Menu16 display mode elements
        Anchor _elmSMenu;

        // Menu32 display mode elements
        Anchor _elmLMenu;

        // Selectable Menu DOM Elements
        Span _elmFsddMenu;
        Span _elmFsddText;

        public bool On
        {
            get
            {
                return Utility.IsTrue(StateProperties[ToggleButtonCommandProperties.On]);
            }
            set
            {
                StateProperties[ToggleButtonCommandProperties.On] = value.ToString();
                SetState(value);
            }
        }

        protected override ControlComponent CreateComponentForDisplayModeInternal(string displayMode)
        {
            ControlComponent comp;
            if (displayMode.StartsWith("Menu"))
            {
                comp = Root.CreateMenuItem(
                    Id + "-" + displayMode + Root.GetUniqueNumber(),
                    displayMode,
                    this);

                _menuItemId = Properties.MenuItemId;
                _commandValueId = Properties.CommandValueId;
                if (string.IsNullOrEmpty(_commandValueId))
                    _commandValueId = _menuItemId;
            }
            else
            {
                comp = base.CreateComponentForDisplayModeInternal(displayMode);
            }

            return comp;
        }

        protected override HtmlElement CreateDOMElementForDisplayMode(string displayMode)
        {
            return CreateDOMElementForDisplayModeCore(displayMode, true);
        }

        private HtmlElement CreateDOMElementForDisplayModeCore(string displayMode, bool attachEvents)
        {
            switch (displayMode)
            {
                case "Large":
                    _elmLarge = CreateStandardControlDOMElement(
                                    this,
                                    Root,
                                    "Large",
                                    Properties,
                                    false,
                                    false);
                    AttachDOMElementsForDisplayMode("Large");
                    if (attachEvents)
                        AttachEventsForDisplayMode("Large");
                    return _elmLarge;
                case "Medium":
                    _elmMedium = CreateStandardControlDOMElement(
                                    this,
                                    Root,
                                    "Medium",
                                    Properties,
                                    false,
                                    false);


                    AttachDOMElementsForDisplayMode("Medium");
                    // Set up event handlers
                    if (attachEvents)
                        AttachEventsForDisplayMode("Medium");
                    return _elmMedium;
                case "Small":
                    _elmSmall = CreateStandardControlDOMElement(
                                   this, 
                                   Root, 
                                   "Small", 
                                   Properties, 
                                   false, 
                                   false);

                    AttachDOMElementsForDisplayMode("Small");
                    // Set up event handlers
                    if (attachEvents)
                        AttachEventsForDisplayMode("Small");
                    return _elmSmall;
                case "Menu":
                    _elmMenu = CreateStandardControlDOMElement(
                        this,
                        Root,
                        "Menu",
                        Properties,
                        true,
                        false);

                    if (attachEvents)
                        AttachEventsForDisplayMode("Menu");
                    return _elmMenu;
                case "Menu16":
                    _elmSMenu = CreateStandardControlDOMElement(
                        this,
                        Root,
                        "Menu16",
                        Properties,
                        true,
                        false);

                    if (attachEvents)
                        AttachEventsForDisplayMode("Menu16");
                    return _elmSMenu;
                case "Menu32":
                    _elmLMenu = CreateStandardControlDOMElement(
                        this,
                        Root,
                        "Menu32",
                        Properties,
                        true,
                        false);

                    if (attachEvents)
                        AttachEventsForDisplayMode("Menu32");
                    return _elmLMenu;
                default:
                    EnsureValidDisplayMode(displayMode);
                    return null;
            }
        }

        private string currentDisplayMode;
        internal override void AttachDOMElementsForDisplayMode(string displayMode)
        {
            currentDisplayMode = displayMode;

            Anchor elm = (Anchor)Browser.Document.GetById(Id + "-" + displayMode);
            StoreElementForDisplayMode(elm, displayMode);

            // Only do hookup for non-menu display modes for now
            switch (displayMode)
            {
                case "Large":
                    if (!CUIUtility.IsNullOrUndefined(elm))
                        _elmLarge = elm;
                    break;
                case "Medium":
                    if (!CUIUtility.IsNullOrUndefined(elm))
                        _elmMedium = elm;
                    break;
                case "Small":
                    if (!CUIUtility.IsNullOrUndefined(elm))
                        _elmSmall = elm;
                    break;
            }
        }

        internal override void AttachEventsForDisplayMode(string displayMode)
        {
            // Right now there is no hookup for menus because they are not server rendered
            switch (displayMode)
            {
                case "Large":
                    AttachEvents(_elmLarge, true);
                    break;
                case "Medium":
                    AttachEvents(_elmMedium, true);
                    break;
                case "Small":
                    AttachEvents(_elmSmall, true);
                    break;
                case "Menu32":
                    AttachEvents(_elmLMenu, false);
                    break;
                case "Menu16":
                    AttachEvents(_elmSMenu, false);
                    break;
                case "Menu":
                    AttachEvents(_elmMenu, false);
                    break;
            }
        }

        private void AttachEvents(HtmlElement elm, bool forMenu)
        {
            if (forMenu)
            {
                elm.MouseOver += OnFocus;
                elm.MouseOut += OnBlur;
            }
            elm.Click += OnClick;
            elm.MouseOver += OnMouseover;
            elm.MouseOut += OnMouseout;
            elm.Focus += OnKeyboardFocus;
            elm.Blur += OnBlur;
        }

        protected override void ReleaseEventHandlers()
        {
            if (!CUIUtility.IsNullOrUndefined(_elmLarge))
                RemoveEvents(_elmLarge, true);
            if (!CUIUtility.IsNullOrUndefined(_elmMedium))
                RemoveEvents(_elmMedium, true);
            if (!CUIUtility.IsNullOrUndefined(_elmSmall))
                RemoveEvents(_elmSmall, true);
            if (!CUIUtility.IsNullOrUndefined(_elmLMenu))
                RemoveEvents(_elmLMenu, false);
            if (!CUIUtility.IsNullOrUndefined(_elmSMenu))
                RemoveEvents(_elmSMenu, false);
            if (!CUIUtility.IsNullOrUndefined(_elmMenu))
                RemoveEvents(_elmMenu, false);
        }

        private void RemoveEvents(HtmlElement elm, bool forMenu)
        {
            if (forMenu)
            {
                elm.MouseOver -= OnFocus;
                elm.MouseOut -= OnBlur;
            }
            elm.Click -= OnClick;
            elm.MouseOver -= OnMouseover;
            elm.MouseOut -= OnMouseout;
            elm.Focus -= OnKeyboardFocus;
            elm.Blur -= OnBlur;
        }

        internal override bool SetFocusOnControl()
        {
            if (!Enabled)
                return false;

            HtmlElement elm = DisplayedComponent.ElementInternal;
            elm.PerformFocus();
            return true;
        }

        public override void OnEnabledChanged(bool enabled)
        {
            Utility.SetEnabledOnElement(this._elmSmall, enabled);
            Utility.SetEnabledOnElement(this._elmMedium, enabled);
            Utility.SetEnabledOnElement(this._elmLarge, enabled);
            Utility.SetEnabledOnElement(_elmMenu, enabled);
            Utility.SetEnabledOnElement(_elmSMenu, enabled);
            Utility.SetEnabledOnElement(_elmLMenu, enabled);

            if (On)
            {
                if (enabled)
                    SetState(true);
                else
                    SetState(false); // when the control is disabled, don't show state (O14:528999)
            }
        }

        internal override string ControlType
        {
            get
            {
                return "ToggleButton";
            }
        }

        protected override void OnStateChanged()
        {
            SetState(Utility.IsTrue(StateProperties[ToggleButtonCommandProperties.On]));
        }

        #region ISelectableControl implementation
        public HtmlElement GetDropDownDOMElementForDisplayMode(string displayMode)
        {
            Span domelem;
            switch (displayMode)
            {
                case "Menu16":
                    domelem = _elmFsddMenu;
                    break;
                case "Text":
                    domelem = _elmFsddText;
                    break;
                default:
                    domelem = new Span();
                    break;
            }

            if (domelem != null)
                return domelem;

            return CreateDropDownDOMElementForDisplayMode(displayMode);
        }

        private HtmlElement CreateDropDownDOMElementForDisplayMode(string displayMode)
        {
            switch (displayMode)
            {
                case "Menu":
                    _elmFsddMenu = (Span)CreateDOMElementForDisplayModeCore("Menu", false).CloneNode(true);
                    return _elmFsddMenu;
                case "Menu16":
                    _elmFsddMenu = (Span)CreateDOMElementForDisplayModeCore("Menu16", false).CloneNode(true);
                    return _elmFsddMenu;
                case "Text":
                    Anchor textA = new Anchor();
                    Utility.NoOpLink(textA);

                    _elmFsddText = new Span();
                    _elmFsddText.ClassName = "ms-cui-textmenuitem";
                    UIUtility.SetInnerText(textA, Properties.LabelText);
                    _elmFsddText.AppendChild(textA);
                    return _elmFsddText;
                default:
                    return new Span();
            }
        }

        public void Deselect()
        {
        }

        public string GetMenuItemId()
        {
            return _menuItemId;
        }

        public string GetCommandValueId()
        {
            return _commandValueId;
        }

        public void FocusOnDisplayedComponent()
        {
            ReceiveFocus();
        }
        #endregion

        #region IMenuItem Implementation
        public override string GetTextValue()
        {
            return Properties.LabelText;
        }

        public override void ReceiveFocus()
        {
            OnBeginFocus();
            ControlComponent comp = DisplayedComponent;
            if (CUIUtility.IsNullOrUndefined(comp))
                return;

            ((MenuItem)comp).Focused = true;
            if (!CUIUtility.IsNullOrUndefined(_elmMenu))
                _elmMenu.PerformFocus();
            if (!CUIUtility.IsNullOrUndefined(_elmSMenu))
                _elmSMenu.PerformFocus();
        }

        public override void OnMenuClosed()
        {
            CloseToolTip();
        }
        #endregion

        protected override void OnClick(HtmlEvent evt)
        {
            if (!CUIUtility.IsNullOrUndefined(typeof(PMetrics)))
                PMetrics.PerfMark(PMarker.perfCUIRibbonToggleButtonOnClickStart);

            CloseToolTip();
            Utility.CancelEventUtility(evt, false, true);
            
            if (!Enabled)
                return;

            Root.LastCommittedControl = this;

            CommandType ct = CommandType.General;
            string cmdtpe = Properties.CommandType;
            if (!string.IsNullOrEmpty(cmdtpe) && cmdtpe == "OptionSelection")
            {
                ct = CommandType.OptionSelection;
                StateProperties[ToggleButtonCommandProperties.CommandValueId] = _commandValueId;
            }

            // Send out the command
            StateProperties[ToggleButtonCommandProperties.On] =
                (!Utility.IsTrue(StateProperties[ToggleButtonCommandProperties.On])).ToString();
            DisplayedComponent.RaiseCommandEvent(Properties.Command,
                                                 ct,
                                                 StateProperties);
            if (Root.PollForState)
                PollForStateAndUpdate();
            else
                SetState(Utility.IsTrue(StateProperties[ToggleButtonCommandProperties.On]));

            if (!CUIUtility.IsNullOrUndefined(typeof(PMetrics)))
                PMetrics.PerfMark(PMarker.perfCUIRibbonToggleButtonOnClickEnd);
        }

        private void Toggle()
        {
            // Toggle the on/off state of the button
            bool on = !Utility.IsTrue(StateProperties[ToggleButtonCommandProperties.On]);
            StateProperties[ToggleButtonCommandProperties.On] = on.ToString();
            SetState(on);
        }

        protected virtual void SetState(bool on)
        {
            // Set the UI of the button to reflect its on/off state
            if (!CUIUtility.IsNullOrUndefined(_elmSmall))
            {
                if (on)
                    Utility.EnsureCSSClassOnElement(_elmSmall, "ms-cui-ctl-on");
                else
                    Utility.RemoveCSSClassFromElement(_elmSmall, "ms-cui-ctl-on");
            }

            if (!CUIUtility.IsNullOrUndefined(_elmMedium))
            {
                if (on)
                    Utility.EnsureCSSClassOnElement(_elmMedium, "ms-cui-ctl-on");
                else
                    Utility.RemoveCSSClassFromElement(_elmMedium, "ms-cui-ctl-on");
            }

            if (!CUIUtility.IsNullOrUndefined(_elmLarge))
            {
                if (on)
                    Utility.EnsureCSSClassOnElement(_elmLarge, "ms-cui-ctl-on");
                else
                    Utility.RemoveCSSClassFromElement(_elmLarge, "ms-cui-ctl-on");
            }

            if (!CUIUtility.IsNullOrUndefined(_elmLMenu))
            {
                if (on)
                    Utility.EnsureCSSClassOnElement(_elmLMenu, "ms-cui-ctl-on");
                else
                    Utility.RemoveCSSClassFromElement(_elmLMenu, "ms-cui-ctl-on");
            }

            if (!CUIUtility.IsNullOrUndefined(_elmMenu))
            {
                if (on)
                    Utility.EnsureCSSClassOnElement(_elmMenu, "ms-cui-ctl-on");
                else
                    Utility.RemoveCSSClassFromElement(_elmMenu, "ms-cui-ctl-on");
            }

            if (!CUIUtility.IsNullOrUndefined(_elmSMenu))
            {
                if (on)
                    Utility.EnsureCSSClassOnElement(_elmSMenu, "ms-cui-ctl-on");
                else
                    Utility.RemoveCSSClassFromElement(_elmSMenu, "ms-cui-ctl-on");
            }
        }

        private void Highlight()
        {
            if (!CUIUtility.IsNullOrUndefined(_elmLMenu))
                Utility.RemoveCSSClassFromElement(_elmLMenu, "ms-cui-ctl-disabledHoveredOver");

            if (!CUIUtility.IsNullOrUndefined(_elmMenu))
                Utility.RemoveCSSClassFromElement(_elmMenu, "ms-cui-ctl-disabledHoveredOver");

            if (!CUIUtility.IsNullOrUndefined(_elmSMenu))
                Utility.RemoveCSSClassFromElement(_elmSMenu, "ms-cui-ctl-disabledHoveredOver");
        }

        private void RemoveHighlight()
        {
            if (!CUIUtility.IsNullOrUndefined(_elmLMenu))
                Utility.EnsureCSSClassOnElement(_elmLMenu, "ms-cui-ctl-disabledHoveredOver");

            if (!CUIUtility.IsNullOrUndefined(_elmMenu))
                Utility.EnsureCSSClassOnElement(_elmMenu, "ms-cui-ctl-disabledHoveredOver");

            if (!CUIUtility.IsNullOrUndefined(_elmSMenu))
                Utility.RemoveCSSClassFromElement(_elmSMenu, "ms-cui-ctl-disabledHoveredOver");
        }

        private void OnKeyboardFocus(HtmlEvent args)
        {
            Root.LastFocusedControl = this;
            OnFocus(args);
        }
        private void OnFocus(HtmlEvent args)
        {
            OnBeginFocus();
            if (!Enabled)
            {
                Highlight();
                return;
            }

            ControlComponent comp = DisplayedComponent;
            if (comp is MenuItem)
                ((MenuItem)comp).Focused = true;

            if (string.IsNullOrEmpty(Properties.CommandPreview))
                return;

            CommandType ct = CommandType.Preview;
            string cmdtpe = Properties.CommandType;
            if (!string.IsNullOrEmpty(cmdtpe) && cmdtpe == "OptionSelection")
            {
                ct = CommandType.OptionPreview;
                StateProperties[ToggleButtonCommandProperties.CommandValueId] = _commandValueId;
            }

            comp.RaiseCommandEvent(Properties.CommandPreview,
                                   ct,
                                   StateProperties);
        }

        private void OnMouseover(HtmlEvent args)
        {
            OnBeginFocus();
            if (!Enabled)
                return;

            if (string.IsNullOrEmpty(Properties.CommandPreview))
                return;

            CommandType ct = CommandType.Preview;
            string cmdtpe = Properties.CommandType;
            if (!string.IsNullOrEmpty(cmdtpe) && cmdtpe == "OptionSelection")
            {
                ct = CommandType.OptionPreview;
                StateProperties[ToggleButtonCommandProperties.CommandValueId] = _commandValueId;
            }

            DisplayedComponent.RaiseCommandEvent(Properties.CommandPreview, 
                                                 ct, 
                                                 StateProperties);
        }


        private void OnBlur(HtmlEvent args)
        {
            OnEndFocus();
            if (!Enabled)
                return;

            ControlComponent comp = DisplayedComponent;
            if (comp is MenuItem)
                ((MenuItem)comp).Focused = false;

            if (string.IsNullOrEmpty(Properties.CommandRevert))
                return;

            CommandType ct = CommandType.PreviewRevert;
            string cmdtpe = Properties.CommandType;
            if (!string.IsNullOrEmpty(cmdtpe) && cmdtpe == "OptionSelection")
            {
                ct = CommandType.OptionPreviewRevert;
                StateProperties[ToggleButtonCommandProperties.CommandValueId] = _commandValueId;
            }

            comp.RaiseCommandEvent(Properties.CommandRevert,
                                   ct,
                                   StateProperties);
        }

        private void OnMouseout(HtmlEvent args)
        {
            OnEndFocus();
            if (!Enabled)
                return;

            if (string.IsNullOrEmpty(Properties.CommandRevert))
                return;

            CommandType ct = CommandType.PreviewRevert;
            string cmdtpe = Properties.CommandType;
            if (!string.IsNullOrEmpty(cmdtpe) && cmdtpe == "OptionSelection")
            {
                ct = CommandType.OptionPreviewRevert;
                StateProperties[ToggleButtonCommandProperties.CommandValueId] = _commandValueId;
            }

            DisplayedComponent.RaiseCommandEvent(Properties.CommandRevert,
                                                 ct,
                                                 StateProperties);
        }

        internal override void PollForStateAndUpdate()
        {
            bool succeeded = PollForStateAndUpdateInternal(Properties.Command,
                                                           Properties.QueryCommand,
                                                           StateProperties,
                                                           false);
            if (succeeded)
                SetState(Utility.IsTrue(StateProperties[ToggleButtonCommandProperties.On]));
        }

        protected virtual void AddDisplayModes()
        {
            AddDisplayMode("Small");
            AddDisplayMode("Medium");
            AddDisplayMode("Large");
            AddDisplayMode("Menu");
            AddDisplayMode("Menu16");
            AddDisplayMode("Menu32");
        }

        public override void Dispose()
        {
            base.Dispose();
            _elmSmall = null;
            _elmMedium = null;
            _elmMenu = null;
            _elmSMenu = null;
            _elmLMenu = null;

            _elmFsddMenu = null;
            _elmFsddText = null;
        }

        private ToggleButtonProperties Properties
        {
            get
            {
                return (ToggleButtonProperties)ControlProperties;
            }
        }
    }
}
