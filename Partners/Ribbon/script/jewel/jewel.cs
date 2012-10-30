using System;
using Microsoft.LiveLabs.JavaScript.Interop;
using Ribbon.Controls;

namespace Ribbon
{
    /// <summary>
    /// The properties of a Jewel
    /// </summary>
    [Import(MemberNameCasing = Casing.Exact)]
    public class JewelProperties : RootProperties
    {
        extern public JewelProperties();
        extern public string Command { get; }
    }

    /// <summary>
    /// This is the class structure that represents the Jewel
    /// </summary>
    public class Jewel : Root
    {
        private JewelMenuLauncher _menuLauncher;

        /// <summary>
        /// Creates a Jewel.
        /// </summary>
        /// <param name="id">The Component id for the Jewel.</param>
        /// <param name="properties">The JewelProperties object for this Jewel. <see cref="JewelProperties"/></param>
        internal Jewel(string id, JewelProperties properties)
            : base(id, properties)
        {
        }

        #region Component Overrides
        /// <summary>
        /// Cause the in memory state of the Jewel Component hierarchy to be reflected in the Jewel's DOMElementInternal.
        /// </summary>
        public override void Refresh()
        {
            RefreshInternal();
            base.Refresh();
        }

        internal override void RefreshInternal()
        {
            // Create the outer DOM Element of the jewel if it hasn't been created yet
            if (CUIUtility.IsNullOrUndefined(ElementInternal))
            {
                // Initialize the outer DOM element of this component
                EnsureDOMElement();
            }

            ElementInternal = Utility.RemoveChildNodes(ElementInternal);
            AppendChildrenToElement(ElementInternal);

            Dirty = false;
        }

        /// <summary>
        /// Checks that the given child component is of the correct type to be under a Jewel root
        /// </summary>
        /// <param name="child">The child component in question</param>
        protected override void EnsureCorrectChildType(Component child)
        {
            if (!typeof(ControlComponent).IsInstanceOfType(child))
                throw new InvalidOperationException("The child \"" + child.Id + "\" is not a ControlComponent");
            if (!typeof(JewelMenuLauncher).IsInstanceOfType(((ControlComponent)child).Control))
                throw new ArgumentException("Only children of type JewelMenuLauncher can be added to a Jewel");
        }

        /// <summary>
        /// The CSS class to apply to this component's main DOMElement
        /// </summary>
        protected override string CssClass
        {
            get 
            { 
                return "ms-cui-jewel " + base.CssClass; 
            }
        }

        protected override string RootType
        {
            get 
            { 
                return "Jewel"; 
            }
        }

        public override bool VisibleInDOM
        {
            get 
            {
                // The Jewel has no scaling, so it only supports one component per control
                // therefore, we always return true for this method.
                return true; 
            }
        }
        #endregion

        internal JewelBuilder JewelBuilder
        {
            get 
            { 
                return (JewelBuilder)Builder; 
            }
            set 
            { 
                Builder = value; 
            }
        }

        internal JewelMenuLauncher JewelMenuLauncher
        {
            get { return _menuLauncher; }
            set { _menuLauncher = value; }
        }

        internal void Focus()
        {
            _menuLauncher.FocusOnLauncher();
        }
    }
}