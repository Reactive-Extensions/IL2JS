using System;
using System.Collections.Generic;
using Microsoft.LiveLabs.Html;

using MSLabel = Microsoft.LiveLabs.Html.Label;

namespace Ribbon.Controls
{
    /// <summary>
    /// A class that displays a split button with state in the Ribbon
    /// The MRUSplitButton takes the following parameters:
    /// OpenMenuCmd - the id of the Command that is issued when the menu is opened
    /// BtnImg - Url to the down arrow that drops down the menu
    /// SelCmd - the id of the Command that is issued when a value is chosen for this split button
    /// InitialItem - the MenuItemId of the item that is chosen on load
    /// Width - the width of the control for Medium display mode
    /// </summary>
    internal class MRUSplitButton : DropDown
    {
        // Large display mode objects
        Span _elmLarge;
        Span _elmLargeSelectedItem;
        Anchor _elmLargeBtn;
        Image _elmLargeArrowImg;

        // Medium & Small display mode objects
        Span _elmMedium;
        Span _elmMediumSelectedItem;
        Anchor _elmMediumBtn;
        Image _elmMediumArrowImg;

        // Small display mode objects
        Span _elmSmall;
        Span _elmSmallSelectedItem;
        Anchor _elmSmallBtn;
        Image _elmSmallArrowImg;

        bool _buttonEnabled = false;
        bool _buildingDOMElement = false;

        public MRUSplitButton(Root root, string id, DropDownProperties properties, Menu menu)
            : base(root, id, properties, menu)
        {
        }

        internal override bool SetFocusOnControl()
        {
            if (!Enabled)
                return false;

            if (_buttonEnabled)
            {
                ((HtmlElement)DisplayedComponent.ElementInternal.FirstChild.FirstChild).PerformFocus();
                return true;
            }

            return false;
        }

