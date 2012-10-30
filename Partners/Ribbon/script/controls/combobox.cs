using System;
using System.Collections.Generic;
using Microsoft.LiveLabs.Html;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace Ribbon.Controls
{
    [Import(MemberNameCasing = Casing.Exact)]
    public class ComboBoxProperties : DropDownProperties
    {
        extern public ComboBoxProperties();
        extern public string AllowFreeForm { get; }
        extern public string AutoComplete { get; set; }
        extern public string AutoCompleteDelay { get; }
        extern public string ImeEnabled { get; }
    }

    public static class ComboBoxCommandProperties
    {
        public static string SelectedItemId = "SelectedItemId";
        public static string IsFreeForm = "IsFreeForm";
        public static string Value = "Value";
    }

    /// <summary>
    /// A class that displays a combo box in the Ribbon.
    /// </summary>
    internal class ComboBox : DropDown
    {
        public ComboBox(Root root, string id, ComboBoxProperties properties, Menu menu)
            : base(root, id, properties, menu)
        {
            if (string.IsNullOrEmpty(CBProperties.AllowFreeForm))
                _allowFreeForm = false;
            else
                _allowFreeForm = CBProperties.AllowFreeForm.ToLower() == "true";
            if (string.IsNullOrEmpty(CBProperties.AutoComplete))
                CBProperties.AutoComplete = "true";

            if (!string.IsNullOrEmpty(CBProperties.AutoCompleteDelay))
            {
                try
                {
                    _autoCompleteDelay = Int32.Parse(CBProperties.AutoCompleteDelay);
                }
                catch
                {
                    // ParseInt failed, so use default
                    _autoCompleteDelay = DEFAULT_AUTOCOMPLETE_DELAY;
                }
            }
        }

        // DOM Element variables
        Span _elmMedium;
        Input _elmMediumInput;
        Anchor _elmMediumBtnA;
        Image _elmMediumArwImg;
        Span _elmMediumArwImgCont;

        const int DEFAULT_AUTOCOMPLETE_DELAY = 100;
        bool _allowFreeForm = false;
        int _autoCompleteDelay = DEFAULT_AUTOCOMPLETE_DELAY;

        protected override HtmlElement CreateDOMElementForDisplayMode(string displayMode)
        {
            switch (displayMode)
            {
                case "Medium":
                    string alt = CUIUtility.SafeString(CBProperties.Alt);
                    string altArrow = CUIUtility.SafeString(CBProperties.AltArrow);

                    // Top Level Element
                    _elmMedium = new Span();
                    _elmMedium.ClassName = "ms-cui-cb";
                    _elmMedium.SetAttribute("mscui:controltype", ControlType);

                    // Input Element
                    _elmMediumInput = new Input();
                    _elmMediumInput.SetAttribute("name", CBProperties.Command);
                    _elmMediumInput.Type = "text";
                    _elmMediumInput.Style.Width = CBProperties.Width;
                    _elmMediumInput.ClassName = "ms-cui-cb-input";
                    _elmMediumInput.SetAttribute("autocomplete", "off");
                    _elmMediumInput.Id = CBProperties.Id;
                    if (string.IsNullOrEmpty(Properties.ToolTipTitle))
                        _elmMediumInput.Title = alt;
                    Utility.SetImeMode(_elmMediumInput, ((ComboBoxProperties)Properties).ImeEnabled);

                    string itemId = (string)StateProperties[ComboBoxCommandProperties.SelectedItemId];
                    if (string.IsNullOrEmpty(itemId))
                        itemId = CBProperties.InitialItem;
                    if (!string.IsNullOrEmpty(itemId))
                        SelectMenuItemById(itemId);

                    Utility.SetAriaTooltipProperties(Properties, _elmMediumInput);

                    // Arrow Button Element
                    _elmMediumBtnA = new Anchor();
                    Utility.EnsureCSSClassOnElement(_elmMediumBtnA, "ms-cui-dd-arrow-button");
                    Utility.NoOpLink(_elmMediumBtnA);

                    _elmMediumBtnA.TabIndex = -1; // Only the input box should be tab-able
                    _elmMediumBtnA.SetAttribute("aria-haspopup", "true");

                    _elmMediumArwImg = new Image();

                    _elmMediumArwImgCont = Utility.CreateClusteredImageContainerNew(
                                                                            ImgContainerSize.Size5by3,
                                                                            Root.Properties.ImageDownArrow,
                                                                            Root.Properties.ImageDownArrowClass,
                                                                            _elmMediumArwImg,
                                                                            true,
                                                                            false,
                                                                            Root.Properties.ImageDownArrowTop,
                                                                            Root.Properties.ImageDownArrowLeft
                                                                            );

                    if (string.IsNullOrEmpty(Properties.ToolTipTitle))
                    {
                        _elmMediumArwImg.Alt = altArrow;
                        _elmMediumBtnA.Title = altArrow;
                    }

                    // Set up event handlers for the Drop Down Arrow Button
                    AttachEventsForDisplayMode("Medium");

                    // Build DOM Structure
                    _elmMedium.AppendChild(_elmMediumInput);
                    _elmMedium.AppendChild(_elmMediumBtnA);

                    _elmMediumBtnA.AppendChild(_elmMediumArwImgCont);
                    return _elmMedium;
                default:
                    EnsureValidDisplayMode(displayMode);
                    return null;
            }
        }

        internal override void AttachDOMElementsForDisplayMode(string displayMode)
        {
            Span elm = (Span)Browser.Document.GetById(Id + "-" + displayMode);
            StoreElementForDisplayMode(elm, displayMode);

            // If we are attaching, we clear the initial item since this is dynamically
            // generated from a DOM element from a subitem.  This will cause the new
            // DOM element to be generated the first time that this is queried.
            StateProperties[DropDownCommandProperties.SelectedItemId] = "";

            // Only do hookup for non-menu display modes for now
            switch (displayMode)
            {
                case "Medium":
                    _elmMedium = elm;
                    _elmMediumInput = (Input)_elmMedium.ChildNodes[0];
                    _elmMediumBtnA = (Anchor)_elmMedium.ChildNodes[1];
                    _elmMediumArwImgCont = (Span)_elmMediumBtnA.ChildNodes[0];
                    _elmMediumArwImg = (Image)_elmMediumArwImgCont.ChildNodes[0];

                    Utility.SetUnselectable(_elmMediumInput, false, false);
                    break;
            }
        }

        internal override void AttachEventsForDisplayMode(string displayMode)
        {

            switch (displayMode)
            {
                case "Medium":
                    AttachEvents();
                    break;
            }
        }

        private void AttachEvents()
        {
            _elmMediumInput.Focus += OnInputFocus;
            _elmMediumInput.Blur += OnInputBlur;
            _elmMediumInput.KeyDown += OnInputKeyDown;
            _elmMediumInput.MouseUp += Utility.ReturnFalse;
            _elmMediumBtnA.MouseOver += OnArrowButtonFocus;
            _elmMediumBtnA.MouseOut += OnArrowButtonBlur;
            _elmMediumBtnA.Click += OnArrowButtonClick;
            _elmMediumBtnA.Focus += OnArrowButtonFocus;
            _elmMediumBtnA.Blur += OnArrowButtonBlur;
            _elmMediumBtnA.KeyPress += OnArrowButtonKeyPress;
        }

        protected override void ReleaseEventHandlers()
        {
            _elmMediumInput.Focus -= OnInputFocus;
            _elmMediumInput.Blur -= OnInputBlur;
            _elmMediumInput.KeyDown -= OnInputKeyDown;
            _elmMediumInput.MouseUp -= Utility.ReturnFalse;
            _elmMediumBtnA.MouseOver -= OnArrowButtonFocus;
            _elmMediumBtnA.MouseOut -= OnArrowButtonBlur;
            _elmMediumBtnA.Click -= OnArrowButtonClick;
            _elmMediumBtnA.Focus -= OnArrowButtonFocus;
            _elmMediumBtnA.Blur -= OnArrowButtonBlur;
            _elmMediumBtnA.KeyPress -= OnArrowButtonKeyPress;
        }

        public override void OnEnabledChanged(bool enabled)
        {
            if (enabled)
            {
                Utility.EnableElement(_elmMediumInput);
                Utility.EnableElement(_elmMedium);
            }
            else
            {
                Utility.DisableElement(_elmMediumInput);
                Utility.DisableElement(_elmMedium);
            }

            base.OnEnabledChanged(enabled);
        }

        internal override string ControlType
        {
            get
            {
                return "ComboBox";
            }
        }

        internal override string AriaRole
        {
            get
            {
                return "combobox";
            }
        }

        Dictionary<string, string> _menuItems;
        /// <summary>
        /// A list of MenuItems' TextValues under this combo box
        /// </summary>
        public Dictionary<string, string> MenuItems
        {
            get
            {
                if (CUIUtility.IsNullOrUndefined(_menuItems))
                {
                    _menuItems = new Dictionary<string, string>();
                }
                return _menuItems;
            }
            set
            {
                _menuItems = value;
            }
        }

        private string GetMenuItem(string idx)
        {
            return _menuItems.ContainsKey(idx) ? _menuItems[idx] : string.Empty;
        }

        protected string[] GetFirstMenuItemThatBeginsWith(string beg)
        {
            foreach (string key in MenuItems.Keys)
            {
                if (key.ToLower().StartsWith(beg.ToLower()))
                    return new string[2] { key, MenuItems[key] };
            }
            return new string[2] { string.Empty, string.Empty };
        }

        string _pendingMenuItemId;
        int _pendingAutoCompleteTimeoutId = -1;
        protected void OnInputKeyDown(HtmlEvent args)
        {
            ClearPendingAutoComplete();

            string currentValue = _elmMediumInput.Value;

            CloseToolTip();

            // Make OACR happy
            if (args != null)
            {
                if (args.KeyCode == (int)Key.Down && args.AltKey)
                {
                    LaunchMenuInternal();
                    return;
                }
            }

            if (string.IsNullOrEmpty(currentValue))
                return;

            // Make OACR happy
            if (args != null)
            {
                int key = args.KeyCode;
                switch (key)
                {
                    case 13 /* Enter */:
                        // If enter is pressed, validate and save
                        ClearPendingAutoComplete();
                        Utility.CancelEventUtility(args, false, true);
                        ValidateAndSave(); // O14:380210 - don't blur the textbox on enter, leave focus inside
                        return;

                    case 27 /* Esc */:
                        ClearPendingAutoComplete();
                        // O14:401158 - don't clear selection if the menu is open on escape
                        if (!MenuLaunched)
                            ResetToPreviousValue();
                        return;

                    case 38 /* Up */:
                        // TODO: go to previous menu item
                        break;

                    case 40 /* Down */:
                        if (args.AltKey)
                        {
                            LaunchMenuInternal();
                            return;
                        }
                        else
                        {
                            // TODO: go to next menu item
                        }
                        break;

                    case 8 /* Backspace */:
                    case 127 /* Del */:
                    case 46 /* Delete */:
                        ClearPendingAutoComplete();
                        _pendingMenuItemId = null;
                        return;

                    case 36 /* Home */:
                    case 35 /* End */:
                    case 33 /* Page Up */:
                    case 34 /* Page Down */:
                    case 37 /* Left */:
                    case 39 /* Right */:
                    case 16 /* Shift */:
                    case 18 /* Alt */:
                    case 17 /* Ctrl */:
                    case 20 /* Caps Lock */:
                        return;
                }
            }

            if (Utility.IsTrue(CBProperties.AutoComplete))
            {
                ClearPendingAutoComplete();
                _pendingAutoCompleteTimeoutId = Browser.Window.SetTimeout(new Action(ExecuteAutoComplete), _autoCompleteDelay);
            }
        }

        protected void ExecuteAutoComplete()
        {
            _pendingAutoCompleteTimeoutId = -1;

            string currentValue = _elmMediumInput.Value;

            string[] bestGuess = GetFirstMenuItemThatBeginsWith(currentValue);
            if (string.IsNullOrEmpty(bestGuess[0]) && string.IsNullOrEmpty(bestGuess[1]))
            {
                _pendingMenuItemId = "";
                return;
            }

            _elmMediumInput.Value = bestGuess[0];

            if (BrowserUtility.InternetExplorer)
            {
                TextRange tr = _elmMediumInput.CreateTextRange();
                tr.MoveStart("character", currentValue.Length);
                tr.MoveEnd("character", _elmMediumInput.Value.Length);
                tr.PerformSelect();
            }
            else
            {
                _elmMediumInput.SetSelectionRange(currentValue.Length, _elmMediumInput.Value.Length);
            }

            _pendingMenuItemId = bestGuess[1];
        }

        protected void ClearPendingAutoComplete()
        {
            if (_pendingAutoCompleteTimeoutId != -1)
            {
                Browser.Window.ClearTimeout(_pendingAutoCompleteTimeoutId);
            }

            _pendingAutoCompleteTimeoutId = -1;
        }

        internal override bool SetFocusOnControl()
        {
            if (!Enabled)
                return false;

            if (!CUIUtility.IsNullOrUndefined(_elmMediumInput))
            {
                _elmMediumInput.PerformFocus();
                return true;
            }
            return false;
        }

        protected void OnInputFocus(HtmlEvent args)
        {
            Root.LastFocusedControl = this;
            OnArrowButtonFocus(args);

            // Dynamically populate the menu if necessary
            if (Utility.IsTrue(CBProperties.PopulateDynamically))
                PollForDynamicMenu(false);

            _elmMediumInput.PerformSelect();

            if (!CUIUtility.IsNullOrUndefined(Menu))
                Menu.RefreshInternal();
        }

        protected void OnInputBlur(HtmlEvent args)
        {
            // O14:574004 - we need to clear the pending autocomplete on blur
            // if the user left, they don't need autocomplete
            ClearPendingAutoComplete();
            OnArrowButtonBlur(args);
        }

        private bool IsFreeForm
        {
            get
            {
                return Utility.IsTrue(StateProperties[ComboBoxCommandProperties.IsFreeForm]);
            }
            set
            {
                StateProperties[ComboBoxCommandProperties.IsFreeForm] = value.ToString();
            }
        }

        protected void ValidateAndSave()
        {
            Dictionary<string, string> commandDict = new Dictionary<string, string>();

            // If value is not a valid menu item
            if (!SelectMenuItemById(_pendingMenuItemId))
            {
                ControlComponent comp = DisplayedComponent;
                if (!Utility.IsTrue(CBProperties.AutoComplete))
                {
                    string menuitemid = GetMenuItem(_elmMediumInput.Value);
                    if (!string.IsNullOrEmpty(menuitemid) 
                        && SelectMenuItemById(menuitemid))
                    {
                        IsFreeForm = false;
                        commandDict["IsFreeForm"] = "false";
                        commandDict["CommandValueId"] = _selectedControl.GetCommandValueId();
                        comp.RaiseCommandEvent(CBProperties.Command, CommandType.OptionSelection, commandDict);
                        return;
                    }
                }
                // If autocomplete was off, but we haven't returned yet, then the value was not a valid item in the menu
                // If free-form entry is allowed, send the value to the PageManager
                if (_allowFreeForm)
                {
                    IsFreeForm = true;
                    commandDict["IsFreeForm"] = "true";
                    commandDict["Value"] = _elmMediumInput.Value;
                    StateProperties[ComboBoxCommandProperties.Value] = _elmMediumInput.Value;
                    comp.RaiseCommandEvent(CBProperties.Command, CommandType.OptionSelection, commandDict);
                }
                // If free-form entry is not allowed, reset to the last valid value and stop
                else
                {
                    ResetToPreviousValue();
                    return;
                }
            }
            // If the value is a valid menu item, send it as a CommandValueId to the PageManager
            else
            {
                IsFreeForm = false;
                commandDict["IsFreeForm"] = "false";
                commandDict["CommandValueId"] = _selectedControl.GetCommandValueId();
                DisplayedComponent.RaiseCommandEvent(CBProperties.Command, CommandType.OptionSelection, commandDict);
            }
        }

        protected void ResetToPreviousValue()
        {
            if (!CUIUtility.IsNullOrUndefined(_selectedControl))
            {
                _elmMediumInput.Value = ((IMenuItem)_selectedControl).GetTextValue();
            }
            else
            {
                _elmMediumInput.Value = "";
            }
        }

        protected override void SelectMenuItem(ISelectableControl isc)
        {
            if (_selectedControl == isc)
                return;

            _selectedControl = isc;
            StateProperties[ComboBoxCommandProperties.SelectedItemId] = isc.GetMenuItemId();

            IMenuItem imi = (IMenuItem)(Control)isc;
            _elmMediumInput.Value = imi.GetTextValue();
        }

        internal override void PollForStateAndUpdate()
        {
            string currentSelectedItemId = (string)StateProperties[ComboBoxCommandProperties.SelectedItemId];

            // If we currently don't have an item set, then we set it here to let the application know
            // since this is what we'll fall back to unless they set something else.
            if (string.IsNullOrEmpty(currentSelectedItemId))
                StateProperties[ComboBoxCommandProperties.SelectedItemId] = CBProperties.InitialItem;

            bool succeeded = PollForStateAndUpdateInternal(CBProperties.Command,
                                                           CBProperties.QueryCommand,
                                                           StateProperties,
                                                           false);

            string newSelectedItemId = StateProperties[ComboBoxCommandProperties.SelectedItemId];

            if (succeeded)
            {
                if (IsFreeForm)
                {
                    _elmMediumInput.Value = StateProperties[ComboBoxCommandProperties.Value];
                    StateProperties[ComboBoxCommandProperties.SelectedItemId] = "";
                }
                else
                {
                    // If the currently selected item is null, use the display text
                    if (!string.IsNullOrEmpty(currentSelectedItemId))
                    {
                        // If the selected item id has changed or if one has never been selected
                        // then we want to select it.
                        if (currentSelectedItemId != newSelectedItemId || !_itemEverSelected)
                        {
                            if (!SelectMenuItemById(newSelectedItemId))
                            {
                                throw new InvalidOperationException("The menu item id requested via polling does not exist");
                            }
                        }
                    }
                    else
                    {
                        string valueKey = ComboBoxCommandProperties.Value;
                        string medValue = StateProperties.ContainsKey(valueKey) ? StateProperties[valueKey] : "";
                        if (!string.IsNullOrEmpty(medValue))
                        {
                            _elmMediumInput.Value = StateProperties[ComboBoxCommandProperties.Value];
                        }
                    }
                }
            }
        }

        protected override void OnDynamicMenuPopulated()
        {
            // If we are dynamically populating the menu, we should build the lookup
            // table for autocomplete now
            ControlComponent cc = null;
            Control c = null;
            ISelectableControl isc = null;
            IMenuItem imi = null;

            string menuitemid = "";
            string labeltext = "";
            if (CUIUtility.IsNullOrUndefined(Menu))
                return;

            foreach (MenuSection ms in Menu.Children)
            {
                foreach (Component comp in ms.Children)
                {
                    // Only check if this is a MenuItem
                    // Ignore Galleries and GroupPopouts
                    if (comp is MenuItem)
                    {
                        cc = (ControlComponent)comp;
                        c = cc.Control;

                        // Get MenuItemId
                        if (c is ISelectableControl)
                        {
                            isc = (ISelectableControl)c;
                            menuitemid = isc.GetMenuItemId();
                        }
                        // Get Label Text
                        if (c is IMenuItem)
                        {
                            imi = (IMenuItem)c;
                            labeltext = imi.GetTextValue();
                        }

                        // If we have both MenuItemId and Label Text, add the item to the table
                        if (!(string.IsNullOrEmpty(menuitemid) || string.IsNullOrEmpty(labeltext)))
                        {
                            MenuItems[labeltext] = menuitemid;
                            labeltext = "";
                            menuitemid = "";
                        }
                    }
                }
            }
        }

        protected void LaunchMenuInternal()
        {
            if (LaunchMenu(_elmMediumInput, new Action(SendMenuCreationCommandEvent)))
            {
                SendMenuCreationCommandEvent();
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            _elmMedium = null;
            _elmMediumArwImg = null;
            _elmMediumArwImgCont = null;
            _elmMediumBtnA = null;
            _elmMediumInput = null;
        }

        // Replaces DropDown.Properties
        // Workaround: ScriptSharp does not support using 'new' here
        // so we just change the name of the property instead
        protected ComboBoxProperties CBProperties
        {
            get
            {
                return (ComboBoxProperties)base.ControlProperties;
            }
        }
    }
}
