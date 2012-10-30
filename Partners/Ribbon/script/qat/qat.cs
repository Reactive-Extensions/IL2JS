using System;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace Ribbon
{
    /// <summary>
    /// The properties of a QAT
    /// </summary>
    [Import(MemberNameCasing = Casing.Exact)]
    public class QATProperties : RootProperties
    {
        extern public QATProperties();
    }

    /// <summary>
    /// This is the class structure that represents the Quick Access Toolbar (QAT)
    /// </summary>
    public class QAT : Root
    {
        /// <summary>
        /// Creates a QAT.
        /// </summary>
        /// <param name="id">The Component id for the QAT.</param>
        /// <param name="properties">The QATProperties object for this QAT. <see cref="QATProperties"/></param>
        internal QAT(string id, QATProperties properties)
            : base(id, properties)
        {
        }

        #region Component Overrides
        /// <summary>
        /// Cause the in memory state of the QAT Component hierarchy to be reflected in the QAT's DOMElementInternal.
        /// </summary>
        public override void Refresh()
        {
            RefreshInternal();
            base.Refresh();
        }

        internal override void RefreshInternal()
        {
            // Create the outer DOM Element of the QAT if it hasn't been created yet
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
        /// Checks that the given child component is of the correct type to be under a QAT root
        /// </summary>
        /// <param name="child">The child component in question</param>
        protected override void EnsureCorrectChildType(Component child)
        {
            if (!typeof(ControlComponent).IsInstanceOfType(child))
                throw new InvalidCastException("Only children of type ControlComponent can be added to a QAT");
        }

        /// <summary>
        /// The CSS class to apply to this component's main DOMElement
        /// </summary>
        protected override string CssClass
        {
            get 
            { 
                return "ms-cui-QAT " + base.CssClass; 
            }
        }

        protected override string RootType
        {
            get 
            { 
                return "QAT"; 
            }
        }

        public override bool VisibleInDOM
        {
            // The QAT has no scaling, so it only supports one component per control
            // therefore, we always return true for this method.
            get 
            { 
                return true; 
            }
        }
        #endregion

        internal QATBuilder QATBuilder
        {
            get 
            { 
                return (QATBuilder)Builder; 
            }
            set 
            { 
                Builder = value; 
            }
        }
    }
}