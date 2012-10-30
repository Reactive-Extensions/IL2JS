using System;
using Microsoft.LiveLabs.Html;
using Microsoft.LiveLabs.JavaScript.Interop;

using MenuType = Ribbon.Menu;

namespace Ribbon.Controls
{
    [Import(MemberNameCasing = Casing.Exact)]
    public class FlyoutAnchorProperties : MenuLauncherControlProperties
    {
        extern public FlyoutAnchorProperties();
        extern public string Alt { get; }
        extern public string Description { get; }
        extern public string Image16by16 { get; }
        extern public string Image16by16Class { get; }
        extern public string Image16by16Top { get; }
        extern public string Image16by16Left { get; }
        extern public string Image32by32 { get; set; }
        extern public string Image32by32Class { get; set; }
        extern public string Image32by32Top { get; set; }
        extern public string Image32by32Left { get; set; }
        extern public string LabelText { get; set; }
    }

    /// <summary>
    /// A class representing a button that launches a menu
    /// This can exist as a button or as a menu item
    /// </summary>
    /// <owner alias="JKern" />
    internal class FlyoutAnchor : MenuLauncher
    {
        // Menu display mode elements
        Image _elmMenuArrowImg;
        Span _elmMenuArrowImgCont;

        // Shared menu elements
        Span _elmMenuLbl;
        Image _elmMenuImg16;
        Span _elmMenuImg16Cont;
        Image _elmMenuImg32;
        Span _elmMenuImg32Cont;

        // Individual display mode containers
        Anchor _elmMenu;
        Anchor _elmMenu16;
        Anchor _elmMenu32;

        // Scalable display modes need separate DOM elements
        // Large display mode
        Anchor _elmLarge;
        Image _elmLargeImg;
        Image _elmLargeArrowImg;

        // Medium display mode
        Anchor _elmMedium;
        Image _elmMediumImg;
        Image _elmMediumArrowImg;

        // Small display mode
        Anchor _elmSmall;
        Image _elmSmallImg;
        Image _elmSmallArrowImg;

        // Thin display mode elements
        Anchor _elmThin;
        Image _elmThinArrowImg;
        Span _elmThinArrowImgCont;

        /// <summary>
        /// Creates a FlyoutAnchor control
        /// </summary>
        /// <param name="root">The Root that this control is part of</param>
        /// <param name="id">A unique identifier for this control</param>
        /// <param name="properties">The set of FlyoutAnchorProperties for this control</param>
        /// <param name="menu">The Menu control to launch from this FlyoutAnchor</param>
        /// <owner alias="JKern" />
        public FlyoutAnchor(Root root,
                              string id,
                              FlyoutAnchorProperties properties,
                              MenuType menu)
            : base(root, id, properties, menu)
        {
            AddDisplayMode("Menu");
            AddDisplayMode("Menu16");
            AddDisplayMode("Menu32");
            AddDisplayMode("Small");
            AddDisplayMode("Medium");
            AddDisplayMode("Large");
            AddDisplayMode("Thin");
        }

