using System;
using Microsoft.LiveLabs.Html;

namespace Ribbon
{
    /// <summary>
    /// A class representing a Row in a Ribbon Section of a Ribbon Group.
    /// </summary>
    internal class Row : Component
    {
        /// <summary>
        /// Row constructor.
        /// </summary>
        /// <param name="ribbon">the Ribbon that created this Row and that it is a part of</param>
        /// <param name="id">Component id of the Row</param>
        internal Row(SPRibbon ribbon, string id)
            : base(ribbon, id, "", "")
        {
        }

        internal override void RefreshInternal()
        {
            EnsureDOMElementAndEmpty();
            base.AppendChildrenToElement(ElementInternal);
            base.RefreshInternal();
        }

        internal override void AttachDOMElements()
        {
            // Row Id example:
            // For example: Ribbon.Edit.Clipboard-Large-3-2 (section number 3, row 2 of the clipboard Large Layout)
            ElementInternal = Browser.Document.GetById(Parent.Id + "-" + Parent.Children.IndexOf(this));
        }

        protected override void EnsureCorrectChildType(Component child)
        {
            if (!typeof(Strip).IsInstanceOfType(child) &&
                !typeof(ControlComponent).IsInstanceOfType(child))
            {
                throw new InvalidCastException("Only children of type Strip and ControlComponent" +
                    " can be added to Row Components.");
            }
        }

        protected override string DOMElementTagName
        {
            get 
            { 
                return "span"; 
            }
        }

        /// <summary>
        /// The css class name that the outer element of Row will use.
        /// </summary>
        protected override string CssClass
        {
            get
            {
                SectionType type = ((Section)Parent).Type;
                if (type == SectionType.OneRow)
                {
                    return "ms-cui-row-onerow";
                }
                else
                {
                    return "ms-cui-row";
                }

            }
        }
    }
}
