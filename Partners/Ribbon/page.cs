using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.LiveLabs.Html;
using Microsoft.LiveLabs.JavaScript;
using Microsoft.LiveLabs.JavaScript.IL2JS;
using Microsoft.LiveLabs.JavaScript.Interop;
using Ribbon.Page;

using LLPage = Microsoft.LiveLabs.Html.Page;

namespace Ribbon
{
    public partial class Ribbon : LLPage
    {
        [EntryPoint]
        public static void Run()
        {
            Ribbon handleRibbon = new Ribbon();
        }

        JSObject _ribbon;
        TestPageComponent _testComponent;
        const string ReadTabId = "Ribbon.Read-title";
        const string LibraryTabId = "Ribbon.Library-title";
        const string DocumentTabId = "Ribbon.Document-title";

        public Ribbon()
        {
            InitializeComponent();
            BrowserUtility.InitBrowserUtility();

            // Register the test page component with command info
            _testComponent = new TestPageComponent();
            PageManager.Instance.AddPageComponent(_testComponent);

            _ribbon = new JSObject();
            _ribbon.SetField<bool>("initStarted", false);
            _ribbon.SetField<bool>("buildMinimized", true);
            _ribbon.SetField<bool>("launchedByKeyboard", false);
            _ribbon.SetField<bool>("initialTabSelectedByUser", false);
            _ribbon.SetField<string>("initialTabId", "Ribbon.Read");

            Anchor readTabA = (Anchor)Browser.Document.GetById(ReadTabId).FirstChild;
            Anchor libraryTabA = (Anchor)Browser.Document.GetById(LibraryTabId).FirstChild;
            Anchor documentTabA = (Anchor)Browser.Document.GetById(DocumentTabId).FirstChild;

            readTabA.Click += RibbonStartInit;
            libraryTabA.Click += RibbonStartInit;
            documentTabA.Click += RibbonStartInit;

            Browser.Window.Resize += HandleWindowResize;
        }

        private void RibbonStartInit(HtmlEvent args)
        {
            if (!NativeUtility.RibbonReadyForInit())
                return;

            if (!CUIUtility.IsNullOrUndefined(args))
                _ribbon.SetField<bool>("initialTabSelectedByUser", true);

            Utility.CancelEventUtility(args, false, true);
            if (_ribbon.GetField<bool>("initStarted"))
                return;
            _ribbon.SetField<bool>("initStarted", true);

            // Get the name of the tab that was just selected
            Anchor tab = (Anchor)args.CurrentTargetElement;
            ListItem parent = (ListItem)tab.ParentNode;
            string initialTabId = parent.Id.Substring(0, parent.Id.IndexOf("-title"));

            string firstTabId = "";
            if (!string.IsNullOrEmpty(initialTabId))
            {
                firstTabId = _ribbon.GetField<string>("initialTabId");
                _ribbon.SetField<string>("initialTabId", initialTabId);
            }

            _ribbon.SetField<bool>("buildMinimized", false);

            if(!string.IsNullOrEmpty(initialTabId))
            {
                NativeUtility.RibbonOnStartInit(_ribbon);
            
                ListItem oldTab = (ListItem)Browser.Document.GetById(firstTabId + "-title");
                if (!CUIUtility.IsNullOrUndefined(oldTab))
                    oldTab.ClassName = "ms-cui-tt";

                ListItem newTab = (ListItem)Browser.Document.GetById(initialTabId + "-title");
                if (!CUIUtility.IsNullOrUndefined(newTab))
                    newTab.ClassName = "ms-cui-tt ms-cui-tt-s";
            }

            RibbonInitFunc1();
        }