        protected override HtmlElement CreateDOMElementForDisplayMode(string displayMode)
        {
            bool needsLabel = true;
            string alt = CUIUtility.SafeString(Properties.Alt);
            string width = (displayMode == "Medium" && !string.IsNullOrEmpty(Properties.Width)) ? Properties.Width : "auto";
            MSLabel hiddenLabel;
            string itemId = StateProperties[DropDownCommandProperties.SelectedItemId];
            if (string.IsNullOrEmpty(itemId))
                itemId = Properties.InitialItem;

            _buildingDOMElement = true;

            switch (displayMode)
            {
                case "Large":

                    _elmLarge = new Span();
                    _elmLarge.SetAttribute("mscui:controltype", ControlType);
                    // _elmLarge.ClassName = "ms-cui-sb";
                    Utility.EnsureCSSClassOnElement(_elmLarge, "ms-cui-ctl-large");

                    _elmLargeSelectedItem = new Span();
                    _elmLargeSelectedItem.ClassName = "ms-cui-mrusb-selecteditem";

                    if (!SelectMenuItemById(itemId))
                    {
                        if (!Utility.IsTrue(Properties.PopulateDynamically))
                        {
                            throw new InvalidOperationException("No menu item with id '" + Properties.InitialItem + "' exists in this control's menu");
                        }
                        else
                        {
                            _elmLargeSelectedItem.Style.Width = "32px";
                            _elmLargeSelectedItem.Style.Height = "32px";
                        }
                    }

                    _elmLargeBtn = new Anchor();
                    _elmLargeBtn.ClassName = "ms-cui-ctl-a2";
                    _elmLargeBtn.Style.Display = "block";
                    _elmLargeBtn.SetAttribute("role", AriaRole);
                    _elmLargeBtn.SetAttribute("aria-haspopup", true.ToString());

                    Utility.NoOpLink(_elmLargeBtn);
                    Utility.SetAriaTooltipProperties(Properties, _elmLargeBtn);

                    _elmLargeArrowImg = new Image();
                    Span elmLargeArrowImgCont = Utility.CreateClusteredImageContainerNew(
                                                                                      ImgContainerSize.Size5by3,
                                                                                      Root.Properties.ImageDownArrow,
                                                                                      Root.Properties.ImageDownArrowClass,
                                                                                      _elmLargeArrowImg,
                                                                                      true,
                                                                                      false,
                                                                                      Root.Properties.ImageDownArrowTop,
                                                                                      Root.Properties.ImageDownArrowLeft
                                                                                      );

                    if(string.IsNullOrEmpty(Properties.ToolTipTitle)) 
                    {
                        needsLabel = false;
                        _elmLargeBtn.Title = alt;
                        _elmLargeArrowImg.Alt = alt;
                    }

                    // Set up event handlers for the drop down button
                    AttachEventsForDisplayMode(displayMode);

                    // Build DOM structure
                    _elmLargeBtn.AppendChild(elmLargeArrowImgCont);

                    if (needsLabel)
                    {
                        hiddenLabel = Utility.CreateHiddenLabel(alt);
                        _elmLargeBtn.AppendChild(hiddenLabel);
                    }
                    _elmLarge.AppendChild(_elmLargeSelectedItem);
                    _elmLarge.AppendChild(_elmLargeBtn);

                    _buildingDOMElement = false;
                    return _elmLarge;
                case "Medium":
                    _elmMedium = new Span();
                    _elmMedium.SetAttribute("mscui:controltype", ControlType);
                    _elmMedium.ClassName = "ms-cui-ctl-medium ms-cui-ctl";

                    _elmMediumSelectedItem = new Span();
                    _elmMediumSelectedItem.ClassName = "ms-cui-mrusb-selecteditem";
                    _elmMediumSelectedItem.Style.Width = width;
                    if (!SelectMenuItemById(itemId))
                        throw new InvalidOperationException("No menu item with id '" + Properties.InitialItem + "' exists in this control's menu");

                    _elmMediumBtn = new Anchor();
                    Utility.NoOpLink(_elmMediumBtn);
                    Utility.SetAriaTooltipProperties(Properties, _elmMediumBtn);
                    _elmMediumBtn.ClassName = "ms-cui-ctl";
                    _elmMediumBtn.SetAttribute("role", AriaRole);
                    _elmMediumBtn.SetAttribute("aria-haspopup", true.ToString());
                    _elmMediumArrowImg = new Image();
                    if(string.IsNullOrEmpty(Properties.ToolTipTitle)) 
                    {
                        needsLabel = false;
                        _elmMediumBtn.Title = alt;
                        _elmMediumArrowImg.Alt = alt;
                    }

                    Span _elmMediumArrowImgCont = Utility.CreateClusteredImageContainerNew(
                                                                             ImgContainerSize.Size5by3,
                                                                             Root.Properties.ImageDownArrow,
                                                                             Root.Properties.ImageDownArrowClass,
                                                                             _elmMediumArrowImg,
                                                                             true,
                                                                             false,
                                                                             Root.Properties.ImageDownArrowTop,
                                                                             Root.Properties.ImageDownArrowLeft
                                                                             );


                    // Set up event handlers for the drop down button
                    AttachEventsForDisplayMode(displayMode);

                    //Build DOM structure
                    _elmMediumBtn.AppendChild(_elmMediumArrowImgCont);
                    if (needsLabel)
                    {
                        hiddenLabel = Utility.CreateHiddenLabel(alt);
                        _elmMediumBtn.AppendChild(hiddenLabel);
                    }
                    _elmMedium.AppendChild(_elmMediumSelectedItem);
                    _elmMedium.AppendChild(_elmMediumBtn);

                    _buildingDOMElement = false;
                    return _elmMedium;
                case "Small":
                    _elmSmall = new Span();
                    _elmSmall.SetAttribute("mscui:controltype", ControlType);
                    _elmSmall.ClassName = "ms-cui-ctl-medium ms-cui-ctl";
                    _elmSmallSelectedItem = new Span();
                    _elmSmallSelectedItem.ClassName = "ms-cui-mrusb-selecteditem";
                    _elmSmallSelectedItem.Style.Width = width;
                    if (!SelectMenuItemById(itemId))
                        throw new InvalidOperationException("No menu item with id '" + Properties.InitialItem + "' exists in this control's menu");

                    _elmSmallBtn = new Anchor();
                    Utility.NoOpLink(_elmSmallBtn);
                    Utility.SetAriaTooltipProperties(Properties, _elmSmallBtn);
                    _elmSmallBtn.SetAttribute("role", AriaRole);
                    _elmSmallBtn.SetAttribute("aria-haspopup", true.ToString());
                    _elmSmallBtn.ClassName = "ms-cui-ctl ms-cui-mrusb-arwbtn";
                    _elmSmallArrowImg = new Image();

                    if(string.IsNullOrEmpty(Properties.ToolTipTitle)) 
                    {
                        _elmSmallBtn.Title = alt;
                        _elmSmallArrowImg.Alt = alt;
                        needsLabel = false;
                    }

                    Span _elmSmallArrowImgCont = Utility.CreateClusteredImageContainerNew(
                                                                             ImgContainerSize.Size5by3,
                                                                             Root.Properties.ImageDownArrow,
                                                                             Root.Properties.ImageDownArrowClass,
                                                                             _elmSmallArrowImg,
                                                                             true,
                                                                             false,
                                                                             Root.Properties.ImageDownArrowTop,
                                                                             Root.Properties.ImageDownArrowLeft
                                                                             );

                    // Set up event handlers for the drop down button
                    AttachEventsForDisplayMode(displayMode);

                    //Build DOM structure
                    _elmSmallBtn.AppendChild(_elmSmallArrowImgCont);
                    if (needsLabel)
                    {
                        hiddenLabel = Utility.CreateHiddenLabel(alt);
                        _elmSmallBtn.AppendChild(hiddenLabel);
                    }
                    _elmSmall.AppendChild(_elmSmallSelectedItem);
                    _elmSmall.AppendChild(_elmSmallBtn);

                    _buildingDOMElement = false;
                    return _elmSmall;
                default:
                    _buildingDOMElement = false;
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
                case "Large":
                    _elmLarge = elm;
                    _elmLargeSelectedItem = (Span)_elmLarge.ChildNodes[0];
                    _elmLargeBtn = (Anchor)_elmLarge.ChildNodes[1];
                    _elmLargeArrowImg = (Image)_elmLargeBtn.ChildNodes[0].ChildNodes[0];
                    break;
                case "Medium":
                    _elmMedium = elm;
                    _elmMediumSelectedItem = (Span)_elmMedium.ChildNodes[0];
                    _elmMediumBtn = (Anchor)_elmMedium.ChildNodes[1];
                    _elmMediumArrowImg = (Image)_elmMediumBtn.ChildNodes[0].ChildNodes[0];
                    break;
                case "Small":
                    _elmSmall = elm;
                    _elmSmallSelectedItem = (Span)_elmSmall.ChildNodes[0];
                    _elmSmallBtn = (Anchor)_elmSmall.ChildNodes[1];
                    _elmSmallArrowImg = (Image)_elmSmallBtn.ChildNodes[0].ChildNodes[0];
                    break;
            }
        }

