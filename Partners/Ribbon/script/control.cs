using System;
using System.Collections.Generic;
using Microsoft.LiveLabs.Html;
using Microsoft.LiveLabs.JavaScript;
using Microsoft.LiveLabs.JavaScript.Interop;
using Ribbon.Controls;

using SPLabel = Microsoft.LiveLabs.Html.Label;

namespace Ribbon
{
    [Import(MemberNameCasing = Casing.Exact)]
    [Interop(State = InstanceState.JavaScriptOnly)]
    public class ControlProperties
    {
        extern public ControlProperties();
        extern public string Command { get; set; }
        extern public string Id { get; }
        extern public string TemplateAlias { get; }
        extern public string ToolTipDescription { get; }
        extern public string ToolTipHelpKeyWord { get; }
        extern public string ToolTipImage32by32 { get; }
        extern public string ToolTipImage32by32Class { get; }
        extern public string ToolTipImage32by32Top { get; }
        extern public string ToolTipImage32by32Left { get; }
        extern public string ToolTipSelectedItemTitle { get; set; }
        extern public string ToolTipShortcutKey { get; }
        extern public string ToolTipTitle { get; }
        extern public string LabelCss { get; }
    }


    /// <summary>
    /// Constant keys for DOM elements in various controls
    /// </summary>
    /// <owner alias="JKern" />
    internal static class ControlElements
    {
        public const string Elm = "elm";
        public const string ElmImg = "elmImg";
        public const string ElmImgCont = "elmImgCont";
        public const string ElmLbl = "elmLbl";
        public const string ElmArrowImg = "elmArrowImg";
        public const string ElmArrowImgCont = "elmArrowImgCont";
    }

    /// <summary>
    /// An interface for controls that may be selected within a Menu
    /// </summary>
    internal interface ISelectableControl
    {
        /// <summary>
        /// Retrieves the DOM elememnt to be displayed in an DropDown object
        /// </summary>
        /// <param name="displayMode">
        /// The display mode of the DropDown object
        /// </param>
        /// <returns></returns>
        HtmlElement GetDropDownDOMElementForDisplayMode(string displayMode);

        /// <summary>
        /// Deselects the control when used in a Menu
        /// </summary>
        void Deselect();

        /// <summary>
        /// Gets the unique Menu Item Id for this control
        /// </summary>
        /// <returns>
        /// The menu item id of this control
        /// </returns>
        string GetMenuItemId();

        /// <summary>
        /// Gets the Command Value Id for this control
        /// </summary>
        /// <returns>
        /// the command value id of this control
        /// </returns>
        string GetCommandValueId();

        /// <summary>
        /// Gets the text value for this control
        /// </summary>
        /// <returns>
        /// the text value of this control
        /// </returns>
        string GetTextValue();

        /// <summary>
        /// Focuses on the anchor tag in the displayed component of this control
        /// </summary>
        void FocusOnDisplayedComponent();
    }


    /// <summary>
    /// Abstract class from which all Controls descend from.
    /// </summary>
    internal abstract class Control : IDisposable, IMenuItem
    {
        Root _root;
        string _id;

        // The ribbon components that represent this Control graphically
        // Hashed on their titles: ie "Small", "Large", "Medium" etc.
        List<ControlComponent> _components;

        // Parameters for this Control
        ControlProperties _properties;

        string _displayModes;
        // Cache DOM elements for display modes so that they can be reused
        Dictionary<string, HtmlElement> _cachedDOMElements;

        // The tool tip for this control (optional)
        ToolTip _toolTip;

        // indicator whether tooltip is open
        bool _toolTipLaunched = false;

        bool _enabled = false;
        bool _enabledHasBeenSet = false;

        /// <summary>
        /// Control constructor.
        /// </summary>
        /// <param name="ribbon">The Ribbon that this Control will be a part of.</param>
        /// <param name="id">Unique identifier for this Control.  ie: "fseaPaste"</param>
        /// <param name="prms">Dictionary of parameters to this Control.</param>
        protected Control(Root root, string id, ControlProperties properties)
        {
            _root = root;
            _id = id;
            _properties = properties;
            _components = new List<ControlComponent>();
            _displayModes = ",";
            _cachedDOMElements = new Dictionary<string, HtmlElement>();
            // TODO(shaozhu): Remove registerControl() after dynamic menu is
            // supported.
            root.RegisterControl(this);
        }


        /// <summary>
        /// The Dictionary of parameters that this Control has.
        /// </summary>
        public ControlProperties ControlProperties
        {
            get
            {
                return _properties;
            }
        }

