using System;
using Microsoft.LiveLabs.Html;

namespace Ribbon
{
    /// <summary>
    /// A class representing a group of buttons in the UI.  Used for layout.
    /// </summary>
    internal class Strip : RibbonComponent
    {
        /// <summary>
        /// Creates a new Strip.
        /// </summary>
        /// <param name="ribbon">The Ribbon that this Strip was created by and is a part of.</param>
        /// <param name="id">The Component id of this Strip.</param>
        public Strip(SPRibbon ribbon, string id)
            : base(ribbon, id, "", "")
        {
        }

        internal override void RefreshInternal()
        {
            EnsureDOMElementAndEmpty();
            AppendChildrenToElement(ElementInternal);
            base.RefreshInternal();
        }

        // For example: Ribbon.Edit.Clipboard-Large-3-2-4 (section number 3, row 2, 
        // component 4 of the clipboard Large Layout)
        internal override void AttachDOMElements()
        {
            ElementInternal = Browser.Document.GetById(Parent.Id + "-" + Parent.Children.IndexOf(this));
        }

        protected override void EnsureCorrectChildType(Component child)
        {
            if (!(child is ControlComponent))
                throw new InvalidCastException("Only children of type Control can be added to Strips.");
        }

        protected override string CssClass
        {
            get 
            { 
                return "ms-cui-strip"; 
            }
        }

        internal override Component Clone(bool deep)
        {
            Strip strip = Ribbon.CreateStrip("clonedStrip-" + Ribbon.GetUniqueNumber());
            if (!deep)
                return strip;

            foreach (ControlComponent comp in Children)
            {
                strip.AddChild(comp.Clone(deep));

            }
            return strip;
        }
    }
}
