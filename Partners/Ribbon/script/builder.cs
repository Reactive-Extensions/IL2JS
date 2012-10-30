using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.LiveLabs.Html;
using Microsoft.LiveLabs.JavaScript;
using Microsoft.LiveLabs.Xml;
using Ribbon.Controls;

using SPLabel = Ribbon.Controls.Label;
using SPButton = Ribbon.Controls.Button;

namespace Ribbon
{
    /// <summary>
    /// Interface of the object that is using the Builder.
    /// </summary>
    public interface IRootBuildClient
    {
        void OnComponentBuilt(Root root, string componentId);
        void OnComponentCreated(Root root, string componentId);
    }

    /// <summary>
    /// Options used to specify how the ribbon or other command ui is built.
    /// </summary>
    public class BuildOptions
    {
        public BuildOptions() {}
        public bool LazyMenuInit { get; set;  }
        public Dictionary<string, bool> TrimmedIds { get; set; }
        public bool AttachToDOM { get; set; }
        public bool ValidateServerRendering { get; set; }
        public bool FixedPositioningEnabled { get; set; }
        public Dictionary<string, List<JSObject>> DataExtensions { get; set; }
        public string ClientID { get; set; }
    }

    /// <summary>
    /// A context that is passed down as the various Components are built.
    /// </summary>
    public class BuildContext
    {
    }

    /// <summary>
    /// A class used for wrapping the JSON objects that represent XML nodes.  It also has all the static strings that are attribute names in the commanduiXML.
    /// </summary>
    internal class DataNodeWrapper
    {
        // XML Parts
        public const string ATTRIBUTES = "attrs";
        public const string CHILDREN = "children";
        public const string NAME = "name";
        //public const string SHALLOW = "shallow";

        // Attributes and values
        public const string ALIGNMENT = "Alignment";
        public const string ALT = "Alt";
        public const string CLASSNAME = "Classname";
        public const string COLOR = "Color";
        public const string COMMAND = "Command";
        public const string CONTEXTUALGROUPID = "ContextualGroupId";
        public const string CSSCLASS = "CssClass";
        public const string DARKBLUE = "DarkBlue";
        public const string DECIMALDIGITS = "DecimalDigits";
        public const string DESCRIPTION = "Description";
        public const string DISPLAYCOLOR = "DisplayColor";
        public const string DISPLAYMODE = "DisplayMode";
        public const string DIVIDER = "Divider";
        public const string ELEMENTDIMENSIONS = "ElementDimensions";
        public const string GREEN = "Green";
        public const string GROUPID = "GroupId";
        public const string ID = "Id";
        public const string INDEX = "Index";
        public const string INTERVAL = "Interval";
        public const string LABELTEXT = "LabelText";
        public const string LAYOUTTITLE = "LayoutTitle";
        public const string LIGHTBLUE = "LightBlue";
        public const string LOWSCALEWARNING = "LowScaleWarning";
        public const string MAGENTA = "Magenta";
        public const string MAXHEIGHT = "MaxHeight";
        public const string MAXIMUMVALUE = "MaximumValue";
        public const string MAXWIDTH = "MaxWidth";
        public const string MENUITEMID = "MenuItemId";
        public const string MESSAGE = "Message";
        public const string MINIMUMVALUE = "MinimumValue";
        public const string NAME_CAPS = "Name";
        public const string ONEROW = "OneRow";
        public const string ORANGE = "Orange";
        public const string POPUP = "Popup";
        public const string POPUPSIZE = "PopupSize";
        public const string PURPLE = "Purple";
        public const string SCROLLABLE = "Scrollable";
        public const string SEQUENCE = "Sequence";
        public const string SIZE = "Size";
        public const string STYLE = "Style";
        public const string TEAL = "Teal";
        public const string TEMPLATEALIAS = "TemplateAlias";
        public const string THREEROW = "ThreeRow";
        public const string TITLE = "Title";
        public const string TWOROW = "TwoRow";
        public const string TYPE = "Type";
        public const string VALUE = "Value";
        public const string YELLOW = "Yellow";

        // Node Names
        public const string RIBBON = "Ribbon";
        public const string QAT = "QAT";
        public const string JEWEL = "Jewel";
        public const string TABS = "Tabs";
        public const string CONTEXTUALTABS = "ContextualTabs";
        public const string CONTEXTUALGROUP = "ContextualGroup";
        public const string TAB = "Tab";
        public const string SCALING = "Scaling";
        public const string MAXSIZE = "MaxSize";
        public const string SCALE = "Scale";
        public const string GROUP = "Group";
        public const string GROUPS = "Groups";
        public const string LAYOUT = "Layout";
        public const string SECTION = "Section";
        public const string OVERFLOWSECTION = "OverflowSection";
        public const string ROW = "Row";
        public const string CONTROL = "ControlRef";
        public const string OVERFLOWAREA = "OverflowArea";
        public const string STRIP = "Strip";
        public const string CONTROLS = "Controls";
        public const string MENU = "Menu";
        public const string MENUSECTION = "MenuSection";
        public const string TEMPLATE = "Template";
        public const string TEMPLATES = "Templates";
        public const string RIBBONTEMPLATES = "RibbonTemplates";
        public const string GROUPTEMPLATE = "GroupTemplate";
        public const string GALLERY = "Gallery";