        /// <summary>
        /// Creates the DOM Element for the given display mode of this Flyout Anchor
        /// </summary>
        /// <param name="displayMode">The identifier of a valid display mode for this control</param>
        /// <returns>A DOMElement that represents this control for the given display mode</returns>
        /// <owner alias="JKern" />
        protected override HtmlElement CreateDOMElementForDisplayMode(string displayMode)
        {
            string alt = string.IsNullOrEmpty(Properties.Alt) ? 
                Properties.LabelText : Properties.Alt;

            switch (displayMode)
            {
                case "Menu":
                    _elmMenu = CreateMenuDOMElement("Menu",
                        "ms-cui-textmenuitem ms-cui-fa-menuitem ms-cui-ctl-menu",
                        alt,
                        null,
                        null,
                        null,
                        null);

                    AttachEventsForDisplayMode(displayMode);
                    _elmMenu.SetAttribute("aria-haspopup", "true");
                    return _elmMenu;
                case "Menu16":
                    _elmMenu16 = CreateMenuDOMElement("Menu16",
                        "ms-cui-fa-menuitem ms-cui-ctl-menu",
                        alt,
                        Properties.Image16by16,
                        Properties.Image16by16Class,
                        Properties.Image16by16Top,
                        Properties.Image16by16Left);

                    AttachEventsForDisplayMode(displayMode);
                    _elmMenu16.SetAttribute("aria-haspopup", "true");
                    return _elmMenu16;
                case "Menu32":
                    _elmMenu32 = CreateMenuDOMElement("Menu32",
                        "ms-cui-fa-menuitem ms-cui-ctl-menu",
                        alt,
                        Properties.Image32by32,
                        Properties.Image32by32Class,
                        Properties.Image32by32Top,
                        Properties.Image32by32Left);
                    _elmMenu32.SetAttribute("aria-haspopup", "true");

                    AttachEventsForDisplayMode(displayMode);
                    return _elmMenu32;

                case "Large":
                    _elmLarge = CreateStandardControlDOMElement(
                                    this,
                                    Root,
                                    "Large",
                                    Properties,
                                    false,
                                    true);
                    _elmLarge.SetAttribute("aria-haspopup", "true");

                    if (IsGroupPopup)
                        Utility.EnsureCSSClassOnElement(_elmLarge, "ms-cui-ctl-large-groupPopup");

                    AttachEventsForDisplayMode("Large");
                    return _elmLarge;

                case "Medium":
                    _elmMedium = CreateStandardControlDOMElement(
                                    this,
                                    Root,
                                    "Medium",
                                    Properties,
                                    false,
                                    true);
                    // Set up event handlers
                    AttachEventsForDisplayMode("Medium");
                    _elmMedium.SetAttribute("aria-haspopup", "true");
                    return _elmMedium;

                case "Small":
                    _elmSmall = CreateStandardControlDOMElement(
                                    this,
                                    Root,
                                    "Small",
                                    Properties,
                                    false,
                                    true);

                    // Set up event handlers
                    AttachEventsForDisplayMode("Small");
                    _elmSmall.SetAttribute("aria-haspopup", "true");
                    return _elmSmall;

                case "Thin":
                    _elmThin = Utility.CreateNoOpLink();
                    _elmThin.ClassName = "ms-cui-ctl-thin";

                    _elmThinArrowImg = new Image();
                    _elmThinArrowImg.Alt = "";
                    if(string.IsNullOrEmpty(Properties.ToolTipTitle))
                    {
                        _elmThin.Title = alt;
                        _elmThinArrowImg.Alt = alt;
                    }

                    Root root = this.Root;
                    _elmThinArrowImgCont = Utility.CreateClusteredImageContainerNew(
                                                                    ImgContainerSize.Size5by3,
                                                                    root.Properties.ImageDownArrow,
                                                                    root.Properties.ImageDownArrowClass,
                                                                    _elmThinArrowImg,
                                                                    true,
                                                                    false,
                                                                    root.Properties.ImageDownArrowTop,
                                                                    root.Properties.ImageDownArrowLeft);

                    _elmThin.AppendChild(_elmThinArrowImgCont);

                    // Attach Events
                    AttachEventsForDisplayMode("Thin");
                    _elmThin.SetAttribute("aria-haspopup", "true");
                    return _elmThin;
                default:
                    EnsureValidDisplayMode(displayMode);
                    break;
            }
            return null;
        }

        internal override void AttachDOMElementsForDisplayMode(string displayMode)
        {
            Anchor elm = (Anchor)Browser.Document.GetById(Id + "-" + displayMode);
            if (!CUIUtility.IsNullOrUndefined(elm))
                StoreElementForDisplayMode(elm, displayMode);

            // Only do hookup for non-menu display modes
            // This is also called from CreateDOMElementForDisplayMode() so if "elm" is null
            // then we should already have the outer DOMElement.
            switch (displayMode)
            {
                case "Large":
                    if (!CUIUtility.IsNullOrUndefined(elm))
                        _elmLarge = elm;
                    _elmLargeImg = (Image)_elmLarge.ChildNodes[0].ChildNodes[0];
                    _elmLargeArrowImg = (Image)_elmLarge.LastChild.LastChild.ChildNodes[0];
                    break;
                case "Medium":
                    if (!CUIUtility.IsNullOrUndefined(elm))
                        _elmMedium = elm;
                    _elmMediumImg = (Image)_elmMedium.ChildNodes[0].ChildNodes[0];
                    _elmMediumArrowImg = (Image)_elmMedium.LastChild.LastChild.ChildNodes[0];
                    break;
                case "Small":
                    if (!CUIUtility.IsNullOrUndefined(elm))
                        _elmSmall = elm;
                    _elmSmallImg = (Image)_elmSmall.ChildNodes[0].ChildNodes[0];
                    _elmSmallArrowImg = (Image)_elmSmall.LastChild.LastChild.ChildNodes[0];
                    break;
                case "Thin":
                    _elmThin = elm;
                    _elmThinArrowImgCont = (Span)elm.FirstChild;
                    _elmThinArrowImg = (Image)_elmThinArrowImgCont.FirstChild;
                    break;
            }
        }

