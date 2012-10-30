using System;
using Microsoft.LiveLabs.Html;
using Microsoft.LiveLabs.JavaScript.Interop;

using MSLabel = Microsoft.LiveLabs.Html.Label;

namespace Ribbon.Controls
{
    [Import(MemberNameCasing = Casing.Exact)]
    public class DropDownProperties : MenuLauncherControlProperties
    {
        extern public DropDownProperties();
        extern public string Alt { get; }
        extern public string AltArrow { get; }
        extern public string CommandPreview { get; }
        extern public string CommandRevert { get; }
        extern public string InitialItem { get; }
        extern public string QueryCommand { get; }
        extern public string Width { get; }
        extern public string SelectedItemDisplayMode { get; }
    }

    /// <summary>
    /// The properties that can be set via polling on a DropDown-based control
    /// </summary>
    public static class DropDownCommandProperties
    {
        public static string SelectedItemId = "SelectedItemId";
        public static string Value = "Value";
    }

    /// <summary>
    /// A class that displays a dropdown in the Ribbon.
    /// The DropDown Control takes the following parameters:
    /// OpenMenuCmd - the id of the Command that is issued when the drop down is opened
    /// BtnImg - Url to the down arrow that drops down the menu
    /// SelCmd - the id of the Command that is issued when a value is chosen for this combo box
    /// Width - The width of the text box(in pixels) that is part of this combo box
    /// InitialItem - the MenuItemId of the item that is chosen on load
    /// </summary>
    internal class DropDown : MenuLauncher
    {
        public DropDown(Root root, string id, DropDownProperties properties, Menu menu)
            : base(root, id, properties, menu)
        {
            AddDisplayModes();
            StateProperties[DropDownCommandProperties.Value] = "";
            StateProperties[DropDownCommandProperties.SelectedItemId] = "";
        }

        Span _elmMedium;
        Span _elmMediumSelectedItem;
        Anchor _elmMediumBtnA;
        Image _elmMediumArwImg;
        Span _elmMediumArwImgCont;

        internal override bool SetFocusOnControl()
        {
            if (!Enabled)
                return false;

            _elmMediumBtnA.PerformFocus();
            return true;
        }

        protected override HtmlElement CreateDOMElementForDisplayMode(string displayMode)
        {
            string alt = CUIUtility.SafeString(Properties.Alt);
            string altArrow = CUIUtility.SafeString(Properties.AltArrow);
            bool needsLabel = true;
            if (string.IsNullOrEmpty(altArrow) && !string.IsNullOrEmpty(Properties.ToolTipTitle))
                altArrow = Properties.ToolTipTitle;

            MSLabel elmHiddenLabel;
            switch (displayMode)
            {
                case "Text": // Remove once project's ribbon doesn't use it anymore
                case "Medium":
                    // Top Level Element
                    _elmMedium = new Span();
                    _elmMedium.ClassName = "ms-cui-dd";
                    _elmMedium.SetAttribute("mscui:controltype", ControlType);

                    // Selected Item Element
                    _elmMediumSelectedItem = new Span();
                    _elmMediumSelectedItem.ClassName = "ms-cui-dd-text";
                    _elmMediumSelectedItem.Style.Width = Properties.Width;

                    string dictKey = DropDownCommandProperties.SelectedItemId;
                    string itemId = StateProperties.ContainsKey(dictKey) ? StateProperties[dictKey] : "";
                    if (string.IsNullOrEmpty(itemId))
                        itemId = Properties.InitialItem;
                    if (!string.IsNullOrEmpty(itemId))
                        SelectMenuItemById(itemId);

                    // Arrow Button Element
                    _elmMediumBtnA = new Anchor();
                    _elmMediumBtnA.SetAttribute("role", AriaRole);
                    _elmMediumBtnA.SetAttribute("aria-haspopup", "true");
                    Utility.EnsureCSSClassOnElement(_elmMediumBtnA, "ms-cui-dd-arrow-button");
                    Utility.SetAriaTooltipProperties(Properties, _elmMediumBtnA);
                    Utility.NoOpLink(_elmMediumBtnA);
                    _elmMediumBtnA.Id = Id;
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
                        _elmMediumBtnA.SetAttribute("title", altArrow);
                        _elmMediumSelectedItem.Title = alt;
                        _elmMediumArwImg.Alt = altArrow;
                        needsLabel = false;
                    }

                    // Set up event handlers for the Drop Down Arrow Button
                    AttachEventsForDisplayMode("Medium");

                    // Build DOM Structure
                    _elmMedium.AppendChild(_elmMediumSelectedItem);
                    _elmMedium.AppendChild(_elmMediumBtnA);
                    _elmMediumBtnA.AppendChild(_elmMediumArwImgCont);

                    if (needsLabel)
                    {
                        elmHiddenLabel = Utility.CreateHiddenLabel(altArrow);
                        _elmMediumBtnA.AppendChild(elmHiddenLabel);
                    }

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

            switch (displayMode)
            {
                case "Text":
                case "Medium":
                    _elmMedium = elm;
                    _elmMediumSelectedItem = (Span)_elmMedium.ChildNodes[0];
                    _elmMediumBtnA = (Anchor)_elmMedium.ChildNodes[1];
                    _elmMediumArwImgCont = (Span)_elmMediumBtnA.ChildNodes[0];
                    _elmMediumArwImg = (Image)_elmMediumArwImgCont.ChildNodes[0];
                    break;
            }
        }