        public const string Colors = "Colors";
        public const string Color = "Color";

        // Control Node Names
        public const string ToggleButton = "ToggleButton";
        public const string ComboBox = "ComboBox";
        public const string DropDown = "DropDown";
        public const string Button = "Button";
        public const string SplitButton = "SplitButton";
        public const string FlyoutAnchor = "FlyoutAnchor";
        public const string GalleryButton = "GalleryButton";
        public const string InsertTable = "InsertTable";
        public const string Label = "Label";
        public const string MRUSplitButton = "MRUSplitButton";
        public const string Spinner = "Spinner";
        public const string TextBox = "TextBox";
        public const string CheckBox = "CheckBox";
        public const string ColorPicker = "ColorPicker";
        public const string Separator = "Separator";

        // Jewel-specific
        public const string JewelMenuLauncher = "JewelMenuLauncher";

        // Toolbar-specific
        public const string BUTTONDOCK = "ButtonDock";
        public const string BUTTONDOCKS = "ButtonDocks";
        public const string CENTERALIGN = "Center";
        public const string LEFTALIGN = "Left";
        public const string RIGHTALIGN = "Right";
        public const string TOOLBAR = "Toolbar";

        // Standard Display Modes
        public const string LARGE = "Large";
        public const string MEDIUM = "Medium";
        public const string SMALL = "Small";

        public const string DIVIDERAFTER = "DividerAfter";
        public const string DIVIDERBEFORE = "DividerBefore";

        public static JSObject GetFirstChildNodeWithName(object data, string name)
        {
            JSObject[] children = DataNodeWrapper.GetNodeChildren(data);

            int l = children.Length;
            for (int i = 0; i < l; i++)
            {
                JSObject child = children[i];
                string nm = DataNodeWrapper.GetNodeName(child);
                if (nm == name)
                    return child;
            }
            return null;
        }

        public static string GetNodeName(object data)
        {
            JSObject obj = (JSObject)data;
            return CUIUtility.SafeString(obj.GetField<string>(NAME));
        }

        public static JSObject[] GetNodeChildren(object data)
        {
            JSObject[] res = ((JSObject)data).GetField<JSObject[]>(CHILDREN);
            return CUIUtility.IsNullOrUndefined(res) ? new JSObject[] { } : res;
        }

        public static JSObject GetNodeAttributes(object data)
        {
            JSObject obj = (JSObject)data;
            return obj.GetField<JSObject>(ATTRIBUTES);
        }

        public static string GetAttribute(object data, string attributeName)
        {
            // We don't want to do a bunch of checks in here because of performance
            // We assume that the attribute is present and crash if it is not
            // If we need something that is more forgiving, we can add another one
            // that does the checks but is slower GetNodeAttributeWithChecks() etc.
            JSObject attrsNode = GetNodeAttributes(data);
            return CUIUtility.SafeString(attrsNode.GetField<string>(attributeName));
        }
    }

    /// <summary>
    /// This class is responsible for building Controls.  Button, ToggleButton etc.
    /// </summary>
    public class Builder : IDisposable
    {
        IRootBuildClient _rootBuildClient;
        public Builder(BuildOptions options,
                       HtmlElement elmPlaceholder,
                       IRootBuildClient rootBuildClient)
        {
            _bo = options;
            if (CUIUtility.IsNullOrUndefined(_bo.TrimmedIds))
                _bo.TrimmedIds = new Dictionary<string, bool>();
            _elmPlaceholder = elmPlaceholder;
            _rootBuildClient = rootBuildClient;
            Browser.Window.Unload += OnPageUnload;
        }

        private void OnPageUnload(HtmlEvent args)
        {
            Dispose();
        }

        /// <summary>
        /// Called after a CommandUI root is built to perform non-root-type-specific actions on the root.
        /// </summary>
        /// <param name="root">The root that was built</param>
        /// <owner alias="JKern"/>
        internal void OnRootBuilt(Root root)
        {
            root.FixedPositioningEnabled = Options.FixedPositioningEnabled;
        }

        public virtual void Dispose()
        {
            _root = null;
            _bo = null;
            _elmPlaceholder = null;
            _rootBuildClient = null;
            _data = null;
            Browser.Window.Unload -= OnPageUnload;
        }