        Dictionary<string, string> _stateProperties;
        protected Dictionary<string, string> StateProperties
        {
            get
            {
                if (CUIUtility.IsNullOrUndefined(_stateProperties))
                    _stateProperties = new Dictionary<string, string>();
                return _stateProperties;
            }
        }

        Dictionary<string, string> _commandProperties;
        protected Dictionary<string, string> CommandProperties
        {
            get
            {
                if (CUIUtility.IsNullOrUndefined(_commandProperties))
                    _commandProperties = new Dictionary<string, string>();
                return _commandProperties;
            }
        }


        public string TemplateAlias
        {
            get
            {
                return ControlProperties.TemplateAlias;
            }
        }

        internal virtual string ControlType
        {
            get
            {
                return "Control";
            }
        }

        internal virtual string AriaRole
        {
            get
            {
                return "button";
            }
        }

        internal virtual string AriaMenuRole
        {
            get
            {
                return "menu";
            }
        }

        /// <summary>
        /// Called to ensure that a display mode is valid for this Control.
        /// </summary>
        /// <param name="displayMode">The display mode to validate.</param>
        protected void EnsureValidDisplayMode(string displayMode)
        {
            if (_displayModes.IndexOf("," + displayMode + ",") != -1)
                return;

            throw new InvalidOperationException("The display mode with name: " + displayMode + " is not valid for this control with id: " + Id);
        }

        protected virtual void RefreshDOMElements()
        {
            // If this Control has state then call the OnStateChanged() method which will refresh
            // the Control's DOMElements based on the state.
            OnStateChanged();

            // DOMElements of Controls are created in the "Enabled" state
            // So, if we are in this method, it is highly likely that we just 
            // Scaled this group.  In this case, we need to set all components
            // underneath it to disabled if this group is disabled so that the
            // representations of the controls in the new scales will be propertly reflected
            // as disabled.  This could also be put at the bottom of Control.CreateDOMElementForDisplayMode()
            // If there are perf issues with this, we can try that.
            if (!Enabled)
                OnEnabledChanged(false);
        }

        protected virtual void OnStateChanged()
        {
        }

        /// <summary>
        /// Must be overriden in subclasses so that their DOMElements can be created.
        /// </summary>
        /// <param name="displayMode">The display mode whose DOMElement is getting created.</param>
        /// <returns>A DOMElement that represents that passed in display mode.</returns>
        protected abstract HtmlElement CreateDOMElementForDisplayMode(string displayMode);

        /// <summary>
        /// This is the display mode that is currently being created.
        /// This is used by controls like MRUSplitButton that need to set an initial
        /// item and in doing so, they need to know what display mode they are being 
        /// built in so that the initialItem from the menu can be build in that display
        /// mode too and be put into the control.
        /// </summary>
        private string _currentlyCreatedDisplayMode = null;
        internal string CurrentlyCreatedDisplayMode
        {
            get
            {
                return _currentlyCreatedDisplayMode;
            }
        }

        /// <summary>
        /// Returns the DOMElement for a particular display mode of this Control.  This will call CreateDOMElementForDisplayMode if the DOMElement has not already been created.
        /// </summary>
        /// <param name="displayMode">The display mode whose DOMElement should be retrieved.</param>
        /// <returns>a DOMElement representing the display mode that was passed in</returns>
        public HtmlElement GetDOMElementForDisplayMode(string displayMode)
        {
            EnsureValidDisplayMode(displayMode);

            // If this DOM element has already been created and is cached
            // then just return it.
            HtmlElement elm = null;
            if (_cachedDOMElements.ContainsKey(displayMode))
                elm = _cachedDOMElements[displayMode];
            if (!CUIUtility.IsNullOrUndefined(elm))
                return elm;

            _currentlyCreatedDisplayMode = displayMode;
            elm = CreateDOMElementForDisplayMode(displayMode);
            _currentlyCreatedDisplayMode = null;

            // we shouldn't override the id if CreateDOMElementForDisplayMode sets it
            if (string.IsNullOrEmpty(elm.Id))
                elm.Id = Id + "-" + displayMode;
            StoreElementForDisplayMode(elm, displayMode);

            RefreshDOMElements();

            return elm;
        }

        protected internal void StoreElementForDisplayMode(HtmlElement elm, string displayMode)
        {
            _cachedDOMElements[displayMode] = elm;
        }

        internal virtual void AttachDOMElementsForDisplayMode(string displayMode)
        {
            // REVIEW(josefl): Should we have a default implementation here or just have it empty?
            HtmlElement elm = Browser.Document.GetById(Id + "-" + displayMode);
            if (!CUIUtility.IsNullOrUndefined(elm))
                StoreElementForDisplayMode(elm, displayMode);
        }