        internal override string ControlType
        {
            get
            {
                return "FlyoutAnchor";
            }
        }

        /// <summary>
        /// Creates a DOMElement for a menu display mode
        /// </summary>
        /// <param name="displayMode">The display mode to create</param>
        /// <param name="cssClass">The CSS Class to wrap this element in</param>
        /// <param name="alt">The alt text</param>
        /// <param name="icon">The path to the icon image (if applicable)</param>
        /// <param name="iconclass">The CSS class to apply to the icon</param>
        /// <returns>A DOMElement for this menu display mode of this control</returns>
        protected Anchor CreateMenuDOMElement(
            string displayMode,
            string cssClass,
            string alt,
            string icon,
            string iconclass,
            string icontop,
            string iconleft)
        {
            // Create DOM Elements
            Anchor elm = Utility.CreateNoOpLink();
            elm.ClassName = cssClass;
            elm.Title = alt;
            elm.SetAttribute("mscui:controltype", ControlType);
            Utility.SetAriaTooltipProperties(this.Properties, elm);
            Span elmIcon = null;

            // Create icons if necessary
            switch (displayMode)
            {
                case "Menu16":
                    {
                        if (CUIUtility.IsNullOrUndefined(_elmMenuImg16))
                        {
                            _elmMenuImg16 = new Image();
                            _elmMenuImg16Cont = Utility.CreateClusteredImageContainerNew(
                                                                            ImgContainerSize.Size16by16,
                                                                            icon,
                                                                            iconclass,
                                                                            _elmMenuImg16,
                                                                            true,
                                                                            true,
                                                                            icontop,
                                                                            iconleft);

                            _elmMenuImg16.Alt = alt;
                            elmIcon = _elmMenuImg16Cont;
                        }
                        break;
                    }
                case "Menu32":
                    {
                        if (CUIUtility.IsNullOrUndefined(_elmMenuImg32))
                        {
                            _elmMenuImg32 = new Image();
                            _elmMenuImg32Cont = Utility.CreateClusteredImageContainerNew(
                                                                            ImgContainerSize.Size32by32,
                                                                            icon,
                                                                            iconclass,
                                                                            _elmMenuImg32,
                                                                            true,
                                                                            true,
                                                                            icontop,
                                                                            iconleft);

                            _elmMenuImg32.Alt = alt;
                            elmIcon = _elmMenuImg32Cont;
                        }
                        break;
                    }
            }

            // Label
            CreateMenuLabelDOMElementIfNeeded(displayMode);

            // Arrow image
            if (CUIUtility.IsNullOrUndefined(_elmMenuArrowImg))
            {
                _elmMenuArrowImg = new Image();
                _elmMenuArrowImgCont = Utility.CreateClusteredImageContainerNew(
                                                                       ImgContainerSize.Size13by13,
                                                                       Root.Properties.ImageSideArrow,
                                                                       Root.Properties.ImageSideArrowClass,
                                                                       _elmMenuArrowImg,
                                                                       false,
                                                                       true,
                                                                       Root.Properties.ImageSideArrowTop,
                                                                       Root.Properties.ImageSideArrowLeft
                                                                       );

                Utility.EnsureCSSClassOnElement(_elmMenuArrowImgCont, "ms-cui-fa-menu-arrow");
            }

            // Build DOM Structure
            if (elmIcon != null)
            {
                Span elmIconContainer = new Span();
                elmIconContainer.ClassName = "ms-cui-ctl-iconContainer";
                elmIconContainer.AppendChild(elmIcon);
                elm.AppendChild(elmIconContainer);
            }

            elm.AppendChild(_elmMenuLbl);
            elm.AppendChild(_elmMenuArrowImgCont);

            return elm;
        }

        /// <summary>
        /// Creates the Label element for menus if it is not yet created
        /// </summary>
        /// <owner alias="JKern" />
        protected void CreateMenuLabelDOMElementIfNeeded(string displayMode)
        {
            if (CUIUtility.IsNullOrUndefined(_elmMenuLbl))
            {
                _elmMenuLbl = new Span();
                _elmMenuLbl.ClassName = "ms-cui-ctl-mediumlabel";
                UIUtility.SetInnerText(_elmMenuLbl, Properties.LabelText);
            }
        }