        internal override void AttachEventsForDisplayMode(string displayMode)
        {

            switch (displayMode)
            {
                case "Text":
                case "Medium":
                    AttachEvents();
                    break;
            }
        }

        private void AttachEvents()
        {
            _elmMedium.Click += OnArrowButtonClick;
            _elmMedium.KeyPress += OnArrowButtonKeyPress;
            _elmMediumBtnA.MouseOver += OnArrowButtonFocus;
            _elmMediumBtnA.MouseOut += OnArrowButtonBlur;
            _elmMediumBtnA.Focus += OnArrowButtonKeyboardFocus;
            _elmMediumBtnA.Blur += OnArrowButtonBlur;
        }

        protected override void ReleaseEventHandlers()
        {
            _elmMedium.Click -= OnArrowButtonClick;
            _elmMedium.KeyPress -= OnArrowButtonKeyPress;
            _elmMediumBtnA.MouseOver -= OnArrowButtonFocus;
            _elmMediumBtnA.MouseOut -= OnArrowButtonBlur;
            _elmMediumBtnA.Focus -= OnArrowButtonKeyboardFocus;
            _elmMediumBtnA.Blur -= OnArrowButtonBlur;
        }

        public override void OnEnabledChanged(bool enabled)
        {
            if (enabled)
            {
                Utility.EnableElement(_elmMedium);
            }
            else
            {
                Utility.DisableElement(_elmMedium);
            }
        }

        internal override string ControlType
        {
            get
            {
                return "DropDown";
            }
        }

        internal override bool OnPreBubbleCommand(CommandEventArgs command)
        {
            // A command has been issued from a component somewhere under this
            // DropDown.  We want to determine if it is a option select command.
            // This would let us know that we need to do some processing so that 
            // the anchor has the appropriate value in it etc.
            if (command.Type == CommandType.OptionSelection)
            {
                MenuItem item = (MenuItem)command.Source;
                if (!(item.Control is ISelectableControl))
                    return base.OnPreBubbleCommand(command);

                ISelectableControl isc = (ISelectableControl)item.Control;
                // If an item is currently selected, deselect it first
                if (!CUIUtility.IsNullOrUndefined(_selectedControl))
                    _selectedControl.Deselect();
                SelectMenuItem(isc);
            }

            if (command.Type == CommandType.OptionSelection
                || command.Type == CommandType.OptionPreview
                || command.Type == CommandType.OptionPreviewRevert)
            {
                string myCommand;
                switch (command.Type)
                {
                    case CommandType.OptionSelection:
                        myCommand = Properties.Command;
                        break;
                    case CommandType.OptionPreview:
                        myCommand = Properties.CommandPreview;
                        break;
                    case CommandType.OptionPreviewRevert:
                        myCommand = Properties.CommandRevert;
                        break;
                    default:
                        // This case should not be hit, but it allows compilation
                        myCommand = Properties.Command;
                        break;
                }

                // Stop the command here and send our own
                DisplayedComponent.RaiseCommandEvent(myCommand,
                                                     command.Type,
                                                     command.Parameters);
                base.OnPreBubbleCommand(command);
                return false;
            }

            // if any other command type
            return base.OnPreBubbleCommand(command);
        }