        internal override void AttachEventsForDisplayMode(string displayMode)
        {
            AttachEvents(displayMode);
        }

        private void AttachEvents(string displayMode)
        {
            switch (displayMode)
            {
                case "Large":
                    _elmLargeBtn.Click += OnArrowButtonClick;
                    _elmLargeSelectedItem.MouseOver += OnSelectedItemMouseover;
                    _elmLargeSelectedItem.MouseOut += OnSelectedItemMouseout;
                    _elmLargeBtn.MouseOver += OnArrowButtonFocus;
                    _elmLargeBtn.MouseOut += OnArrowButtonBlur;
                    _elmLargeBtn.Focus += OnArrowButtonKeyboardFocus;
                    _elmLargeBtn.Blur += OnArrowButtonBlur;
                    _elmLargeBtn.KeyPress += OnArrowButtonKeyPress;
                    break;
                case "Medium":
                    _elmMediumBtn.Click += OnArrowButtonClick;
                    _elmMediumSelectedItem.MouseOver += OnSelectedItemMouseover;
                    _elmMediumSelectedItem.MouseOut += OnSelectedItemMouseout;
                    _elmMediumBtn.MouseOver += OnArrowButtonFocus;
                    _elmMediumBtn.MouseOut += OnArrowButtonBlur;
                    _elmMediumBtn.Focus += OnArrowButtonKeyboardFocus;
                    _elmMediumBtn.Blur += OnArrowButtonBlur;
                    _elmMediumBtn.KeyPress += OnArrowButtonKeyPress;
                    break;
                case "Small":
                    _elmSmallBtn.Click += OnArrowButtonClick;
                    _elmSmallSelectedItem.MouseOver += OnSelectedItemMouseover;
                    _elmSmallSelectedItem.MouseOut += OnSelectedItemMouseout;
                    _elmSmallBtn.MouseOver += OnArrowButtonFocus;
                    _elmSmallBtn.MouseOut += OnArrowButtonBlur;
                    _elmSmallBtn.Focus += OnArrowButtonKeyboardFocus;
                    _elmSmallBtn.Blur += OnArrowButtonBlur;
                    _elmSmallBtn.KeyPress += OnArrowButtonKeyPress;
                    break;
            }
        }