        /// <summary>
        /// Attaches events to the DOMElements for the given display mode
        /// </summary>
        /// <param name="displayMode">The display mode to attach events for</param>
        /// <owner alias="JKern" />
        internal override void AttachEventsForDisplayMode(string displayMode)
        {
            Anchor elm = GetDisplayedDOMElement(displayMode);

            if (CUIUtility.IsNullOrUndefined(elm))
                return;

            elm.Click += OnClick;
            elm.Blur += OnBlur;
            elm.KeyPress += OnKeyPress;
            elm.Focus += OnTabFocus;
            if (BrowserUtility.InternetExplorer)
            {
                if (displayMode.StartsWith("Menu"))
                {
                    elm.MouseEnter += OnMouseenter;
                    elm.MouseLeave += OnMouseleave;
                }
                else
                {
                    elm.MouseEnter += OnFocus;
                    elm.MouseLeave += OnBlur;
                }
            }
            else
            {
                if (displayMode.StartsWith("Menu"))
                {
                    elm.MouseOver += OnMouseover;
                    elm.MouseOut += OnMouseout;
                }
                else
                {
                    elm.MouseOver += OnFocus;
                    elm.MouseOut += OnBlur;
                }
            }
        }

        protected override void ReleaseEventHandlers()
        {
            if (!CUIUtility.IsNullOrUndefined(_elmMenu))
                RemoveEvents(_elmMenu, true);
            if (!CUIUtility.IsNullOrUndefined(_elmMenu16))
                RemoveEvents(_elmMenu16, true);
            if (!CUIUtility.IsNullOrUndefined(_elmMenu32))
                RemoveEvents(_elmMenu32, true);
            if (!CUIUtility.IsNullOrUndefined(_elmLarge))
                RemoveEvents(_elmLarge, false);
            if (!CUIUtility.IsNullOrUndefined(_elmMedium))
                RemoveEvents(_elmMedium, false);
            if (!CUIUtility.IsNullOrUndefined(_elmSmall))
                RemoveEvents(_elmSmall, false);
            if (!CUIUtility.IsNullOrUndefined(_elmThin))
                RemoveEvents(_elmThin, false);
        }

        private void RemoveEvents(HtmlElement elm, bool isMenu)
        {
            elm.Click -= OnClick;
            elm.Blur -= OnBlur;
            elm.KeyPress -= OnKeyPress;
            elm.Focus -= OnTabFocus;

            if (BrowserUtility.InternetExplorer)
            {
                if (isMenu)
                {
                    elm.MouseEnter -= OnMouseenter;
                    elm.MouseLeave -= OnMouseleave;
                }
                else
                {
                    elm.MouseEnter -= OnFocus;
                    elm.MouseLeave -= OnBlur;
                }
            }
            else
            {
                if (isMenu)
                {
                    elm.MouseOut -= OnMouseout;
                    elm.MouseOver -= OnMouseover;
                }
                else
                {
                    elm.MouseOut -= OnBlur;
                    elm.MouseOver -= OnFocus;
                }
            }
        }

        /// <summary>
        /// Changes the look of this control's DOMElements when the enabled state changes
        /// </summary>
        /// <param name="enabled">Whether this control is enabled</param>
        /// <owner alias="JKern" />
        public override void OnEnabledChanged(bool enabled)
        {
            Utility.SetEnabledOnElement(_elmMenu, enabled);
            Utility.SetEnabledOnElement(_elmMenu16, enabled);
            Utility.SetEnabledOnElement(_elmMenu32, enabled);
            Utility.SetEnabledOnElement(_elmLarge, enabled);
            Utility.SetEnabledOnElement(_elmMedium, enabled);
            Utility.SetEnabledOnElement(_elmSmall, enabled);
            Utility.SetEnabledOnElement(_elmThin, enabled);
        }

        #region Menu Positioning
        /// <summary>
        /// Specialized menu positioning method. If this MenuLauncher is in a Menu display mode, use flyout-style sideways menu positioning
        /// </summary>
        /// <param name="menu">The DOMElement of the menu to be launched</param>
        /// <param name="launcher">The DOMElement of the launcher</param>
        /// <owner alias="JKern" />
        protected override void PositionMenu(HtmlElement menu, HtmlElement launcher)
        {
            // If not in a menu, use the standard menu positioning logic
            if (DisplayedComponent.DisplayMode.StartsWith("Menu"))
            {
                // If we're in a menu, we want the position the flyout horizontally
                Root.PositionFlyOutHorizontal(menu, launcher);
            }
            else
            {
                // If not in a menu, use the standard menu positioning logic
                base.PositionMenu(menu, launcher);
            }
        }
        #endregion