        private void RibbonInitFunc1()
        {
            RibbonBuildOptions rbOpts = new RibbonBuildOptions();
            rbOpts.LazyTabInit = true;
            rbOpts.ShallowTabs = true;
            rbOpts.LazyMenuInit = true;
            rbOpts.AttachToDOM = false;
            rbOpts.InitialScalingIndex = 0;
            rbOpts.ValidateServerRendering = false;

            rbOpts.ShowQATId = "";
            rbOpts.ShowJewelId = "";
            rbOpts.ShownContextualGroups = null;
            rbOpts.FixedPositioningEnabled = false;
            rbOpts.NormalizedContextualGroups = null;

            rbOpts.DataExtensions = null;
            rbOpts.TrimEmptyGroups = true;
            rbOpts.ScalingHint = "-1819788779";
            rbOpts.ClientID = "RibbonContainer";
            rbOpts.Minimized = _ribbon.GetField<bool>("buildMinimized");
            rbOpts.LaunchedByKeyboard = _ribbon.GetField<bool>("launchedByKeyboard");
            rbOpts.InitialTabSelectedByUser = _ribbon.GetField<bool>("initialTabSelectedByUser");

            rbOpts.ShownTabs = new Dictionary<string, bool>();
            rbOpts.ShownTabs.Add("Ribbon.Read", true);
            rbOpts.ShownTabs.Add("Ribbon.Library", true);
            rbOpts.ShownTabs.Add("Ribbon.Document", true);

            rbOpts.InitiallyVisibleContextualGroups = new Dictionary<string, bool>();
            rbOpts.InitiallyVisibleContextualGroups.Add("Ribbon.LibraryContextualGroup", true);

            rbOpts.TrimmedIds = new Dictionary<string, bool>();
            rbOpts.TrimmedIds.Add("Ribbon.List.GanttView", true);
            rbOpts.TrimmedIds.Add("Ribbon.List.Share.AlertMe", true);
            rbOpts.TrimmedIds.Add("Ribbon.Library.Share.AlertMe", true);
            rbOpts.TrimmedIds.Add("Ribbon.Documents.FormActions", true);
            rbOpts.TrimmedIds.Add("Ribbon.ListItem.Share.AlertMe", true);
            rbOpts.TrimmedIds.Add("Ribbon.Documents.Share.AlertMe", true);
            rbOpts.TrimmedIds.Add("Ribbon.List.Actions.AllMeetings", true);
            rbOpts.TrimmedIds.Add("Ribbon.WebPartPage.Share.AlertMe", true);
            rbOpts.TrimmedIds.Add("Ribbon.Library.Actions.AllMeetings", true);
            rbOpts.TrimmedIds.Add("Ribbon.Calendar.Events.Share.AlertMe", true);
            rbOpts.TrimmedIds.Add("Ribbon.Calendar.Calendar.Share.AlertMe", true);
            rbOpts.TrimmedIds.Add("Ribbon.ListItem.Actions.ChangeItemOrder", true);
            rbOpts.TrimmedIds.Add("Ribbon.WebPartInsert.InsertRelatedDataToListForm", true);

            // Get parent Ribbon Container and prepare to build
            HtmlElement ribbonCont = Browser.Document.GetById("RibbonContainer");
            RibbonBuilder builder = new RibbonBuilder(rbOpts, ribbonCont, PageManager.Instance);

            // Set the data source and build tab
            DataSource dataSource = new DataSource("\u002f_layouts\u002fcommandui.ashx", "-829476993", "1033");
            builder.DataSource = dataSource;
            builder.BuildRibbonAndInitialTab(_ribbon.GetField<string>("initialTabId"));

            PMetrics.PerfReport();
        }

        private void HandleWindowResize(HtmlEvent args)
        {
            if (_ribbon.GetField<bool>("initStarted"))
                return;

            NativeUtility.RibbonScaleHeader((HtmlElement)Browser.Document.GetById("RibbonTopBarsElt"), false);
        }
    }

    public class TestPageComponent : PageComponent
    {
        #region Constants
        const string ReadTabName = "Ribbon.Read";

        // Tab Commands
        const string TabSwitchCommand = "CommandContextChanged";
        const string LibraryTabCommand = "LibraryTab";
        const string DocumentTabCommand = "DocumentTab";
        const string LibraryContextualGroupCommand = "LibraryContextualGroup";

        // Documents Tab Group Commands
        const string DocumentNewGroupCommand = "DocumentNewGroup";
        const string DocumentEditCheckoutGroupCommand = "DocumentEditCheckoutGroup";
        const string DocumentManageGroupCommand = "DocumentManageGroup";
        const string DocumentCopiesGroupCommand = "DocumentCopiesGroup";
        const string WorkflowGroupCommand = "WorkflowGroup";

        //Library Tab Group Commands
        const string ViewFormatGroupCommand = "ViewFormatGroup";
        const string DatasheetGroupCommand = "DatasheetGroup";
        const string CustomViewsGroupCommand = "CustomViewsGroup";
        const string ShareGroupCommand = "ShareGroup";
        const string ActionsGroupCommand = "ActionsGroup";
        const string SettingsGroupCommand = "SettingsGroup";