        internal virtual void AttachEventsForDisplayMode(string displayMode)
        {
        }

        /// <summary>
        /// Creates a Component for the passed in display mode.
        /// </summary>
        /// <param name="displayMode">The display mode whose ControlComponent should be created.</param>
        /// <returns>The ControlComponent for the passed in display mode.</returns>
        public virtual ControlComponent CreateComponentForDisplayMode(string displayMode)
        {
            ControlComponent comp = CreateComponentForDisplayModeInternal(displayMode);
            Components.Add(comp);
            return comp;
        }

        protected List<ControlComponent> Components
        {
            get
            {
                return _components;
            }
        }

        protected virtual ControlComponent CreateComponentForDisplayModeInternal(string displayMode)
        {
            ControlComponent comp = _root.CreateControlComponent(
                            _id + "-" + displayMode + _root.GetUniqueNumber(),
                            displayMode,
                            this);
            return comp;
        }

        // Called up to cleanup anything that this Control needs to cleanup.
        public virtual void Dispose()
        {
            ReleaseEventHandlers();
            _root = null;
            _components = null;
            _displayModes = null;

            if (!CUIUtility.IsNullOrUndefined(_cachedDOMElements))
            {
                _cachedDOMElements.Clear();
                _cachedDOMElements = null;
            }

            if (!CUIUtility.IsNullOrUndefined(_toolTip))
                _toolTip.Dispose();
        }

        /// <summary>
        /// This releases all event handler of all DOM Elements that the display modes of this control
        /// have.
        /// </summary>
        protected virtual void ReleaseEventHandlers()
        {
        }

        /// <summary>
        /// The unique id for this instance of this control.  ie "fseaPaste".
        /// </summary>
        public string Id
        {
            get
            {
                return _id;
            }
        }

        /// <summary>
        /// The Ribbon that this Control's ControlComponent(s) are and will be a part of.
        /// </summary>
        public Root Root
        {
            get
            {
                return _root;
            }
        }

        /// <summary>
        /// Adds a display mode to the list of valid display modes for this Control.
        /// </summary>
        /// <param name="displayMode">The display mode that is to be added.</param>
        /// <seealso cref="EnsureValidDisplayMode"/>
        protected void AddDisplayMode(string displayMode)
        {
            // The display mode is already here
            if (_displayModes.IndexOf("," + displayMode + ",") != -1)
                return;

            _displayModes += displayMode + ",";
        }

        // Ensure that the child type and conditions is right for this child
        // Component to be added to this control's ControlComponent
        public virtual void EnsureCorrectChildType(Component child)
        {
            if (!(typeof(ToolTip).IsInstanceOfType(child)))
            {
                throw new InvalidOperationException("Child Components may not be added to this type of ControlComponent.");
            }
        }

        // TODO(josefl) Either make it be public or provide another way to 
        // raise command event.
        public ControlComponent DisplayedComponent
        {
            get
            {
                int l = _components.Count;
                for (int i = 0; i < l; i++)
                {
                    ControlComponent comp = (ControlComponent)_components[i];
                    if (comp.VisibleInDOM)
                        return comp;
                }
                return null;
            }
        }

        /// <summary>
        /// Called in the OnPreBubbleCommand() method of the ControlComponents that have come from this Control.
        /// </summary>
        /// <param name="command">The Command that is being bubbled.</param>
        /// <returns>true if the Command should continue to be bubbled and false to cancel the Command bubbling.</returns>
        internal virtual bool OnPreBubbleCommand(CommandEventArgs command)
        {
            return true;
        }

        /// Called in the OnPreBubbleCommand() method of the ControlComponents that have come from this Control.
        /// </summary>
        /// <param name="command">The Command that is being bubbled.</param>
        /// <returns>true if the Command should continue to be bubbled and false to cancel the Command bubbling.</returns>
        internal virtual void OnPostBubbleCommand(CommandEventArgs command)
        {
        }

        /// <summary>
        /// Should be overriden by subclasses.  Called by ControlComponents of this Control when their Enable value changes.
        /// </summary>
        /// <param name="enabled"></param>
        public abstract void OnEnabledChanged(bool enabled);

        public virtual void OnMenuClosed()
        {
        }

        protected virtual void OnClick(HtmlEvent evt)
        {
            Utility.CancelEventUtility(evt, true, true);
        }

        /// <summary>
        /// On IE, double clicking a button fires two events, a click and a dblclick.
        /// OnClick captures the first one, and then this method calls OnClick for the second event.
        /// A DomEventHandler must be added to a control to use this method
        /// </summary>
        /// <param name="evt"></param>
        protected virtual void OnDblClick(HtmlEvent evt)
        {
            Utility.CancelEventUtility(evt, false, true);
            if (!Enabled)
                return;

            // IE doesn't fire a second click event on double-click, so we need to fire one for it
            if (BrowserUtility.InternetExplorer)
                OnClick(evt);
        }
        #region ToolTip-related functions