        internal IRootBuildClient BuildClient
        {
            get
            {
                return _rootBuildClient;
            }
        }

        Root _root;
        public Root Root
        {
            get
            {
                return _root;
            }
            set
            {
                _root = value;
            }
        }

        bool _inQuery = false;
        protected bool InQuery
        {
            get
            {
                return _inQuery;
            }
            set
            {
                _inQuery = value;
            }
        }

        DataSource _data;
        public DataSource DataSource
        {
            get
            {
                return _data;
            }
            set
            {
                _data = value;
            }
        }

        protected HtmlElement _elmPlaceholder;
        protected HtmlElement Placeholder
        {
            get
            {
                return _elmPlaceholder;
            }
        }

        BuildOptions _bo;
        internal BuildOptions Options
        {
            get
            {
                return _bo;
            }
        }

        protected bool IsIdTrimmed(string id)
        {
            return (_bo.TrimmedIds.ContainsKey(id) && _bo.TrimmedIds[id]);
        }

        protected bool IsNodeTrimmed(JSObject dataNode)
        {
            string id = DataNodeWrapper.GetAttribute(dataNode, DataNodeWrapper.ID);
            return IsIdTrimmed(id);
        }


        internal Control BuildControl(object data, BuildContext bc)
        {
            Control control = null;
            string name = DataNodeWrapper.GetNodeName(data);
            switch (name)
            {
                case DataNodeWrapper.ToggleButton:
                    control = BuildToggleButton(data, bc);
                    break;
                case DataNodeWrapper.ComboBox:
                    control = BuildComboBox(data, bc);
                    break;
                case DataNodeWrapper.DropDown:
                    control = BuildDropDown(data, bc);
                    break;
                case DataNodeWrapper.Button:
                    control = BuildButton(data, bc);
                    break;
                case DataNodeWrapper.SplitButton:
                    control = BuildSplitButton(data, bc);
                    break;
                case DataNodeWrapper.FlyoutAnchor:
                    control = BuildFlyoutAnchor(data, bc);
                    break;
                case DataNodeWrapper.GalleryButton:
                    control = BuildGalleryButton(data, bc, null);
                    break;
                case DataNodeWrapper.InsertTable:
                    control = BuildInsertTable(data, bc);
                    break;
                case DataNodeWrapper.Label:
                    control = BuildLabel(data, bc);
                    break;
                case DataNodeWrapper.MRUSplitButton:
                    control = BuildMRUSplitButton(data, bc);
                    break;
                case DataNodeWrapper.Spinner:
                    control = BuildSpinner(data, bc);
                    break;
                case DataNodeWrapper.TextBox:
                    control = BuildTextBox(data, bc);
                    break;
                case DataNodeWrapper.ColorPicker:
                    control = BuildColorPicker(data, bc);
                    break;
                case DataNodeWrapper.CheckBox:
                    control = BuildCheckBox(data, bc);
                    break;
                case DataNodeWrapper.Separator:
                    control = BuildSeparator(data, bc);
                    break;
                default:
                    JSObject attrs = DataNodeWrapper.GetNodeAttributes(data);
                    string className = DataNodeWrapper.GetAttribute(data, DataNodeWrapper.CLASSNAME);
                    if (CUIUtility.IsNullOrUndefined(className))
                        throw new InvalidOperationException("Unable to create Control with tagname: " + name);
                    break;
            }
            return control;
        }

        internal Menu BuildMenu(JSObject data, BuildContext bc, bool lazyInit)
        {
            JSObject attrs = DataNodeWrapper.GetNodeAttributes(data);

            string id = DataNodeWrapper.GetAttribute(data, DataNodeWrapper.ID);
            string title = DataNodeWrapper.GetAttribute(data, DataNodeWrapper.TITLE);
            string description = DataNodeWrapper.GetAttribute(data, DataNodeWrapper.DESCRIPTION);
            string maxwidth = DataNodeWrapper.GetAttribute(data, DataNodeWrapper.MAXWIDTH);
            Menu menu = Root.CreateMenu(id,
                                        title,
                                        description,
                                        maxwidth);

            if (_bo.LazyMenuInit && lazyInit)
            {
                menu.SetDelayedInitData(new DelayedInitHandler(this.DelayInitMenu), data, bc);
                return menu;
            }

            FillMenu(menu, data, bc);
            return menu;
        }

