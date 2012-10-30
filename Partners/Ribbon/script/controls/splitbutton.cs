using System.Collections.Generic;
using Microsoft.LiveLabs.Html;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace Ribbon.Controls
{
    [Import(MemberNameCasing = Casing.Exact)]
    public class SplitButtonProperties : MenuLauncherControlProperties
    {
        extern public SplitButtonProperties();
        extern public string Alt { get; }
        extern public string CommandPreview { get; }
        extern public string CommandRevert { get; }
        extern public string Image16by16 { get; }
        extern public string Image16by16Class { get; }
        extern public string Image16by16Top { get; }
        extern public string Image16by16Left { get; }
        extern public string Image32by32 { get; }
        extern public string Image32by32Class { get; }
        extern public string Image32by32Top { get; }
        extern public string Image32by32Left { get; }
        extern public string LabelText { get; }
        extern public string MenuAlt { get; }
        extern public string QueryCommand { get; }
    }

    /// <summary>
    /// A class representing a splitbutton that executes an action or drops a menu.
    /// This Control takes the following parameters:
    /// ArrowImg - Url to down arrow image.
    /// BtnAlt - Alt text of button anchor
    /// BtnClkCmd - The id of the Command that is issued when the main button is clicked
    /// LargeImg - Url to the main button's image
    /// LblTxt - Text below the big image.
    /// MnuBtnAlt - Alt text of the menu button anchor.
    /// MnuBtnClkCmd - id of the Command that is issued when the menu button is clicked
    /// </summary>
    internal class SplitButton : MenuLauncher
    {
        Span _elmLarge;
        Span _elmLargeImgCont;
        Anchor _elmLargeButton;
        Anchor _elmLargeMenuButton;
        Image _elmLargeArrowImg;

        Span _elmMedium;
        Span _elmMediumImgCont;
        Anchor _elmMediumButton;
        Anchor _elmMediumMenuButton;
        Image _elmMediumArrowImg;

        Span _elmSmall;
        Span _elmSmallImgCont;
        Anchor _elmSmallButton;
        Anchor _elmSmallMenuButton;
        Image _elmSmallArrowImg;

        public SplitButton(Root root,
                           string id,
                           SplitButtonProperties properties,
                           Menu menu)
            : base(root, id, properties, menu)
        {
            AddDisplayMode("Large");
            AddDisplayMode("Medium");
            AddDisplayMode("Small");
        }

        protected override HtmlElement CreateDOMElementForDisplayMode(string displayMode)
        {
            string alt = string.IsNullOrEmpty(Properties.Alt) ?
                Properties.LabelText : Properties.Alt;
            string menuAlt = string.IsNullOrEmpty(Properties.MenuAlt) ?
                alt : Properties.MenuAlt;

            currentDisplayMode = displayMode;

            if (string.IsNullOrEmpty(alt))
                alt = "";
            if (string.IsNullOrEmpty(menuAlt))
                menuAlt = "";

            switch (displayMode)
            {
                case "Large":
                    _elmLarge = CreateTwoAnchorControlDOMElementCore(
                        this,
                        Root,
                        "Large",
                        Properties.Id,
                        Properties.Image32by32,
                        Properties.Image32by32Class,
                        Properties.Image32by32Top,
                        Properties.Image32by32Left,
                        Properties.Image16by16,
                        Properties.Image16by16Class,
                        Properties.Image16by16Top,
                        Properties.Image16by16Left,
                        Properties.LabelText,
                        Properties.Alt,
                        Properties.ToolTipTitle,
                        true);

                    AttachDOMElementsForDisplayMode("Large");
                    // Set up event handlers for top button
                    AttachEventsForDisplayMode(displayMode);
                    return _elmLarge;

                case "Medium":
                    _elmMedium = CreateTwoAnchorControlDOMElementCore(
                        this,
                        Root,
                        "Medium",
                        Properties.Id,
                        Properties.Image32by32,
                        Properties.Image32by32Class,
                        Properties.Image32by32Top,
                        Properties.Image32by32Left,
                        Properties.Image16by16,
                        Properties.Image16by16Class,
                        Properties.Image16by16Top,
                        Properties.Image16by16Left,
                        Properties.LabelText,
                        Properties.Alt,
                        Properties.ToolTipTitle,
                        true);

                    AttachDOMElementsForDisplayMode("Medium");
                    // Set up event handlers for top button
                    AttachEventsForDisplayMode(displayMode);
                    return _elmMedium;
                case "Small":
                    _elmSmall = CreateTwoAnchorControlDOMElementCore(
                        this,
                        Root,
                        "Small",
                        Properties.Id,
                        Properties.Image32by32,
                        Properties.Image32by32Class,
                        Properties.Image32by32Top,
                        Properties.Image32by32Left,
                        Properties.Image16by16,
                        Properties.Image16by16Class,
                        Properties.Image16by16Top,
                        Properties.Image16by16Left,
                        Properties.LabelText,
                        Properties.Alt,
                        Properties.ToolTipTitle,
                        true);

                    AttachDOMElementsForDisplayMode("Small");
                    // Set up event handlers for top button
                    AttachEventsForDisplayMode(displayMode);
                    return _elmSmall;
                default:
                    this.EnsureValidDisplayMode(displayMode);
                    return null;
            }
        }

        private string currentDisplayMode;
        internal override void AttachDOMElementsForDisplayMode(string displayMode)
        {
            Span elm = (Span)Browser.Document.GetById(Id + "-" + displayMode);
            StoreElementForDisplayMode(elm, displayMode);
            currentDisplayMode = displayMode;

            switch (displayMode)
            {
                case "Large":
                    {
                        if (!CUIUtility.IsNullOrUndefined(elm))
                            _elmLarge = elm;
                        _elmLargeButton = (Anchor)_elmLarge.ChildNodes[0];
                        _elmLargeImgCont = (Span)_elmLargeButton.ChildNodes[0].ChildNodes[0];
                        _elmLargeMenuButton = (Anchor)_elmLarge.ChildNodes[1];
                        HtmlElement elmLargeLabel = (HtmlElement)_elmLargeMenuButton.ChildNodes[0];

                        _elmLargeArrowImg = null;
                        // We might have a <br> and/or text nodes in here, so we have to search for the element we want
                        HtmlElementCollection candidates = elmLargeLabel.GetElementsByTagName("span");
                        foreach (HtmlElement elt in candidates)
                        {
                            string tag = elt.TagName;
                            if (CUIUtility.SafeString(tag) == "span")
                            {
                                _elmLargeArrowImg = (Image)elt.ChildNodes[0];
                                break;
                            }
                        }
                        break;
                    }
                case "Medium":
                    if (!CUIUtility.IsNullOrUndefined(elm))
                        _elmMedium = elm;
                    _elmMediumButton = (Anchor)_elmMedium.ChildNodes[0];
                    _elmMediumImgCont = (Span)_elmMediumButton.ChildNodes[0].ChildNodes[0];
                    _elmMediumMenuButton = (Anchor)_elmMedium.ChildNodes[1];
                    _elmMediumArrowImg = (Image)_elmMediumMenuButton.ChildNodes[1].ChildNodes[0];
                    break;
                case "Small":
                    if (!CUIUtility.IsNullOrUndefined(elm))
                        _elmSmall = elm;
                    _elmSmallButton = (Anchor)_elmSmall.ChildNodes[0];
                    _elmSmallImgCont = (Span)_elmSmallButton.ChildNodes[0].ChildNodes[0];
                    _elmSmallMenuButton = (Anchor)_elmSmall.ChildNodes[1];
                    _elmSmallArrowImg = (Image)_elmSmallMenuButton.ChildNodes[1].ChildNodes[0];
                    break;
            }
        }

        internal override void AttachEventsForDisplayMode(string displayMode)
        {
            AttachEvents(displayMode);
        }

        private void AttachEvents(string displayMode)
        {
            Anchor elmButton = null;
            Anchor elmMenuButton = null;
            switch (displayMode)
            {
                case "Large":
                    elmButton = _elmLargeButton;
                    elmMenuButton = _elmLargeMenuButton;
                    break;
                case "Medium":
                    elmButton = _elmMediumButton;
                    elmMenuButton = _elmMediumMenuButton;
                    break;
                case "Small":
                    elmButton = _elmSmallButton;
                    elmMenuButton = _elmSmallMenuButton;
                    break;
            }

            elmButton.Click += OnButtonClick;
            elmButton.DblClick += OnDblClick;
            elmButton.MouseOver += OnMouseOver;
            elmButton.MouseOut += OnMouseOut;
            elmButton.MouseOver += OnButtonFocus;
            elmButton.MouseOut += OnButtonBlur;
            elmButton.Focus += OnButtonKeyboardFocus;
            elmButton.Blur += OnButtonBlur;

            // Set up event handlers for menu button
            elmMenuButton.Click += OnMenuButtonClick;
            elmMenuButton.MouseOver += OnMenuButtonFocus;
            elmMenuButton.MouseOut += OnMenuButtonBlur;
            elmMenuButton.Focus += OnMenuButtonKeyboardFocus;
            elmMenuButton.Blur += OnMenuButtonABlur;
            elmMenuButton.KeyPress += OnMenuButtonKeyPress;
        }

        protected override void ReleaseEventHandlers()
        {
            if (!CUIUtility.IsNullOrUndefined(_elmLargeButton) &&
                    !CUIUtility.IsNullOrUndefined(_elmLargeMenuButton))
                RemoveEvents(_elmLargeButton, _elmLargeMenuButton);
            if (!CUIUtility.IsNullOrUndefined(_elmMediumButton) &&
                    !CUIUtility.IsNullOrUndefined(_elmMediumMenuButton))
                RemoveEvents(_elmMediumButton, _elmMediumMenuButton);
            if (!CUIUtility.IsNullOrUndefined(_elmSmallButton) &&
                    !CUIUtility.IsNullOrUndefined(_elmSmallMenuButton))
                RemoveEvents(_elmSmallButton, _elmSmallMenuButton);
        }

        private void RemoveEvents(HtmlElement elmButton, HtmlElement elmMenuButton)
        {
            elmButton.Click -= OnButtonClick;
            elmButton.DblClick -= OnDblClick;
            elmButton.MouseOver -= OnMouseOver;
            elmButton.MouseOut -= OnMouseOut;
            elmButton.MouseOver -= OnButtonFocus;
            elmButton.MouseOut -= OnButtonBlur;
            elmButton.Focus -= OnButtonKeyboardFocus;
            elmButton.Blur -= OnButtonBlur;

            // Set up event handlers for menu button
            elmMenuButton.Click -= OnMenuButtonClick;
            elmMenuButton.MouseOver -= OnMenuButtonFocus;
            elmMenuButton.MouseOut -= OnMenuButtonBlur;
            elmMenuButton.Focus -= OnMenuButtonKeyboardFocus;
            elmMenuButton.Blur -= OnMenuButtonABlur;
            elmMenuButton.KeyPress -= OnMenuButtonKeyPress;
        }

        bool focusOnArrow = false;
        internal override bool SetFocusOnControl()
        {
            if (!Enabled)
                return false;

            HtmlElement elm = DisplayedComponent.ElementInternal;
            if (!CUIUtility.IsNullOrUndefined(elm))
            {
                if (focusOnArrow || !_buttonEnabled)
                {
                    elm = (HtmlElement)elm.ChildNodes[1];
                }
                else
                {
                    elm = (HtmlElement)elm.ChildNodes[0];
                }

                elm.PerformFocus();
                return true;
            }
            return false;
        }

        public override void OnEnabledChanged(bool enabled)
        {
            OnEnabledChangedForControl(enabled);
            OnEnabledChangedForButton(enabled);
            OnEnabledChangedForMenu(enabled);
        }

        internal override string ControlType
        {
            get
            {
                return "SplitButton";
            }
        }

        protected void OnEnabledChangedForControl(bool enabled)
        {
            const string disabledClass = "ms-cui-disabled";

            // We can't use Utility.EnableElement because it only works for Anchors
            if (enabled)
            {
                Utility.RemoveCSSClassFromElement(_elmLarge, disabledClass);
                Utility.RemoveCSSClassFromElement(_elmMedium, disabledClass);
                Utility.RemoveCSSClassFromElement(_elmSmall, disabledClass);
            }
            else
            {
                Utility.EnsureCSSClassOnElement(_elmLarge, disabledClass);
                Utility.EnsureCSSClassOnElement(_elmMedium, disabledClass);
                Utility.EnsureCSSClassOnElement(_elmSmall, disabledClass);
            }
        }

        bool _menuButtonEnabled = true;
        protected void OnEnabledChangedForMenu(bool enabled)
        {
            Utility.SetEnabledOnElement(_elmLargeMenuButton, enabled);
            Utility.SetEnabledOnElement(_elmMediumMenuButton, enabled);
            Utility.SetEnabledOnElement(_elmSmallMenuButton, enabled);
            RemoveHighlightMenuButton();
            _menuButtonEnabled = enabled;
        }

        private void SetTextEnabled(bool enabled)
        {
            Utility.SetEnabledOnElement(_elmLargeMenuButton, enabled);
            Utility.SetEnabledOnElement(_elmMediumMenuButton, enabled);
            Utility.SetEnabledOnElement(_elmSmallMenuButton, enabled);
        }

        bool _buttonEnabled = false;
        protected void OnEnabledChangedForButton(bool enabled)
        {
            Utility.SetEnabledOnElement(_elmLargeButton, enabled);
            Utility.SetEnabledOnElement(_elmMediumButton, enabled);
            Utility.SetEnabledOnElement(_elmSmallButton, enabled);
            RemoveHighlightButton();
            _buttonEnabled = enabled;
        }

        protected void OnButtonClick(HtmlEvent evt)
        {
            Utility.CancelEventUtility(evt, false, true);

            CloseToolTip();
            if (!Enabled || !_buttonEnabled)
                return;

            Root.LastCommittedControl = this;
            focusOnArrow = false;

            Dictionary<string, string> dict = this.StateProperties;
            dict["CommandValueId"] = Properties.CommandValueId;
            DisplayedComponent.RaiseCommandEvent(Properties.Command,
                                                 CommandType.General,
                                                 dict);
        }

        protected override void OnDblClick(HtmlEvent evt)
        {
            Utility.CancelEventUtility(evt, false, true);

            CloseToolTip();
            if (!Enabled)
                return;

            OnButtonClick(evt);
        }

        protected void OnMouseOver(HtmlEvent args)
        {
            OnBeginFocus();

            if (!Enabled || !_buttonEnabled)
                return;

            if (string.IsNullOrEmpty(Properties.CommandPreview))
                return;

            Dictionary<string, string> dict = this.StateProperties;
            dict["CommandValueId"] = Properties.CommandValueId;
            DisplayedComponent.RaiseCommandEvent(Properties.CommandPreview,
                                                 CommandType.Preview,
                                                 dict);
        }

        protected void OnButtonKeyboardFocus(HtmlEvent args)
        {
            Root.LastFocusedControl = this;
            focusOnArrow = false;
            OnButtonFocus(args);
        }

        protected void OnButtonFocus(HtmlEvent args)
        {
            OnBeginFocus();
            if (!Enabled || !_buttonEnabled)
                return;

            HighlightButton();
        }

        protected void OnMouseOut(HtmlEvent args)
        {
            OnEndFocus();
            if (!Enabled)
                return;

            if (string.IsNullOrEmpty(Properties.CommandRevert))
                return;

            Dictionary<string, string> dict = this.StateProperties;
            dict["CommandValueId"] = Properties.CommandValueId;
            DisplayedComponent.RaiseCommandEvent(Properties.CommandRevert,
                                                 CommandType.PreviewRevert,
                                                 dict);
        }

        protected void OnButtonBlur(HtmlEvent args)
        {
            OnEndFocus();
            if (!Enabled || !_buttonEnabled)
                return;

            RemoveHighlightButton();
        }

        protected void OnMenuButtonClick(HtmlEvent args)
        {
            if (CUIUtility.IsNullOrUndefined(args))
                return;

            Utility.CancelEventUtility(args, false, true);
            CloseToolTip();
            if (!Enabled || !_menuButtonEnabled)
                return;

            Root.FixedPositioningEnabled = false;
            Root.LastCommittedControl = this;
            focusOnArrow = true;

            HtmlElement prev = args.TargetElement;
            LaunchMenu(prev);
            DisplayedComponent.RaiseCommandEvent(Properties.CommandMenuOpen, CommandType.MenuCreation, null);
        }

        protected void OnMenuButtonKeyboardFocus(HtmlEvent args)
        {
            OnMenuButtonFocus(args);
            focusOnArrow = true;
            Root.LastFocusedControl = this;
        }

        protected void OnMenuButtonFocus(HtmlEvent args)
        {
            OnBeginFocus();
            if (!Enabled || !_menuButtonEnabled)
                return;

            HighlightMenuButton();
        }

        protected void OnMenuButtonBlur(HtmlEvent args)
        {
            OnEndFocus();
            if (!Enabled || MenuLaunched || !_menuButtonEnabled)
                return;

            RemoveHighlightMenuButton();
        }

        protected void OnMenuButtonABlur(HtmlEvent args)
        {
            OnEndFocus();
            if (!Enabled || !_menuButtonEnabled)
                return;

            OnMenuButtonBlur(args);
        }

        protected void OnMenuButtonKeyPress(HtmlEvent args)
        {
            CloseToolTip();
            if (!Enabled || !_menuButtonEnabled)
                return;

            if (args.KeyCode == (int)Key.Enter)
                LaunchedByKeyboard = true;
        }

        protected override void OnLaunchedMenuClosed()
        {
            CloseToolTip();
            RemoveHighlight();
            DisplayedComponent.RaiseCommandEvent(Properties.CommandMenuClose,
                                                 CommandType.MenuClose,
                                                 null);

            base.OnLaunchedMenuClosed();
        }

        private void RemoveHighlight()
        {
            RemoveHighlightButton();
            RemoveHighlightMenuButton();
        }

        private void RemoveHighlightButton()
        {
            HtmlElement elm = GetDisplayedComponentElement();
            if (CUIUtility.IsNullOrUndefined(elm))
                return;

            Utility.RemoveCSSClassFromElement((HtmlElement)elm.ChildNodes[1], "ms-cui-ctl-split-hover");
            Utility.RemoveCSSClassFromElement((HtmlElement)elm.ChildNodes[0],
                                              "ms-cui-ctl-light-hoveredOver");
        }

        private void HighlightButton()
        {
            HtmlElement elm = GetDisplayedComponentElement();
            if (CUIUtility.IsNullOrUndefined(elm))
                return;

            Utility.EnsureCSSClassOnElement((HtmlElement)elm.ChildNodes[1], "ms-cui-ctl-split-hover");
            Utility.EnsureCSSClassOnElement((HtmlElement)elm.ChildNodes[0],
                                            "ms-cui-ctl-light-hoveredOver");
        }
        private void RemoveHighlightMenuButton()
        {
            HtmlElement elm = GetDisplayedComponentElement();
            if (CUIUtility.IsNullOrUndefined(elm))
                return;

            Utility.RemoveCSSClassFromElement((HtmlElement)elm.ChildNodes[0], "ms-cui-ctl-split-hover");
            Utility.RemoveCSSClassFromElement((HtmlElement)elm.ChildNodes[1],
                                              "ms-cui-ctl-light-hoveredOver");
        }
        private void HighlightMenuButton()
        {
            HtmlElement elm = GetDisplayedComponentElement();
            if (CUIUtility.IsNullOrUndefined(elm))
                return;

            Utility.EnsureCSSClassOnElement((HtmlElement)elm.ChildNodes[0], "ms-cui-ctl-split-hover");
            Utility.EnsureCSSClassOnElement((HtmlElement)elm.ChildNodes[1], "ms-cui-ctl-light-hoveredOver");
        }

        /// <summary>
        /// This control needs to poll for both the button and the menu button
        /// separately to see if they are enabled.
        /// </summary>
        internal override void PollForStateAndUpdate()
        {
            bool buttonEnabled = Root.PollForCommandState(Properties.Command,
                                                          null,
                                                          null);

            bool menuEnabled = true;

            if (!string.IsNullOrEmpty(Properties.CommandMenuOpen))
            {
                menuEnabled = Root.PollForCommandState(Properties.CommandMenuOpen,
                                                       null,
                                                       null);
            }
            else
            {
                // If this SplitButton does not have a command specified for the menu,
                // then the menu portion simply has the same enabled state as the button.
                menuEnabled = buttonEnabled;
            }
            
            // If any of the two parts changed their enabled state, then we need to
            // update the UI and the internal enabled member variable.
            if (menuEnabled != _menuButtonEnabled || buttonEnabled != _buttonEnabled)
            {
                // The control is enabled if either of the two parts are enabled
                // and the text also gets enabled etc.
                EnabledInternal = menuEnabled || buttonEnabled;

                // The control itself should only show full highlight effects when both
                // the menu and the top button are enabled. Otherwise, we just want the 
                // individual effects to show up.
                bool oldControlEnabled = _menuButtonEnabled && _buttonEnabled;
                bool controlEnabled = menuEnabled && buttonEnabled;
                if (oldControlEnabled != controlEnabled)
                    OnEnabledChangedForControl(controlEnabled);

                SetTextEnabled(EnabledInternal);

                // Update the two buttons depending on if their enabled states changed
                if (buttonEnabled != _buttonEnabled)
                    OnEnabledChangedForButton(buttonEnabled);
                if (menuEnabled != _menuButtonEnabled)
                    OnEnabledChangedForMenu(menuEnabled);

                // Store the changes to the two button states
                _menuButtonEnabled = menuEnabled;
                _buttonEnabled = buttonEnabled;
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            _elmLarge = null;
            _elmLargeImgCont = null;
            _elmLargeButton = null;
            _elmLargeMenuButton = null;
            _elmLargeArrowImg = null;

            _elmMedium = null;
            _elmMediumImgCont = null;
            _elmMediumButton = null;
            _elmMediumMenuButton = null;
            _elmMediumArrowImg = null;

            _elmSmall = null;
            _elmSmallImgCont = null;
            _elmSmallButton = null;
            _elmSmallMenuButton = null;
            _elmSmallArrowImg = null;
        }

        private SplitButtonProperties Properties
        {
            get
            {
                return (SplitButtonProperties)base.ControlProperties;
            }
        }
    }
}