        /// <summary>
        /// Starts timer that will launch tooltip if this control has focus for 500 ms.
        /// </summary>
        /// <owner alias="HillaryM" />
        public virtual void OnBeginFocus()
        {
            Browser.Window.ClearInterval(Root.TooltipLauncherTimer);
            // if there is currently an open tooltip and it was not launched by this control, close it
            if (!CUIUtility.IsNullOrUndefined(Root.TooltipLauncher))
            {
                if(Root.TooltipLauncher.Id == this.Id)
                {
                    // launch tooltip immediately (bug O14: 299699)
                    LaunchToolTip();
                    return;
                }
                else
                {
                    Root.CloseOpenTootips();
                    Root.TooltipLauncherTimer = Browser.Window.SetTimeout(new Action(LaunchToolTip), 500);
                }
            }
            else
            {
                Root.TooltipLauncherTimer = Browser.Window.SetTimeout(new Action(LaunchToolTip), 500);
            }
        }

        /// <summary>
        /// Stops the tooltip launching timer if it was started by the OnBeginFocus call. 
        /// Closes this control's tooltip if it was open.
        /// </summary>
        /// <owner alias="HillaryM" />
        public virtual void OnEndFocus()
        {
            Browser.Window.ClearInterval(Root.TooltipLauncherTimer);
            if (_toolTipLaunched)
            {
                Root.TooltipLauncherTimer = Browser.Window.SetTimeout(new Action(CloseToolTip), 100);
            }
        }

        /// <summary>
        /// Adds event handlers for the Help key (F2) press event. Also unregisters
        /// Help PageComponent.
        /// </summary>
        /// <owner alias="HillaryM" />
        protected void OnToolTipOpenned()
        {
            Browser.Document.Click += _toolTip.OnClick;
            Browser.Document.KeyDown += OnHelpKeyPress;
        }

        /// <summary>
        /// Removes event handlers for the Help key (F2) press event. Also unregisters
        /// Help PageComponent.
        /// </summary>
        /// <owner alias="HillaryM" />
        protected void OnToolTipClosed()
        {
            Browser.Document.Click -= _toolTip.OnClick;
            Browser.Document.KeyDown -= OnHelpKeyPress;
        }

        /// <summary>
        /// Handle the keypress of the help key (F1)
        /// </summary>
        /// <param name="evt"></param>
        /// <owner alias="HillaryM" />
        protected void OnHelpKeyPress(HtmlEvent args)
        {
            if (!CUIUtility.IsNullOrUndefined(_toolTip))
            {
                _toolTip.OnKeyPress(args);
            }
        }

        /// <summary>
        /// Launch this Control's ToolTip.
        /// </summary>        
        /// <owner alias="HillaryM" />
        protected void LaunchToolTip()
        {
            if (CUIUtility.IsNullOrUndefined(Root))
            {
                return;
            }

            // clear the tooltip launching timer
            Browser.Window.ClearInterval(Root.TooltipLauncherTimer);
            
            // If the tooltip is already launched, don't launch it twice            
            if (_toolTipLaunched)
                return;

            // if there is currently an open tooltip and it was not launched by this control, close it
            if ((!CUIUtility.IsNullOrUndefined(Root.TooltipLauncher)) &&
                    (Root.TooltipLauncher.Id != this.Id)) 
            {
                Root.CloseOpenTootips();
            }

            // If there is no ToolTip title, we don't launch the ToolTip
            if (string.IsNullOrEmpty(_properties.ToolTipTitle))
                return;

            _toolTip = new ToolTip(Root, Id + "_ToolTip", _properties.ToolTipTitle, _properties.ToolTipDescription, _properties);
                        
            if (!Enabled)
            {
                // Show message indicating that the control is disabled and give reason.               
                DisabledCommandInfoProperties disabledInfo = new DisabledCommandInfoProperties();
                disabledInfo.Icon = Root.Properties.ToolTipDisabledCommandImage16by16;
                disabledInfo.IconClass = Root.Properties.ToolTipDisabledCommandImage16by16Class;
                disabledInfo.IconTop = Root.Properties.ToolTipDisabledCommandImage16by16Top;
                disabledInfo.IconLeft = Root.Properties.ToolTipDisabledCommandImage16by16Left;
                disabledInfo.Title = Root.Properties.ToolTipDisabledCommandTitle;
                disabledInfo.Description = Root.Properties.ToolTipDisabledCommandDescription;
                disabledInfo.HelpKeyWord = Root.Properties.ToolTipDisabledCommandHelpKey;
                _toolTip.DisabledCommandInfo = disabledInfo;
            }

            ControlComponent comp = DisplayedComponent;

            if (!CUIUtility.IsNullOrUndefined(comp))
            {
                comp.EnsureChildren();
                comp.AddChild(_toolTip);
                _toolTip.Display();
                _toolTipLaunched = true;
                Root.TooltipLauncher = this;
                OnToolTipOpenned();
            }
            else
            {
                _toolTip = null;
            }
        }

