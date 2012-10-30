using System;
using System.Collections.Generic;
using Microsoft.LiveLabs.Html;
using Microsoft.LiveLabs.JavaScript.Interop;
using Ribbon.Controls;

namespace Ribbon
{
    [Import(MemberNameCasing = Casing.Exact)]
    public class ContextMenuRootProperties : RootProperties
    {
        extern public ContextMenuRootProperties();
        extern public string CommandMenuOpen { get; }
        extern public string CommandMenuClose { get; }
    }

    /// <summary>
    /// The base client-side control that contains all ContextMenu-related content.
    /// </summary>
    public class ContextMenuRoot : Root
    {
        private Dictionary<string, ContextMenuControl> _createdMenuControls;

        #region Constructor
        /// <summary>
        /// Creates a ContextMenuRoot control.
        /// </summary>
        /// <param name="id">The Component id for the ContextMenuRoot.</param>
        public ContextMenuRoot(string id, ContextMenuRootProperties properties)
            : base(id, properties)
        {
            _createdMenuControls = new Dictionary<string, ContextMenuControl>();
        }
        #endregion Constructor

        #region Component overrides
        /// <summary>
        /// Refreshes the visual state of the control.
        /// </summary>
        public override void Refresh()
        {
            RefreshInternal();
            base.RefreshInternal();
        }

        /// <summary>
        /// The job of RefreshInternal is to synchronize the in-memory component
        /// hierarchy with the DOM.  In other words, refresh()'s job is to 
        /// cause the DOM to reflect the in-memory hierarchy.
        /// </summary>
        internal override void RefreshInternal()
        {
            // Create the outer DOM Element if it hasn't been created yet
            if (CUIUtility.IsNullOrUndefined(ElementInternal))
            {
                // Initialize the outer DOM element of this component
                EnsureDOMElement();
            }

            ElementInternal = Utility.RemoveChildNodes(ElementInternal);

            AppendChildrenToElement(ElementInternal);

            Dirty = false;
        }

        protected override string RootType
        {
            get
            {
                return "ContextMenu";
            }
        }

        /// <summary>
        /// Override to provide a builder for dynamic context menus.
        /// </summary>
        internal override Builder Builder
        {
            get
            {
                if (base.Builder == null)
                {
                    BuildOptions options = new BuildOptions();
                    options.LazyMenuInit = false;
                    base.Builder = new Builder(options, null, null);
                    base.Builder.Root = this;
                }
                return base.Builder;
            }
            set
            {
                base.Builder = value;
            }
        }

        public ContextMenuRootProperties ContextMenuRootProperties
        {
            get
            {
                return (ContextMenuRootProperties)Properties;
            }
        }

        /// <summary>
        /// This is called by AddChild and AddChildAtIndex to ensure that the child is of a legal type. 
        /// We throw an Exception if the child is not a ContextMenuDock.
        /// </summary>
        /// <param name="child">The child whose type should be checked.</param>
        protected override void EnsureCorrectChildType(Component child)
        {
            if (!typeof(ContextMenuDock).IsInstanceOfType(child))
                throw new ArgumentException("Only children of type ContextMenuDock can be added to a ContextMenuRoot");
        }
        #endregion Component overrides

        public void CreateContextMenu(ContextMenuControlProperties props, string id, string title, string description, string maxWidth)
        {
            ContextMenu menu = new ContextMenu(this, id, title, description, null);

            if (props == null)
                props = new ContextMenuControlProperties();

            props.CommandMenuOpen = ContextMenuRootProperties.CommandMenuOpen;
            props.CommandMenuClose = ContextMenuRootProperties.CommandMenuClose;

            ContextMenuControl control = new ContextMenuControl(
                                                                this,
                                                                id + "Launcher",
                                                                props,
                                                                menu);

            _createdMenuControls[id] = control;

            ContextMenuDock dock = new ContextMenuDock(this, "dock" + id);
            dock.AddChild(control.CreateComponentForDisplayMode("Menu"));
            this.AddChild(dock);
            this.Refresh();
        }

        private ContextMenuControl GetContextMenuControl(string id)
        {
            return _createdMenuControls.ContainsKey(id) ? _createdMenuControls[id] : null;
        }

        /// <summary>
        /// Obsolete API will be removed soon
        /// </summary>
        public void ShowContextMenu(string id, HtmlElement focusedElement, HtmlEvent triggeringEvent)
        {
            ContextMenuControl control = GetContextMenuControl(id);

            if (CUIUtility.IsNullOrUndefined(control))
                throw new ArgumentNullException("The context menu \"" + id + "\" does not exist");

            control.LaunchContextMenu(focusedElement, triggeringEvent);
        }

        /// <summary>
        /// Launch this MenuLauncher's Menu at given x and y
        /// </summary>
        public void ShowContextMenuAt(string id, HtmlElement elmHadFocus, int x, int y)
        {
            ContextMenuControl control = GetContextMenuControl(id);

            if (CUIUtility.IsNullOrUndefined(control))
                throw new ArgumentNullException("The context menu \"" + id + "\" does not exist");

            control.LaunchContextMenuAt(elmHadFocus, x, y);
        }

    }
}

