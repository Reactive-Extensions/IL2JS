using Microsoft.LiveLabs.Html;
using Microsoft.LiveLabs.JavaScript.Interop;

using MSLabel = Microsoft.LiveLabs.Html.Label;

namespace Ribbon.Controls
{
    [Import(MemberNameCasing = Casing.Exact)]
    internal class CheckBoxProperties : BooleanControlProperties
    {
        // Inherits properties from BooleanControlProperties
        extern public CheckBoxProperties();
    }

    public static class CheckBoxCommandProperties
    {
        public const string On = "On";
        public const string CommandValueId = "CommandValueId";
    }

    /// <summary>
    /// A class representing a check box with an optional attached label
    /// </summary>
    internal class CheckBox : ToggleButton
    {
        public CheckBox(Root root, string id, CheckBoxProperties properties)
            : base(root, id, properties)
        {
        }

        // Display elements
        Span _elmSmall;
        Input _elmSmallCheckboxInput;

        Span _elmMedium;
        Input _elmMediumCheckboxInput;
        MSLabel _elmMediumLabel;

        protected override HtmlElement CreateDOMElementForDisplayMode(string displayMode)
        {
            string alt = string.IsNullOrEmpty(Properties.Alt) ?
                        Properties.LabelText : Properties.Alt;
            alt = CUIUtility.SafeString(alt);

            switch (displayMode)
            {
                case "Small":
                    // Create DOM elements
                    _elmSmall = new Span();
                    _elmSmall.ClassName = "ms-cui-cbx";
                    _elmSmall.SetAttribute("mscui:controltype", ControlType);

                    _elmSmallCheckboxInput = new Input();
                    _elmSmallCheckboxInput.Type = "checkbox";
                    _elmSmallCheckboxInput.ClassName = "ms-cui-cbx-input";
                    _elmSmallCheckboxInput.Id = _id + "-Small-checkbox";
                    if (string.IsNullOrEmpty(Properties.ToolTipTitle))
                    {
                        _elmSmallCheckboxInput.Title = alt;
                    }

                    _elmSmallCheckboxInput.SetAttribute("role", AriaRole);
                    Utility.SetAriaTooltipProperties(Properties, _elmSmallCheckboxInput);

                    // Set up event handlers
                    AttachEvents(_elmSmallCheckboxInput, null);

                    //Build DOM Structure
                    _elmSmall.AppendChild(_elmSmallCheckboxInput);

                    return _elmSmall;
                case "Medium":
                    // Create DOM elements
                    _elmMedium = new Span();
                    _elmMedium.ClassName = "ms-cui-cbx";
                    _elmMedium.SetAttribute("mscui:controltype", ControlType);

                    _elmMediumCheckboxInput = new Input();
                    _elmMediumCheckboxInput.Type = "checkbox";
                    _elmMediumCheckboxInput.ClassName = "ms-cui-cbx-input";
                    _elmMediumCheckboxInput.Id = _id + "-Medium-checkbox";
                    if (string.IsNullOrEmpty(Properties.ToolTipTitle))
                    {
                        _elmMediumCheckboxInput.Title = alt;

                    }
                    _elmMediumCheckboxInput.SetAttribute("role", AriaRole);
                    Utility.SetAriaTooltipProperties(Properties, _elmMediumCheckboxInput);

                    bool hasLabel = false;
                    if (!string.IsNullOrEmpty(Properties.LabelText))
                    {
                        _elmMediumLabel = new MSLabel();
                        if (BrowserUtility.InternetExplorer7)
                        {
                            _elmMediumLabel.SetAttribute("htmlFor", _id + "-Medium-checkbox");
                        }
                        else
                        {
                            _elmMediumLabel.SetAttribute("for", _id + "-Medium-checkbox");
                        }
                        UIUtility.SetInnerText(_elmMediumLabel, Properties.LabelText);
                        hasLabel = true;
                    }

                    // Set up event handlers
                    AttachEvents(_elmMediumCheckboxInput, _elmMediumLabel);

                    // Build DOM Structure
                    _elmMedium.AppendChild(_elmMediumCheckboxInput);
                    if (hasLabel)
                        _elmMedium.AppendChild(_elmMediumLabel);

                    return _elmMedium;
                default:
                    EnsureValidDisplayMode(displayMode);
                    return null;
            }
        }