        /// <summary>
        /// Close this Control's tool tip.
        /// </summary>
        /// <owner alias="HillaryM" />
        internal virtual void CloseToolTip()
        {
            if (!CUIUtility.IsNullOrUndefined(_root))
            {
                // clear launching timer
                Browser.Window.ClearInterval(_root.TooltipLauncherTimer);
            }
            
            if (!CUIUtility.IsNullOrUndefined(_toolTip))
            {
                _toolTip.Hide();

                _toolTipLaunched = false;
                OnToolTipClosed();
                
                // Remove the tooltip floating div from the DOM            
                UIUtility.RemoveNode(_toolTip.ElementInternal);
                _toolTip = null;
            }
        }
        #endregion

        /// <summary>
        /// Whether this Control is enabled or not.
        /// </summary>
        public bool Enabled
        {
            get
            {
                return _enabled;
            }
            set
            {
                // If the value is not changing and this is not the first time that
                // it is being set, then we don't need to do anything
                if (_enabled == value && _enabledHasBeenSet)
                    return;

                _enabled = value;
                _enabledHasBeenSet = true;
                OnEnabledChanged(value);
            }
        }

        protected bool EnabledInternal
        {
            get
            {
                return _enabled;
            }
            set
            {
                _enabled = value;
            }
        }

        internal void SetEnabledAndForceUpdate(bool enabled)
        {
            _enabled = enabled;
            OnEnabledChanged(enabled);
        }

        internal virtual void PollForStateAndUpdate()
        {
            if (!string.IsNullOrEmpty(ControlProperties.Command))
                PollForStateAndUpdateInternal(ControlProperties.Command, string.Empty, null, false);
        }

        protected bool PollForStateAndUpdateInternal(string command,
                                                     string queryCommand,
                                                     Dictionary<string, string> properties,
                                                     bool alwaysExecuteQueryCommand)
        {
            bool succeeded = (Root.PollForCommandStateCore(command,
                                                           queryCommand,
                                                           properties,
                                                           alwaysExecuteQueryCommand) & 1) > 0;
            Enabled = succeeded;
            return succeeded;
        }

        // IMenuItem interface
        public virtual string GetTextValue()
        {
            return "";
        }

        public virtual void ReceiveFocus()
        {
        }

        internal virtual bool FocusNext(HtmlEvent evt)
        {
            return FocusInternal();
        }

        internal virtual bool FocusPrevious(HtmlEvent evt)
        {
            return FocusInternal();
        }

        private bool FocusInternal()
        {
            ControlComponent comp = this.DisplayedComponent;

            // We currently do not support this for non-menuitems.
            if (!typeof(MenuItem).IsInstanceOfType(comp))
            {
                return false;

            }
            // REVIEW(josefl): Consider moving knowledge about whether this is focused
            // or not into the Control.  For now, we still get it from the MenuItem Component.
            if (!((MenuItem)comp).Focused)
            {
                ((IMenuItem)this).ReceiveFocus();
                return true;
            }

            return false;
        }

        internal virtual void CommitCurrentStateToApplication()
        {
        }

        static protected Anchor CreateStandardControlDOMElement(
            Control control,
            Root root,
            string displayMode,
            ControlProperties properties,
            bool menu,
            bool arrow)
        {
            // We cast the properties to FlyoutAnchor properties because it has the superset
            // of all the possible properties that we will want to use.
            JSObject tempProps = JSObject.From<ControlProperties>(properties);
            FlyoutAnchorProperties props = tempProps.To<FlyoutAnchorProperties>();
            return CreateStandardControlDOMElementCore(
                control,
                root,
                displayMode,
                props.Id,
                props.Image32by32,
                props.Image32by32Class,
                props.Image32by32Top,
                props.Image32by32Left,
                props.Image16by16,
                props.Image16by16Class,
                props.Image16by16Top,
                props.Image16by16Left,
                props.LabelText,
                props.LabelCss,
                props.Alt,
                props.Description,
                props.ToolTipTitle,
                menu,
                arrow);
        }

