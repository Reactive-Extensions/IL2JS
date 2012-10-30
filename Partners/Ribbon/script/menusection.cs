using System;
using Microsoft.LiveLabs.Html;

namespace Ribbon
{
    /// <summary>
    /// A class for helping with Menu layout.  This class should have very little functionality of its own.
    /// Its main purpose is for layout.  Functionally, its child control components are part of the parent Menu. 
    /// </summary>
    internal class MenuSection : Component
    {
        /// <summary>
        /// MenuSection constructor.
        /// </summary>
        /// <param name="ribbon">The Ribbon that created this MenuSection and that it is a part of.</param>
        /// <param name="id">The Component id of this MenuSection.</param>
        /// <param name="title">The Title of this MenuSection.</param>
        /// <param name="description">The Description of this MenuSection.</param>
        /// <param name="dict">A bag of properties of this MenuSection</param>
        /// <param name="maxheight">The maximum height of this MenuSection</param>
        /// <param name="scrollable">Whether this MenuSection is scrollable</param>
        /// <param name="displayMode">The display mode of the children of this MenuSection</param>
        public MenuSection(Root root, string id, string title, string description, bool scrollable, string maxheight, string displayMode)
            : base(root, id, title, description)
        {
            _scrollable = scrollable;
            _maxHeight = maxheight;
            _displayMode = displayMode;
        }

        bool _scrollable;
        string _maxHeight;
        int _focusedIndex = -1;
        string _displayMode;
        Div _elmWrapper;
        Div _elmTitle;
        UnorderedList _elmItems;

        internal override void RefreshInternal()
        {
            EnsureDOMElementAndEmpty();
            _elmWrapper = new Div();
            _elmWrapper.ClassName = "ms-cui-menusection";
            ElementInternal.AppendChild(_elmWrapper);
            if (!string.IsNullOrEmpty(Title))
            {
                _elmTitle = new Div();
                UIUtility.SetInnerText(_elmTitle, Title);
                _elmTitle.ClassName = "ms-cui-menusection-title";
                _elmWrapper.AppendChild(_elmTitle);
            }
            _elmItems = new UnorderedList();
            _elmItems.ClassName = "ms-cui-menusection-items";

            string cssclassname;

            if (_displayMode == "Menu32")
            {
                if (Root.TextDirection == Direction.LTR)
                {
                    cssclassname = "ms-cui-menusection-items32";
                }
                else
                {
                    cssclassname = "ms-cui-menusection-items32rtl";
                }

                Component parent = Parent;
                if (parent is Menu)
                {
                    // For IE7, we can't put a max width on the menu section since hasLayout
                    // will become enabled and force the menu items to not be full-width
                    // We can, however, set a max-width on the menu itself if there are any menu32
                    // sections within it. (O14:448689)
                    Utility.EnsureCSSClassOnElement(parent.ElementInternal, "ms-cui-menu32");
                }
            }
            else if (_displayMode == "Menu16")
            {
                if (Root.TextDirection == Direction.LTR)
                {
                    cssclassname = "ms-cui-menusection-items16";
                }
                else
                {
                    cssclassname = "ms-cui-menusection-items16rtl";
                }
            }
            else
            {
                cssclassname = "";
            }

            if (cssclassname != "")
            {
                Utility.EnsureCSSClassOnElement(_elmItems, cssclassname);
            }

            if (_scrollable)
            {
                _elmItems.Style.OverflowY = "auto";
                _elmItems.Style.Position = "relative";
            }

            if (!string.IsNullOrEmpty(_maxHeight))
                _elmItems.Style.MaxHeight = _maxHeight;
            _elmWrapper.AppendChild(_elmItems);
            AppendChildrenToElement(_elmItems);
        }

        protected override string DOMElementTagName
        {
            get
            {
                return "div";
            }
        }

        protected override void AppendChildrenToElement(HtmlElement elm)
        {
            ListItem listItem;

            foreach (Component child in Children)
            {
                // Put all menu items into a list item for semantics
                listItem = new ListItem();
                listItem.ClassName = "ms-cui-menusection-items";

                // This is very important for performance
                // We should always try to append an empty child DOM Element
                // first and then call EnsureRefreshed() where the child fills it
                child.EnsureDOMElement();
                listItem.AppendChild(child.ElementInternal);
                elm.AppendChild(listItem);
                child.EnsureRefreshed();
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

            if (_focusedIndex > -1)
                ((Component)Children[_focusedIndex]).ResetFocusedIndex();

            _focusedIndex = 0;
            ((Component)Children[_focusedIndex]).FocusOnFirstItem(evt);
        }

        internal override void FocusOnLastItem(HtmlEvent evt)
        {
            int count = Children.Count;
            if (count == 0)
                return;

            if (_focusedIndex > -1)
                ((Component)Children[_focusedIndex]).ResetFocusedIndex();
            _focusedIndex = count - 1;
            ((Component)Children[_focusedIndex]).FocusOnLastItem(evt);
        }

        internal override bool FocusOnItemById(string menuItemId)
        {
            if (Children.Count == 0)
                return false;

            int i = 0;
            foreach (Component c in Children)
            {
                if (c.FocusOnItemById(menuItemId))
                {
                    if (_focusedIndex > -1)
                        ((Component)Children[_focusedIndex]).ResetFocusedIndex();
                    _focusedIndex = i;
                    return true;
                }
                i++;
            }
            return false;
        }

        internal override bool FocusPrevious(HtmlEvent evt)
        {
            int count = Children.Count;
            if (_focusedIndex == -1)
                _focusedIndex = count - 1;

            int i = _focusedIndex;
            while (i > -1)
            {
                Component comp = Children[i];

                if (comp.FocusPrevious(evt))
                {
                    // If focus is not moving, don't reset the focus of the menu item
                    if (i != _focusedIndex)
                    {
                        ((Component)Children[_focusedIndex]).ResetFocusedIndex();
                        _focusedIndex = i;
                    }
                    return true;
                }
                i--;
            }
            if (count > 0)
                ((Component)Children[_focusedIndex]).ResetFocusedIndex();
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
                Component comp = Children[i];

                if (comp.FocusNext(evt))
                {
                    // If focus is not moving, don't reset the focus of the menu item
                    if (i != _focusedIndex)
                    {
                        if (!CUIUtility.IsNullOrUndefined(Children[_focusedIndex]))
                            ((Component)Children[_focusedIndex]).ResetFocusedIndex();
                        _focusedIndex = i;
                    }
                    return true;
                }
                i++;
            }

            if (Children.Count > 0)
                ((Component)Children[_focusedIndex]).ResetFocusedIndex();
            _focusedIndex = -1;
            return false;
        }

        protected override void EnsureCorrectChildType(Component child)
        {
            if (!typeof(MenuItem).IsInstanceOfType(child)
                && !typeof(Gallery).IsInstanceOfType(child)
#if !CUI_NORIBBON
                && !typeof(GroupPopup).IsInstanceOfType(child)
#endif
)
            {
                throw new ArgumentException("MenuSections can only have children of type MenuItem, Gallery or GroupPopup.");
            }
        }

        // TODO: revisit this.  InsertTable uses this.  Should we make this public so that third parties can us it too?
        internal void SetTitleImmediate(string title)
        {
            TitleInternal = title;
            UIUtility.SetInnerText(_elmTitle, title);
        }

        public override void Dispose()
        {
            base.Dispose();
            _elmItems = null;
            _elmTitle = null;
            _elmWrapper = null;
        }
    }
}
