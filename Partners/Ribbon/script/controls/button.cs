using System.Collections.Generic;
using Microsoft.LiveLabs.Html;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace Ribbon.Controls
{
    [Import(MemberNameCasing = Casing.Exact)]
    public class ButtonProperties : MenuItemControlProperties
    {
        extern public ButtonProperties();
        extern public string Alt { get; }
        extern public string CommandPreview { get; }
        extern public string CommandRevert { get; }
        extern public string CommandType { get; }
        extern public string Image16by16 { get; }
        extern public string Image16by16Class { get; }
        extern public string Image16by16Top { get; }
        extern public string Image16by16Left { get; }
        extern public string Image32by32 { get; }
        extern public string Image32by32Class { get; }
        extern public string Image32by32Top { get; }
        extern public string Image32by32Left { get; }
        extern public string LabelText { get; }
        extern public string Description { get; }
        extern public string QueryCommand { get; }
    }

    /// <summary>
    /// A class that represents a button that can be clicked in the ribbon.  These can be in the Ribbon itself or in Menus.
    /// This Control takes the following parameters:
    /// ClkCmd - The id of the Command that is raised when the button is clicked
    /// CmdTpe - The type of the command that is issued when this control is clicked.  It defaults to "General".  It can also be set to "OptionSelect" for when this Control is part of a combo box type menu.
    /// LargeImg - Url to large image(32x32) icon for the "Large" display mode of this Control
    /// LblTxt - Alt text for pictures as well as the text that appears under the Large icon in the "Large" display mode and next to the small icon in the "Medium" display mode
    /// SmallImg - Url to the small image(16x16) icon for the "Small" and "Medium" display modes of this control
    /// </summary>
    internal class Button : Control, IMenuItem, ISelectableControl
    {
        // Large display mode elements
        Anchor _elmLarge;

        // Medium display mode elements
        Anchor _elmMedium;

        // Small display mode elements
        Anchor _elmSmall;

        // Menu16 display mode elements
        Anchor _elmMenu16;

        // Menu32 display mode elements
        Anchor _elmMenu32;

        // ISelectableControl Variables
        string _menuItemId;
        string _commandValueId;
        Anchor _elmFssbLarge;
        Anchor _elmFssbMedium;
        Anchor _elmFssbSmall;
        Anchor _elmFsddMenu;
        Span _elmFsddText;

        public Button(Root root,
                               string id,
                               ButtonProperties properties)
            : base(root, id, properties)
        {
            AddDisplayMode("Small");
            AddDisplayMode("Medium");
            AddDisplayMode("Large");
            AddDisplayMode("Menu");
            AddDisplayMode("Menu16");
            AddDisplayMode("Menu32");
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

        private Anchor CreateDOMElementForDisplayModeCore(string displayMode, bool attachEvents)
        {
            string alt = string.IsNullOrEmpty(Properties.Alt) ?
                        GetLabel() : Properties.Alt;

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


                    // Set up event handlers
                    if (attachEvents)
                        AttachEventsForDisplayMode("Small");
                    return _elmSmall;
                case "Menu":
                // REVIEW(josefl):  Perhaps we should just combine Menu16 and Menu into one and the same
                // display mode that will show the image if it is there.                           
                // TODO(JKern): Add no-icon menu display mode here
                case "Menu16":
                    _elmMenu16 = CreateStandardControlDOMElement(
                         this,
                         Root,
                         "Menu16",
                         Properties,
                         true,
                         false);

                    if (attachEvents)
                        AttachEventsForDisplayMode("Menu16");
                    return _elmMenu16;
                case "Menu32":
                    _elmMenu32 = CreateStandardControlDOMElement(
                                    this,
                                    Root,
                                    "Menu32",
                                    Properties,
                                    true,
                                    false);

                    if (attachEvents)
                        AttachEventsForDisplayMode("Menu32");
                    return _elmMenu32;
                default:
                    EnsureValidDisplayMode(displayMode);
                    return null;
            }
        }

        internal override void AttachDOMElementsForDisplayMode(string displayMode)
        {
            Anchor elm = (Anchor)Browser.Document.GetById(Id + "-" + displayMode);
            StoreElementForDisplayMode(elm, displayMode);

            // Only do hookup for non-menu display modes for now
            switch (displayMode)
            {
                case "Large":
                    _elmLarge = elm;
                    break;
                case "Medium":
                    _elmMedium = elm;
                    break;
                case "Small":
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
                    AttachEvents(_elmMenu32, false);
                    break;
                case "Menu16":
                    AttachEvents(_elmMenu16, false);
                    break;
                case "Menu":
                    AttachEvents(_elmMenu16, false);
                    break;
            }
        }

        private void AttachEvents(HtmlElement elm, bool doubleClick)
        {
            elm.Click += OnClick;
            if (doubleClick)
                elm.DblClick += OnDblClick;
            elm.MouseOver += HandleMouseFocus;
            elm.MouseOut += HandleMouseBlur;
            elm.Focus += HandleTabFocus;
            elm.Blur += HandleTabBlur;
        }

        protected override void ReleaseEventHandlers()
        {
            if (!CUIUtility.IsNullOrUndefined(_elmLarge))
                RemoveEvents(_elmLarge, true);
            if (!CUIUtility.IsNullOrUndefined(_elmMedium))
                RemoveEvents(_elmMedium, true);
            if (!CUIUtility.IsNullOrUndefined(_elmSmall))
                RemoveEvents(_elmSmall, true);
            if (!CUIUtility.IsNullOrUndefined(_elmMenu32))
                RemoveEvents(_elmMenu32, false);
            if (!CUIUtility.IsNullOrUndefined(_elmMenu16))
                RemoveEvents(_elmMenu16, false);
        }

        private void RemoveEvents(HtmlElement elm, bool dblClick)
        {
            elm.Click -= OnClick;
            if (dblClick)
                elm.DblClick -= OnDblClick;
            elm.MouseOver -= HandleMouseFocus;
            elm.MouseOut -= HandleMouseBlur;
            elm.Focus -= HandleTabFocus;
            elm.Blur -= HandleTabBlur;
        }

        public override void OnEnabledChanged(bool enabled)
        {
            Utility.SetEnabledOnElement(_elmLarge, enabled);
            Utility.SetEnabledOnElement(_elmMedium, enabled);
            Utility.SetEnabledOnElement(_elmSmall, enabled);
            Utility.SetEnabledOnElement(_elmMenu32, enabled);
            Utility.SetEnabledOnElement(_elmMenu16, enabled);

            if (!enabled)
                RemoveHighlight();
        }

        internal override string ControlType
        {
            get
            {
                return "Button";
            }
        }

        #region IMenuItem Implementation
        public override string GetTextValue()
        {
            return (string)GetLabel();
        }
        // REVIEW(josefl): Get rid of this
        protected string GetLabel()
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
            if (!CUIUtility.IsNullOrUndefined(_elmMenu16))
                _elmMenu16.PerformFocus();
            if (!CUIUtility.IsNullOrUndefined(_elmMenu32))
                _elmMenu32.PerformFocus();
        }
        #endregion

        #region ISelectableControl Members
        public HtmlElement GetDropDownDOMElementForDisplayMode(string displayMode)
        {
            HtmlElement d;
            switch (displayMode)
            {
                case "Large":
                    d = _elmFssbLarge;
                    break;
                case "Medium":
                    d = _elmFssbMedium;
                    break;
                case "Small":
                    d = _elmFssbSmall;
                    break;
                case "Menu":
                    d = _elmFsddMenu;
                    break;
                case "Text":
                    d = _elmFsddText;
                    break;
                default:
                    d = new Span();
                    break;
            }

            if (d != null)
                return d;

            return CreateDropDownDOMElementForDisplayMode(displayMode);
        }

        private HtmlElement CreateDropDownDOMElementForDisplayMode(string displayMode)
        {
            Anchor d;

            // After we create the DOM element and the clone it, we set the member variable DOM 
            // element to null so that we do not leak it.
            // O14:394328
            switch (displayMode)
            {
                case "Large":
                    d = (Anchor)CreateDOMElementForDisplayModeCore(displayMode, false).CloneNode(true);
                    _elmLarge = null;
                    d.Style.Height = "auto";
                    ((HtmlElement)d.ChildNodes[1]).Style.Height = "auto";
                    _elmFssbLarge = d;
                    break;
                case "Medium":
                    d = (Anchor)CreateDOMElementForDisplayModeCore(displayMode, false).CloneNode(true);
                    _elmMedium = null;
                    _elmFssbMedium = d;
                    break;
                case "Small":
                    d = (Anchor)CreateDOMElementForDisplayModeCore(displayMode, false).CloneNode(true);
                    _elmSmall = null;
                    _elmFssbSmall = d;
                    break;
                case "Menu":
                    _elmFsddMenu = (Anchor)CreateDOMElementForDisplayModeCore("Menu", false).CloneNode(true);
                    _elmMenu16 = null;
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
                    EnsureValidDisplayMode(displayMode);
                    return null;
            }

            return d;
        }

        public void Deselect()
        {
            // This control does not hold state, so no deselection is necessary
        }

        public string GetMenuItemId()
        {
            return _menuItemId;
        }

        public string GetCommandValueId()
        {
            return _commandValueId;
        }

        internal override bool SetFocusOnControl()
        {
            if (!Enabled)
                return false;

            DisplayedComponent.ElementInternal.PerformFocus();
            return true;
        }

        public void FocusOnDisplayedComponent()
        {
            ReceiveFocus();
        }
        #endregion

        #region Event Handlers
        protected override void OnClick(HtmlEvent args)
        {
            if (!CUIUtility.IsNullOrUndefined(typeof(PMetrics)))
                PMetrics.PerfMark(PMarker.perfCUIRibbonButtonOnClickStart);

            Utility.CancelEventUtility(args, true, true);
            CloseToolTip();
            if (!Enabled)
                return;

            Root.LastCommittedControl = this;

            CommandType ct = CommandType.General;
            Dictionary<string, string> dict = this.StateProperties;
            string cmdtpe = Properties.CommandType;
            if (!string.IsNullOrEmpty(cmdtpe) && cmdtpe == "OptionSelection")
            {
                ct = CommandType.OptionSelection;
            }
            dict["CommandValueId"] = this._commandValueId;
            dict["MenuItemId"] = this._menuItemId;
            dict["SourceControlId"] = this.Id;
            DisplayedComponent.RaiseCommandEvent(Properties.Command,
                                                 ct,
                                                 dict);

            if (!CUIUtility.IsNullOrUndefined(typeof(PMetrics)))
                PMetrics.PerfMark(PMarker.perfCUIRibbonButtonOnClickEnd);
        }

        protected void HandleMouseFocus(HtmlEvent args)
        {
            OnBeginFocus();
            if (!Enabled)
                return;
            ControlComponent comp = DisplayedComponent;
            if (comp is MenuItem)
                ((MenuItem)comp).Focused = true;
            if (string.IsNullOrEmpty(Properties.CommandPreview))
                return;

            Dictionary<string, string> dict = this.StateProperties;
            dict["CommandValueId"] = this._commandValueId;
            dict["MenuItemId"] = this._menuItemId;

            CommandType ct = CommandType.Preview;
            string cmdtpe = Properties.CommandType;
            if (!string.IsNullOrEmpty(cmdtpe) && cmdtpe == "OptionSelection")
            {
                ct = CommandType.OptionPreview;
            }

            comp.RaiseCommandEvent(Properties.CommandPreview,
                                                 ct,
                                                 dict);
        }

        protected void HandleMouseBlur(HtmlEvent args)
        {
            RemoveHighlight();
            OnEndFocus();
            if (!Enabled)
                return;
            ControlComponent comp = DisplayedComponent;
            if (comp is MenuItem)
                ((MenuItem)comp).Focused = false;
            if (string.IsNullOrEmpty(Properties.CommandRevert))
                return;

            CommandType ct = CommandType.PreviewRevert;
            Dictionary<string, string> dict = this.StateProperties;
            dict["CommandValueId"] = this._commandValueId;
            dict["MenuItemId"] = this._menuItemId;

            string cmdtpe = Properties.CommandType;
            if (!string.IsNullOrEmpty(cmdtpe) && cmdtpe == "OptionSelection")
            {
                ct = CommandType.OptionPreviewRevert;
            }

            comp.RaiseCommandEvent(Properties.CommandRevert,
                                         ct,
                                         dict);
        }

        protected void HandleTabFocus(HtmlEvent args)
        {
            OnBeginFocus();
            ControlComponent comp = DisplayedComponent;
            if (comp is MenuItem)
            {
                ((MenuItem)comp).Focused = true;
                Highlight(Enabled);
            }
            else if (Enabled)
            {
                Root.LastFocusedControl = this;
            }
        }

        protected void HandleTabBlur(HtmlEvent args)
        {
            RemoveHighlight();
            OnEndFocus();
            if (!Enabled)
                return;

            ControlComponent comp = DisplayedComponent;
            if (comp is MenuItem)
                ((MenuItem)comp).Focused = false;
        }

        public override void OnMenuClosed()
        {
            RemoveHighlight();
            CloseToolTip();
        }
        #endregion

        private void RemoveHighlight()
        {
            Utility.RemoveCSSClassFromElement(_elmLarge, "ms-cui-ctl-hoveredOver");
            Utility.RemoveCSSClassFromElement(_elmMedium, "ms-cui-ctl-hoveredOver");
            Utility.RemoveCSSClassFromElement(_elmSmall, "ms-cui-ctl-hoveredOver");
            Utility.RemoveCSSClassFromElement(_elmMenu32, "ms-cui-ctl-hoveredOver");
            Utility.RemoveCSSClassFromElement(_elmMenu32, "ms-cui-ctl-disabledHoveredOver");
            Utility.RemoveCSSClassFromElement(_elmMenu16, "ms-cui-ctl-hoveredOver");
            Utility.RemoveCSSClassFromElement(_elmMenu16, "ms-cui-ctl-disabledHoveredOver");
        }

        private void Highlight(bool enabled)
        {
            string cssClass = "ms-cui-ctl-hoveredOver";
            if (!enabled)
            {
                cssClass = "ms-cui-ctl-disabledHoveredOver";
                Utility.EnsureCSSClassOnElement(_elmMenu16, cssClass);
                Utility.EnsureCSSClassOnElement(_elmMenu32, cssClass);
            }
            else
            {
                Utility.EnsureCSSClassOnElement(_elmLarge, cssClass);
                Utility.EnsureCSSClassOnElement(_elmMedium, cssClass);
                Utility.EnsureCSSClassOnElement(_elmSmall, cssClass);
                Utility.EnsureCSSClassOnElement(_elmMenu16, cssClass);
                Utility.EnsureCSSClassOnElement(_elmMenu32, cssClass);
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            _elmLarge = null;
            _elmMedium = null;
            _elmSmall = null;
            _elmMenu16 = null;
            _elmMenu32 = null;
            _elmFssbLarge = null;
            _elmFssbMedium = null;
            _elmFssbSmall = null;
            _elmFsddMenu = null;
            _elmFsddText = null;
        }

        private ButtonProperties Properties
        {
            get
            {
                return (ButtonProperties)base.ControlProperties;
            }
        }

    }
}