        private void FillMenu(Menu menu, JSObject data, BuildContext bc)
        {
            JSObject[] children = DataNodeWrapper.GetNodeChildren(data);
            int l = children.Length;
            for (int i = 0; i < l; i++)
            {
                JSObject child = children[i];
                string name = DataNodeWrapper.GetNodeName(child);
                if (name != DataNodeWrapper.MENUSECTION)
                {
                    throw new InvalidOperationException("Tags with the name: " + name + " cannot be children of Menu tags.");
                }

                // Skip over menu sections that have been trimmed
                if (IsNodeTrimmed(child))
                    continue;

                MenuSection ms = BuildMenuSection(child, bc);
                menu.AddChild(ms);
            }
        }

        private Component DelayInitMenu(Component component,
                                        object data,
                                        object buildContext)
        {
            Menu menu = (Menu)component;
            FillMenu(menu, (JSObject)data, (BuildContext)buildContext);
            menu.OnDelayedInitFinished(true);
            return menu;
        }

        private MenuSection BuildMenuSection(JSObject data, BuildContext bc)
        {
            string displayMode = DataNodeWrapper.GetAttribute(data, DataNodeWrapper.DISPLAYMODE);
            if (CUIUtility.IsNullOrUndefined(displayMode))
                displayMode = "Menu";

            string id = DataNodeWrapper.GetAttribute(data, DataNodeWrapper.ID);
            string title = DataNodeWrapper.GetAttribute(data, DataNodeWrapper.TITLE);
            string description = DataNodeWrapper.GetAttribute(data, DataNodeWrapper.DESCRIPTION);
            bool scrollable = Utility.IsTrue(DataNodeWrapper.GetAttribute(data, DataNodeWrapper.SCROLLABLE));
            string maxheight = DataNodeWrapper.GetAttribute(data, DataNodeWrapper.MAXHEIGHT);
            MenuSection ms = Root.CreateMenuSection(id,
                                                    title,
                                                    description,
                                                    scrollable,
                                                    maxheight,
                                                    displayMode);

            JSObject[] menuSectionChildren = DataNodeWrapper.GetNodeChildren(data);
            JSObject msChild = menuSectionChildren[0];
            string msChildName = DataNodeWrapper.GetNodeName(msChild);

            if (msChildName == DataNodeWrapper.CONTROLS)
            {
                // Get the <MenuSection><Controls> node's children
                JSObject[] individualControls = DataNodeWrapper.GetNodeChildren(msChild);
                int l = individualControls.Length;

                JSObject child = null;
                for (int i = 0; i < l; i++)
                {
                    child = individualControls[i];
                    if (IsNodeTrimmed(child))
                        continue;
                    Control control = BuildControl(child, bc);
                    ms.AddChild(control.CreateComponentForDisplayMode(displayMode));
                }
            }
            else if (msChildName == DataNodeWrapper.GALLERY)
            {
                Gallery gallery = BuildGallery(msChild, bc, true);
                ms.AddChild(gallery);
            }

            return ms;
        }


        private Gallery BuildGallery(object data, BuildContext bc, bool isInMenu)
        {
            JSObject attrs = DataNodeWrapper.GetNodeAttributes(data);
            GalleryProperties properties = DataNodeWrapper.GetNodeAttributes(data).To<GalleryProperties>();
            Gallery gallery = Root.CreateGallery(properties.Id,
                                                DataNodeWrapper.GetAttribute(attrs, DataNodeWrapper.TITLE),
                                                DataNodeWrapper.GetAttribute(attrs, DataNodeWrapper.DESCRIPTION),
                                                properties);

            string displayMode = isInMenu ? "Menu" : "Default";

            JSObject[] children = DataNodeWrapper.GetNodeChildren(data);
            int l = children.Length;
            for (int i = 0; i < l; i++)
            {
                JSObject child = children[i];
                if (IsNodeTrimmed(child))
                    continue;
                
                // NOTE: currently, galleries can only host GalleryButton controls.
                // In the future, the gallery could support other control types, so those should be added here.
                Control control;
                switch (DataNodeWrapper.GetNodeName(child))
                {
                    case DataNodeWrapper.GalleryButton:
                        control = BuildGalleryButton(child, bc, properties.ElementDimensions);
                        break;
                    default:
                        control = BuildControl(child, bc);
                        break;
                }
                gallery.AddChild(control.CreateComponentForDisplayMode(displayMode));   
            }

            return gallery;
        }

        private GalleryButton BuildGalleryButton(object data, BuildContext bc, string strElmDims)
        {
            GalleryElementDimensions elmDims;
            // If elmDims is null, try to get the value from data
            if (string.IsNullOrEmpty(strElmDims))
            {
                JSObject attrs = DataNodeWrapper.GetNodeAttributes(data);
                strElmDims = DataNodeWrapper.GetAttribute(attrs, DataNodeWrapper.ELEMENTDIMENSIONS);
            }
            // If elmDims is still null (no value defined in data), default to 32x32
            if (string.IsNullOrEmpty(strElmDims))
            {
                elmDims = GalleryElementDimensions.Size32by32;
            }
            else
            {
                elmDims = Gallery.ConvertStringToGalleryElementDimensions(strElmDims);
            }

            GalleryButtonProperties properties =
                DataNodeWrapper.GetNodeAttributes(data).To<GalleryButtonProperties>();
            GalleryButton gb = new GalleryButton(Root,
                                                properties.Id,
                                                properties,
                                                elmDims);
            return gb;
        }

