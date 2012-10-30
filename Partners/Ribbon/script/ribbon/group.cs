using System;
using Microsoft.LiveLabs.Html;
using Microsoft.LiveLabs.JavaScript;
using Microsoft.LiveLabs.JavaScript.Interop;

using Ribbon.Controls;

namespace Ribbon
{
    [Import(MemberNameCasing = Casing.Exact)]
    [Interop(State = InstanceState.JavaScriptOnly)]
    public class GroupProperties
    {
        extern public GroupProperties();
        extern public string Image32by32Popup { get; }
        extern public string Image32by32PopupClass { get; }
        extern public string Image32by32PopupTop { get; }
        extern public string Image32by32PopupLeft { get; }
        extern public string PopupWidth { get; }
    }

    /// <summary>
    /// A Component that represents the "Chunk" or "Group" in the Ribbon.
    /// </summary>
    internal class Group : RibbonComponent
    {
        // DOM Elements that make up this group
        Span _elmBody;
        Span _elmTitle;
        Span _elmSeparator;
        Layout _selectedLayout;
        GroupProperties _properties;

        /// <summary>
        /// Creates a new Group.
        /// </summary>
        /// <param name="ribbon">The Ribbon that this Group is created by and is a part of.</param>
        /// <param name="id">The unique Component id of this Group.</param>
        /// <param name="title">The Title of this Group.</param>
        /// <param name="description">The Description of this Group.</param>
        internal Group(SPRibbon ribbon,
                       string id,
                       string title,
                       string description,
                       string command,
                       GroupProperties properties)
            : base(ribbon, id, title, description)
        {
            _command = command;
            _properties = properties;
        }

        GroupProperties Properties
        {
            get 
            { 
                return _properties; 
            }
        }

        internal override void RefreshInternal()
        {
            // TODO: possible perf implication.
            // We do not always have to remove the children.
            EnsureDOMElementAndEmpty();

            if (CUIUtility.IsNullOrUndefined(_elmTitle))
            {
                _elmTitle = new Span();
                _elmTitle.ClassName = "ms-cui-groupTitle";
            }
            else
            {
                _elmTitle = (Span)Utility.RemoveChildNodes(_elmTitle);
            }

            if (CUIUtility.IsNullOrUndefined(_elmBody))
            {
                _elmBody = new Span();
                _elmBody.ClassName = "ms-cui-groupBody";
            }
            else
            {
                _elmBody = (Span)Utility.RemoveChildNodes(_elmBody);
            }

            if (CUIUtility.IsNullOrUndefined(_elmSeparator))
            {
                _elmSeparator = new Span();
                _elmSeparator.ClassName = "ms-cui-groupSeparator";
            }

            // Refresh the text of the group name
            string title = Title;
            if (!string.IsNullOrEmpty(title))
            {
                UIUtility.SetInnerText(_elmTitle, title);

            }

            _elmTitle.Title = Title;

            if (!CUIUtility.IsNullOrUndefined(_selectedLayout) &&
                typeof(GroupPopupLayout).IsInstanceOfType(_selectedLayout))
            {
                _selectedLayout.EnsureDOMElement();
                ElementInternal.AppendChild(_selectedLayout.ElementInternal);
                ElementInternal.AppendChild(_elmSeparator);
                _selectedLayout.EnsureRefreshed();
            }
            else
            {
                Span elmContainer = new Span();
                elmContainer.ClassName = "ms-cui-groupContainer";

                elmContainer.AppendChild(_elmBody);
                elmContainer.AppendChild(_elmTitle);

                ElementInternal.AppendChild(elmContainer);
                ElementInternal.AppendChild(_elmSeparator);

                if (!CUIUtility.IsNullOrUndefined(_selectedLayout))
                {
                    _selectedLayout.EnsureDOMElement();
                    _elmBody.AppendChild(_selectedLayout.ElementInternal);
                    _selectedLayout.EnsureRefreshed();
                }
            }

            base.RefreshInternal();
        }

        internal override void AttachDOMElements()
        {
            base.AttachDOMElements();
            if (!CUIUtility.IsNullOrUndefined(_selectedLayout) &&
                !typeof(GroupPopupLayout).IsInstanceOfType(_selectedLayout))
            {
                // We are in the normal case and not in the group popup case
                HtmlElement elmContainer = (HtmlElement)ElementInternal.ChildNodes[0];
                _elmSeparator = (Span)ElementInternal.ChildNodes[1];
                _elmBody = (Span)elmContainer.ChildNodes[0];
                _elmTitle = (Span)elmContainer.ChildNodes[1];
            }
        }

