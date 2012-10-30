using System;
using Microsoft.LiveLabs.Html;

namespace Ribbon
{
    /// <summary>
    /// The color theme of a ContextualGroup.
    /// </summary>
    public enum ContextualColor
    {
        None = 0,
        DarkBlue = 1,
        LightBlue = 2,
        Teal = 3,
        Orange = 4,
        Green = 5,
        Magenta = 6,
        Yellow = 7,
        Purple = 8
    }

    /// <summary>
    /// Represents a group of tabs in the Ribbon that are part of a contextual tab group.
    /// </summary>
    internal class ContextualGroup : IDisposable
    {
        string _id;
        string _title;
        ContextualColor _color;
        string _command;
        int _tabCount;

        /// <summary>
        /// Creates a new ContextualGroup.
        /// </summary>
        /// <param name="id">The id of the ContextualGroup.  ie. "ctxgrpPictureTools"</param>
        /// <param name="title">The title of the ContextualGroup that will appear above the Tabs that are in that group when the group is made visible.</param>
        /// <param name="color">The color scheme for this ContextualGroup.  This determines both the color of the group title that will appear above the Tabs in that group but also the color of the Tabs themselves.</param>
        public ContextualGroup(string id, string title, ContextualColor color, string command)
        {
            _id = id;
            _tabCount = 0;
            _title = title;
            _color = color;
            _command = command;
        }

        /// <summary>
        /// The id of this contextual group.  For example: "ctxgrpPictureTools".
        /// </summary>
        public string Id
        {
            get 
            { 
                return _id; 
            }
        }

        /// <summary>
        /// The number of tabs visible in contextual group.  
        /// </summary>
        public int Count
        {
            get 
            { 
                return _tabCount; 
            }
        }

        /// <summary>
        /// The title of this ContextualGroup as it will appear above the Tabs in this group when the group is made visible.
        /// </summary>
        public string Title
        {
            get 
            { 
                return _title; 
            }
        }

        /// <summary>
        /// The color of this ContextualGroup and the Tabs in the group.
        /// </summary>
        public ContextualColor Color
        {
            get 
            { 
                return _color; 
            }
        }

        public string Command
        {
            get 
            { 
                return _command; 
            }
        }


        Div _elmTitle;
        ListItem _elmMain;
        UnorderedList _elmTabTitleContainer;
        /// <summary>
        /// The DOMElement that will appear above the Tabs that are part of this group when they are made visible.
        /// </summary>
        internal HtmlElement ElementInternal
        {
            get
            {
                if (CUIUtility.IsNullOrUndefined(_elmMain))
                {
                    _elmMain = new ListItem();
                    if (!string.IsNullOrEmpty(Id))
                        _elmMain.Id = Id;
                    _elmMain.ClassName = "ms-cui-cg";

                    string strColor = GetColorNameForContextualTabColor(_color);
                    if (!string.IsNullOrEmpty(strColor))
                        Utility.EnsureCSSClassOnElement(_elmMain, "ms-cui-cg-" + strColor);

                    Div elmInternal = new Div();
                    elmInternal.ClassName = "ms-cui-cg-i";
                    elmInternal.Title = _title;
                    _elmMain.AppendChild(elmInternal);

                    _elmTitle = new Div();
                    _elmTitle.ClassName = "ms-cui-cg-t";
                    elmInternal.AppendChild(_elmTitle);

                    Span elmTitleInternal = new Span();
                    elmTitleInternal.ClassName = "ms-cui-cg-t-i";
                    UIUtility.SetInnerText(elmTitleInternal, _title);
                    _elmTitle.AppendChild(elmTitleInternal);

                    _elmTabTitleContainer = new UnorderedList();
                    _elmTabTitleContainer.ClassName = "ms-cui-ct-ul";
                    _elmMain.AppendChild(_elmTabTitleContainer);

                    _tabCount = 0;
                }
                return _elmMain;
            }
        }

        /// <summary>
        /// Named "Attempt" because it is a valid case if the DOM element is not present in the DOM.
        /// This is unlike all the components whose DOM elements are expected to be there and 
        /// it is a bug if they are not when Attach() is called.
        /// </summary>
        internal void AttemptAttachDOMElements()
        {
            ListItem elm = (ListItem)Browser.Document.GetById(Id);
            if (!CUIUtility.IsNullOrUndefined(elm))
            {
                _elmMain = elm;
                _elmTitle = (Div)_elmMain.ChildNodes[0].ChildNodes[0];
                _elmTabTitleContainer = (UnorderedList)_elmMain.ChildNodes[1];
            }
        }

        internal void AddTabTitleDOMElement(HtmlElement tabTitle)
        {
            _elmTabTitleContainer.AppendChild(tabTitle);
            _tabCount++;

            if (_tabCount == 1)
            {
                Utility.EnsureCSSClassOnElement(_elmTabTitleContainer, "ms-cui-oneCtxTab");
            }
            else if (_tabCount == 2)
            {
                Utility.RemoveCSSClassFromElement(_elmTabTitleContainer, "ms-cui-oneCtxTab");
            }
        }

        internal void EnsureTabTitlesCleared()
        {
            if (!CUIUtility.IsNullOrUndefined(_elmTabTitleContainer))
                Utility.RemoveChildNodesSlow(_elmTabTitleContainer);
            _tabCount = 0;
        }

        public void Dispose()
        {
            _elmMain = null;
            _elmTitle = null;
            _elmTabTitleContainer = null;
            _tabCount = 0;
        }

        internal static string GetColorNameForContextualTabColor(ContextualColor color)
        {
            switch (color)
            {
                case ContextualColor.DarkBlue:
                    return "db";
                case ContextualColor.LightBlue:
                    return "lb";
                case ContextualColor.Teal:
                    return "tl";
                case ContextualColor.Orange:
                    return "or";
                case ContextualColor.Green:
                    return "gr";
                case ContextualColor.Magenta:
                    return "mg";
                case ContextualColor.Yellow:
                    return "yl";
                case ContextualColor.Purple:
                    return "pp";
                default:
                    return string.Empty;
            }
        }
    }
}
