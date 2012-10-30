using System;
using Microsoft.LiveLabs.Html;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace Ribbon.Controls
{
    [Import(MemberNameCasing = Casing.Exact)]
    public class SpinnerProperties : ControlProperties
    {
        extern public SpinnerProperties();
        extern public string AccelerationInterval { get; }
        extern public string AltDownArrow { get; }
        extern public string AltUpArrow { get; }
        extern public string DefaultUnit { get; }
        extern public string DefaultValue { get; }
        extern public string ImeEnabled { get; }
        extern public string MultiplierInterval { get; }
        extern public string QueryCommand { get; }
    }

    public static class SpinnerCommandProperties
    {
        public const string ChangedByMouse = "ChangedByMouse";
        public const string ChangeType = "ChangeType";
        public const string Value = "Value";
        public const string Unit = "Unit";
    }

    /// <summary>
    /// A class representing the spinner control.
    /// </summary>
    internal class Spinner : Control
    {
        // TODO(jkern): integrate this with new SpinnerProperties etc.
        public Spinner(Root root, string id, SpinnerProperties properties, Unit[] validUnits)
            : base(root, id, properties)
        {
            AddDisplayMode("Medium");

            _accIterations = 0;
            _multiplier = 1;
            _validUnits = validUnits;
            _value = Double.Parse(Properties.DefaultValue);
            StateProperties[SpinnerCommandProperties.Value] = _value.ToString();
            _currentUnit = FindUnitByNameOrAbbreviation(Properties.DefaultUnit);

            if (CUIUtility.IsNullOrUndefined(_currentUnit))
                throw new ArgumentOutOfRangeException("The default unit is not in the list of valid units");
            StateProperties[SpinnerCommandProperties.Unit] = _currentUnit.Name;
        }

        Span _elmDefault;
        Input _elmDefaultInput;
        Span _elmDefaultArwBox;
        Anchor _elmDefaultUpArw;
        Anchor _elmDefaultDownArw;
        Image _upArwImg;
        Span _upArwImgCont;
        Image _downArwImg;
        Span _downArwImgCont;
        Unit _currentUnit;
        Unit[] _validUnits;

        double _value;
        bool _valueHadCommaSeparator;
        long _accIterations;
        int _accInterval;
        int _multInterval;
        int _multiplier;
        int _intID;
        int _timID;
        string _cmdChanged;
        bool _changedByMouse;

        // Constants
        const int INC = 1;
        const int DEC = -1;

        protected override HtmlElement CreateDOMElementForDisplayMode(string displayMode)
        {
            switch (displayMode)
            {
                case "Medium":
                    _elmDefault = new Span();
                    _elmDefault.ClassName = "ms-cui-spn";
                    _elmDefault.SetAttribute("mscui:controltype", ControlType);

                    _elmDefaultInput = new Input();
                    _elmDefaultInput.ClassName = "ms-cui-spn-txtbx";
                    _elmDefaultInput.Id = Id;
                    _elmDefaultInput.SetAttribute("role", "textbox");

                    Utility.SetAriaTooltipProperties(Properties, _elmDefaultInput);
                    Utility.SetImeMode(_elmDefaultInput, Properties.ImeEnabled);

                    _elmDefaultArwBox = new Span();
                    _elmDefaultArwBox.ClassName = "ms-cui-spn-arwbx";

                    _elmDefaultUpArw = new Anchor();
                    _elmDefaultUpArw.ClassName = "ms-cui-spn-btnup";
                    _elmDefaultUpArw.SetAttribute("role", "spinbutton");

                    _elmDefaultDownArw = new Anchor();
                    _elmDefaultDownArw.ClassName = "ms-cui-spn-btndown";
                    _elmDefaultDownArw.SetAttribute("role", "spinbutton");

                    _upArwImg = new Image();
                    _upArwImg.Alt = "";
                    _upArwImgCont = Utility.CreateClusteredImageContainerNew(
                                                                    ImgContainerSize.Size5by3,
                                                                    Root.Properties.ImageUpArrow,
                                                                    Root.Properties.ImageUpArrowClass,
                                                                    _upArwImg,
                                                                    true,
                                                                    false,
                                                                    Root.Properties.ImageUpArrowTop,
                                                                    Root.Properties.ImageUpArrowLeft);
                                                                    
                    
                    Utility.EnsureCSSClassOnElement(_upArwImgCont, "ms-cui-spn-imgcnt");
                    _elmDefaultUpArw.Title = CUIUtility.SafeString(Properties.AltUpArrow);

                    _downArwImg = new Image();
                    _downArwImg.Alt = "";
                    _downArwImgCont = Utility.CreateClusteredImageContainerNew(
                                                                      ImgContainerSize.Size5by3,
                                                                      Root.Properties.ImageDownArrow,
                                                                      Root.Properties.ImageDownArrowClass,
                                                                      _downArwImg,
                                                                      true,
                                                                      false,
                                                                      Root.Properties.ImageDownArrowTop,
                                                                      Root.Properties.ImageDownArrowLeft);

                    Utility.EnsureCSSClassOnElement(_downArwImgCont, "ms-cui-spn-imgcnt");
                    _elmDefaultDownArw.Title = CUIUtility.SafeString(Properties.AltDownArrow);

                    // Set up event handlers
                    AttachEventsForDisplayMode(displayMode);

                    // Build DOM Structure
                    _elmDefault.AppendChild(_elmDefaultInput);
                    _elmDefault.AppendChild(_elmDefaultArwBox);
                    _elmDefaultArwBox.AppendChild(_elmDefaultUpArw);
                    _elmDefaultArwBox.AppendChild(_elmDefaultDownArw);
                    _elmDefaultUpArw.AppendChild(_upArwImgCont);
                    _elmDefaultDownArw.AppendChild(_downArwImgCont);

                    return _elmDefault;
                default:
                    EnsureValidDisplayMode(displayMode);
                    return null;
            }
        }

        private void SetDefaultValues()
        {
            // Setup default values
            SetValueInternal(Double.Parse(Properties.DefaultValue));
            _accInterval = Int32.Parse(Properties.AccelerationInterval);
            _multInterval = Int32.Parse(Properties.MultiplierInterval);
            _cmdChanged = Properties.Command;
        }


        internal override void AttachDOMElementsForDisplayMode(string displayMode)
        {
            Span elm = (Span)Browser.Document.GetById(Id + "-" + displayMode);
            StoreElementForDisplayMode(elm, displayMode);

            switch (displayMode)
            {
                case "Medium":
                    _elmDefault = elm;
                    _elmDefaultInput = (Input)_elmDefault.ChildNodes[0];
                    _elmDefaultArwBox = (Span)_elmDefault.ChildNodes[1];
                    _elmDefaultUpArw = (Anchor)_elmDefaultArwBox.ChildNodes[0];
                    _elmDefaultDownArw = (Anchor)_elmDefaultArwBox.ChildNodes[1];
                    _upArwImgCont = (Span)_elmDefaultUpArw.ChildNodes[0];
                    _downArwImgCont = (Span)_elmDefaultDownArw.ChildNodes[0];
                    _upArwImg = (Image)_upArwImgCont.ChildNodes[0];
                    _downArwImg = (Image)_downArwImgCont.ChildNodes[0];
                    break;
            }
        }

        internal override void AttachEventsForDisplayMode(string displayMode)
        {
            // Right now there is no hookup for menus because they are not server rendered
            switch (displayMode)
            {
                case "Medium":
                    AttachEvents();
                    SetDefaultValues();
                    break;
            }
        }

        private void AttachEvents()
        {
            // Set up event handlers
            _elmDefaultInput.Focus += OnFocus;
            _elmDefaultInput.Blur += OnBlur;
            _elmDefaultInput.Change += OnChanged;
            _elmDefaultInput.MouseOver += OnHover;
            _elmDefaultInput.MouseOut += OnUnHover;
            _elmDefaultInput.KeyPress += OnKeypress;
            _elmDefaultInput.KeyDown += OnKeydown;
            _elmDefaultInput.KeyUp += OnKeyup;
            _elmDefaultUpArw.MouseOver += OnHoverUp;
            _elmDefaultUpArw.MouseOut += OnUnHoverUp;
            _elmDefaultUpArw.MouseDown += OnMouseDownUp;
            _elmDefaultUpArw.MouseUp += OnMouseUpUp;
            _elmDefaultDownArw.MouseOver += OnHoverDown;
            _elmDefaultDownArw.MouseOut += OnUnHoverDown;
            _elmDefaultDownArw.MouseDown += OnMouseDownDown;
            _elmDefaultDownArw.MouseUp += OnMouseUpDown;
        }

        protected override void ReleaseEventHandlers()
        {
            // Clean up event handlers
            _elmDefaultInput.Focus -= OnFocus;
            _elmDefaultInput.Blur -= OnBlur;
            _elmDefaultInput.Change -= OnChanged;
            _elmDefaultInput.MouseOver -= OnHover;
            _elmDefaultInput.MouseOut -= OnUnHover;
            _elmDefaultInput.KeyPress -= OnKeypress;
            _elmDefaultInput.KeyDown -= OnKeydown;
            _elmDefaultInput.KeyUp -= OnKeyup;
            _elmDefaultUpArw.MouseOver -= OnHoverUp;
            _elmDefaultUpArw.MouseOut -= OnUnHoverUp;
            _elmDefaultUpArw.MouseDown -= OnMouseDownUp;
            _elmDefaultUpArw.MouseUp -= OnMouseUpUp;
            _elmDefaultDownArw.MouseOver -= OnHoverDown;
            _elmDefaultDownArw.MouseOut -= OnUnHoverDown;
            _elmDefaultDownArw.MouseDown -= OnMouseDownDown;
            _elmDefaultDownArw.MouseUp -= OnMouseUpDown;
        }

        internal override string ControlType
        {
            get
            {
                return "Spinner";
            }
        }

        /// <summary>
        /// Create a new Unit object.
        /// </summary>
        /// <param name="name">The full name of the Unit (used to set the default unit for an Spinner).</param>
        /// <param name="abbreviations">An array of abbreviations to accept for this Unit.</param>
        /// <param name="min">The minimum value allowed.</param>
        /// <param name="max">The maximum value allowed.</param>
        /// <param name="decimaldigits">The maximum amount of decimal digits allowed.</param>
        /// <param name="step">The step interval when one of the arrow buttons on an Spinner is pressed.</param>
        /// <returns>A new Unit object with the specified properties.</returns>
        public static Unit CreateUnit(string name, string[] abbreviations, double min, double max, int decimaldigits, double step)
        {
            return new Unit(name, abbreviations, min, max, decimaldigits, step);
        }

        #region Events
        public override void OnEnabledChanged(bool enabled)
        {
            Utility.SetEnabledOnElement(_elmDefaultInput, enabled);
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

            Root.LastFocusedControl = this;
            LockHover(args);
            _elmDefaultInput.PerformSelect();
        }

        private void OnBlur(HtmlEvent args)
        {
            OnEndFocus();
            if (!Enabled)
                return;

            // If a keyboard-spin is in progress, stop it
            if (_keySpin)
                StopSpin();

            LockHover(args);
        }

        bool _locked = false;
        private void LockHover(HtmlEvent args)
        {
            _locked = !_locked;
            if (_locked)
                OnHover(args);
            else
                OnUnHover(args);
        }

        private void OnChanged(HtmlEvent args)
        {
            CloseToolTip();

            if (!Enabled)
                return;

            if (!ValidateInput())
            {
                ResetToPreviousValue();
                return;
            }

            CommandType ct = CommandType.General;
            StateProperties[SpinnerCommandProperties.ChangeType] = "manual";
            StateProperties[SpinnerCommandProperties.ChangedByMouse] = false.ToString();
            DisplayedComponent.RaiseCommandEvent(_cmdChanged,
                                                    ct,
                                                    StateProperties);
        }

        /// <summary>
        /// Force the Spinner to send out its state to the application
        /// </summary>
        internal override void CommitCurrentStateToApplication()
        {
            // Passing null here works becuase currently OnChanged() does not use 
            // the DomEvent object.  If it ever needs to then this needs to be 
            // refactored.
            OnChanged(null);
        }


        private void OnHover(HtmlEvent args)
        {
            if (!Enabled)
                return;

            Utility.EnsureCSSClassOnElement(_elmDefaultInput, "ms-cui-spn-txtbx-hover");
            Utility.EnsureCSSClassOnElement(_elmDefaultUpArw, "ms-cui-spn-btnup-ctl-hover");
            Utility.EnsureCSSClassOnElement(_elmDefaultDownArw, "ms-cui-spn-btndown-ctl-hover");
        }

        private void OnUnHover(HtmlEvent args)
        {
            if (!Enabled)
                return;

            if (!_locked)
            {
                Utility.RemoveCSSClassFromElement(_elmDefaultInput, "ms-cui-spn-txtbx-hover");
                Utility.RemoveCSSClassFromElement(_elmDefaultUpArw, "ms-cui-spn-btnup-ctl-hover");
                Utility.RemoveCSSClassFromElement(_elmDefaultDownArw, "ms-cui-spn-btndown-ctl-hover");
                Utility.RemoveCSSClassFromElement(_elmDefaultUpArw, "ms-cui-spn-btnup-down");
                Utility.RemoveCSSClassFromElement(_elmDefaultDownArw, "ms-cui-spn-btndown-down");
            }
        }

        private void OnHoverUp(HtmlEvent args)
        {
            if (!Enabled)
                return;

            OnHover(args);
            Utility.RemoveCSSClassFromElement(_elmDefaultUpArw, "ms-cui-spn-btnup-ctl-hover");
            Utility.EnsureCSSClassOnElement(_elmDefaultUpArw, "ms-cui-spn-btnup-hover");
        }

        private void OnUnHoverUp(HtmlEvent args)
        {
            if (!Enabled)
                return;

            if (!_keySpin)
            {
                // Stop spin if needed
                OnMouseUpUp(args);
            }

            OnUnHover(args);
            if (_locked)
                Utility.EnsureCSSClassOnElement(_elmDefaultUpArw, "ms-cui-spn-btnup-ctl-hover");

            Utility.RemoveCSSClassFromElement(_elmDefaultUpArw, "ms-cui-spn-btnup-hover");
        }

        /// <summary>
        /// When mouse is clicked on up button
        /// </summary>
        private void OnMouseDownUp(HtmlEvent args)
        {
            if (!Enabled)
                return;

            _changedByMouse = true;
            if (args.Button != (int)MouseButton.LeftButton)
            {
                StopSpin();
                return;
            }

            StartSpin(INC);
            Utility.EnsureCSSClassOnElement(_elmDefaultUpArw, "ms-cui-spn-btnup-down");
        }

        /// <summary>
        /// When mouse stops holding on the up button
        /// </summary>
        private void OnMouseUpUp(HtmlEvent args)
        {
            if (!Enabled)
                return;

            StopSpin();
            Root.LastFocusedControl = this;
            Utility.RemoveCSSClassFromElement(_elmDefaultUpArw, "ms-cui-spn-btnup-down");
        }

        private void Increase(int mult)
        {
            if (!Enabled)
                return;

            if (!ValidateAndSave(_currentUnit, _value + mult * _currentUnit.Step))
            {
                // If multiplier prevents value from reaching max, force to max
                if (_value < _currentUnit.Max)
                    ValidateAndSave(_currentUnit, _currentUnit.Max);
                else
                {
                    ResetToPreviousValue();
                    return;
                }
            }

            CommandType ct = CommandType.IgnoredByMenu;
            StateProperties[SpinnerCommandProperties.ChangeType] = "increase";
            StateProperties[SpinnerCommandProperties.ChangedByMouse] = _changedByMouse.ToString();
            DisplayedComponent.RaiseCommandEvent(_cmdChanged,
                                                    ct,
                                                    StateProperties);
        }

        private void OnHoverDown(HtmlEvent args)
        {
            if (!Enabled)
                return;

            OnHover(args);
            Utility.RemoveCSSClassFromElement(_elmDefaultDownArw, "ms-cui-spn-btndown-ctl-hover");
            Utility.EnsureCSSClassOnElement(_elmDefaultDownArw, "ms-cui-spn-btndown-hover");
        }

        private void OnUnHoverDown(HtmlEvent args)
        {
            if (!Enabled)
                return;

            if (!_keySpin)
            {
                // Stop spin if needed
                OnMouseUpDown(args);
            }

            OnUnHover(args);
            if (_locked)
                Utility.EnsureCSSClassOnElement(_elmDefaultDownArw, "ms-cui-spn-btndown-ctl-hover");
            Utility.RemoveCSSClassFromElement(_elmDefaultDownArw, "ms-cui-spn-btndown-hover");
        }

        /// <summary>
        /// When mouse clicks on down button
        /// </summary>
        private void OnMouseDownDown(HtmlEvent args)
        {
            if (!Enabled)
                return;

            _changedByMouse = true;
            if (args.Button != (int)MouseButton.LeftButton)
            {
                StopSpin();
                return;
            }

            StartSpin(DEC);
            Utility.EnsureCSSClassOnElement(_elmDefaultDownArw, "ms-cui-spn-btndown-down");
        }

        /// <summary>
        /// When mouse stops clicking on down button
        /// </summary>
        private void OnMouseUpDown(HtmlEvent args)
        {
            if (!Enabled)
                return;

            StopSpin();
            Root.LastFocusedControl = this;
            Utility.RemoveCSSClassFromElement(_elmDefaultDownArw, "ms-cui-spn-btndown-down");
        }

        private void Decrease(int mult)
        {
            if (!Enabled)
                return;

            if (!ValidateAndSave(_currentUnit, _value - mult * _currentUnit.Step))
            {
                // If multiplier prevents value from reaching min, force to min
                if (_value > _currentUnit.Min)
                    ValidateAndSave(_currentUnit, _currentUnit.Min);
                else
                {
                    ResetToPreviousValue();
                    return;
                }
            }

            CommandType ct = CommandType.IgnoredByMenu;
            StateProperties[SpinnerCommandProperties.ChangeType] = "decrease";
            StateProperties[SpinnerCommandProperties.ChangedByMouse] = _changedByMouse.ToString();
            DisplayedComponent.RaiseCommandEvent(_cmdChanged,
                                                    ct,
                                                    StateProperties);
        }

        private void OnKeypress(HtmlEvent evt)
        {
            if (!Enabled)
                return;

            _changedByMouse = false;
            int key = evt.KeyCode;
            if (key == (int)Key.Esc)
                ResetToPreviousValue();
            else if (key == (int)Key.Enter)
            {
                OnChanged(evt);
                Utility.CancelEventUtility(evt, false, true);
            }
        }

        bool _keySpin = false;
        private void OnKeydown(HtmlEvent args)
        {
            if (!Enabled)
                return;
            if (_keySpin)
                return;

            _changedByMouse = false;
            int key = args.KeyCode;
            if (key == (int)Key.Up)
            {
                StartSpin(INC);
                Utility.EnsureCSSClassOnElement(_elmDefaultUpArw, "ms-cui-spn-btnup-down");
            }
            else if (key == (int)Key.Down)
            {
                StartSpin(DEC);
                Utility.EnsureCSSClassOnElement(_elmDefaultDownArw, "ms-cui-spn-btndown-down");
            }
            else
                return;

            _keySpin = true;
        }

        private void OnKeyup(HtmlEvent args)
        {
            if (!Enabled)
                return;

            if (!_keySpin)
                return;

            // Stop spinning on any keyup (same as client)
            StopSpin();

            Utility.RemoveCSSClassFromElement(_elmDefaultUpArw, "ms-cui-spn-btnup-down");
            Utility.RemoveCSSClassFromElement(_elmDefaultDownArw, "ms-cui-spn-btndown-down");

            _keySpin = false;
        }
        #endregion

        #region Acceleration
        private void AccelerationIncrease()
        {
            _accIterations++;
            Increase(GetAccelerationFactor());
        }

        private void AccelerationDecrease()
        {
            _accIterations++;
            Decrease(GetAccelerationFactor());
        }

        private void StartSpin(int direction)
        {
            // If a spin is already in progress, don't start a new one.
            if (_intID > -1 || _timID > -1)
                return;

            // Do a single step first
            if (direction == INC)
                Increase(1);
            else
                Decrease(1);

            // Wait 0.5 sec before starting accelerated spin
            if (direction == INC)
                _timID = Browser.Window.SetTimeout(new Action(StartIncreaseSpin), 500);
            else
                _timID = Browser.Window.SetTimeout(new Action(StartDecreaseSpin), 500);
        }

        private void StopSpin()
        {
            if (_timID > -1)
            {
                Browser.Window.ClearTimeout(_timID);
                _timID = -1;
            }
            if (_intID > -1)
            {
                Browser.Window.ClearInterval(_intID);
                _intID = -1;
                _multiplier = 1;
                _accIterations = 0;
            }
        }

        private void StartIncreaseSpin()
        {
            if (_intID != -1)  // if spin is currently active, don't start a new one
                return;

            _intID = Browser.Window.SetInterval(new Action(AccelerationIncrease), _accInterval);
        }

        private void StartDecreaseSpin()
        {
            if (_intID != -1)  // if spin is currently active, don't start a new one
                return;
            _intID = Browser.Window.SetInterval(new Action(AccelerationDecrease), _accInterval);
        }

        private int GetAccelerationFactor()
        {
            long elapsed = _accIterations * _accInterval;
            if (elapsed >= _multiplier * _multInterval && _multiplier <= 3)
                _multiplier++;

            return _multiplier;
        }
        #endregion

        #region Validation
        private void SetValueInternal(double val)
        {
            _value = val;
            _elmDefaultInput.Value = FixInputDisplay(val, _currentUnit.DefaultAbbreviation);
            StateProperties[SpinnerCommandProperties.Value] = _value.ToString();
        }

        /// <summary>
        /// Property containing the current numeric value of the control
        /// </summary>
        public double Value
        {
            get
            {
                return _value;
            }
            set
            {
                if (!ValidateAndSave(_currentUnit, value))
                    throw new ArgumentOutOfRangeException("Invalid value");
            }
        }

        /// <summary>
        /// Property containing the current unit of the control
        /// </summary>
        public string UnitString
        {
            get
            {
                return _currentUnit.DefaultAbbreviation;
            }
            set
            {
                if (!ValidateInputInternal(_value.ToString() + value))
                {
                    ResetToPreviousValue();
                    throw new ArgumentOutOfRangeException("Invalid unit");
                }
            }
        }

        private bool _isDecimalSeparatorCommaInited = false;
        private bool _isDecimalSeparatorComma = false;
        /// <summary>
        /// Whether the decimal separator in this locale is a comma or not
        /// </summary>
        private bool IsDecimalSeparatorComma
        {
            get
            {
                if (!_isDecimalSeparatorCommaInited)
                {
                    double n = 1.1;
                    string str = n.ToString().Substring(1, 1);
                    _isDecimalSeparatorComma = str == ",";
                    _isDecimalSeparatorCommaInited = true;
                }

                return _isDecimalSeparatorComma;
            }
        }

        // Call this method if both the unit and value may change
        private bool ValidateInput()
        {
            return true;

            /*
            string input = _elmDefaultInput.Value;
            return ValidateInputInternal(input);
            */
        }

        private bool ValidateInputInternal(string input)
        {
            /*
            string pat = @"(\-)?[0-9]*[\.,]?[0-9]+";
            Regex rgx = new Regex(pat);
            input = input.Trim();
            Match m = rgx.Match(input);
            if (!m.Success)       // no value entered, only a unit (ex. "in")
                return false;

            string novalue = input.Replace(m.ToString(), "");   // remove value part from input
            string valueString = input.Replace(novalue, "");    // remove unit part from input
            string unit = novalue.Trim();
            */ 
            
            string unit = "";
            string valueString = input;

            Unit u;
            double value;
            bool valueHasCommaSeparator = false;

            // Javascript does not support comma as a decimal separator, so we must strip it
            // before processing and then re-add it when we put the value into the textbox
            if (IsDecimalSeparatorComma)
            {
                // If there is a period, it is a thousands-separator which we can throw out
                valueString = valueString.Replace(".", "");

                if (valueString.IndexOf(',') > -1)
                {
                    valueHasCommaSeparator = true;
                    valueString = valueString.Replace(",", ".");
                }
            }
            else
            {
                // If there is a comma, it is a thousands-separator which we can throw out
                valueString = valueString.Replace(",", "");
            }
            value = Double.Parse(valueString);

            if (!string.IsNullOrEmpty(unit))   // if there is a unit to parse, do it
            {
                if (_currentUnit.ContainsAbbreviation(unit))
                    u = _currentUnit;
                else
                    u = FindUnit(unit);
            }
            else   // if there is no unit, use the existing one
            {
                u = _currentUnit;
            }

            return ValidateAndSave(u, value, valueHasCommaSeparator);
        }

        private bool ValidateAndSave(Unit u, double value)
        {
            return ValidateAndSave(u, value, IsDecimalSeparatorComma);
        }

        // Call this method directly if unit is not changing
        private bool ValidateAndSave(Unit u, double value, bool valueHadCommaSeparator)
        {
            if (u == null) // invalid unit entered
                return false;
            int resCode = u.ValidateNumber(value);
            if (resCode == Unit.INVALID) // invalid number entered
                return false;
            else if (resCode == Unit.NEEDSROUNDING) // too many decimal places
                value = FixValueDecimal(value, u);
            else if (resCode == Unit.BELOWMIN) // value less than minimum
                value = u.Min;
            else if (resCode == Unit.ABOVEMAX) // value greater than maximum
                value = u.Max;
            
            // if resCode == Unit.VALID, number is fine
            _elmDefaultInput.Value = FixInputDisplay(value, u.DefaultAbbreviation, valueHadCommaSeparator);

            _currentUnit = u;
            _value = value;
            _valueHadCommaSeparator = valueHadCommaSeparator;
            StateProperties[SpinnerCommandProperties.Unit] = u.Name;
            StateProperties[SpinnerCommandProperties.Value] = value.ToString();
            return true;
        }

        private void ResetToPreviousValue()
        {
            _elmDefaultInput.Value = FixInputDisplay(_value, _currentUnit.DefaultAbbreviation, _valueHadCommaSeparator);
        }

        private string FixInputDisplay(double value, string unit)
        {
            return FixInputDisplay(value, unit, IsDecimalSeparatorComma);
        }

        private string FixInputDisplay(double value, string unit, bool valueHadCommaSeparator)
        {
            string valueString = value.ToString();
            if (valueHadCommaSeparator)
                valueString = valueString.Replace(".", ",");
            return valueString + " " + unit;
        }

        static private double FixValueDecimal(double value, Unit unit)
        {
            double mult = Math.Pow(10.0, unit.DecimalDigits);
            return Math.Round(value * mult) / mult;
        }

        private Unit FindUnit(string abbrv)
        {
            for (int i = 0; i < _validUnits.Length; i++)
            {
                if (_validUnits[i].ContainsAbbreviation(abbrv))
                    return _validUnits[i];
            }
            return null;
        }

        private Unit FindUnitByNameOrAbbreviation(string name)
        {
            for (int i = 0; i < _validUnits.Length; i++)
            {
                if (_validUnits[i].Name == name)
                    return _validUnits[i];
                if (_validUnits[i].ContainsAbbreviation(name))
                    return _validUnits[i];
            }
            return null;
        }
        #endregion

        #region Polling
        internal override void PollForStateAndUpdate()
        {
            // Fix for bug O14:93091 - polling occurs when the spinner DOM element is not
            // yet created, so a JS error happens when calling ValidateAndSave()
            if (CUIUtility.IsNullOrUndefined(_elmDefault))
                return;

            bool succeeded = PollForStateAndUpdateInternal(Properties.Command,
                                                           Properties.QueryCommand,
                                                           StateProperties,
                                                           true);

            Unit u = FindUnitByNameOrAbbreviation((string)StateProperties[SpinnerCommandProperties.Unit]);
            if (!ValidateAndSave(u, Double.Parse(StateProperties[SpinnerCommandProperties.Value])))
                throw new ArgumentOutOfRangeException("Invalid valid and/or unit returned when polling");

        }
        #endregion

        public override void Dispose()
        {
            base.Dispose();
            _downArwImg = null;
            _downArwImgCont = null;
            _upArwImg = null;
            _upArwImgCont = null;
            _elmDefault = null;
            _elmDefaultArwBox = null;
            _elmDefaultDownArw = null;
            _elmDefaultInput = null;
            _elmDefaultUpArw = null;
        }

        private SpinnerProperties Properties
        {
            get
            {
                return (SpinnerProperties)base.ControlProperties;
            }
        }
    }
}
