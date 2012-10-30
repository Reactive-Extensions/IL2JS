using System;
using System.Collections.Generic;
using Microsoft.LiveLabs.Html;

namespace Ribbon
{
    /// <summary>
    /// Interface that must be implemented by Controls that generate MenuItem Components.
    /// </summary>
    public interface IMenuItem
    {
        /// <summary>
        /// This is still under development and may be removed.
        /// </summary>
        /// <returns></returns>
        string GetTextValue();

        /// <summary>
        /// This is still under development and may be removed.
        /// </summary>
        void ReceiveFocus();

        /// <summary>
        /// Called when the menu that this menu item is contained in gets closed
        /// </summary>
        void OnMenuClosed();
    }

    /// <summary>
    /// THE MENU FRAMEWORK IS STILL UNDER DEVELOPMENT AND IS SUBJECT TO CHANGE.  A general Menu class that all Menu types subclass.  It provides some basic functionality like DOMElement, scrollbars, layout etc.
    /// </summary>
    internal class Menu : Component
    {
        Div _elmInnerDiv;
        MenuItem _lastMenuItem;
        MenuItem _firstMenuItem;
        MenuItem _selectedMenuItem;
        int _focusedIndex = -1;
        string _maxWidth;

        /// <summary>
        /// Menu Contructor.
        /// </summary>
        /// <param name="ribbon">The Ribbon that this Menu was created by and is a part of.</param>
        /// <param name="id">The Component id of this Menu.</param>
        /// <param name="title">The Title of this Menu.</param>
        /// <param name="description">The Description of this Menu.</param>
        internal Menu(Root root,
                      string id,
                      string title,
                      string description,
                      string maxWidth)
            : base(root, id, title, description)
        {
            _maxWidth = maxWidth;
        }

        internal override void RefreshInternal()
        {
            // If this menu hasn't been refreshed yet, then we synchronously refresh it
            // TODO(josefl): revisit this to see if we need to make this work asynchronously
            // like Tabs.  Right now the plan is to always download all Menu/Gallery info
            // but to delay initialize them.  If we ever need to fetch them from the server
            // on demand like Tabs, then this code needs to change to suppor that.
            if (NeedsDelayIniting)
                DoDelayedInit();

            EnsureDOMElementAndEmpty();

            // Right now we always set this
            // This means that there is no "inherit" setting
            // If we need this, we should add Direction.Inherit
            Direction dir = Root.TextDirection;
            HtmlElement _elementInternal = ElementInternal;
            Div _innerDiv = InnerDiv;
            if (dir == Direction.LTR)
            {
                _elementInternal.Style.Direction = "ltr";
            }
            else if (dir == Direction.RTL)
            {
                Utility.EnsureCSSClassOnElement(_elementInternal, "ms-cui-rtl");
                _elementInternal.Style.Direction = "rtl";
            }

            if (CUIUtility.IsNullOrUndefined(_innerDiv))
            {
                _innerDiv = new Div();
                _innerDiv.ClassName = "ms-cui-smenu-inner";
            }

            // Setting aria role attribute
            _elementInternal.SetAttribute("role", "menu");
            _elementInternal.AppendChild(_innerDiv);

            if (!string.IsNullOrEmpty(_maxWidth))
            {
                _elementInternal.Style.MaxWidth = _maxWidth;
            }

            this.AppendChildrenToElement(_innerDiv);
            base.RefreshInternal();
            _elementInternal.ContextMenu += Utility.ReturnFalse;
        }

        protected override string CssClass
        {
            get
            {
                return "ms-cui-menu";
            }
        }

        protected override string DOMElementTagName
        {
            get
            {
                return "div";
            }
        }

        protected override void EnsureCorrectChildType(Component child)
        {
            if (!typeof(MenuSection).IsInstanceOfType(child))
                throw new ArgumentException("Only MenuSection Components can be added to Menu Components.");
        }