        protected override void ReleaseEventHandlers()
        {
            if (!CUIUtility.IsNullOrUndefined(_elmLargeBtn) &&
                    !CUIUtility.IsNullOrUndefined(_elmLargeSelectedItem))
                RemoveEvents(_elmLargeBtn, _elmLargeSelectedItem);
            if (!CUIUtility.IsNullOrUndefined(_elmMediumBtn) &&
                    !CUIUtility.IsNullOrUndefined(_elmMediumSelectedItem))
                RemoveEvents(_elmMediumBtn, _elmMediumSelectedItem);
            if (!CUIUtility.IsNullOrUndefined(_elmSmallBtn) &&
                    !CUIUtility.IsNullOrUndefined(_elmSmallSelectedItem))
                RemoveEvents(_elmSmallBtn, _elmSmallSelectedItem);
        }

        private void RemoveEvents(HtmlElement elmButton, HtmlElement elmItem)
        {
            elmItem.MouseOver += OnSelectedItemMouseover;
            elmItem.MouseOut += OnSelectedItemMouseout;
            elmButton.Click += OnArrowButtonClick;
            elmButton.MouseOver += OnArrowButtonFocus;
            elmButton.MouseOut += OnArrowButtonBlur;
            elmButton.Focus += OnArrowButtonKeyboardFocus;
            elmButton.Blur += OnArrowButtonBlur;
            elmButton.KeyPress += OnArrowButtonKeyPress;
        }

        internal override string ControlType
        {
            get
            {
                return "MRUSplitButton";
            }
        }

        protected override void SelectMenuItem(ISelectableControl isc)
        {
            // Same item selected and we are not in the middle of building a DOM element
            // In this case we need to construct the selected item DOM element for this new display mode.
            if (_selectedControl == isc && !_buildingDOMElement)
                return;

            // We either get the current display mode of this control through what is presently 
            // shown in the ribbon.  If not, then we are creating it and just setting the initially shown 
            // item.  In this case, we want to set the menu item to the one that we are currently creating.
            string displayMode = !CUIUtility.IsNullOrUndefined(DisplayedComponent) ?
                DisplayedComponent.Title : CurrentlyCreatedDisplayMode;

            Span itemContainer;
            switch (displayMode)
            {
                case "Large":
                    itemContainer = _elmLargeSelectedItem;
                    break;
                case "Medium":
                    itemContainer = _elmMediumSelectedItem;
                    break;
                case "Small":
                    itemContainer = _elmSmallSelectedItem;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("Invalid display mode on split button while selecting a menu item");
            }

            _selectedControl = isc;
            StateProperties[DropDownCommandProperties.SelectedItemId] = isc.GetMenuItemId();

            Control iscControl = (Control)isc;

            if (iscControl.DisplayedComponent is MenuItem)
                Menu.SelectedMenuItem = (MenuItem)iscControl.DisplayedComponent;
            Span selectedItem = (Span)_selectedControl.GetDropDownDOMElementForDisplayMode(displayMode);

            // The drop down element should not have a <BR> in it because it is
            // used in an MRUSplitButton and there is only room for one line of text.
            if (selectedItem.ChildNodes.Length > 1)
            {
                HtmlElement elm = (HtmlElement)selectedItem.ChildNodes[1];
                if (elm.ChildNodes.Length > 1)
                {
                    if (((HtmlElement)elm.ChildNodes[1]).TagName.ToLower() == "br")
                    {
                        Span elmText = new Span();
                        UIUtility.SetInnerText(elmText, " ");
                        elm.ReplaceChild(elmText, elm.ChildNodes[1]);
                    }
                }
            }

            // Set the ID to null since this DOM element is now hosted in this MRUSplitButton.
            selectedItem.Id = Id + "-SelectedItem";
            if (itemContainer.HasChildNodes())
            {
                HtmlElement oldSelectedItem = (HtmlElement)itemContainer.FirstChild;
                itemContainer.ReplaceChild(selectedItem, oldSelectedItem);
            }
            else
                itemContainer.AppendChild(selectedItem);

            selectedItem.Click += OnSelectedItemClick;
            selectedItem.DblClick += OnDblClick;
        }

