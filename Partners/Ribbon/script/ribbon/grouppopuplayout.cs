using System;

namespace Ribbon
{
    internal class GroupPopupLayout : Layout
    {
        Group _group;
        internal GroupPopupLayout(SPRibbon ribbon, string id, Group group)
            : base(ribbon, id, "Popup")
        {
            _group = group;
        }

        internal override void RefreshInternal()
        {
            base.RefreshInternal();
        }

        protected override string CssClass
        {
            get 
            { 
                return ""; 
            }
        }

        protected override void EnsureCorrectChildType(Component child)
        {
            if (!(child is ControlComponent))
                throw new InvalidCastException("Only ControlComponents can be added to GroupPopupLayout.");
            if (Children.Count > 0)
                throw new ArgumentException("GroupPopupLayouts can only have one child");
        }
    }
}
