using System;
using Microsoft.LiveLabs.Html;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace Ribbon
{
    /// <summary>
    /// Property set definition for Toolbar.
    /// </summary>
    [Import(MemberNameCasing = Casing.Exact)]
    public class ToolbarProperties : RootProperties
    {
        extern public ToolbarProperties();
        extern public string Size { get; }
    }

    /// <summary>
    /// The base client-side control that contains all toolbar content.
    /// </summary>
    public class Toolbar : Root
    {
        Div _elmToolbarTopBars; // will hold the two top bar containers
        Div _elmTopBar1; // Center & Right peripheral content
        Div _elmTopBar2; // the jewel and buttondocks
        Div _elmJewelPlaceholder; // will hold the jewel button

        // Peripheral content containers
        Div _elmQATRowCenter;
        Div _elmQATRowRight;

        ToolbarBuilder _builder;

        private bool _hasJewel;
        protected Jewel _jewel;
        public Jewel Jewel
        {
            get 
            { 
                return _jewel; 
            }
            set 
            { 
                _jewel = value; 
            }
        }

        #region Object Management
        /// <summary>
        /// Creates a toolbar control.
        /// </summary>
        /// <param name="id">The Component id for the toolbar.</param>
        public Toolbar(string id, ToolbarProperties properties, ToolbarBuilder builder, bool hasJewel)
            : base(id, properties)
        {
            _builder = builder;
            _hasJewel = hasJewel;
        }
        #endregion Object Management

        #region Component overrides
        /// <summary>
        /// Refreshes the visual state of the toolbar control.
        /// </summary>
        public override void Refresh()
        {
            RefreshInternal();
            base.RefreshInternal();
        }

        internal override void RefreshInternal()
        {
            ToolbarProperties props = (ToolbarProperties)Properties;
            bool twoRow = (CUIUtility.SafeString(props.Size) == DataNodeWrapper.TWOROW);

            // Create the outer DOM Element of the toolbar if it hasn't been created yet
            if (CUIUtility.IsNullOrUndefined(ElementInternal))
            {
                CreateToolbarStructure(twoRow);
            }

            ElementInternal = Utility.RemoveChildNodes(ElementInternal);

            if (twoRow)
            {
                // Add the toolbar structure to the page
                ElementInternal.AppendChild(_elmToolbarTopBars);

                // Add the buttondocks
                AppendChildrenToElement(_elmTopBar2);
            }
            else
            {
                // Add the jewel to the toolbar
                if (_hasJewel)
                    ElementInternal.AppendChild(_elmJewelPlaceholder);

                // Add the buttondocks
                AppendChildrenToElement(ElementInternal);
            }

            Dirty = false;
        }

        protected void CreateToolbarStructure(bool twoRow)
        {
            if (_hasJewel)
            {
                _elmJewelPlaceholder = new Div();
                _elmJewelPlaceholder.Id = "jewelcontainer";
                _elmJewelPlaceholder.ClassName = "ms-cui-jewel-container";
                _elmJewelPlaceholder.Style.Display = "none";
            }

            if (twoRow)
            {
                _elmTopBar1 = new Div();
                _elmTopBar1.ClassName = "ms-cui-topBar1";
                _elmTopBar1.Style.Display = "none";

                _elmTopBar2 = new Div();
                _elmTopBar2.ClassName = "ms-cui-topBar2";

                if (_hasJewel)
                    _elmTopBar2.AppendChild(_elmJewelPlaceholder);

                _elmToolbarTopBars = new Div();
                _elmToolbarTopBars.ClassName = "ms-cui-ribbonTopBars";
                _elmToolbarTopBars.AppendChild(_elmTopBar1);
                _elmToolbarTopBars.AppendChild(_elmTopBar2);

                // Create peripheral content placeholders as necessary
                _elmQATRowCenter = (Div)Browser.Document.GetById(ClientID + "-" + RibbonPeripheralSection.QATRowCenter);
                _elmQATRowRight = (Div)Browser.Document.GetById(ClientID + "-" + RibbonPeripheralSection.QATRowRight);

                if (!CUIUtility.IsNullOrUndefined(_elmQATRowCenter))
                {
                    _elmQATRowCenter.ParentNode.RemoveChild(_elmQATRowCenter);
                    _elmTopBar1.AppendChild(_elmQATRowCenter);
                    _elmQATRowCenter.Style.Display = "inline-block";
                    _elmTopBar1.Style.Display = "block";
                    Utility.SetUnselectable(_elmQATRowCenter, true, false);
                }

                if (!CUIUtility.IsNullOrUndefined(_elmQATRowRight))
                {
                    _elmQATRowRight.ParentNode.RemoveChild(_elmQATRowRight);
                    _elmTopBar1.AppendChild(_elmQATRowRight);
                    _elmQATRowRight.Style.Display = "inline-block";
                    _elmTopBar1.Style.Display = "block";
                    Utility.SetUnselectable(_elmQATRowRight, true, false);
                }
            }

            // Initialize the outer DOM element of this component
            EnsureDOMElement();
        }

        internal override void PollForStateAndUpdateInternal()
        {
            if (!CUIUtility.IsNullOrUndefined(this.Jewel))
                this.Jewel.PollForStateAndUpdate();

            base.PollForStateAndUpdateInternal();
        }

        protected internal override void EnsureGlobalDisablingRemoved()
        {
            base.EnsureGlobalDisablingRemoved();
            if (!CUIUtility.IsNullOrUndefined(this.Jewel))
                this.Jewel.Enabled = true;
        }

        protected override void EnsureCorrectChildType(Component child)
        {
            if (!typeof(ButtonDock).IsInstanceOfType(child))
                throw new ArgumentOutOfRangeException("Only children of type ButtonDock can be added to a Toolbar");

            ButtonDock dock = (ButtonDock)child;
            if (dock.Alignment == DataNodeWrapper.CENTERALIGN)
            {
                foreach (ButtonDock current in Children)
                {
                    if (current.Alignment == DataNodeWrapper.CENTERALIGN)
                        throw new InvalidOperationException("Can't add a centered buttondock because one is already present.");
                }
            }
        }

        protected override string CssClass
        {
            get 
            { 
                return "ms-cui-toolbar-toolbar " + base.CssClass; 
            }
        }

        protected override string DOMElementTagName
        {
            get 
            { 
                return "div"; 
            }
        }

        protected override string RootType
        {
            get 
            { 
                return "Toolbar"; 
            }
        }

        internal ToolbarBuilder ToolbarBuilder
        {
            get 
            { 
                return (ToolbarBuilder)Builder; 
            }
            set 
            { 
                Builder = value; 
            }
        }
        #endregion Component overrides

        internal ButtonDock CreateButtonDock(object data, ToolbarBuildContext buildContext)
        {
            ButtonDockProperties properties = 
                DataNodeWrapper.GetNodeAttributes(data).To<ButtonDockProperties>();
            ButtonDock dock = new ButtonDock(Root, properties.Id, properties);

            return dock;
        }

        internal void AttachAndBuildJewelFromData(object jewelData)
        {
            if (!_hasJewel)
            {
                return;
            }

            _elmJewelPlaceholder.Style.Display = "block";

            JewelBuildContext jbc = new JewelBuildContext();
            jbc.JewelId = DataNodeWrapper.GetAttribute(jewelData, "Id");

            // Build Jewel
            JewelBuildOptions options = new JewelBuildOptions();
            options.TrimmedIds = _builder.Options.TrimmedIds;
            JewelBuilder builder = new JewelBuilder(options,
                 _elmJewelPlaceholder,
                 _builder.BuildClient);

            builder.BuildJewelFromData(jewelData, jbc);

            this.Jewel = builder.Jewel;
        }

        internal override void EnsureDOMElement()
        {
            base.EnsureDOMElement();
            ElementInternal.SetAttribute("role", "toolbar");
        }

        /// <summary>
        /// Focuses on the Jewel of the Toolbar
        /// </summary>
        public void SetFocusOnJewel()
        {
            if (!CUIUtility.IsNullOrUndefined(this.Jewel))
                this.Jewel.Focus();
        }

    }
}