        protected override void AddDisplayModes()
        {
            AddDisplayMode("Large");
            AddDisplayMode("Medium");
            AddDisplayMode("Small");
        }

        protected override void LaunchMenuInternal(HtmlEvent args)
        {
            bool launchSucceeded = false;
            Root.FixedPositioningEnabled = false;

            switch (DisplayedComponent.Title)
            {
                case "Large":
                    launchSucceeded = LaunchMenu(_elmLargeBtn, new Action(SendMenuCreationCommandEvent));
                    break;
                case "Medium":
                    launchSucceeded = LaunchMenu(_elmMediumBtn, new Action(SendMenuCreationCommandEvent));
                    break;
                case "Small":
                    launchSucceeded = LaunchMenu(_elmSmallBtn, new Action(SendMenuCreationCommandEvent));
                    break;
            }
            if (launchSucceeded)
            {
                // _selectedControl.FocusOnDisplayedComponent();
                // Send out the menu launch command if it has been specified
                SendMenuCreationCommandEvent();
            }
        }

        public override void OnEnabledChanged(bool enabled)
        {
            base.OnEnabledChanged(enabled);
            const string disabledClass = "ms-cui-disabled";
            _buttonEnabled = enabled;


            if (enabled)
            {
                Utility.RemoveCSSClassFromElement(_elmLarge, disabledClass);
                Utility.EnableElement(_elmLargeBtn);

                Utility.RemoveCSSClassFromElement(_elmMedium, disabledClass);
                Utility.EnableElement(_elmMediumBtn);

                Utility.RemoveCSSClassFromElement(_elmSmall, disabledClass);
                Utility.EnableElement(_elmSmallBtn);
            }
            else
            {
                Utility.EnsureCSSClassOnElement(_elmLarge, disabledClass);
                Utility.DisableElement(_elmLargeBtn);

                Utility.EnsureCSSClassOnElement(_elmMedium, disabledClass);
                Utility.DisableElement(_elmMediumBtn);

                Utility.EnsureCSSClassOnElement(_elmSmall, disabledClass);
                Utility.DisableElement(_elmSmallBtn);
            }

            Utility.SetEnabledOnElement(_elmLargeBtn, enabled);
            Utility.SetEnabledOnElement(_elmMediumBtn, enabled);
            Utility.SetEnabledOnElement(_elmSmallBtn, enabled);
        }

        protected override void OnDblClick(HtmlEvent evt)
        {
            CloseToolTip();
            Utility.CancelEventUtility(evt, false, true);
            
            if (!Enabled)
                return;

            OnSelectedItemClick(evt);
        }

        private void OnSelectedItemClick(HtmlEvent evt)
        {
#if PERF_METRICS
            PMetrics.PerfMark(PMarker.perfCUIRibbonEditWikiPageStart);
#endif
            CloseToolTip();
            Utility.CancelEventUtility(evt, false, true);
            
            if (!Enabled)
                return;

            CommandType ct = CommandType.OptionSelection;
            
            Dictionary<string, string> commandTemp = new Dictionary<string, string>();
            commandTemp["CommandValueId"] = _selectedControl.GetCommandValueId();
            DisplayedComponent.RaiseCommandEvent(Properties.Command, 
                                                ct, 
                                                commandTemp);
        }

        private void OnSelectedItemMouseover(HtmlEvent args)
        {
            OnBeginFocus();
            if (!Enabled)
                return;

            switch (DisplayedComponent.Title)
            {
                case "Large":
                    Utility.EnsureCSSClassOnElement(_elmLargeSelectedItem,
                                                    "ms-cui-ctl-light-hoveredOver");
                    Utility.EnsureCSSClassOnElement(_elmLargeBtn,
                                                    "ms-cui-ctl-split-hover");
                    break;
                case "Medium":
                    Utility.EnsureCSSClassOnElement(_elmMediumSelectedItem,
                                                    "ms-cui-ctl-light-hoveredOver");
                    Utility.EnsureCSSClassOnElement(_elmMediumBtn,
                                                    "ms-cui-ctl-split-hover");
                    break;
                case "Small":
                    Utility.EnsureCSSClassOnElement(_elmSmallSelectedItem,
                                                    "ms-cui-ctl-light-hoveredOver");
                    Utility.EnsureCSSClassOnElement(_elmSmallBtn,
                                                    "ms-cui-ctl-split-hover");
                    break;
            }

            if (string.IsNullOrEmpty(Properties.CommandPreview))
                return;

            Dictionary<string, string> commandTemp = new Dictionary<string, string>();
            commandTemp["CommandValueId"] = _selectedControl.GetCommandValueId();
            DisplayedComponent.RaiseCommandEvent(Properties.CommandPreview, 
                                                CommandType.Preview, 
                                                commandTemp);
        }