        internal override void AttachDOMElementsForDisplayMode(string displayMode)
        {
            Span elm = (Span)Browser.Document.GetById(Id + "-" + displayMode);
            this.StoreElementForDisplayMode(elm, displayMode);

            // Only do hookup for non-menu display modes for now
            switch (displayMode)
            {
                case "Medium":
                    _elmMedium = elm;
                    _elmMediumCheckboxInput = (Input)elm.FirstChild;
                    _elmMediumLabel = (MSLabel)elm.ChildNodes[1];
                    break;
                case "Small":
                    _elmSmall = elm;
                    _elmSmallCheckboxInput = (Input)elm.FirstChild;
                    break;
            }
        }

        internal override void AttachEventsForDisplayMode(string displayMode)
        {
            switch (displayMode)
            {
                case "Medium":
                    AttachEvents(_elmMediumCheckboxInput, _elmMediumLabel);
                    break;
                case "Small":
                    AttachEvents(_elmSmallCheckboxInput, null);
                    break;
            }
        }

        private void AttachEvents(HtmlElement elmCheckbox, HtmlElement elmLabel)
        {
            elmCheckbox.Click += OnClick;
            elmCheckbox.Focus += OnFocus;
            elmCheckbox.Blur += OnBlur;
            elmCheckbox.MouseOver += OnMouseover;
            elmCheckbox.MouseOut += OnMouseout;
            elmCheckbox.KeyDown += OnKeyDown;

            if (!CUIUtility.IsNullOrUndefined(elmLabel))
            {
                elmLabel.Click += OnLabelClick;
                elmLabel.KeyDown += OnKeyDown;
                elmLabel.MouseOver += OnMouseover;
                elmLabel.MouseOut += OnMouseout;
            }
        }

        protected override void ReleaseEventHandlers()
        {
            if (!CUIUtility.IsNullOrUndefined(_elmMediumCheckboxInput))
                RemoveEvents(_elmMediumCheckboxInput, _elmMediumLabel);
            if (!CUIUtility.IsNullOrUndefined(_elmSmallCheckboxInput))
                RemoveEvents(_elmSmallCheckboxInput, null);
        }

        private void RemoveEvents(HtmlElement elmCheckbox, HtmlElement elmLabel)
        {
            elmCheckbox.Click -= OnClick;
            elmCheckbox.Focus -= OnFocus;
            elmCheckbox.Blur -= OnBlur;
            elmCheckbox.MouseOver -= OnMouseover;
            elmCheckbox.MouseOut -= OnMouseout;
            elmCheckbox.KeyDown -= OnKeyDown;

            if (!CUIUtility.IsNullOrUndefined(elmLabel))
            {
                elmLabel.Click -= OnLabelClick;
                elmLabel.KeyDown -= OnKeyDown;
                elmLabel.MouseOver -= OnMouseover;
                elmLabel.MouseOut -= OnMouseout;
            }
        }

        public override void OnEnabledChanged(bool enabled)
        {
            if (enabled)
            {
                Utility.EnableElement(_elmSmall);
                Utility.EnableElement(_elmMedium);
            }
            else
            {
                Utility.DisableElement(_elmSmall);
                Utility.DisableElement(_elmMedium);
            }

            Utility.SetDisabledAttribute(_elmSmallCheckboxInput, !enabled);
            Utility.SetDisabledAttribute(_elmMediumCheckboxInput, !enabled);

        }

        internal override string ControlType
        {
            get
            {
                return "CheckBox";
            }
        }

        internal override string AriaRole
        {
            get
            {
                return "checkbox";
            }
        }