        /// <summary>
        /// Creates the ControlComponent for the given display mode
        /// </summary>
        /// <param name="displayMode">The display mode to create the Component for</param>
        /// <returns>A ControlComponent for the given display mode</returns>
        /// <owner alias="JKern" />
        protected override ControlComponent CreateComponentForDisplayModeInternal(string displayMode)
        {
            ControlComponent comp;
            if (displayMode.StartsWith("Menu"))
            {
                comp = this.Root.CreateMenuItem(
                    Id + "-" + displayMode + Root.GetUniqueNumber(),
                    displayMode,
                    this);
            }
            else
            {
                comp = base.CreateComponentForDisplayModeInternal(displayMode);
            }
            return comp;
        }

        /// <summary>
        /// Returns the outer DOM element of the given display mode
        /// </summary>
        /// <param name="displayMode">The display mode to get the DOMElement for</param>
        /// <owner alias="JKern" />
        protected Anchor GetDisplayedDOMElement(string displayMode)
        {
            switch (displayMode)
            {
                case "Menu":
                    return _elmMenu;
                case "Menu16":
                    return _elmMenu16;
                case "Menu32":
                    return _elmMenu32;
                case "Large":
                    return _elmLarge;
                case "Medium":
                    return _elmMedium;
                case "Small":
                    return _elmSmall;
                case "Thin":
                    return _elmThin;
                default:
                    EnsureValidDisplayMode(displayMode);
                    return null;
            }
        }

        protected override void OnClick(HtmlEvent evt)
        {
            bool enabled = Enabled;
#if PERF_METRICS
            if (enabled && !MenuLaunched)
                PMetrics.PerfMark(PMarker.perfCUIFlyoutAnchorOnClickStart);
#endif
            CloseToolTip();
            Utility.CancelEventUtility(evt, false, true);
            if (!enabled || MenuLaunched)
                return;

            Root.LastCommittedControl = this;
            ControlComponent comp = DisplayedComponent;
            Anchor elm = (Anchor)comp.ElementInternal;
            LaunchMenuInternal(elm);
            if (!string.IsNullOrEmpty(Properties.Command))
            {
                comp.RaiseCommandEvent(Properties.Command,
                                       CommandType.MenuCreation,
                                       null);
            }
#if PERF_METRICS
            PMetrics.PerfMark(PMarker.perfCUIFlyoutAnchorOnClickEnd);
#endif
        }

        private void OnFocus(HtmlEvent args)
        {
            OnBeginFocus();
            if (!Enabled)
                return;
        }

        private void OnMouseenter(HtmlEvent args)
        {
            OnBeginFocus();
            if (!Enabled || MenuLaunched)
                return;

            ControlComponent comp = DisplayedComponent;
            Anchor elm = (Anchor)comp.ElementInternal;
            LaunchMenuInternal(elm);

            string command = Properties.Command;
            if (!string.IsNullOrEmpty(command))
            {
                comp.RaiseCommandEvent(command,
                                       CommandType.MenuCreation,
                                       null);
            }
        }

        private void OnMouseover(HtmlEvent args)
        {
            OnBeginFocus();
            if (!Enabled || MenuLaunched)
                return;

            HtmlElement target = args.TargetElement;
            HtmlElement relatedTarget = args.RelatedTarget;

            // Check if mouseover is to this element or not
            if (!(target == _elmMenu || target == _elmMenu16 || target == _elmMenu32))
                return;

            while (!CUIUtility.IsNullOrUndefined(relatedTarget) && relatedTarget != target)
            {
                try
                {
                    if (relatedTarget.NodeName.ToLower() == "body")
                    {
                        break;
                    }
                }
                catch
                {
                    // Firefox will sometimes start trying to iterate its own chrome nodes such as
                    // the scrollbar which causes an access denied exception. If we get here, there's
                    // nothing we can do, so just break out of the loop
                    break;
                }

                relatedTarget = (HtmlElement)relatedTarget.ParentNode;
            }

            // Still moused over the flyout anchor, don't handle event
            if (relatedTarget == target)
                return;

            ControlComponent comp = DisplayedComponent;
            Anchor elm = (Anchor)comp.ElementInternal;
            LaunchMenuInternal(elm);

            string command = Properties.Command;
            if (!string.IsNullOrEmpty(command))
            {
                comp.RaiseCommandEvent(command,
                                       CommandType.MenuCreation,
                                       null);
            }
        }