        private ToggleButton BuildToggleButton(object data, BuildContext bc)
        {
            ToggleButtonProperties properties =
                DataNodeWrapper.GetNodeAttributes(data).To<ToggleButtonProperties>();
            ToggleButton fsbc = new ToggleButton(Root,
                                                       properties.Id,
                                                       properties);
            return fsbc;
        }

        private CheckBox BuildCheckBox(object data, BuildContext bc)
        {
            CheckBoxProperties properties =
                DataNodeWrapper.GetNodeAttributes(data).To<CheckBoxProperties>();
            CheckBox cb = new CheckBox(Root,
                                       properties.Id,
                                       properties);
            return cb;
        }

        private ColorPicker BuildColorPicker(object data, BuildContext bc)
        {
            ColorPickerProperties properties = DataNodeWrapper.GetNodeAttributes(data).To<ColorPickerProperties>();
            JSObject[] colorNodes = DataNodeWrapper.GetNodeChildren(
                                      DataNodeWrapper.GetFirstChildNodeWithName(data,
                                      DataNodeWrapper.Colors));
            int numColors = colorNodes.Length;
            ColorStyle[] colors = new ColorStyle[numColors];
            for (int i = 0; i < numColors; i++)
            {
                ColorStyle color = new ColorStyle();
                JSObject dict = DataNodeWrapper.GetNodeAttributes(colorNodes[i]);
                string title = DataNodeWrapper.GetAttribute(dict, DataNodeWrapper.TITLE);
                color.Title = string.IsNullOrEmpty(title) ?
                    DataNodeWrapper.GetAttribute(dict, DataNodeWrapper.ALT) : title;
                color.Color = DataNodeWrapper.GetAttribute(dict, DataNodeWrapper.COLOR);
                color.DisplayColor = DataNodeWrapper.GetAttribute(dict, DataNodeWrapper.DISPLAYCOLOR);
                color.Style = DataNodeWrapper.GetAttribute(dict, DataNodeWrapper.STYLE);
                colors[i] = color;
            }

            
            ColorPicker cp = new ColorPicker(Root,
                                             properties.Id,
                                             properties,
                                             colors);
            return cp;
        }

        private ComboBox BuildComboBox(object data, BuildContext bc)
        {
            ComboBoxProperties properties =
                DataNodeWrapper.GetNodeAttributes(data).To<ComboBoxProperties>();
            JSObject[] children = DataNodeWrapper.GetNodeChildren(data);

            Menu menu = null;

            MenuLauncherControlProperties launcherProperties = 
                DataNodeWrapper.GetNodeAttributes(data).To<MenuLauncherControlProperties>();

            Dictionary<string, string> menuItems = null;

            if (!Utility.IsTrue(launcherProperties.PopulateDynamically))
            {
                // Since PopulateDynamically is not true, we pass in "false" for LazyInit
                menu = BuildMenu(children[0], bc, false);

                // Parse XML subtree to build MenuItem list for auto-complete
                menuItems = new Dictionary<string, string>();
                JSObject[] sections = DataNodeWrapper.GetNodeChildren(children[0]);
                int l = sections.Length;
                for (int i = 0; i < l; i++)
                {
                    // Get children of the MenuSection node
                    JSObject[] sectionChildren = DataNodeWrapper.GetNodeChildren(sections[i]);
                    // Get children of the Controls node within the MenuSection
                    // There should only be 1 Controls node within the MenuSection subtree
                    JSObject[] items = DataNodeWrapper.GetNodeChildren(sectionChildren[0]);
                    int m = items.Length;
                    for (int j = 0; j < m; j++)
                    {
                        string labeltext = DataNodeWrapper.GetAttribute(items[j], DataNodeWrapper.LABELTEXT);
                        string menuitemid = DataNodeWrapper.GetAttribute(items[j], DataNodeWrapper.MENUITEMID);
                        menuItems[labeltext] = menuitemid;
                    }
                }
            }

            ComboBox fscb = new ComboBox(Root,
                                             properties.Id,
                                             properties,
                                             menu);
            fscb.MenuItems = menuItems;
            return fscb;
        }

