using System;
using Microsoft.LiveLabs.Html;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace Ribbon.Controls
{
    // TODO(josefl): Adri copied this control from ToggleButton early on.  I've removed most of the stuff
    // that does not make sense for TextBox.  We should make sure that the polling for state etc. works as expected.
    [Import(MemberNameCasing = Casing.Exact)]
    public class TextBoxProperties : ControlProperties
    {
        extern public TextBoxProperties();
        extern public string CommandPreview { get; }
        extern public string CommandRevert { get; }
        extern public string ImeEnabled { get; }
        extern public string QueryCommand { get; }
        extern public string MaxLength { get; }
        extern public string ShowAsLabel { get; }
        extern public string Width { get; }
    }

    public static class TextBoxCommandProperties
    {
        public const string Value = "Value";
    }

    /// <summary>
    /// A text box control.
    /// </summary>
    internal class TextBox : Control
    {
        public TextBox(Root root, string id, TextBoxProperties properties)
            : base(root, id, properties)
        {
            AddDisplayMode("Medium");
        }

        Input _elmDefaultInput = null;

        public string Value
        {
            get
            {
                EnsureInput();
                return _elmDefaultInput.Value;
            }
            set
            {
                EnsureInput();
                if (value != null)
                {
                    _elmDefaultInput.Value = value;
                }
                else
                {
                    _elmDefaultInput.Value = "";
                }
            }
        }

        private void EnsureInput()
        {
            if (this._elmDefaultInput == null)
            {
                this._elmDefaultInput = new Input();
                this._elmDefaultInput.Type = "text";
                Utility.SetImeMode(_elmDefaultInput, Properties.ImeEnabled);
            }
        }

        protected override HtmlElement CreateDOMElementForDisplayMode(string displayMode)
        {
            switch (displayMode)
            {
                case "Medium":

                    EnsureInput();
                    _elmDefaultInput.Id = this.Id;
                    _elmDefaultInput.SetAttribute("mscui:controltype", ControlType);
                    _elmDefaultInput.SetAttribute("role", AriaRole);
                    Utility.SetAriaTooltipProperties(Properties, _elmDefaultInput);

                    _elmDefaultInput.ClassName = "ms-cui-tb";

                    if (!string.IsNullOrEmpty(Properties.MaxLength))
                    {
                        double maxLength = Double.Parse(Properties.MaxLength);
                        if (Double.IsNaN(maxLength))
                        {
                            _elmDefaultInput.SetAttribute("maxlength", maxLength.ToString());
                        }
                    }

                    if (Utility.IsTrue(Properties.ShowAsLabel))
                    {
                        Utility.EnsureCSSClassOnElement(_elmDefaultInput, "ms-cui-tb-labelmode");
                        _elmDefaultInput.Disabled = true;
                    }

                    if (!string.IsNullOrEmpty(Properties.Width))
                        _elmDefaultInput.Style.Width = Properties.Width;

                    // Set up event handlers
                    AttachEvents();

                    return _elmDefaultInput;
                default:
                    EnsureValidDisplayMode(displayMode);
                    return null;
            }
        }

        internal override void AttachDOMElementsForDisplayMode(string displayMode)
        {
            Input elm = (Input)Browser.Document.GetById(Id);
            this.StoreElementForDisplayMode(elm, displayMode);

            switch (displayMode)
            {
                case "Medium":
                    _elmDefaultInput = elm;
                    break;
            }
        }

        internal override void AttachEventsForDisplayMode(string displayMode)
        {
            AttachEvents();
        }

        private void AttachEvents()
        {
            _elmDefaultInput.Change += OnChange;
            _elmDefaultInput.Focus += OnFocus;
            _elmDefaultInput.Blur += OnBlur;
            _elmDefaultInput.MouseUp += Utility.ReturnFalse;
            _elmDefaultInput.MouseOver += OnMouseover;
            _elmDefaultInput.MouseOut += OnMouseout;
            _elmDefaultInput.KeyPress += OnKeypress;
        }

        protected override void ReleaseEventHandlers()
        {
            _elmDefaultInput.Change -= OnChange;
            _elmDefaultInput.Focus -= OnFocus;
            _elmDefaultInput.Blur -= OnBlur;
            _elmDefaultInput.MouseUp -= Utility.ReturnFalse;
            _elmDefaultInput.MouseOver -= OnMouseover;
            _elmDefaultInput.MouseOut -= OnMouseout;
            _elmDefaultInput.KeyPress -= OnKeypress;
        }

        public override void OnEnabledChanged(bool enabled)
        {
            if (enabled)
            {
                Utility.EnableElement(_elmDefaultInput);
            }
            else
            {
                Utility.DisableElement(_elmDefaultInput);
            }
        }

        internal override string ControlType
        {
            get
            {
                return "TextBox";
            }
        }

        internal override string AriaRole
        {
            get
            {
                return "textbox";
            }
        }

        private void OnChange(HtmlEvent args)
        {
            CloseToolTip();
            if (!Enabled)
                return;

            // Send out the command
            StateProperties[TextBoxCommandProperties.Value] = this.Value;
            DisplayedComponent.RaiseCommandEvent(Properties.Command,
                                                 CommandType.General,
                                                 StateProperties);
            if (Root.PollForState)
                PollForStateAndUpdate();
            else
                SetState(null);
        }

        /// <summary>
        /// Force the TextBox to send out its state to the application
        /// </summary>
        internal override void CommitCurrentStateToApplication()
        {
            // Passing null here works becuase currently OnChange() does not use 
            // the DomEvent object.  If it ever needs to then this needs to be
            // refactored.
            OnChange(null);
        }

        private void SetState(string value)
        {
            // Set the UI of the button to reflect its on/off state
            if (!CUIUtility.IsNullOrUndefined(_elmDefaultInput))
            {
                if (!string.IsNullOrEmpty(value))
                    this.Value = value;
            }
        }

        internal override bool SetFocusOnControl()
        {
            if (!Enabled)
                return false;

            _elmDefaultInput.PerformFocus();
            return true;
        }

        private void OnFocus(HtmlEvent args)
        {
            OnBeginFocus();
            if (!Enabled)
                return;

            _elmDefaultInput.PerformSelect();
            Root.LastFocusedControl = this;
        }

        private void OnBlur(HtmlEvent args)
        {
            OnEndFocus();
            if (!Enabled)
                return;
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

        private void OnKeypress(HtmlEvent evt)
        {
            if (!CUIUtility.IsNullOrUndefined(evt))
            {
                if (evt.KeyCode == (int)Key.Enter)
                {
                    OnChange(evt);
                    Utility.CancelEventUtility(evt, false, true);
                }
            }
        }

        internal override void PollForStateAndUpdate()
        {
            bool succeeded = PollForStateAndUpdateInternal(Properties.Command,
                                                           Properties.QueryCommand,
                                                           StateProperties,
                                                           false);
            if (succeeded)
            {
                SetState((string)StateProperties[TextBoxCommandProperties.Value]);
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            _elmDefaultInput = null;
        }

        private TextBoxProperties Properties
        {
            get
            {
                return (TextBoxProperties)base.ControlProperties;
            }
        }
    }
}
