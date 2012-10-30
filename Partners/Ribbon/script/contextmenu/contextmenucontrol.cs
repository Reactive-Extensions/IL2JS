using Microsoft.LiveLabs.Html;
using Microsoft.LiveLabs.JavaScript.Interop;

using MenuType = Ribbon.Menu;

namespace Ribbon.Controls
{
    [Import(MemberNameCasing = Casing.Exact)]
    public class ContextMenuControlProperties : MenuLauncherControlProperties
    {
        // purely for extensiblity purpose
        extern public ContextMenuControlProperties();
    }

    /// <summary>
    /// A class representing a launcher for context menu.
    /// </summary>
    internal class ContextMenuControl : ContextMenuLauncher
    {
        Span m_elmDefault;

        /// <summary>
        /// ContextMenuControl
        /// </summary>
        internal ContextMenuControl(Root root,
                                    string id,
                                    ContextMenuControlProperties properties,
                                    MenuType menu)
            : base(root, id, properties, menu)
        {
            AddDisplayMode("Menu");
        }

        /// <summary>
        /// Created the DOMElement for the given displaymode
        /// </summary>
        protected override HtmlElement CreateDOMElementForDisplayMode(string displayMode)
        {
            switch (displayMode)
            {
                case "Menu":
                    // This is needed so that hierarchy is built
                    m_elmDefault = new Span();
                    return m_elmDefault;
                default:
                    this.EnsureValidDisplayMode(displayMode);
                    return null;
            }
        }

        /// <summary>
        /// Invoked when the Menu is opened
        /// </summary>
        public override void OnEnabledChanged(bool enabled)
        {
            // nothing to handle here
        }

        /// <summary>
        /// Invoked when the Menu is opened
        /// </summary>
        protected void OnMenuButtonClick(HtmlEvent evt)
        {
            if (!Enabled)
                return;

            LaunchContextMenu(null, evt);
            DisplayedComponent.RaiseCommandEvent(Properties.CommandMenuOpen,
                                                 CommandType.MenuCreation,
                                                 null);
        }

        /// <summary>
        /// Invoked when the Menu is closed
        /// </summary>
        protected override void OnLaunchedMenuClosed()
        {
            DisplayedComponent.RaiseCommandEvent(Properties.CommandMenuClose,
                                                 CommandType.MenuClose,
                                                 null);
        }

        /// <summary>
        /// Properties for the context menu
        /// </summary>
        private ContextMenuControlProperties Properties
        {
            get
            {
                return (ContextMenuControlProperties)base.ControlProperties;
            }
        }
    }
}