        private void OnMenuMouseover(HtmlEvent args)
        {
            int mlIndex = Root.MenuLauncherStack.IndexOf(this);
            int pendingMenuCloseTimeoutId = Root.PendingMenuCloseTimeoutId;

            if (pendingMenuCloseTimeoutId != -1 &&
                mlIndex >= Root.PendingMenuCloseMenuLauncherStackIndex)
            {
                Browser.Window.ClearTimeout(pendingMenuCloseTimeoutId);
                Root.PendingMenuCloseTimeoutId = -1;
                Root.PendingMenuCloseMenuLauncherStackIndex = -1;
            }
        }

        private void OnTabFocus(HtmlEvent args)
        {
            OnBeginFocus();
            if (DisplayedComponent.DisplayMode.StartsWith("Menu"))
                Highlight(Enabled);
            if (Enabled)
                Root.LastFocusedControl = this;
        }

        private void OnBlur(HtmlEvent args)
        {
            OnEndFocus();
            if (MenuLaunched)
                return;
            RemoveHighlight();
        }

        private void OnMouseleave(HtmlEvent args)
        {
            OnEndFocus();
            if (!Enabled)
                return;

            if (MenuLaunched)
            {
                // If mouse is over any menu that is nested in this one, don't close
                int mlIndex = Root.MenuLauncherStack.IndexOf(this);
                for (int i = mlIndex; i < Root.MenuLauncherStack.Count; i++)
                {
                    if (Utility.IsDescendantOf(((MenuLauncher)Root.MenuLauncherStack[i]).Menu.ElementInternal, args.ToElement))
                        return;
                }

                SetCloseMenuStackTimeout();
            }
        }

        private void OnMenuMouseleave(HtmlEvent args)
        {
            OnEndFocus();
            if (Utility.IsDescendantOf(DisplayedComponent.ElementInternal, args.ToElement))
                return;

            if (MenuLaunched)
            {
                // If mouse is over any menu that is nested in this one, don't close
                int mlIndex = Root.MenuLauncherStack.IndexOf(this);
                for (int i = mlIndex; i < Root.MenuLauncherStack.Count; i++)
                {
                    if (Utility.IsDescendantOf(((MenuLauncher)Root.MenuLauncherStack[i]).Menu.ElementInternal, args.ToElement))
                        return;
                }

                SetCloseMenuStackTimeout();
            }
        }

        private void OnMouseout(HtmlEvent args)
        {
            OnEndFocus();
            if (!Enabled || Utility.IsDescendantOf(DisplayedComponent.ElementInternal, args.RelatedTarget))
                return;

            if (MenuLaunched)
            {
                // If mouse is over any menu that is nested in this one, don't close
                int mlIndex = Root.MenuLauncherStack.IndexOf(this);
                for (int i = mlIndex; i < Root.MenuLauncherStack.Count; i++)
                {
                    if (Utility.IsDescendantOf(Root.MenuLauncherStack[i].Menu.ElementInternal, args.RelatedTarget))
                        return;
                }

                SetCloseMenuStackTimeout();
            }
        }

        private void OnMenuMouseout(HtmlEvent args)
        {
            OnEndFocus();
            if (Utility.IsDescendantOf(DisplayedComponent.ElementInternal, args.RelatedTarget))
                return;

            if (MenuLaunched)
            {
                // If mouse is over any menu that is nested in this one, don't close
                int mlIndex = Root.MenuLauncherStack.IndexOf(this);
                for (int i = mlIndex; i < Root.MenuLauncherStack.Count; i++)
                {
                    if (Utility.IsDescendantOf(Root.MenuLauncherStack[i].Menu.ElementInternal, args.RelatedTarget))
                        return;
                }

                SetCloseMenuStackTimeout();
            }

            RemoveHighlight();
        }