        /// <summary>
        /// The inner div of this Menu.
        /// </summary>
        protected Div InnerDiv
        {
            get
            {
                return _elmInnerDiv;
            }
            set
            {
                _elmInnerDiv = value;
            }
        }

        private MenuLauncher _openSubMenuLauncher = null;
        internal MenuLauncher OpenSubMenuLauncher
        {
            get
            {
                return _openSubMenuLauncher;
            }
            set
            {
                if (value != null && !CUIUtility.IsNullOrUndefined(_openSubMenuLauncher))
                {
                    // Close the old menu and all submenus under it.
                    Root.CloseMenuStack(_openSubMenuLauncher);
                }

                _openSubMenuLauncher = value;
            }
        }

        #region Position & Size
        internal override int ComponentWidth
        {
            get
            {
                if (_componentWidth == -1 && !CUIUtility.IsNullOrUndefined(ElementInternal))
                {
                    _componentWidth = ElementInternal.OffsetWidth;
                }

                return _componentWidth;
            }
        }

        internal override int ComponentHeight
        {
            get
            {
                if (_componentHeight == -1 && !CUIUtility.IsNullOrUndefined(ElementInternal))
                {
                    _componentHeight = ElementInternal.OffsetHeight;
                }

                return _componentHeight;
            }
        }

        internal override int ComponentTopPosition
        {
            get
            {
                if (_componentTopPosition == -1 && !CUIUtility.IsNullOrUndefined(ElementInternal))
                {
                    _componentTopPosition = UIUtility.CalculateOffsetTop(ElementInternal);
                }

                return _componentTopPosition;
            }
        }

        internal override int ComponentLeftPosition
        {
            get
            {
                if (_componentLeftPosition == -1 && !CUIUtility.IsNullOrUndefined(ElementInternal))
                {
                    _componentLeftPosition = UIUtility.CalculateOffsetLeft(ElementInternal);
                }

                return _componentLeftPosition;
            }
        }

        internal void InvalidatePositionAndSizeData()
        {
            _componentWidth = -1;
            _componentHeight = -1;
            _componentTopPosition = -1;
            _componentLeftPosition = -1;
        }
        #endregion

        internal override void PollForStateAndUpdateInternal()
        {
            LastPollTime = DateTime.Now;
            base.PollForStateAndUpdateInternal();
        }

        internal MenuItem LastItem
        {
            get
            {
                if (CUIUtility.IsNullOrUndefined(_lastMenuItem))
                    _lastMenuItem = LastItemInternal;
                return _lastMenuItem;
            }
        }

        private MenuItem LastItemInternal
        {
            get
            {
                foreach (Component section in Children)
                {
                    foreach (MenuItem item in section.Children)
                    {
                        if (item.Visible && item.Enabled)
                            return item;
                    }
                }
                return null;
            }
        }

        internal MenuItem FirstItem
        {
            get
            {
                if (CUIUtility.IsNullOrUndefined(_firstMenuItem))
                    _firstMenuItem = FirstItemInternal;
                return _firstMenuItem;
            }
        }

        private MenuItem FirstItemInternal
        {
            get
            {
                foreach (Component section in Children)
                {
                    foreach (MenuItem item in section.Children)
                    {
                        if (item.Visible && item.Enabled)
                        {
                            return item;
                        }
                    }
                }
                return null;
            }
        }

        internal MenuItem SelectedMenuItem
        {
            get
            {
                return _selectedMenuItem;
            }
            set
            {
                _selectedMenuItem = value;
            }
        }

        internal override void ResetFocusedIndex()
        {
            if (Children.Count == 0)
                return;

            _focusedIndex = 0;
            foreach (Component c in Children)
            {
                c.ResetFocusedIndex();
            }
        }

        internal override void FocusOnFirstItem(HtmlEvent evt)
        {
            if (Children.Count == 0)
                return;

            _focusedIndex = 0;
            ((Component)Children[0]).FocusOnFirstItem(evt);
            FocusNext(evt);
        }