        static protected Anchor CreateStandardControlDOMElementCore(
            Control control,
            Root root,
            string displayMode,
            string id,
            string image32by32,
            string image32by32Class,
            string image32by32Top,
            string image32by32Left,
            string image16by16,
            string image16by16Class,
            string image16by16Top,
            string image16by16Left,
            string labelText,
            string labelCss,
            string alt,
            string description,
            string tooltipTitle,
            bool menu,
            bool arrow)
        {
            // O14:503464 - Missing label text should be empty instead of "undefined"
            if (string.IsNullOrEmpty(labelText))
                labelText = "";

            bool isMenu = false;
            bool needsHiddenLabel = true;
            Anchor elm = Utility.CreateNoOpLink();

            string outerStyle = null;
            if (displayMode == "Large")
                outerStyle = "ms-cui-ctl-large";
            else if (displayMode == "Medium")
                outerStyle = "ms-cui-ctl-medium";
            else if (displayMode == "Menu16" || displayMode == "Menu")
            {
                outerStyle = "ms-cui-ctl-menu";
                isMenu = true;
            }
            else if (displayMode == "Menu32")
            {
                outerStyle = "ms-cui-ctl-menu ms-cui-ctl-menu32";
                isMenu = true;
            }
            else
                outerStyle = "ms-cui-ctl";

            Utility.EnsureCSSClassOnElement(elm, outerStyle);

            if (displayMode == "Menu")
                Utility.EnsureCSSClassOnElement(elm, "ms-cui-textmenuitem");

            if (!string.IsNullOrEmpty(tooltipTitle))
                elm.SetAttribute("aria-describedby", id + "_ToolTip");

            elm.SetAttribute("mscui:controltype", control.ControlType);

            // Create the image
            Image elmImage = new Image();

            string imageUrl = null;
            string imageClass = null;
            string imageTop = null;
            string imageLeft = null;
            ImgContainerSize imgSize = ImgContainerSize.None;

            elmImage.Alt = "";
            alt = string.IsNullOrEmpty(alt) ? labelText : alt;
            elm.SetAttribute("role", control.AriaRole);
            if (control is FlyoutAnchor)
                elm.SetAttribute("aria-haspopup", "true");

            if(string.IsNullOrEmpty(tooltipTitle)) 
            {
                elm.Title = alt;
                elmImage.Alt = alt;
                needsHiddenLabel = false;
            }

            if (displayMode == "Large" || displayMode == "Menu32")
            {
                imageUrl = image32by32;
                imageClass = image32by32Class;
                imageTop = image32by32Top;
                imageLeft = image32by32Left;
                imgSize = ImgContainerSize.Size32by32;
            }
            else
            {
                imageUrl = image16by16;
                imageClass = image16by16Class;
                imageTop = image16by16Top;
                imageLeft = image16by16Left;
                imgSize = ImgContainerSize.Size16by16;
            }

            Span elmImageCont = Utility.CreateClusteredImageContainerNew(
                                                                       imgSize,
                                                                       imageUrl,
                                                                       imageClass,
                                                                       elmImage,
                                                                       true,
                                                                       false,
                                                                       imageTop,
                                                                       imageLeft);
            Span elmIconContainer = new Span();
            elmIconContainer.ClassName = displayMode == "Large" ? "ms-cui-ctl-largeIconContainer" : "ms-cui-ctl-iconContainer";
            elmIconContainer.AppendChild(elmImageCont);

            // Create the label
            // The small display mode of controls does not have label text
            // However, controls with arrows like FlyoutAnchor still need
            // this element.
            SPLabel hiddenLabel = null;
            HtmlElement elmLabel = null;
            if (needsHiddenLabel)
            {
                hiddenLabel = Utility.CreateHiddenLabel(alt);

            }
            if (displayMode != "Small" || arrow)
            {
                elmLabel = new Span();
                if (displayMode != "Small")
                {
                    if (displayMode == "Large")
                    {
                        Utility.EnsureCSSClassOnElement(elmLabel, "ms-cui-ctl-largelabel");
                        elmLabel.InnerHtml = Utility.FixLargeControlText(labelText, arrow);
                    }
                    else
                    {
                        string text = labelText;
                        if (arrow)
                            text = text + " ";

                        Utility.EnsureCSSClassOnElement(elmLabel, "ms-cui-ctl-mediumlabel");
                        UIUtility.SetInnerText(elmLabel, text);
                    }
                    if (!string.IsNullOrEmpty(labelCss))
                    {
                        elmLabel.Style.CssText = labelCss;
                    }
                }
                else
                {
                    // If the displaymode is Small and there is an arrow
                    Utility.EnsureCSSClassOnElement(elmLabel, "ms-cui-ctl-smalllabel");
                    UIUtility.SetInnerText(elmLabel, " ");
                }
            }
            else if (needsHiddenLabel)
            {
                elmLabel = Utility.CreateHiddenLabel(alt);
            }

            // Create the arrow image if one was specified
            Span elmArrowCont = null;
            if (arrow)
            {
                Image elmArrowImage = new Image();
                elmArrowImage.Alt = "";
                if(string.IsNullOrEmpty(tooltipTitle)) 
                {
                    elmArrowImage.Alt = alt;
                }
                elmArrowCont = Utility.CreateClusteredImageContainerNew(
                                                                ImgContainerSize.Size5by3,
                                                                root.Properties.ImageDownArrow,
                                                                root.Properties.ImageDownArrowClass,
                                                                elmArrowImage,
                                                                true,
                                                                false,
                                                                root.Properties.ImageDownArrowTop,
                                                                root.Properties.ImageDownArrowLeft);
            }

            // This is used for Menu32.  It can have a description under the label text.
            Span elmTextContainer = null;
            Span elmDescriptionText = null;
            Span elmMenu32Clear = null;
            if (displayMode == "Menu32")
            {
                elmTextContainer = new Span();
                elmTextContainer.ClassName = "ms-cui-ctl-menulabel";
                Utility.EnsureCSSClassOnElement(elmLabel, "ms-cui-btn-title");
                elmTextContainer.AppendChild(elmLabel);
                if (!string.IsNullOrEmpty(description))
                {
                    elmDescriptionText = new Span();
                    Utility.EnsureCSSClassOnElement(elmDescriptionText, "ms-cui-btn-menu-description");
                    UIUtility.SetInnerText(elmDescriptionText, description);
                    elmDescriptionText.Style.Display = "block";
                    elmTextContainer.AppendChild(elmDescriptionText);
                }
                elmMenu32Clear = new Span();
                elmMenu32Clear.ClassName = "ms-cui-ctl-menu32clear";
                elmMenu32Clear.InnerHtml = "&nbsp;";
            }

            elm.AppendChild(elmIconContainer);

            if (!CUIUtility.IsNullOrUndefined(elmLabel))
            {
                if (!CUIUtility.IsNullOrUndefined(elmTextContainer))
                {
                    elm.AppendChild(elmTextContainer);
                    elm.AppendChild(elmMenu32Clear);
                }
                else
                {
                    elm.AppendChild(elmLabel);
                    if (displayMode == "Small" && arrow && needsHiddenLabel) // if no alt is present add a hidden label for MSAA
                        elm.AppendChild(hiddenLabel);
                }
                if (!CUIUtility.IsNullOrUndefined(elmArrowCont))
                    elmLabel.AppendChild(elmArrowCont);
            }

            if (isMenu)
            {
                Span elmMenuGlass = Utility.CreateGlassElement();
                elm.AppendChild(elmMenuGlass);
            }

            return elm;
        }