        // We override this because we do not want AttachInternal() called recursively
        // over all the Layout children in the Group because only one layout is selected at a time.
        internal override void AttachInternal(bool recursive)
        {
            AttachDOMElements();
            AttachEvents();
            Dirty = false;
            if (recursive && !CUIUtility.IsNullOrUndefined(_selectedLayout))
                _selectedLayout.AttachInternal(true);
        }

        internal override void EnsureDOMElement()
        {
            HtmlElement prevElm = ElementInternal;
            base.EnsureDOMElement();
        }

        protected override string CssClass
        {
            get 
            { 
                return "ms-cui-group"; 
            }
        }

        protected override string DOMElementTagName
        {
            get 
            { 
                return "li"; 
            }
        }

        /// <summary>
        /// Make this Group so that no Layout is selected.
        /// </summary>
        public void UnselectLayout()
        {
            SelectLayout(null, null);
        }

        /// <summary>
        /// Selects a Layout(by Title of the Layout) that will be displayed in this Group.
        /// </summary>
        /// <param name="name"></param>
        public void SelectLayout(string name, string popupSize)
        {
            Layout layout = null;
            if (name != "Popup")
            {
                layout = string.IsNullOrEmpty(name) ? null : 
                   (Layout)GetChildByTitle(name);
            }
            else
            {
                // If popupSize is null, then we just use the default that came from the template
                // If popupSize is not null, then we use the passed in one that was specified
                // on the <ScaleStep> node.
                if (!string.IsNullOrEmpty(popupSize))
                    PopupLayoutTitle = popupSize;

                EnsurePopup();
                layout = _popupLayout;
            }

            // If this layout was already selected then we don't need to do anything
            if (layout == _selectedLayout ||
                    CUIUtility.IsNullOrUndefined(layout) &&
                    CUIUtility.IsNullOrUndefined(_selectedLayout))
                return;

            _selectedLayout = !CUIUtility.IsNullOrUndefined(layout) ? layout : null;
            // We have to set the layout to dirty because layout DOM subtrees share
            // DOM elements with other layouts.  So, it is possible that some of the
            // DOM elements in this layout's DOM subtree have been "snatched" via
            // ElementInternal.appendChild() for use in another layout's DOM subtree.  The
            // only way to ensure that this layout is graphically complete is to 
            // refresh it and thereby construct its DOM subtree again.
            if (!CUIUtility.IsNullOrUndefined(layout))
            {
                layout.SetDirtyRecursively(true);
                // If the Popup Layout it being selected, then we need to dirty its
                // Menu too.  It will not be dirtied by the call "layout.SErDirtyRecursively(true)"
                // because it is not attached to the hierarchy when it isn't showing.
                if (name == "Popup")
                    _popupMenu.SetDirtyRecursively(true);
            }
            this.OnDirtyingChange();
        }

        /// <summary>
        /// The Layout that is currently selected in this Group.
        /// </summary>
        public Layout SelectedLayout
        {
            get 
            { 
                return _selectedLayout; 
            }
        }

        protected override void EnsureCorrectChildType(Component child)
        {
            if (!typeof(Layout).IsInstanceOfType(child) && !typeof(GroupPopupLayout).IsInstanceOfType(child))
                throw new InvalidCastException("Only children of type Layout can be added to Groups");
            if (!CUIUtility.IsNullOrUndefined(GetChildByTitle(child.Title)))
            {
                throw new ArgumentNullException("A Layout with title " + child.Title +
                                    " already exists in this Group.");
            }
        }

        /// <summary>
        /// If this Group is overflowing its DOMElement rectangle (used for scaling).
        /// </summary>
        internal bool Overflowing
        {
            get
            {
                if (CUIUtility.IsNullOrUndefined(_selectedLayout) ||
                        CUIUtility.IsNullOrUndefined(_selectedLayout.ElementInternal))
                {
                    return false;

                }
                return ElementInternal.OffsetHeight < _selectedLayout.ElementInternal.OffsetHeight ||
                       ElementInternal.OffsetWidth < _selectedLayout.ElementInternal.OffsetWidth;
            }
        }

        string _command;
        public string Command
        {
            get 
            { 
                return _command; 
            }
        }

        #region Group Popup
        private GroupPopupLayout PopupLayout
        {
            get 
            { 
                return _popupLayout; 
            }
        }