        private void OnKeyPress(HtmlEvent args)
        {
            CloseToolTip();
            if (!Enabled)
                return;
            int key = args.KeyCode;

            if (MenuLaunched)
            {
                if ((Root.TextDirection == Direction.LTR && key == (int)Key.Right) ||
                        (Root.TextDirection == Direction.RTL && key == (int)Key.Left))
                    Menu.FocusOnFirstItem(args);
            }
            else
            {
                if (key == (int)Key.Enter || key == (int)Key.Space ||
                    (((Root.TextDirection == Direction.LTR && key == (int)Key.Right) ||
                    (Root.TextDirection == Direction.RTL && key == (int)Key.Left)) &&
                    (!args.CtrlKey || !args.ShiftKey)))
                {
                    LaunchedByKeyboard = true;
                    ControlComponent comp = DisplayedComponent;
                    Anchor elm = (Anchor)comp.ElementInternal;
                    string command = Properties.Command;
                    if (!string.IsNullOrEmpty(command))
                    {
                        comp.RaiseCommandEvent(command,
                                               CommandType.MenuCreation,
                                               null);
                    }
                    LaunchMenuInternal(elm);
                }
            }
        }

        bool _focusSet = false;
        public override void OnModalKeyPress(HtmlEvent args)
        {
            if (!CUIUtility.IsNullOrUndefined(args))
            {
                if ((((Root.TextDirection == Direction.LTR && args.KeyCode == (int)Key.Left) ||
                    (Root.TextDirection == Direction.RTL && args.KeyCode == (int)Key.Right)) &&
                    (DisplayedComponent.DisplayMode).StartsWith("Menu")) || args.KeyCode == (int)Key.Esc)
                {
                    Root.CloseMenuStack(this);
                    return;
                }
            }

            if (IsGroupPopup)
            {
                if (_focusSet)
                    return;
                if (Menu.SetFocusOnFirstControl())
                    _focusSet = true;

                Utility.CancelEventUtility(args, false, true);
            }
            else
            {
                base.OnModalKeyPress(args);
            }
        }

        // No matter where we are in the stack, a click anywhere other than on a menu should close all menus
        public override void OnModalBodyClick(HtmlEvent args)
        {
            Root.CloseAllMenus();
        }

        protected override void OnLaunchedMenuClosed()
        {
            int pendingMenuCloseTimeoutId = Root.PendingMenuCloseTimeoutId;
            if (pendingMenuCloseTimeoutId != -1)
                Browser.Window.ClearTimeout(pendingMenuCloseTimeoutId);
            Root.PendingMenuCloseTimeoutId = -1;
            Root.PendingMenuCloseMenuLauncherStackIndex = -1;

            RemoveHighlight();
            CloseToolTip();

            ControlComponent comp = DisplayedComponent;
            if (comp.DisplayMode.StartsWith("Menu"))
            {
                // We know that if we're in a Menu, the structure must be Menu > MenuSection > MenuItem
                // and the DisplayedComponent must be the MenuItem
                Menu parentMenu = (Menu)comp.Parent.Parent;
                parentMenu.OpenSubMenuLauncher = null;
            }

            // If Properties.CommandMenuClose is not set, Root will not send the command to the root user,
            // so this won't hit page components. It is important that we do this though so that bugs like
            // O14:653413 are mitigated.
            comp.RaiseCommandEvent(Properties.CommandMenuClose,
                                   CommandType.MenuClose,
                                   null);

            base.OnLaunchedMenuClosed();
        }

        private void SetCloseMenuStackTimeout()
        {
            int pendingMenuCloseTimeoutId = Root.PendingMenuCloseTimeoutId;
            if (pendingMenuCloseTimeoutId != -1)
                Browser.Window.ClearTimeout(pendingMenuCloseTimeoutId);

            Root.PendingMenuCloseMenuLauncherStackIndex = Root.MenuLauncherStack.IndexOf(this);
            Root.PendingMenuCloseTimeoutId = Browser.Window.SetTimeout(new Action(CloseMenuStack), 500);
        }

        private void CloseMenuStack()
        {
            Root.CloseMenuStack(this);
            Root.PendingMenuCloseTimeoutId = -1;
            Root.PendingMenuCloseMenuLauncherStackIndex = -1;
        }

