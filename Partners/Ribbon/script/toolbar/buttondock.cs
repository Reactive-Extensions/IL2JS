using System;
using Microsoft.LiveLabs.Html;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace Ribbon
{
    /// <summary>
    /// Property set definition for ButtonDock.
    /// </summary>
    [Import(MemberNameCasing = Casing.Exact)]
    internal class ButtonDockProperties : ControlProperties
    {
        extern public ButtonDockProperties();
        extern public string Alignment { get; }
    }

    /// <summary>
    /// A class representing a group of buttons in the UI.  Used for layout.
    /// </summary>
    internal class ButtonDock : Component
    {
        private string _alignment;

        /// <summary>
        /// Creates a new Strip.
        /// </summary>
        /// <param name="ribbon">The Toolbar that this ButtonDock was created by and is a part of.</param>
        /// <param name="id">The Component id of this ButtonDock.</param>
        internal ButtonDock(Root root, string id, ButtonDockProperties properties)
            : base(root, id, "", "")
        {
            _alignment = CUIUtility.SafeString(properties.Alignment);
        }

        internal override void RefreshInternal()
        {
            EnsureDOMElementAndEmpty();
            AppendChildrenToElement(ElementInternal);
            base.RefreshInternal();
        }

        protected override void AppendChildrenToElement(HtmlElement elm)
        {
            foreach (Component child in Children)
            {
                // This is very important for performance
                // We should always try to append an empty child DOM Element
                // first and then call EnsureRefreshed() where the child fills it
                child.EnsureDOMElement();

                // Make sure the buttons are all floating the right way via CSS.
                switch (Alignment)
                {
                    case DataNodeWrapper.LEFTALIGN:
                        Utility.EnsureCSSClassOnElement(child.ElementInternal, "ms-cui-toolbar-button-left");
                        break;
                    case DataNodeWrapper.CENTERALIGN:
                        Utility.EnsureCSSClassOnElement(child.ElementInternal, "ms-cui-toolbar-button-center");
                        break;
                    case DataNodeWrapper.RIGHTALIGN:
                        Utility.EnsureCSSClassOnElement(child.ElementInternal, "ms-cui-toolbar-button-right");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(Alignment);
                }

                elm.AppendChild(child.ElementInternal);
                child.EnsureRefreshed();
            }
        }

        protected override void EnsureCorrectChildType(Component child)
        {
            if (!typeof(ControlComponent).IsInstanceOfType(child))
                throw new ArgumentOutOfRangeException("Only children of type ControlComponent can be added to a ButtonDock.");
        }

        protected override string CssClass
        {
            get
            {
                switch (Alignment)
                {
                    case DataNodeWrapper.LEFTALIGN:
                        return "ms-cui-toolbar-buttondock alignleft";
                    case DataNodeWrapper.CENTERALIGN:
                        return "ms-cui-toolbar-buttondock aligncenter";
                    case DataNodeWrapper.RIGHTALIGN:
                        return "ms-cui-toolbar-buttondock alignright";
                    default:
                        throw new ArgumentOutOfRangeException(Alignment);
                }
            }
        }

        public override bool VisibleInDOM
        {
            get 
            {
                // Layouts aren't an issue for toolbars, so we just ignore them.
                return true; 
            }
        }

        /// <summary>
        /// The alignment for this particular button dock. Note that only one centered dock can
        /// exist in a given toolbar, but multiple right and left-aligned ones can be created.
        /// </summary>
        public string Alignment
        {
            get { return _alignment; }
        }
    }
}

