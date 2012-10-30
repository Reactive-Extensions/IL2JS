using System;
using Microsoft.LiveLabs.Html;

namespace Ribbon
{
    /// <summary>
    /// ContextMenu
    /// </summary>
    internal class ContextMenu : Menu
    {
        #region Constructor
        /// <summary>
        /// ContextMenu Contructor.
        /// </summary>
        /// <param name="root">The Root that this Menu was created by and is a part of.</param>
        /// <param name="id">The Component id of this Menu.</param>
        /// <param name="title">The Title of this Menu.</param>
        /// <param name="description">The Description of this Menu.</param>
        internal ContextMenu(Root root,
                             string id,
                             string title,
                             string description,
                             string maxWidth)
            : base(root, id, title, description, maxWidth)
        {
        }
        #endregion Constructor

        #region method overrides

        /// <summary>
        /// The job of RefreshInternal is to synchronize the in-memory component
        /// hierarchy with the DOM.  In other words, refresh()'s job is to 
        /// cause the DOM to reflect the in-memory hierarchy.
        /// </summary>
        internal override void RefreshInternal()
        {
            if (NeedsDelayIniting)
                DoDelayedInit();

            EnsureDOMElementAndEmpty();
            if (CUIUtility.IsNullOrUndefined(InnerDiv))
            {
                InnerDiv = new Div();
                InnerDiv.ClassName = "ms-cui-contextmenu-inner";
            }

            ElementInternal.AppendChild(InnerDiv);
            this.AppendChildrenToElement(InnerDiv);
            base.RefreshInternal();
        }
        #endregion method overrides

        #region property overrides

        /// <summary>
        /// The css class name that the outer element of this Component will use.
        /// </summary>
        protected override string CssClass
        {
            get
            {
                return "ms-cui-contextmenu";
            }
        }
        #endregion property overrides
    }
}