        private DropDown BuildDropDown(object data, BuildContext bc)
        {
            DropDownProperties properties =
                DataNodeWrapper.GetNodeAttributes(data).To<DropDownProperties>();

            JSObject[] children = DataNodeWrapper.GetNodeChildren(data);

            Menu menu = null;

            MenuLauncherControlProperties launcherProperties =
                DataNodeWrapper.GetNodeAttributes(data).To<MenuLauncherControlProperties>();

            if (!Utility.IsTrue(launcherProperties.PopulateDynamically))
                menu = BuildMenu(children[0], bc, false);

            DropDown fsdd = new DropDown(Root,
                                             properties.Id,
                                             properties,
                                             menu);
            return fsdd;
        }

        private SPButton BuildButton(object data, BuildContext bc)
        {
            ButtonProperties properties =
                DataNodeWrapper.GetNodeAttributes(data).To<ButtonProperties>();
            SPButton fsea = new SPButton(Root,
                                                       properties.Id,
                                                       properties);
            return fsea;
        }

        private SplitButton BuildSplitButton(object data, BuildContext bc)
        {
            SplitButtonProperties properties =
                DataNodeWrapper.GetNodeAttributes(data).To<SplitButtonProperties>();
            JSObject[] children = DataNodeWrapper.GetNodeChildren(data);
            Menu menu = null;
            if (!Utility.IsTrue(properties.PopulateDynamically))
                menu = BuildMenu(children[0], bc, true);

            SplitButton fseo = 
                new SplitButton(Root,
                    properties.Id,
                    properties,
                    menu);
            return fseo;
        }

        private FlyoutAnchor BuildFlyoutAnchor(object data, BuildContext bc)
        {
            FlyoutAnchorProperties properties =
                DataNodeWrapper.GetNodeAttributes(data).To<FlyoutAnchorProperties>();

            JSObject[] children = DataNodeWrapper.GetNodeChildren(data);

            Menu menu = null;

            MenuLauncherControlProperties launcherProperties =
                DataNodeWrapper.GetNodeAttributes(data).To<MenuLauncherControlProperties>();

            if (!Utility.IsTrue(launcherProperties.PopulateDynamically))
                menu = BuildMenu(children[0], bc, true);

            FlyoutAnchor fsfa = new FlyoutAnchor(Root,
                                                    properties.Id,
                                                     properties,
                                                     menu);
            return fsfa;
        }

        private InsertTable BuildInsertTable(object data, BuildContext bc)
        {
            InsertTableProperties properties = 
                DataNodeWrapper.GetNodeAttributes(data).To<InsertTableProperties>();
            InsertTable fsit = new InsertTable(Root,
                                                   properties.Id,
                                                   properties);
            return fsit;
        }

        private SPLabel BuildLabel(object data, BuildContext bc)
        {
            LabelProperties properties = 
                DataNodeWrapper.GetNodeAttributes(data).To<LabelProperties>();
            SPLabel fslb = new SPLabel(Root,
                                       properties.Id,
                                       properties);
            return fslb;
        }

        private MRUSplitButton BuildMRUSplitButton(object data, BuildContext bc)
        {
            DropDownProperties properties =
                DataNodeWrapper.GetNodeAttributes(data).To<DropDownProperties>();

            JSObject[] children = DataNodeWrapper.GetNodeChildren(data);

            Menu menu = null;

            MenuLauncherControlProperties launcherProperties =
                DataNodeWrapper.GetNodeAttributes(data).To<MenuLauncherControlProperties>();

            if (!Utility.IsTrue(launcherProperties.PopulateDynamically))
                menu = BuildMenu(children[0], bc, false);

            MRUSplitButton fssb = new MRUSplitButton(Root,
                                             properties.Id,
                                             properties,
                                             menu);
            return fssb;
        }

        private Separator BuildSeparator(object data, BuildContext bc)
        {
            SeparatorProperties properties = 
                DataNodeWrapper.GetNodeAttributes(data).To<SeparatorProperties>();
            Separator sep = new Separator(Root,
                                          properties.Id,
                                          properties);
            return sep;
        }

        private Spinner BuildSpinner(object data, BuildContext bc)
        {
            SpinnerProperties properties = 
                DataNodeWrapper.GetNodeAttributes(data).To<SpinnerProperties>();
            Spinner fssp = new Spinner(Root,
                                       properties.Id,
                                       properties,
                                       BuildUnits(data));
            return fssp;
        }

        private TextBox BuildTextBox(object data, BuildContext bc)
        {
            TextBoxProperties properties = 
                DataNodeWrapper.GetNodeAttributes(data).To<TextBoxProperties>();
            TextBox fstb = new TextBox(Root,
                                           properties.Id,
                                           properties);
            return fstb;
        }



