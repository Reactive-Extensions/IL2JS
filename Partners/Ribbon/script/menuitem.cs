using Microsoft.LiveLabs.Html;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace Ribbon
{
    [Import(MemberNameCasing = Casing.Exact)]
    public class MenuItemControlProperties : ControlProperties
    {
        extern public MenuItemControlProperties();
        extern public string MenuItemId { get; }
        extern public string CommandValueId { get; set; }
    }

    internal class MenuItem : ControlComponent
    {
        bool _focused;

        /// <summary>
        /// THE MENU FRAMEWORK IS UNDER ACTIVE DEVELOPMENT AND IS SUBJECT TO CHANGE.  A Component that is an item in a Menu.
        /// </summary>
        /// <param name="ribbon">The Ribbon that this MenuItem was created by and is a part of.</param>
        /// <param name="id">The Component id of this MenuItem.</param>
        /// <param name="displayMode">The display mode of this MenuItem's Control that this MenuItem represents.</param>
        /// <param name="control">The Control that created this MenuItem.</param>
        public MenuItem(Root root, string id, string displayMode, Control control)
            : base(root, id, displayMode, control)
        {

        }

        /// <summary>
        /// Checks if this MenuItem is focused on
        /// </summary>
        /// <returns>
        /// true if focused, false otherwise
        /// </returns>
        internal virtual bool Focused
        {
            get
            {
                return _focused;
            }
            set
            {
                _focused = value;
            }
        }

        internal override bool FocusOnItemById(string menuItemId)
        {
            if (Control is ISelectableControl)
            {
                ISelectableControl isc = (ISelectableControl)Control;
                if (isc.GetMenuItemId() == menuItemId)
                {
                    if (Visible && Enabled)
                    {
                        ReceiveFocus();
                        Focused = true;
                        return true;
                    }
                }
            }
            return false;
        }

        internal override bool FocusPrevious(HtmlEvent evt)
        {
            if (!Visible)
                return false;
            Focused = Control.FocusPrevious(evt);
            return Focused;
        }

        internal override bool FocusNext(HtmlEvent evt)
        {
            if (!Visible)
                return false;
            Focused = Control.FocusNext(evt);
            return Focused;
        }

        internal override void ResetFocusedIndex()
        {
            Focused = false;
        }
    }
}