        #region Event Handlers
        protected override void OnClick(HtmlEvent args)
        {
            // evt.PreventDefault() will prevent checkbox state from being toggled
            CloseToolTip();
            if (!Enabled)
                return;

            CommandType ct = CommandType.IgnoredByMenu;
            ControlComponent comp = DisplayedComponent;

            // Choose appropriate check box
            switch (comp.DisplayMode)
            {
                case "Small":
                    StateProperties[CheckBoxCommandProperties.On] = _elmSmallCheckboxInput.Checked.ToString();
                    break;
                case "Medium":
                    StateProperties[CheckBoxCommandProperties.On] = _elmMediumCheckboxInput.Checked.ToString();
                    break;
                default:
                    EnsureValidDisplayMode(comp.DisplayMode);
                    return;
            }

            // Send command
            comp.RaiseCommandEvent(Properties.Command,
                                                 ct,
                                                 StateProperties);
            if (Root.PollForState)
                PollForStateAndUpdate();
            else
                SetState(Utility.IsTrue(StateProperties[CheckBoxCommandProperties.On]));
        }

        private void OnFocus(HtmlEvent args)
        {
            OnBeginFocus();
            if (!Enabled)
                return;

            Root.LastFocusedControl = this;
            if (string.IsNullOrEmpty(Properties.CommandPreview))
                return;

            DisplayedComponent.RaiseCommandEvent(Properties.CommandPreview,
                                                 CommandType.Preview,
                                                 StateProperties);
        }

        private void OnKeyDown(HtmlEvent args)
        {
            if (!CUIUtility.IsNullOrUndefined(args))
            {
                if ((Key)args.KeyCode == Key.Enter)
                    OnLabelClick(args);
            }
        }

        private void OnMouseover(HtmlEvent args)
        {
            OnBeginFocus();
            if (!Enabled)
                return;

            if (string.IsNullOrEmpty(Properties.CommandPreview))
                return;

            DisplayedComponent.RaiseCommandEvent(Properties.CommandPreview,
                                                 CommandType.Preview,
                                                 StateProperties);
        }

        private void OnBlur(HtmlEvent args)
        {
            OnEndFocus();
            if (!Enabled)
                return;

            if (string.IsNullOrEmpty(Properties.CommandRevert))
                return;

            DisplayedComponent.RaiseCommandEvent(Properties.CommandRevert,
                                                 CommandType.PreviewRevert,
                                                 StateProperties);
        }

        private void OnMouseout(HtmlEvent args)
        {
            OnEndFocus();
            if (!Enabled)
                return;

            if (string.IsNullOrEmpty(Properties.CommandRevert))
                return;

            DisplayedComponent.RaiseCommandEvent(Properties.CommandRevert,
                                                 CommandType.PreviewRevert,
                                                 StateProperties);
        }

        private void OnLabelClick(HtmlEvent args)
        {
            Utility.CancelEventUtility(args, false, true);
            CloseToolTip();
            if (!Enabled)
                return;

            Root.LastFocusedControl = this;

            // Toggle the checkbox and send the command
            SetState(!_elmMediumCheckboxInput.Checked);
            OnClick(args);
        }
        #endregion

        #region Polling & State
        protected override void SetState(bool on)
        {
            // Set the UI of the button to reflect its on/off state
            if (!CUIUtility.IsNullOrUndefined(_elmSmallCheckboxInput))
                _elmSmallCheckboxInput.Checked = on;
            if (!CUIUtility.IsNullOrUndefined(_elmMediumCheckboxInput))
                _elmMediumCheckboxInput.Checked = on;
        }

        internal override void PollForStateAndUpdate()
        {
            PollForStateAndUpdateInternal(Properties.Command,
                                          Properties.QueryCommand,
                                          StateProperties,
                                          true);

            SetState(Utility.IsTrue(StateProperties[CheckBoxCommandProperties.On]));
        }
        #endregion

        protected override void AddDisplayModes()
        {
            AddDisplayMode("Small");
            AddDisplayMode("Medium");
        }

        internal override bool SetFocusOnControl()
        {
            if (!Enabled)
                return false;

            HtmlElement elm = DisplayedComponent.ElementInternal;
            ((HtmlElement)elm.FirstChild).PerformFocus();
            return true;
        }

        public override void Dispose()
        {
            base.Dispose();
            _elmMedium = null;
            _elmMediumCheckboxInput = null;
            _elmMediumLabel = null;
            _elmSmall = null;
            _elmSmallCheckboxInput = null;
        }

        private CheckBoxProperties Properties
        {
            get
            {
                return (CheckBoxProperties)base.ControlProperties;
            }
        }
    }
}
