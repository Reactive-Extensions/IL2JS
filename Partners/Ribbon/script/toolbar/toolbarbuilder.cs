using System;
using Microsoft.LiveLabs.Html;
using Microsoft.LiveLabs.JavaScript;

namespace Ribbon
{
    /// <summary>
    ///  Build Context for Toolbars.
    /// </summary>
    internal class ToolbarBuildContext : BuildContext
    {
        public Toolbar Toolbar = null;
    }

    /// <summary>
    /// Build Options for Toolbars.
    /// </summary>
    public class ToolbarBuildOptions : BuildOptions
    {
        public ToolbarBuildOptions() {}
    }

    /// <summary>
    /// Class that builds Toolbar specific Components.
    /// </summary>
    public class ToolbarBuilder : Builder
    {
        public ToolbarBuilder(
            ToolbarBuildOptions options,
            HtmlElement elmPlaceholder,
            IRootBuildClient rootBuildClient)
            : base(options, elmPlaceholder, rootBuildClient)
        {
            if (CUIUtility.IsNullOrUndefined(elmPlaceholder))
                throw new ArgumentNullException("Toolbar placeholder DOM element is null or undefined.");
        }

        public Toolbar Toolbar
        {
            get
            {
                return (Toolbar)Root;
            }
            private set
            {
                Root = value;
            }
        }

        /// <summary>
        /// The public method to create a toolbar using the datasource specified in the .DataSource property
        /// </summary>
        public void BuildToolbar()
        {
            ToolbarBuildContext context = new ToolbarBuildContext();

            DataQuery query = new DataQuery();
            query.TabQuery = false;
            query.Id = "toolbar";
            query.QueryType = DataQueryType.All;
            query.Handler = new DataReturnedEventHandler(OnReturnToolbarData);
            query.Data = context;

            DataSource.RunQuery(query);
        }

        /// <summary>
        /// Builds the toolbar and attaches it to the page.
        /// Called once the DataQuery completes and the toolbar data is available.
        /// </summary>
        private void OnReturnToolbarData(DataQueryResult res)
        {
            ToolbarBuildContext context = (ToolbarBuildContext)res.ContextData;

            // Apply any extensions to the data.
            res.QueryData = ApplyDataExtensions(res.QueryData);

            Toolbar = BuildToolbarFromData(res.QueryData, context);
            Toolbar.ToolbarBuilder = this;
            BuildClient.OnComponentCreated(Toolbar, Toolbar.Id);
            Toolbar.RefreshInternal();

            Placeholder.AppendChild(Toolbar.ElementInternal);

            // If there's a jewel on the toolbar, position the left buttondock adjacent to it
            foreach (ButtonDock dock in Toolbar.Children)
            {
                if (dock.Alignment == DataNodeWrapper.LEFTALIGN)
                {
                    Div jewelContainer = (Div)Browser.Document.GetById("jewelcontainer");
                    if (!CUIUtility.IsNullOrUndefined(jewelContainer))
                    {
                        if (Toolbar.TextDirection == Direction.LTR)
                        {
                            dock.ElementInternal.Style.Left = jewelContainer.OffsetWidth + "px";
                        }
                        else
                        {
                            dock.ElementInternal.Style.Right = jewelContainer.OffsetWidth + "px";
                        }
                    }
                    break;
                }
            }

            Utility.EnsureCSSClassOnElement(Placeholder, "loaded");
            BuildClient.OnComponentBuilt(Toolbar, Toolbar.Id);
        }