        // Documents Tab Button Commands
        const string NewDefaultDocumentCommand = "NewDefaultDocument";
        const string NewDocumentMenuOpenCommand = "NewDocumentMenuOpen";
        const string UploadDocumentCommand = "UploadDocument";
        const string UploadDocumentMenuOpenCommand = "UploadDocumentMenuOpen";
        const string UploadMultipleDocumentsCommand = "UploadMultipleDocuments";
        const string NewFolderCommand = "NewFolder";
        const string EditDocumentCommand = "EditDocument";
        const string CheckOutCommand = "CheckOut";
        const string CheckInCommand = "CheckIn";
        const string DiscardCheckOutCommand = "DiscardCheckOut";
        const string ViewPropertiesCommand = "ViewProperties";
        const string EditPropertiesCommand = "EditProperties";
        const string ViewVersionsCommand = "ViewVersions";
        const string ManagePermissionsCommand = "ManagePermissions";
        const string DeleteCommand = "Delete";
        const string EmailLinkCommand = "EmailLink";
        const string DownloadCopyCommand = "DownloadCopy";
        const string ManageCopiesCommand = "ManageCopies";
        const string SendToCommand = "SendTo";
        const string PopulateSendToMenuCommand = "PopulateSendToMenu";
        const string SendToOtherLocationCommand = "SendToOtherLocation";
        const string SendToExistingCopiesCommand = "SendToExistingCopies";
        const string SendToRecommendedLocationCommand = "SendToRecommendedLocation";
        const string GotoSourceItemCommand = "GotoSourceItem";
        const string ViewWorkflowsCommand = "ViewWorkflows";
        const string PublishCommand = "Publish";
        const string UnpublishCommand = "Unpublish";
        const string ModerateCommand = "Moderate";
        const string CancelApprovalCommand = "CancelApproval";

        // Library Tab Button Commands
        const string DisplayStandardViewCommand = "DisplayStandardView";
        const string DisplayDatasheetViewCommand = "DisplayDatasheetView";
        const string CreateViewCommand = "CreateView";
        const string CreateColumnCommand = "CreateColumn";
        const string EmailLibraryLinkCommand = "EmailLibraryLink";
        const string ViewRSSFeedCommand = "ViewRSSFeed";
        const string TakeOfflineToClientCommand = "TakeOfflineToClient";
        const string ConnectToClientCommand = "ConnectToClient";
        const string ExportToSpreadsheetCommand = "ExportToSpreadsheet";
        const string OpenWithExplorerCommand = "OpenWithExplorer";
        const string AddButtonCommand = "AddButton";
        const string ListSettingsCommand = "ListSettings";
        const string LibraryPermissionsCommand = "LibraryPermissions";
        const string ManageWorkflowsCommand = "ManageWorkflows";
        #endregion

        int _selectionCount;
        Table _listViewTable;
        Input _selectAllCheckbox;
        string[] _globalCommands;
        Dictionary<int, bool> _itemSelected;
        Dictionary<int, Input> _itemCbxCache;
        Dictionary<string, bool> _alwaysEnabledCommands;


        public TestPageComponent()
        {
            _selectionCount = 0;
            _itemSelected = new Dictionary<int, bool>();
            _itemCbxCache = new Dictionary<int, Input>();
            _listViewTable = (Table)Browser.Document.GetById("onetidDoclibViewTbl0");

            _selectAllCheckbox = (Input)Browser.Document.GetById("selectAllCbx");
            _selectAllCheckbox.Click += OnSelectAllCbxClick;

            for (int i = 1; i < 5; i++)
            {
                // Set that each item is unselected and add event handler
                _itemSelected[i] = false;

                string cbxName = "item" + i.ToString() + "cbx";
                Input itemcbx = (Input)Browser.Document.GetById(cbxName);
                itemcbx.Click += OnItemCbxClick;
                _itemCbxCache[i] = itemcbx;
            }
        }

        #region DOM Utilities
        private void OnItemCbxClick(HtmlEvent args)
        {
            Input cbx = (Input)args.CurrentTargetElement;
            int itemId = Int32.Parse(cbx.GetAttribute("itemId"));

            if (_itemSelected[itemId])
            {
                if (_selectionCount == 4)
                    _selectAllCheckbox.Checked = false;

                ToggleSelectionState(cbx, itemId, false);
            }
            else
            {
                if (_selectionCount == 3)
                    _selectAllCheckbox.Checked = true;

                ToggleSelectionState(cbx, itemId, true);
            }

            // Signal state changed so Ribbon will recompute state
            PageManager.Instance.CommandDispatcher.ExecuteCommand(Commands.CommandIds.ApplicationStateChanged, null);
        }