        private Unit[] BuildUnits(object data)
        {
            JSObject[] children = DataNodeWrapper.GetNodeChildren(data);
            int l = children.Length;
            Unit[] units = new Unit[l];
            for (int i = 0; i < l; i++)
            {
                JSObject childData = children[i];
                string name = DataNodeWrapper.GetAttribute(childData, DataNodeWrapper.NAME_CAPS);
                string minValue = DataNodeWrapper.GetAttribute(childData, DataNodeWrapper.MINIMUMVALUE);
                string maxValue = DataNodeWrapper.GetAttribute(childData, DataNodeWrapper.MAXIMUMVALUE);
                string decimalDigits = DataNodeWrapper.GetAttribute(childData, DataNodeWrapper.DECIMALDIGITS);
                string interval = DataNodeWrapper.GetAttribute(childData, DataNodeWrapper.INTERVAL);
                units[i] = Spinner.CreateUnit(name,
                                    BuildUnitAbbreviations(DataNodeWrapper.GetNodeChildren(childData)),
                                    Double.Parse(minValue),
                                    Double.Parse(maxValue),
                                    Int32.Parse(decimalDigits),
                                    Double.Parse(interval));
            }
            return units;
        }

        private string[] BuildUnitAbbreviations(JSObject[] children)
        {
            int l = children.Length;
            string[] abbreviations = new string[l];
            for (int i = 0; i < l; i++)
                abbreviations[i] = DataNodeWrapper.GetAttribute(children[i], DataNodeWrapper.VALUE);

            return abbreviations;
        }

        internal static string ConvertXMLStringToJSON(string xml)
        {
            XmlDocument document = new XmlDocument(xml);
            if (CUIUtility.IsNullOrUndefined(document))
                return string.Empty;

            if (document.ChildNodes.Length == 0)
                return string.Empty;

            StringBuilder sb = new StringBuilder();
            sb.Append("(");
            sb.Append(ConvertNodeToJSON(document.ChildNodes[0]));
            sb.Append(")");

            return sb.ToString();
        }

        protected static string ConvertNodeToJSON(XmlNode node)
        {
            if (CUIUtility.IsNullOrUndefined(node))
                return string.Empty;

            StringBuilder sb = new StringBuilder();
            sb.Append("{\"name\" : \"");
            sb.Append(UIUtility.EcmaScriptStringLiteralEncode(node.NodeName));
            sb.Append("\",\"attrs\": {");

            XmlNamedNodeMap attrs = node.Attributes;
            int numAttrs = attrs.Length;
            for (int idx = 0; idx < numAttrs; idx++)
            {
                XmlNode attr = attrs[idx];

                if (idx != 0)
                    sb.Append(",");

                sb.Append("\"");
                sb.Append(UIUtility.EcmaScriptStringLiteralEncode(attr.NodeName));
                sb.Append("\":\"");
                sb.Append(UIUtility.EcmaScriptStringLiteralEncode(attr.NodeValue));
                sb.Append("\"");
            }

            sb.Append("},children:[");

            bool first = true;
            foreach (XmlNode child in node.ChildNodes)
            {
                if (string.Compare(child.NodeName, "#text") == 0)
                    continue;

                if (first)
                    first = false;
                else
                    sb.Append(",");

                sb.Append(ConvertNodeToJSON(child));
            }

            sb.Append("]}");
            return sb.ToString();
        }

        /// <summary>
        /// Apply any data customizations that were passed in as part of the BuildOptions
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected object ApplyDataExtensions(object data)
        {
            if (!CUIUtility.IsNullOrUndefined(Options.DataExtensions))
                return ApplyDataNodeExtensions(data, Options.DataExtensions);
            else
                return data;
        }