        string _popupLayoutTitle = "";
        public string PopupLayoutTitle
        {
            get 
            { 
                return _popupLayoutTitle; 
            }
            set
            {
                if (value == "Popup")
                    throw new ArgumentOutOfRangeException("PopupLayoutTitle cannot be set to 'Popup'");

                Layout layout = string.IsNullOrEmpty(value) ? null : 
                    (Layout)GetChildByTitle(value);

                // If this Group doesn't have a Layout with that Title
                if (CUIUtility.IsNullOrUndefined(layout))
                    throw new InvalidOperationException("This Group does not have a Layout with Title: " + value);

                // If there is no change in the popup layout title, we just return
                if (_popupLayoutTitle == value)
                    return;

                // Set the popup title to the new value and then dirty the GroupPopup
                _popupLayoutTitle = value;
                if (!CUIUtility.IsNullOrUndefined(_popup))
                    _popup.LayoutTitle = value;
            }
        }

        GroupPopupLayout _popupLayout;
        FlyoutAnchor _popupAnchor;
        GroupPopup _popup;
        Menu _popupMenu;
        MenuSection _popupMenuSection;
        private void EnsurePopup()
        {
            if (!CUIUtility.IsNullOrUndefined(_popupLayout))
                return;

            if (string.IsNullOrEmpty(PopupLayoutTitle))
                throw new ArgumentNullException("No PopupLayoutTitle has been set.");

            // Create the Components and Controls needed for the Group Popup
            _popupLayout = Ribbon.CreateGroupPopupLayout(this.Id + "-Popup", this);
            _popupMenu = Ribbon.CreateMenu(this.Id + "-popupMenu", null, null, null);
            _popupMenuSection = Ribbon.CreateMenuSection(this.Id + "-popupMenuSection", null, null, false, null, null);

            JSObject tempObj = new JSObject();
            FlyoutAnchorProperties props = tempObj.To<FlyoutAnchorProperties>();
            props.LabelText = Title;

            RibbonProperties ribbonProperties = Ribbon.RibbonProperties;

            if (!string.IsNullOrEmpty(Properties.Image32by32Popup))
            {
                props.Image32by32 = Properties.Image32by32Popup;
                props.Image32by32Class = Properties.Image32by32PopupClass;
                props.Image32by32Top = Properties.Image32by32PopupTop;
                props.Image32by32Left = Properties.Image32by32PopupLeft;
            }
            else
            {
                props.Image32by32 = ribbonProperties.Image32by32GroupPopupDefault;
                props.Image32by32Class = ribbonProperties.Image32by32GroupPopupDefaultClass;
                props.Image32by32Left = ribbonProperties.Image32by32GroupPopupDefaultLeft;
                props.Image32by32Top = ribbonProperties.Image32by32GroupPopupDefaultTop;
            }
            props.Command = this.Command;

            _popupAnchor = new FlyoutAnchor(Ribbon,
                                            this.Id + "-PopupAnchor", 
                                            props, 
                                            _popupMenu);
            _popupAnchor.IsGroupPopup = true;

            // Set the enabled state of the anchor to the enabled state of the Group initially
            _popupAnchor.Enabled = Enabled;
            _popup = Ribbon.CreateGroupPopup(this.Id + "-popupMenuItem", this);

            // Compose the hierarchy needed for the Group Popup
            _popupLayout.AddChild(_popupAnchor.CreateComponentForDisplayMode("Large"));
            _popupMenu.AddChild(_popupMenuSection);
            _popupMenuSection.AddChild(_popup);

            _popup.LayoutTitle = PopupLayoutTitle;
            AddChild(_popupLayout);
        }

        #endregion

        internal override void PollForStateAndUpdateInternal()
        {
            // Poll for this Groups Command
            // A Group is automatically enabled if it does not have a command defined
            Enabled = string.IsNullOrEmpty(Command) ? true : 
                Ribbon.PollForCommandState(Command, null, null);

            // If this Group is disabled, then everything underneath it is also
            // disabled so we do not need to poll any further.
            if (!Enabled || CUIUtility.IsNullOrUndefined(_selectedLayout))
                return;

            _selectedLayout.PollForStateAndUpdateInternal();
        }

        protected override void OnEnabledChanged(bool enabled)
        {
            base.OnEnabledChanged(enabled);
            if (!CUIUtility.IsNullOrUndefined(this._popupAnchor))
                _popupAnchor.Enabled = enabled;
        }

        internal override bool SetFocusOnFirstControl()
        {
            if (!CUIUtility.IsNullOrUndefined(_selectedLayout) &&
                typeof(GroupPopupLayout).IsInstanceOfType(_selectedLayout))
            {
                _selectedLayout.ElementInternal.GetElementsByTagName("A")[0].PerformFocus();
                return true;
            }

            return base.SetFocusOnFirstControl();
        }
    }
}