        private void OnSelectAllCbxClick(HtmlEvent args)
        {
            if (_selectionCount == 4)
            {
                for (int idx = 1; idx < 5; idx++)
                {
                    Input itemCbx = _itemCbxCache[idx];
                    if (_itemSelected[idx])
                    {
                        itemCbx.Checked = false;
                        ToggleSelectionState(itemCbx, idx, false);
                    }
                }
            }
            else
            {
                for (int idx = 1; idx < 5; idx++)
                {
                    Input itemCbx = _itemCbxCache[idx];
                    if (!_itemSelected[idx])
                    {
                        itemCbx.Checked = true;
                        ToggleSelectionState(itemCbx, idx, true);
                    }
                }
            }

            // Signal state changed so Ribbon will recompute state
            PageManager.Instance.CommandDispatcher.ExecuteCommand(Commands.CommandIds.ApplicationStateChanged, null);
        }

        private void ToggleSelectionState(Input cbx, int itemId, bool select)
        {
            // Get parent table row to update CSS classes
            TableRow pRow = (TableRow)cbx.ParentNode.ParentNode;

            if (select)
            {
                _selectionCount++;
                _itemSelected[itemId] = true;
                Utility.EnsureCSSClassOnElement(pRow, "s4-itm-selected");
            }
            else
            {
                _selectionCount--;
                _itemSelected[itemId] = false;
                Utility.RemoveCSSClassFromElement(pRow, "s4-itm-selected");
            }
        }
        #endregion

        #region PageComponent Methods
        /// <summary>
        /// Allows the component to initialize itself.
        /// </summary>
        public override void Init()
        {
            InitializeCommandInfo();
        }

        private void InitializeCommandInfo()
        {
            _globalCommands = new string[] {
                /* Tab and Group Commands */
                TabSwitchCommand,
                LibraryContextualGroupCommand,
                LibraryTabCommand,
                DocumentTabCommand,
                DocumentNewGroupCommand,
                DocumentEditCheckoutGroupCommand,
                DocumentManageGroupCommand,
                DocumentCopiesGroupCommand,
                WorkflowGroupCommand,
                ViewFormatGroupCommand,
                DatasheetGroupCommand,
                CustomViewsGroupCommand,
                ShareGroupCommand,
                ActionsGroupCommand,
                SettingsGroupCommand,
                /* Individual Document Tab Commands */
                NewDefaultDocumentCommand,
                NewDocumentMenuOpenCommand,
                UploadDocumentCommand,
                UploadDocumentMenuOpenCommand,
                UploadMultipleDocumentsCommand,
                NewFolderCommand,
                EditDocumentCommand,
                CheckOutCommand,
                CheckInCommand,
                DiscardCheckOutCommand,
                ViewPropertiesCommand,
                EditPropertiesCommand,
                ViewVersionsCommand,
                ManagePermissionsCommand,
                DeleteCommand,
                EmailLinkCommand,
                DownloadCopyCommand,
                ManageCopiesCommand,
                SendToCommand,
                PopulateSendToMenuCommand,
                SendToOtherLocationCommand,
                SendToExistingCopiesCommand,
                SendToRecommendedLocationCommand,
                GotoSourceItemCommand,
                ViewWorkflowsCommand,
                PublishCommand,
                UnpublishCommand,
                ModerateCommand,
                CancelApprovalCommand,
                /* Individual Library Tab Commands */
                DisplayStandardViewCommand,
                DisplayDatasheetViewCommand,
                CreateViewCommand,
                CreateColumnCommand,
                EmailLibraryLinkCommand,
                ViewRSSFeedCommand,
                TakeOfflineToClientCommand,
                ConnectToClientCommand,
                ExportToSpreadsheetCommand,
                OpenWithExplorerCommand,
                AddButtonCommand,
                ListSettingsCommand,
                LibraryPermissionsCommand,
                ManageWorkflowsCommand
            };

            // Add Tab and Group commands that are always enabled
            _alwaysEnabledCommands = new Dictionary<string, bool>();
            _alwaysEnabledCommands[TabSwitchCommand] = true;
            _alwaysEnabledCommands[LibraryContextualGroupCommand] = true;
            _alwaysEnabledCommands[LibraryTabCommand] = true;
            _alwaysEnabledCommands[DocumentTabCommand] = true;
            _alwaysEnabledCommands[DocumentNewGroupCommand] = true;
            _alwaysEnabledCommands[DocumentEditCheckoutGroupCommand] = true;
            _alwaysEnabledCommands[DocumentManageGroupCommand] = true;
            _alwaysEnabledCommands[DocumentCopiesGroupCommand] = true;
            _alwaysEnabledCommands[WorkflowGroupCommand] = true;
            _alwaysEnabledCommands[ViewFormatGroupCommand] = true;
            _alwaysEnabledCommands[DatasheetGroupCommand] = true;
            _alwaysEnabledCommands[CustomViewsGroupCommand] = true;
            _alwaysEnabledCommands[ShareGroupCommand] = true;
            _alwaysEnabledCommands[ActionsGroupCommand] = true;
            _alwaysEnabledCommands[SettingsGroupCommand] = true;

            // Add individual commands that are always enabled
            _alwaysEnabledCommands[NewFolderCommand] = true;
            _alwaysEnabledCommands[ViewRSSFeedCommand] = true;
            _alwaysEnabledCommands[ListSettingsCommand] = true;
            _alwaysEnabledCommands[UploadDocumentCommand] = true;
            _alwaysEnabledCommands[UploadDocumentMenuOpenCommand] = true;
            _alwaysEnabledCommands[UploadMultipleDocumentsCommand] = true;

            _alwaysEnabledCommands[CreateViewCommand] = true;
            _alwaysEnabledCommands[OpenWithExplorerCommand] = true;
            _alwaysEnabledCommands[LibraryPermissionsCommand] = true;
            _alwaysEnabledCommands[ExportToSpreadsheetCommand] = true;
        }