        protected virtual void SelectMenuItem(ISelectableControl isc)
        {
            if (_selectedControl == isc)  // same menu item selected
                return;

            _selectedControl = isc;
            StateProperties[DropDownCommandProperties.SelectedItemId] = isc.GetMenuItemId();

            // If SelectedItemDisplayMode is not set, Medium to "Text"
            string selectedItemDisplayMode;
            if (string.IsNullOrEmpty(Properties.SelectedItemDisplayMode))
                selectedItemDisplayMode = "Text";
            else
                selectedItemDisplayMode = Properties.SelectedItemDisplayMode;

            Anchor selectedItem;

            if (selectedItemDisplayMode == "Text")
            {
                string text = isc.GetTextValue();
                selectedItem = new Anchor();
                UIUtility.SetInnerText(selectedItem, text);
            }
            else
                selectedItem = (Anchor)_selectedControl.GetDropDownDOMElementForDisplayMode(selectedItemDisplayMode);

            if (_elmMediumSelectedItem.HasChildNodes())
            {
                Anchor oldSelectedItem = (Anchor)_elmMediumSelectedItem.FirstChild;
                _elmMediumSelectedItem.ReplaceChild(selectedItem, oldSelectedItem);
            }
            else
                _elmMediumSelectedItem.AppendChild(selectedItem);
        }

        protected bool _itemEverSelected = false;
        internal bool SelectMenuItemById(string menuItemId)
        {
            // We must have a MenuItemId and a Menu in order to select an item
            // The Menu can be null if this control's menu gets populated dynamically
            if (string.IsNullOrEmpty(menuItemId) || CUIUtility.IsNullOrUndefined(Menu))
                return false;

            ISelectableControl isc = Menu.GetItemById(menuItemId);
            if (CUIUtility.IsNullOrUndefined(isc))
                return false;

            SelectMenuItem(isc);
            _itemEverSelected = true;
            return true;
        }

        protected virtual void AddDisplayModes()
        {
            AddDisplayMode("Medium");
            AddDisplayMode("Text"); // Remove once project's ribbon doesn't use it anymore
        }

        protected void OnArrowButtonClick(HtmlEvent evt)
        {
            bool enabled = Enabled;
#if PERF_METRICS
            if (enabled)
                PMetrics.PerfMark(PMarker.perfCUIDropDownOnArrowButtonClickStart);
#endif
            CloseToolTip();
            Utility.CancelEventUtility(evt, false, true);

            if (!enabled)
                return;

            Root.LastFocusedControl = this;
            LaunchMenuInternal(evt);
#if PERF_METRICS
            PMetrics.PerfMark(PMarker.perfCUIDropDownOnArrowButtonClickEnd);
#endif
        }

        protected virtual void LaunchMenuInternal(HtmlEvent args)
        {
            if (LaunchMenu(_elmMediumBtnA, new Action(SendMenuCreationCommandEvent)))
            {
                SendMenuCreationCommandEvent();
            }
        }

        protected void SendMenuCreationCommandEvent()
        {
            DisplayedComponent.RaiseCommandEvent(Properties.CommandMenuOpen,
                                                 CommandType.MenuCreation,
                                                 null);
        }