        private void LaunchMenuInternal(Anchor launcher)
        {
            CloseToolTip();
            Highlight(true);
            Root.FixedPositioningEnabled = false;
            _focusSet = false;

            ControlComponent comp = DisplayedComponent;
            bool isInMenu = comp.DisplayMode.StartsWith("Menu");

            if (isInMenu)
            {
                // We know that if we're in a Menu, the structure must be Menu > MenuSection > MenuItem
                // and the DisplayedComponent must be the MenuItem
                Menu parentMenu = (Menu)comp.Parent.Parent;
                parentMenu.OpenSubMenuLauncher = this;
            }

            LaunchMenu(launcher);

            if (!isInMenu)
            {
                return;

            }
            Menu.ElementInternal.MouseOver += OnMenuMouseover;

            if (BrowserUtility.InternetExplorer)
            {
                Menu.ElementInternal.MouseLeave += OnMenuMouseleave;
            }
            else
            {
                Menu.ElementInternal.MouseOut += OnMenuMouseout;
            }
        }

        private void RemoveHighlight()
        {
            const string disabledCssClass = "ms-cui-ctl-disabledHoveredOver";
            const string hoverCss = "ms-cui-ctl-hoveredOver";

            Utility.RemoveCSSClassFromElement(_elmMenu, hoverCss);
            Utility.RemoveCSSClassFromElement(_elmMenu16, hoverCss);
            Utility.RemoveCSSClassFromElement(_elmMenu32, hoverCss);
            Utility.RemoveCSSClassFromElement(_elmLarge, hoverCss);
            Utility.RemoveCSSClassFromElement(_elmMedium, hoverCss);
            Utility.RemoveCSSClassFromElement(_elmSmall, hoverCss);
            Utility.RemoveCSSClassFromElement(_elmThin, hoverCss);

            Utility.RemoveCSSClassFromElement(_elmMenu, disabledCssClass);
            Utility.RemoveCSSClassFromElement(_elmMenu16, disabledCssClass);
            Utility.RemoveCSSClassFromElement(_elmMenu32, disabledCssClass);
        }

        private void Highlight(bool enabled)
        {
            string hoverCss = "ms-cui-ctl-hoveredOver";

            if (!enabled)
            {
                hoverCss = "ms-cui-ctl-disabledHoveredOver";
                Utility.EnsureCSSClassOnElement(_elmMenu, hoverCss);
                Utility.EnsureCSSClassOnElement(_elmMenu16, hoverCss);
                Utility.EnsureCSSClassOnElement(_elmMenu32, hoverCss);
            }
            else
            {
                Utility.EnsureCSSClassOnElement(_elmMenu, hoverCss);
                Utility.EnsureCSSClassOnElement(_elmMenu16, hoverCss);
                Utility.EnsureCSSClassOnElement(_elmMenu32, hoverCss);
                Utility.EnsureCSSClassOnElement(_elmLarge, hoverCss);
                Utility.EnsureCSSClassOnElement(_elmMedium, hoverCss);
                Utility.EnsureCSSClassOnElement(_elmSmall, hoverCss);
                Utility.EnsureCSSClassOnElement(_elmThin, hoverCss);
            }
        }

        bool _isGroupPopup = false;
        internal bool IsGroupPopup
        {
            get
            {
                return _isGroupPopup;
            }
            set
            {
                _isGroupPopup = value;
            }
        }

        #region Implementation of IMenuItem
        public override string GetTextValue()
        {
            return Properties.LabelText;
        }

        internal override bool SetFocusOnControl()
        {
            if (!Enabled)
                return false;
            ReceiveFocus();
            return true;
        }

        public override void ReceiveFocus()
        {
            Anchor elm = (Anchor)DisplayedComponent.ElementInternal;
            if (!CUIUtility.IsNullOrUndefined(elm))
                elm.PerformFocus();
        }

        public override void OnMenuClosed()
        {
            RemoveHighlight();
        }
        #endregion

        public override void Dispose()
        {
            base.Dispose();
            _elmThin = null;
            _elmThinArrowImg = null;
            _elmThinArrowImgCont = null;
            _elmLarge = null;
            _elmLargeArrowImg = null;
            _elmLargeImg = null;
            _elmMedium = null;
            _elmMediumArrowImg = null;
            _elmMediumImg = null;
            _elmMenu = null;
            _elmMenu16 = null;
            _elmMenu32 = null;
            _elmMenuArrowImg = null;
            _elmMenuArrowImgCont = null;
            _elmMenuImg16 = null;
            _elmMenuImg16Cont = null;
            _elmMenuImg32 = null;
            _elmMenuImg32Cont = null;
            _elmMenuLbl = null;
            _elmSmall = null;
            _elmSmallArrowImg = null;
            _elmSmallImg = null;
        }

        private FlyoutAnchorProperties Properties
        {
            get
            {
                return (FlyoutAnchorProperties)base.ControlProperties;
            }
        }
    }
}