        /// <summary>
        /// Gets a string[] of commandids that this component is interested in.  These commands will be executed on the component no matter if the component has the focus or not.
        /// </summary>
        /// <returns>a string[] of commandids</returns>
        public override string[] GetGlobalCommands()
        {
            return _globalCommands;
        }

        /// <summary>
        /// Called in order to have this component handle a command that it has registered for.
        /// </summary>
        /// <param name="commandId">the id of the command</param>
        /// <param name="properties">the properties of the command</param>
        /// <param name="sequence">the sequence number of the command</param>
        /// <returns>true if the command was successfully handled by the component</returns>
        public override bool HandleCommand(string commandId, Dictionary<string, string> properties, int sequence)
        {
            switch (commandId)
            {
                case TabSwitchCommand:
                    string tabName = properties.ContainsKey("NewContextId") ? properties["NewContextId"] : "";
                    bool ribbonMinimized = (string.Compare(tabName, ReadTabName) == 0);
                    NativeUtility.OnRibbonMinimizedChanged(ribbonMinimized);
                    break;
                case DeleteCommand:
                    PrintItemCommand("Delete");
                    break;
                case CheckInCommand:
                    PrintItemCommand("Check In");
                    break;
                case CheckOutCommand:
                    PrintItemCommand("Checkout");
                    break;
                case DiscardCheckOutCommand:
                    PrintItemCommand("Discard Checkout");
                    break;
                case ViewVersionsCommand:
                    PrintItemCommand("View Version History");
                    break;
                case ViewPropertiesCommand:
                    PrintItemCommand("View Item Properties");
                    break;
                case EditPropertiesCommand:
                    PrintItemCommand("EditItem Properties");
                    break;
                case ManagePermissionsCommand:
                    PrintItemCommand("Manage Item Permissions");
                    break;
                case UnpublishCommand:
                    PrintItemCommand("Unpublish Item Version");
                    break;
                case ModerateCommand:
                    PrintItemCommand("Moderate Item Version");
                    break;
                case CancelApprovalCommand:
                    PrintItemCommand("Cancel Item Version Approval");
                    break;
                case NewFolderCommand:
                    Browser.Window.Alert("Creating a new Folder");
                    break;
                case ViewRSSFeedCommand:
                    Browser.Window.Alert("View RSS feed for this library");
                    break;
                case ListSettingsCommand:
                    Browser.Window.Alert("Update settings for this list");
                    break;
                case UploadDocumentCommand:
                    Browser.Window.Alert("Upload a document to this library");
                    break;
                case UploadMultipleDocumentsCommand:
                    Browser.Window.Alert("Upload multiple documents to this library");
                    break;
                case UploadDocumentMenuOpenCommand:
                    Browser.Window.Alert("Open the Upload Document dropdown menu");
                    break;
                case CreateViewCommand:
                    Browser.Window.Alert("Create a new view for this library");
                    break;
                case OpenWithExplorerCommand:
                    Browser.Window.Alert("View this library in Windows Explorer");
                    break;
                case LibraryPermissionsCommand:
                    Browser.Window.Alert("Modify user permissions for this library");
                    break;
                case ExportToSpreadsheetCommand:
                    Browser.Window.Alert("Export items in this library to Excel");
                    break;
                case PopulateSendToMenuCommand:
                    properties["PopulationXML"] = GetSendToMenuXml();
                    Browser.Window.Alert("Display the Send To button dropdown menu");
                    break;
                case SendToOtherLocationCommand:
                    PrintItemCommand("Send to Other Location");
                    break;
                case SendToRecommendedLocationCommand:
                    PrintItemCommand("Send to Recommended Location");
                    break;
                default:
                    return false;
            }
            return true;
        }