        static protected Span CreateTwoAnchorControlDOMElementCore(
            Control control,
            Root root,
            string displayMode,
            string id,
            string image32by32,
            string image32by32Class,
            string image32by32Top,
            string image32by32Left,
            string image16by16,
            string image16by16Class,
            string image16by16Top,
            string image16by16Left,
            string labelText,
            string alt,
            string tooltipTitle,
            bool arrow)
        {
            bool needsHiddenLabel = true;
            labelText = CUIUtility.SafeString(labelText);

            // Create the outer <span> element for this two anchor control
            Span elm = new Span();

            if (displayMode == "Large")
                elm.ClassName = "ms-cui-ctl-large";
            else if (displayMode == "Medium")
                elm.ClassName = "ms-cui-ctl ms-cui-ctl-medium";
            else
                elm.ClassName = "ms-cui-ctl ms-cui-ctl-small";

            if (!string.IsNullOrEmpty(tooltipTitle))
                elm.SetAttribute("aria-describedby", id + "_ToolTip");

            elm.SetAttribute("mscui:controltype", control.ControlType);

            Anchor elmA1 = Utility.CreateNoOpLink();
            Anchor elmA2 = Utility.CreateNoOpLink();

            elmA1.ClassName = "ms-cui-ctl-a1";
            elmA2.ClassName = "ms-cui-ctl-a2";
            alt = string.IsNullOrEmpty(alt) ? labelText : alt;

            // Setting aria properties for screen readers
            elmA1.SetAttribute("role", control.AriaRole);
            elmA2.SetAttribute("role", control.AriaRole);
            elmA2.SetAttribute("aria-haspopup", "true");

            Span elmA1Internal = new Span();
            elmA1Internal.ClassName = "ms-cui-ctl-a1Internal";

            // Create the image
            Image elmImage = new Image();
            string imageUrl = null;
            string imageClass = null;
            string imageTop = null;
            string imageLeft = null;
            ImgContainerSize imgSize = ImgContainerSize.None;
            elmImage.Alt = "";

            // Display alt only if no supertooltip is present
            if(string.IsNullOrEmpty(tooltipTitle)) 
            {
                elmA1.Title = alt;
                elmA2.Title = alt;
                elmImage.Alt = alt;
                needsHiddenLabel = false;
            }

            if (displayMode == "Large" || displayMode == "Menu32")
            {
                imageUrl = image32by32;
                imageClass = image32by32Class;
                imageTop = image32by32Top;
                imageLeft = image32by32Left;
                imgSize = ImgContainerSize.Size32by32;
            }
            else
            {
                imageUrl = image16by16;
                imageClass = image16by16Class;
                imageTop = image16by16Top;
                imageLeft = image16by16Left;
                imgSize = ImgContainerSize.Size16by16;
            }

            Span elmImageCont = Utility.CreateClusteredImageContainerNew(
                                                                       imgSize,
                                                                       imageUrl,
                                                                       imageClass,
                                                                       elmImage,
                                                                       true,
                                                                       false,
                                                                       imageTop,
                                                                       imageLeft);

            // Controls lacking a label and with supertooltips need hidden labels
            SPLabel elmHiddenBtnLabel = null;
            SPLabel elmHiddenArrowLabel = null;
            if (needsHiddenLabel)
            {
                elmHiddenBtnLabel = Utility.CreateHiddenLabel(alt);
                elmHiddenArrowLabel = Utility.CreateHiddenLabel(alt);
            }

            // Create the label
            // The small display mode of controls does not have label text
            // However, controls with arrows like FlyoutAnchor still need
            // this element.
            Span elmLabel = null;
            if (displayMode != "Small" || arrow)
            {
                elmLabel = new Span();
                if (displayMode != "Small")
                {
                    if (displayMode == "Large")
                    {
                        Utility.EnsureCSSClassOnElement(elmLabel, "ms-cui-ctl-largelabel");
                        elmLabel.InnerHtml = Utility.FixLargeControlText(labelText, arrow);
                    }
                    else if (displayMode == "Medium")
                    {
                        Utility.EnsureCSSClassOnElement(elmLabel, "ms-cui-ctl-mediumlabel");
                        UIUtility.SetInnerText(elmLabel, labelText);
                    }
                }
            }

            Span elmArrowCont = null;
            if (arrow)
            {
                Image elmArrowImage = new Image(); 
                if(string.IsNullOrEmpty(tooltipTitle)) 
                {
                    elmArrowImage.Alt = alt;
                }
                elmArrowCont = Utility.CreateClusteredImageContainerNew(
                                                                ImgContainerSize.Size5by3,
                                                                root.Properties.ImageDownArrow,
                                                                root.Properties.ImageDownArrowClass,
                                                                elmArrowImage,
                                                                true,
                                                                false,
                                                                root.Properties.ImageDownArrowTop,
                                                                root.Properties.ImageDownArrowLeft);
            }

            elm.AppendChild(elmA1);
            elm.AppendChild(elmA2);
            elmA1.AppendChild(elmA1Internal);
            elmA1Internal.AppendChild(elmImageCont);

            if (!CUIUtility.IsNullOrUndefined(elmLabel))
            {
                if (displayMode == "Large")
                {
                    elmA2.AppendChild(elmLabel);
                    if (needsHiddenLabel)
                        elmA1.AppendChild(elmHiddenBtnLabel);
                }
                else
                {
                    elmA1Internal.AppendChild(elmLabel);
                    if (needsHiddenLabel)
                        elmA2.AppendChild(elmHiddenArrowLabel);
                }

                if (displayMode == "Small" && needsHiddenLabel)
                {
                    elmA1.AppendChild(elmHiddenBtnLabel);
                }
            }
            if (!CUIUtility.IsNullOrUndefined(elmArrowCont))
            {
                if (displayMode == "Large")
                {
                    elmLabel.AppendChild(elmArrowCont);
                }
                else
                {
                    elmA2.AppendChild(elmArrowCont);
                }
            }

            return elm;
        }

        internal HtmlElement GetDisplayedComponentElement()
        {
            Component comp = DisplayedComponent;
            if (CUIUtility.IsNullOrUndefined(comp))
                return null;

            return comp.ElementInternal;
        }

        internal virtual bool SetFocusOnControl()
        {
            return false;
        }
    }
}