        internal override void FocusOnLastItem(HtmlEvent evt)
        {
            int count = Children.Count;
            if (count == 0)
                return;

            _focusedIndex = count - 1;
            ((Component)Children[_focusedIndex]).FocusOnLastItem(evt);
            FocusPrevious(evt);
        }

        internal override bool FocusOnItemById(string menuItemId)
        {
            if (Children.Count == 0)
                return false;

            _focusedIndex = 0;
            int i = 0;
            foreach (Component c in Children)
            {
                if (c.FocusOnItemById(menuItemId))
                {
                    _focusedIndex = i;
                    return true;
                }
                i++;
            }
            return false;
        }

        internal override bool FocusPrevious(HtmlEvent evt)
        {
            if (_focusedIndex == -1)
                _focusedIndex = Children.Count - 1;

            int i = _focusedIndex;
            while (i > -1)
            {
                Component comp = Children[i];

                if (comp.FocusPrevious(evt))
                {
                    _focusedIndex = i;
                    return true;
                }
                i--;
            }
            _focusedIndex = -1;
            return false;
        }

        internal override bool FocusNext(HtmlEvent evt)
        {
            if (_focusedIndex == -1)
                _focusedIndex = 0;

            int i = _focusedIndex;
            while (i < Children.Count)
            {
                Component comp = Children[_focusedIndex];

                if (comp.FocusNext(evt))
                {
                    _focusedIndex = i;
                    return true;
                }
                i++;
            }
            _focusedIndex = -1;
            return false;
        }

        /// <summary>
        /// Gets the menu item with the given MenuItemId
        /// </summary>
        /// <param name="id">
        /// The unique menu item id of the requested component
        /// </param>
        /// <returns>
        /// A reference to the Component that has the given MenuItemId
        /// </returns>
        internal ISelectableControl GetItemById(string id)
        {
            return GetItemByIdInternal(this, id);
        }

        // Iterate recursively through the subtree of child components until either
        // a match is found or the end of the subtree is reached
        private ISelectableControl GetItemByIdInternal(Component comp, string id)
        {
            ISelectableControl tmp;
            if (comp is ControlComponent)
            {
                ControlComponent concomp = (ControlComponent)comp;
                if (concomp.Control is ISelectableControl)
                {
                    ISelectableControl isc = (ISelectableControl)concomp.Control;
                    if (isc.GetMenuItemId() == id)
                        return isc;
                }
            }

            List<Component> children = comp.Children;
            if (!CUIUtility.IsNullOrUndefined(children))
            {
                foreach (Component c in children)
                {
                    tmp = GetItemByIdInternal(c, id);
                    if (tmp != null)
                        return tmp;
                }
            }
            return null;
        }

        /// <summary>
        /// Whether this menu has any visible items within it
        /// </summary>
        /// <returns>
        /// True if there are any items that are visible in this menu
        /// </returns>
        /// <remarks>
        /// Enabled state does not affect this, disabled items that are visible count as a valid item
        /// The most common case when running this is to have 2 iterations, 1->first menu section, 2->first menu item
        /// so concerns about this double-loop being non-performant can be put to rest
        /// </remarks>
        internal bool HasItems()
        {
            foreach (Component section in Children)
            {
                foreach (MenuItem item in section.Children)
                {
                    if (item.Visible)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public override bool VisibleInDOM
        {
            get
            {
                return this._launched;
            }
        }

        bool _launched = false;
        internal bool Launched
        {
            get
            {
                return _launched;
            }
            set
            {
                _launched = value;
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            _elmInnerDiv = null;
            _lastMenuItem = null;
            _firstMenuItem = null;
            _selectedMenuItem = null;

            if (ElementInternal != null)
                ElementInternal.ContextMenu -= Utility.ReturnFalse;
        }
    }
}