        private void PrintItemCommand(string commandId)
        {
            string itemString = (_selectionCount == 1) ? "Item: " : "Items: ";
            string output = "Command: " + commandId + " - " + itemString;

            bool first = true;
            for (int id = 1; id < 5; id++)
            {
                if (_itemSelected.ContainsKey(id) && _itemSelected[id])
                {
                    if (!first)
                        output += ", ";
                    else
                        first = false;

                    output += id.ToString();
                }
            }

            Browser.Window.Alert(output);
        }

        /// <summary>
        /// Called to find out if this component can currently handle a command.
        /// </summary>
        /// <param name="commandId">the name of the command</param>
        /// <returns>true if this component can currently handle the command</returns>
        public override bool CanHandleCommand(string commandId)
        {
            if (_alwaysEnabledCommands.ContainsKey(commandId))
                return true;

            switch (commandId)
            {
                case DeleteCommand:
                case CheckInCommand:
                case CheckOutCommand:
                case DiscardCheckOutCommand:
                    return _selectionCount > 0;
                case SendToCommand:
                case PopulateSendToMenuCommand:
                case SendToOtherLocationCommand:
                case SendToRecommendedLocationCommand:
                case ViewVersionsCommand:
                case ViewPropertiesCommand:
                case EditPropertiesCommand:
                case ManagePermissionsCommand:
                case UnpublishCommand:
                case ModerateCommand:
                case CancelApprovalCommand:
                    return _selectionCount == 1;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Gets the id of this PageComponent
        /// </summary>
        /// <returns>the id of this PageComponent</returns>
        public override string GetId()
        {
            return "TestPageComponent";
        }
        #endregion

        #region SendTo Menu
        private string GetSendToMenuXml()
        {
            // Need to localize this
            StringBuilder sb = new StringBuilder();
            sb.Append("<Menu Id='Ribbon.Document.All.SendTo.Menu'>");
            sb.Append("<MenuSection Id='Ribbon.Document.All.SendTo.Menu.Items' DisplayMode='Menu16'>");
            sb.Append("<Controls Id='Ribbon.Document.All.SendTo.Menu.Items.Controls'>");

            // Send to Recommended Location of this list (set in Advanced Settings for List)
            sb.Append("<Button");
            sb.Append(" Id='Ribbon.Document.All.SendTo.Menu.Items.RecommendedLocation'");
            sb.Append(" Command='");
            sb.Append(SendToRecommendedLocationCommand);
            sb.Append("' LabelText='Test Location'/>");

            // Send to Existing Copies if this item has been copied before
            sb.Append("<Button");
            sb.Append(" Id='Ribbon.Document.All.SendTo.Menu.Items.ExistingCopies'");
            sb.Append(" Command='");
            sb.Append(SendToExistingCopiesCommand);
            sb.Append("' Image16by16='/_layouts/images/existingLocations.gif'");
            sb.Append(" LabelText='Existing Copies'/>");

            // Send to Other Locations
            sb.Append("<Button");
            sb.Append(" Id='Ribbon.Document.All.SendTo.Menu.Items.OtherLocation'");
            sb.Append(" Command='");
            sb.Append(SendToOtherLocationCommand);
            sb.Append("' Image16by16='/_layouts/images/sendOtherLoc.gif'");
            sb.Append(" LabelText='Other Location'/>");

            sb.Append("</Controls>");
            sb.Append("</MenuSection>");
            sb.Append("</Menu>");

            return sb.ToString();
        }
        #endregion
    }
}
