using System;
using Microsoft.LiveLabs.Html;

namespace Ribbon
{
    /// <summary>
    /// A class that represents a particular visual layout of a Chunk/Group.
    /// </summary>
    internal class Layout : RibbonComponent
    {
        /// <summary>
        /// Creates a Layout.
        /// </summary>
        /// <param name="ribbon">The Ribbon that this Layout was created by and is a part of.</param>
        /// <param name="id">The Component id of this Layout.</param>
        /// <param name="title">The Title of this Layout. ie "Large", "Medium", "Small" etc.</param>
        internal Layout(SPRibbon ribbon, string id, string title)
            : base(ribbon, id, title, "")
        {
        }

        internal override void RefreshInternal()
        {
            if (NeedsDelayIniting)
                DoDelayedInit();

            EnsureDOMElementAndEmpty();
            AppendChildrenToElement(ElementInternal);
            base.RefreshInternal();
        }

        internal override void AttachDOMElements()
        {
            // Layouts can be delay initialized
            // Delayed initialization creates all the sub components of the Layout
            // They need to be created before they can be attached.
            if (NeedsDelayIniting)
                DoDelayedInit();

            // A Layout's id in the DOM is GROUPID-LAYOUTTITLE  for example:
            // "Ribbon.Edit.Clipboard-Large"
            ElementInternal = Browser.Document.GetById(Parent.Id + "-" + Title);
        }

        protected override void EnsureCorrectChildType(Component child)
        {
            if (!typeof(Section).IsInstanceOfType(child))
                throw new InvalidOperationException("Only children of Section can be added to a Layout");
        }

        protected override string CssClass
        {
            get 
            { 
                return "ms-cui-layout"; 
            }
        }

        internal override Component Clone(bool deep)
        {
            if (NeedsDelayIniting)
                DoDelayedInit();

            Layout layout = Ribbon.CreateLayout("clonedLayout-" + Ribbon.GetUniqueNumber(), this.Title);
            if (!deep)
                return layout;

            foreach (Section section in Children)
            {
                Section clonedSection = (Section)section.Clone(deep);
                layout.AddChild(clonedSection);
            }
            return layout;
        }

        public override bool VisibleInDOM
        {
            get
            {
                if (typeof(Group).IsInstanceOfType(Parent))
                {
                    Group parent = (Group)Parent;
                    return parent.SelectedLayout == this;
                }
                else if (typeof(GroupPopup).IsInstanceOfType(Parent))
                {
                    // If the parent is a GroupPopup then
                    // we know that we are in the selected layout because
                    // it only has one Layout.
                    return true;
                }
                else
                {
                    // TODO(josefl): fix this.  This cloning for popup groups does not scale.
                    // it should only be recloned if the master layout has been dirtied and in 
                    // this case, somehow the control should not have to keep track of all the old 
                    // stale ControlComponents.
                    return false;
                }
            }
        }
    }
}