        public override void OnBeginFocus()
        {
            string dictKey = DropDownCommandProperties.Value;
            string selectedItemTitle = StateProperties.ContainsKey(dictKey) ? StateProperties[dictKey] : "";
            if (string.IsNullOrEmpty(selectedItemTitle)) 
            {
                // get currently selected item ID
                string itemKey = DropDownCommandProperties.SelectedItemId;
                string currentSelectedItemId = StateProperties.ContainsKey(itemKey) ? StateProperties[itemKey] : "";

                // If we currently don't have an item set, then we set it here to let the application know
                // since this is what we'll fall back to unless they set something else.
                if (string.IsNullOrEmpty(currentSelectedItemId))
                    currentSelectedItemId = Properties.InitialItem;

                // get the title of the selected item
                // The Menu can be null if this control's menu gets populated dynamically
                if ((!string.IsNullOrEmpty(currentSelectedItemId)) && 
                    (!CUIUtility.IsNullOrUndefined(Menu)))
                {
                    ISelectableControl isc = Menu.GetItemById(currentSelectedItemId);
                    if (!CUIUtility.IsNullOrUndefined(isc))
                    {
                        selectedItemTitle = isc.GetTextValue();
                    }
                }

            }
            if (!string.IsNullOrEmpty(selectedItemTitle))
            {
                Properties.ToolTipSelectedItemTitle = selectedItemTitle;
            }
            base.OnBeginFocus();
        }

        protected void OnArrowButtonKeyboardFocus(HtmlEvent args)
        {
            Root.LastFocusedControl = this;
            OnArrowButtonFocus(args);
        }

        protected void OnArrowButtonFocus(HtmlEvent args)
        {
            OnBeginFocus();
            if (!Enabled)
                return;
            Highlight();
        }

        protected void OnArrowButtonBlur(HtmlEvent args)
        {
            OnEndFocus();
            if (!Enabled || MenuLaunched)
                return;
            RemoveHighlight();
        }

        protected void OnArrowButtonKeyPress(HtmlEvent args)
        {
            CloseToolTip();
            if (!Enabled)
                return;

            int key = args.KeyCode;
            if (key == (int)Key.Enter || key == (int)Key.Space || key == (int)Key.Down)
            {
                LaunchedByKeyboard = true;
                LaunchMenuInternal(args);
            }
        }

        protected virtual void Highlight()
        {
            Utility.EnsureCSSClassOnElement(_elmMediumBtnA,
                                            "ms-cui-ctl-light-hoveredOver");
        }

        protected virtual void RemoveHighlight()
        {
            Utility.RemoveCSSClassFromElement(_elmMediumBtnA,
                                              "ms-cui-ctl-light-hoveredOver");
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

        internal override void PollForStateAndUpdate()
        {
            string itemKey = DropDownCommandProperties.SelectedItemId;
            string currentSelectedItemId = StateProperties.ContainsKey(itemKey) ? StateProperties[itemKey] : "";

            // If we currently don't have an item set, then we set it here to let the application know
            // since this is what we'll fall back to unless they set something else.
            if (string.IsNullOrEmpty(currentSelectedItemId))
                StateProperties[DropDownCommandProperties.SelectedItemId] = Properties.InitialItem;

            PollForStateAndUpdateInternal(Properties.Command,
                                          Properties.QueryCommand,
                                          StateProperties,
                                          false);

            string newSelectedItemId = StateProperties[DropDownCommandProperties.SelectedItemId];

            // If the currently selected item is null, use the display text
            if (!string.IsNullOrEmpty(newSelectedItemId))
            {
                // We want to select the item if it is different than the one that we currently
                // have or if we have never selected one (this happens when 
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
                string valueKey = DropDownCommandProperties.Value;
                string title = StateProperties.ContainsKey(valueKey) ? StateProperties[valueKey] : "";
                if (!string.IsNullOrEmpty(title))
                {
                    // O14:330988 Put it in an <a> tag so it picks up the correct style.
                    Anchor newAnchor = new Anchor();
                    newAnchor.InnerHtml = title;
                    _elmMediumSelectedItem.AppendChild(newAnchor);
                }
            }
        }


        public override void Dispose()
        {
            base.Dispose();
            _elmMedium = null;
            _elmMediumArwImg = null;
            _elmMediumArwImgCont = null;
            _elmMediumBtnA = null;
            _elmMediumSelectedItem = null;
        }

        protected DropDownProperties Properties
        {
            get
            {
                return (DropDownProperties)base.ControlProperties;
            }
        }
    }
}