        /// <summary>
        /// Constructs a toolbar from its JSON data.
        /// </summary>
        private Toolbar BuildToolbarFromData(object data, ToolbarBuildContext context)
        {
            JSObject toolbarElement = DataNodeWrapper.GetFirstChildNodeWithName(data, DataNodeWrapper.TOOLBAR);

            if (CUIUtility.IsNullOrUndefined(toolbarElement))
                throw new ArgumentNullException("No toolbar element was present in the data");

            bool hasJewel = !CUIUtility.IsNullOrUndefined(DataNodeWrapper.GetFirstChildNodeWithName(data, DataNodeWrapper.JEWEL));

            Toolbar = new Toolbar(
                DataNodeWrapper.GetAttribute(toolbarElement, DataNodeWrapper.ID),
                DataNodeWrapper.GetNodeAttributes(toolbarElement).To<ToolbarProperties>(),
                this,
                hasJewel);

            Toolbar.ClientID = Options.ClientID;
            Toolbar.UseDataCookie = true;

            Toolbar.RefreshInternal(); // We need to refresh before we can attach the jewel.

            if (hasJewel)
            {
                Toolbar.AttachAndBuildJewelFromData(data);
            }

            // Build the ButtonDocks (the Docks will build their subcontrols).
            JSObject docks = DataNodeWrapper.GetFirstChildNodeWithName(toolbarElement, DataNodeWrapper.BUTTONDOCKS);
            JSObject[] dockChildren = DataNodeWrapper.GetNodeChildren(docks);

            for (int i = 0; i < dockChildren.Length; i++)
            {
                ButtonDock dock = BuildButtonDock(dockChildren[i], context);
                Toolbar.AddChild(dock);
            }

            return Toolbar;
        }

        /// <summary>
        /// Build up a ButtonDock based on the JSON object provided.
        /// </summary>
        private ButtonDock BuildButtonDock(object data, ToolbarBuildContext buildContext)
        {
            ButtonDock dock = Toolbar.CreateButtonDock(data, buildContext);

            JSObject controlsNode = DataNodeWrapper.GetFirstChildNodeWithName(data, DataNodeWrapper.CONTROLS);
            JSObject[] controls = DataNodeWrapper.GetNodeChildren(controlsNode);

            for (int i = 0; i < controls.Length; i++)
            {
                // Don't build trimmed controls
                if (IsNodeTrimmed(controls[i]))
                    continue;

                Component currentDisplayComponent = BuildToolbarControlComponent(controls[i], buildContext);
                dock.AddChild(currentDisplayComponent);
            }

            return dock;
        }

        /// <summary>
        /// The toolbar doesn't require scaling code -- it just builds with a static set of display modes,
        /// since it only supports one display mode per control type.
        /// </summary>
        private Component BuildToolbarControlComponent(object data, ToolbarBuildContext buildContext)
        {
            Control control = null;
            string name = DataNodeWrapper.GetNodeName(data);
            string displayMode = DataNodeWrapper.GetAttribute(data, DataNodeWrapper.DISPLAYMODE);

            switch (name)
            {
                case DataNodeWrapper.Button:
                    control = BuildControl(data, buildContext);
                    return control.CreateComponentForDisplayMode(
                        string.IsNullOrEmpty(displayMode) ? "Small" : displayMode);
                case DataNodeWrapper.CheckBox:
                    control = BuildControl(data, buildContext);
                    return control.CreateComponentForDisplayMode(
                        string.IsNullOrEmpty(displayMode) ? "Small" : displayMode);
                case DataNodeWrapper.ComboBox:
                    control = BuildControl(data, buildContext);
                    return control.CreateComponentForDisplayMode("Medium");
                case DataNodeWrapper.FlyoutAnchor:
                    control = BuildControl(data, buildContext);
                    return control.CreateComponentForDisplayMode(
                        string.IsNullOrEmpty(displayMode) ? "Medium" : displayMode);
                case DataNodeWrapper.Label:
                    control = BuildControl(data, buildContext);
                    return control.CreateComponentForDisplayMode(
                        string.IsNullOrEmpty(displayMode) ? "Small" : displayMode);
                case DataNodeWrapper.Separator:
                    control = BuildControl(data, buildContext);
                    return control.CreateComponentForDisplayMode("Small");
                case DataNodeWrapper.TextBox:
                    control = BuildControl(data, buildContext);
                    return control.CreateComponentForDisplayMode(
                        string.IsNullOrEmpty(displayMode) ? "Medium" : displayMode);
                case DataNodeWrapper.ToggleButton:
                    control = BuildControl(data, buildContext);
                    return control.CreateComponentForDisplayMode(
                        string.IsNullOrEmpty(displayMode) ? "Small" : displayMode);
                default:
                    throw new InvalidOperationException("Invalid control type.");
            }
        }
    }
}