        private void OnSelectedItemMouseout(HtmlEvent args)
        {
            OnEndFocus();
            if (!Enabled)
                return;

            switch (DisplayedComponent.Title)
            {
                case "Large":
                    Utility.RemoveCSSClassFromElement(_elmLargeSelectedItem,
                                                    "ms-cui-ctl-light-hoveredOver");
                    Utility.RemoveCSSClassFromElement(_elmLargeBtn,
                                                    "ms-cui-ctl-split-hover");
                    break;
                case "Medium":
                    Utility.RemoveCSSClassFromElement(_elmMediumSelectedItem,
                                                    "ms-cui-ctl-light-hoveredOver");
                    Utility.RemoveCSSClassFromElement(_elmMediumBtn,
                                                    "ms-cui-ctl-split-hover");
                    break;
                case "Small":
                    Utility.RemoveCSSClassFromElement(_elmSmallSelectedItem,
                                                    "ms-cui-ctl-light-hoveredOver");
                    Utility.RemoveCSSClassFromElement(_elmSmallBtn,
                                                    "ms-cui-ctl-split-hover");
                    break;
            }

            if (string.IsNullOrEmpty(Properties.CommandRevert))
                return;

            Dictionary<string, string> commandTemp = new Dictionary<string, string>();
            commandTemp["CommandValueId"] = _selectedControl.GetCommandValueId();
            DisplayedComponent.RaiseCommandEvent(Properties.CommandRevert, 
                                                CommandType.PreviewRevert, 
                                                commandTemp);
        }


        protected override void Highlight()
        {
            switch (DisplayedComponent.Title)
            {
                case "Large":
                    Utility.EnsureCSSClassOnElement(_elmLargeSelectedItem,
                                                    "ms-cui-ctl-split-hover");
                    Utility.EnsureCSSClassOnElement(_elmLargeBtn,
                                                    "ms-cui-ctl-light-hoveredOver");
                    break;
                case "Medium":
                    Utility.EnsureCSSClassOnElement(_elmMediumSelectedItem,
                                                    "ms-cui-ctl-split-hover");
                    Utility.EnsureCSSClassOnElement(_elmMediumBtn,
                                                    "ms-cui-ctl-light-hoveredOver");
                    break;
                case "Small":
                    Utility.EnsureCSSClassOnElement(_elmSmallSelectedItem,
                                                    "ms-cui-ctl-split-hover");
                    Utility.EnsureCSSClassOnElement(_elmSmallBtn,
                                                    "ms-cui-ctl-light-hoveredOver");
                    break;
            }
        }

        protected override void RemoveHighlight()
        {
            switch (DisplayedComponent.Title)
            {
                case "Large":
                    Utility.RemoveCSSClassFromElement(_elmLargeSelectedItem,
                                                    "ms-cui-ctl-split-hover");
                    Utility.RemoveCSSClassFromElement(_elmLargeBtn,
                                                    "ms-cui-ctl-light-hoveredOver");
                    break;
                case "Medium":
                    Utility.RemoveCSSClassFromElement(_elmMediumSelectedItem,
                                                    "ms-cui-ctl-split-hover");
                    Utility.RemoveCSSClassFromElement(_elmMediumBtn,
                                                    "ms-cui-ctl-light-hoveredOver");
                    break;

                case "Small":
                    Utility.RemoveCSSClassFromElement(_elmSmallSelectedItem,
                                                    "ms-cui-ctl-split-hover");
                    Utility.RemoveCSSClassFromElement(_elmSmallBtn,
                                                    "ms-cui-ctl-light-hoveredOver");
                    break;
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            _elmLarge = null;
            _elmLargeArrowImg = null;
            _elmLargeBtn = null;
            _elmLargeSelectedItem = null;
            _elmMedium = null;
            _elmMediumArrowImg = null;
            _elmMediumBtn = null;
            _elmMediumSelectedItem = null;
            _elmSmall = null;
            _elmSmallArrowImg = null;
            _elmSmallBtn = null;
            _elmSmallSelectedItem = null;
        }
    }
}