        /// <summary>
        /// As much as we'd like to fail gracefully in every corner case for this,
        /// we have to maintain a "garbage in garbage out policy" because this 
        /// recursive set of routines is very performance sensitive since it
        /// will run on every single node in the CUI data.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="extensions"></param>
        /// <returns></returns>
        protected static JSObject ApplyDataNodeExtensions(object data, Dictionary<string, List<JSObject>> extensions)
        {
            // If a data node does not have any attributes, then it cannot be extended
            if (CUIUtility.IsNullOrUndefined(DataNodeWrapper.GetNodeAttributes(data)))
                return (JSObject)data;

            string id = DataNodeWrapper.GetAttribute(data, "Id");

            List<JSObject> replacementData = extensions.ContainsKey(id) ? extensions[id] : null;

            // Has this data node been overridden?
            if (!CUIUtility.IsNullOrUndefined(replacementData))
            {
                JSObject winner = null;
                int winningSequence = Int32.MaxValue;

                // We now go through and find the correct replacement depending on sequence number.
                // We can only pick one replacement if there are multiple replacements.
                int l = replacementData.Count;
                for (int i = 0; i < l; i++)
                {
                    JSObject replacementNode = replacementData[i];

                    // If there is an entry in the array but it is null, then 
                    // we remove this node from the final data by returning null.
                    // Because this means that it was basically overriden with "nothing".
                    // aka "removed".                    
                    if (replacementNode == null)
                        return null;

                    string sequence = DataNodeWrapper.GetAttribute(replacementNode,
                                                                       DataNodeWrapper.SEQUENCE);

                    // If this extension does not have a sequence, then it has lowest precedence.
                    // This means that it will only be the winner if there is no previous winner.
                    if (string.IsNullOrEmpty(sequence))
                    {
                        if (CUIUtility.IsNullOrUndefined(winner))
                            winner = replacementNode;
                        continue;
                    }

                    // If this extension node has a lower sequence value than anything previously seen
                    // then it becomes the new winner.  "Lowest Sequence Wins".
                    int seq = Int32.Parse(sequence);
                    if (seq < winningSequence)
                    {
                        winner = replacementNode;
                        winningSequence = seq;
                    }
                }

                // Set the actual data node that we will be returning to the
                // node the winner that was determined by examining all the possible extensions.
                if (!CUIUtility.IsNullOrUndefined(winner))
                    data = winner;
            }

            JSObject[] children = DataNodeWrapper.GetNodeChildren(data);

            // If there is not a children node, then we create one so that we can add extensions
            if (CUIUtility.IsNullOrUndefined(children))
            {
                children = new JSObject[1];
                children[0] = new JSObject();
                ((JSObject)data).SetField<JSObject[]>("children", children);
            }

            // Now we make a temporary list where we will put the data nodes and the 
            // extension nodes and then sort them according to sequence before saving
            // them back into the children array in the data node.
            List<JSObject> combinedNodes = new List<JSObject>();
            int m = children.Length;
            for (int i = 0; i < m; i++)
                combinedNodes.Add(children[i]);

            // Have any children been added to this node through this extension mechanism?
            string extKey = id + "._children";
            List<JSObject> childrenReplacementData = extensions.ContainsKey(extKey) ? extensions[extKey] : null;

            // Add the extension nodes if there are any
            if (!CUIUtility.IsNullOrUndefined(childrenReplacementData))
            {
                int n = childrenReplacementData.Count;
                for (int i = 0; i < n; i++)
                    combinedNodes.Add(childrenReplacementData[i]);

                // Now do a sort over the combined list to get the final order
                combinedNodes.Sort(new CompareDataNodeOrder());
            }

            // Now that we have the maximal set of child nodes from the data and the customizations,
            // we need to allow for extensibility on each one before we finally add them to the main
            // parent node.
            int ln = combinedNodes.Count;
            JSObject[] finalChildNodes = new JSObject[ln];
            for (int i = 0; i < ln; i++)
            {
                // Recurse on this child node to get its customizations
                JSObject dataNode = ApplyDataNodeExtensions(combinedNodes[i], extensions);
                if (!CUIUtility.IsNullOrUndefined(dataNode))
                    finalChildNodes[i] = dataNode;
            }

            // Now insert this combined, sorted and extended list of children into the data node
            ((JSObject)data).SetField<JSObject[]>("children", finalChildNodes);
            return (JSObject)data;
        }

        private class CompareDataNodeOrder : IComparer<JSObject>
        {
            public CompareDataNodeOrder() { }
            public int Compare(JSObject node1, JSObject node2)
            {
                string sequence1 = DataNodeWrapper.GetAttribute(node1, DataNodeWrapper.SEQUENCE);
                string sequence2 = DataNodeWrapper.GetAttribute(node2, DataNodeWrapper.SEQUENCE);

                // If both nodes don't have Sequences, then they are equal
                if (string.IsNullOrEmpty(sequence2) && string.IsNullOrEmpty(sequence1))
                {
                    return 0;
                }

                // If the second node has no sequence number then it should always go last so we leave it.
                // node1 should be higher
                if (string.IsNullOrEmpty(sequence2))
                    return -1; // node1 should be higher

                // If the second node does have a sequence number and 
                // the first one does not then the second one should sort higher than the first.
                if (string.IsNullOrEmpty(sequence1))
                    return 1; // node1 should be lower

                // If we reach this point then it means that both nodes have sequence numbers
                // In this case we need to compare and move the smallest sequence number up.
                int seq1 = Int32.Parse(sequence1);
                int seq2 = Int32.Parse(sequence2);
                if (seq1 < seq2)
                    return -1;  // node1 should be higher
                else if (seq1 > seq2)
                    return 1; // node2 should be higher

                return 0; // They are equal.  Presumably they stay in their same order
            }
        }
    }
}
