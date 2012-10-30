using System;

namespace Ribbon
{
    /// <summary>
    /// Used for ContextMenu layout
    /// </summary>
    internal class ContextMenuDock : Component
    {
        /// <summary>
        /// Creates a new Strip.
        /// </summary>
        /// <param name="root">The ContextMenuRoot that this ContextMenuDock was created by and is a part of.</param>
        /// <param name="id">The id of this ContextMenuDock.</param>
        public ContextMenuDock(Root root, string id)
            : base(root, id, "", "")
        {
        }

        internal override void RefreshInternal()
        {
            EnsureDOMElementAndEmpty();
            AppendChildrenToElement(ElementInternal);
            base.RefreshInternal();
        }

        protected override void EnsureCorrectChildType(Component child)
        {
            if (!typeof(ControlComponent).IsInstanceOfType(child))
                throw new ArgumentException("Only children of type Control can be added to Strips.");
        }

        public override bool VisibleInDOM
        {
            get
            {
                return true;
            }
        }
    }
}

