using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.LiveLabs.Html;
using Microsoft.LiveLabs.JavaScript;

namespace Ribbon
{
    internal class RibbonBuildContext : BuildContext
    {
        public RibbonBuildContext Clone()
        {
            RibbonBuildContext rbc = new RibbonBuildContext();
            rbc.InitializedTab = this.InitializedTab;
            rbc.InitialScalingIndex = this.InitialScalingIndex;
            rbc.InitialTabId = this.InitialTabId;
            rbc.Ribbon = this.Ribbon;
            return rbc;
        }

        public Tab InitializedTab { get; set; }
        public string InitialTabId { get; set; }
        // The scale index of the initial tab that was rendered by the server
        public int InitialScalingIndex { get; set; }
        public SPRibbon Ribbon { get; set; }
    }

    public class RibbonBuildOptions : BuildOptions
    {
        public RibbonBuildOptions() {}
        public bool LazyTabInit { get; set; }
        public bool ShallowTabs { get; set; }
        public string ShowQATId { get; set; }
        public string ShowJewelId { get; set; }
        public bool Minimized { get; set; }
        public Dictionary<string, bool> ShownTabs { get; set; }
        public Dictionary<string, string> ShownContextualGroups { get; set; }
        public Dictionary<string, bool> InitiallyVisibleContextualGroups { get; set;  }
        public Dictionary<string, bool> NormalizedContextualGroups { get; set; }
        public bool TrimEmptyGroups { get; set; }
        public int InitialScalingIndex { get; set; }
        public bool InitialTabSelectedByUser { get; set; }
        public bool LaunchedByKeyboard { get; set; }
        public string ScalingHint { get; set; }
    }

    /// <summary>
    /// Class that builds Ribbon specific Components.
    /// </summary>
    public class RibbonBuilder : Builder
    {
        public RibbonBuilder(RibbonBuildOptions options,
                             HtmlElement elmPlaceholder,
                             IRootBuildClient rootBuildClient)
            : base(options, elmPlaceholder, rootBuildClient)
        {
            if (CUIUtility.IsNullOrUndefined(elmPlaceholder))
                throw new ArgumentNullException("Ribbon placeholder DOM element is null or undefined.");
        }

        public SPRibbon Ribbon
        {
            get 
            { 
                return (SPRibbon)Root; 
            }
            private set 
            { 
                Root = value; 
            }
        }

        protected internal RibbonBuildOptions RibbonBuildOptions
        {
            get 
            { 
                return (RibbonBuildOptions)Options; 
            }
        }

        public bool BuildRibbonAndInitialTab(string initialTabId)
        {
            if (string.IsNullOrEmpty(initialTabId))
                throw new ArgumentNullException("Initial tab for ribbon is null or undefined");

            if (InQuery)
                return false;

            RibbonBuildContext rbc = new RibbonBuildContext();
            rbc.InitialTabId = initialTabId;

            // If this is server rendered, then we want to set and use the initial 
            // scaling index for this first tab.
            if (!CUIUtility.IsNullOrUndefined(RibbonBuildOptions.AttachToDOM) &&
                    RibbonBuildOptions.AttachToDOM)
            {
                rbc.InitialScalingIndex = this.RibbonBuildOptions.InitialScalingIndex;
            }

            InQuery = true;
            DataQuery query = new DataQuery();
            query.TabQuery = false;
            query.Id = rbc.InitialTabId;
            query.QueryType = DataQueryType.RibbonVisibleTabDeep;
            query.Handler = new DataReturnedEventHandler(OnReturnRibbonAndInitialTab);
            query.Data = rbc;

            DataSource.RunQuery(query);
            return true;
        }

        public void BuildRibbonFromData(object dataNode, string initialTabId)
        {
            RibbonBuildContext rbc = new RibbonBuildContext();
            rbc.InitialTabId = initialTabId;

            DataQueryResult res = new DataQueryResult();
            res.Success = true;
            res.QueryData = dataNode;
            res.ContextData = rbc;
            OnReturnRibbonAndInitialTab(res);
        }

        private void OnReturnRibbonAndInitialTab(DataQueryResult res)
        {
            PMetrics.PerfMark(PMarker.perfCUIRibbonInitStart);

            RibbonBuildContext rbc = (RibbonBuildContext)res.ContextData;

            // Apply any extensions to the data.
            res.QueryData = ApplyDataExtensions(res.QueryData);
            Utility.EnsureCSSClassOnElement(Placeholder, "loaded");
            JSObject templates = DataNodeWrapper.GetFirstChildNodeWithName(res.QueryData, DataNodeWrapper.TEMPLATES);
            if (!CUIUtility.IsNullOrUndefined(templates))
                TemplateManager.Instance.LoadTemplates(templates);

            Ribbon = BuildRibbon(res.QueryData, rbc);
            Ribbon.RibbonBuilder = this;
            BuildClient.OnComponentCreated(Ribbon, Ribbon.Id);
            if (RibbonBuildOptions.Minimized)
            {
                Ribbon.MinimizedInternal = true;
            }
            else
            {
                Ribbon.MinimizedInternal = false;
                Tab firstTab = (Tab)Ribbon.GetChild(rbc.InitialTabId);
                if (!CUIUtility.IsNullOrUndefined(firstTab))
                {
                    // We need this in order to set the "ChangedByUser" property of the first
                    // TabSwitch command that comes out of the ribbon correctly.
                    firstTab.SelectedByUser = RibbonBuildOptions.InitialTabSelectedByUser;
                    Ribbon.MakeTabSelectedInternal(firstTab);
                }
            }

            Ribbon.ClientID = RibbonBuildOptions.ClientID;

            bool shouldAttach = !RibbonBuildOptions.Minimized && RibbonBuildOptions.AttachToDOM;

            if (shouldAttach)
            {
                // Scale the ribbon to the scaling index that matches the ribbon that was
                // rendered by the server.  This sets the in memory Ribbon structure to match
                // what was rendered by the server.  This is needed so that Ribbon.AttachInternal()
                // will work properly.  
                if (!((RibbonBuildOptions)Options).Minimized)
                {
                    // We subtract one from this scaling index because internally
                    // this scaling index is an entry into an array of "<ScaleStep>" so
                    // the MaxSize for all the groups is actually index "-1" and the first 
                    // step is index 0.
                    Ribbon.ScaleIndex(rbc.InitialScalingIndex - 1);
                }

                Ribbon.AttachInternal(true);

                // Attach to the QAT and Jewel
                if (!string.IsNullOrEmpty(RibbonBuildOptions.ShowQATId))
                    Ribbon.BuildAndSetQAT(RibbonBuildOptions.ShowQATId, true, DataSource);
                if (!string.IsNullOrEmpty(RibbonBuildOptions.ShowJewelId))
                    Ribbon.BuildAndSetJewel(RibbonBuildOptions.ShowJewelId, true, DataSource);

#if DEBUG
                // Validate that the server rendered ribbon is identical to the client rendered one
                // for this tab.
                if (Options.ValidateServerRendering)
                {
                    RibbonBuilder rb2 = new RibbonBuilder(this.RibbonBuildOptions,
                                                          this.Placeholder,
                                                          null);

                    DataSource ds = new DataSource(this.DataSource.DataUrl,
                                                   this.DataSource.Version,
                                                   this.DataSource.Lcid);

                    rb2.DataSource = ds;
                    SPRibbon r2 = rb2.BuildRibbon(res.QueryData, rbc);
                    r2.Id += "-client";
                    r2.ClientID = RibbonBuildOptions.ClientID + "-client";
                    r2.RibbonBuilder = this;
                    if (!RibbonBuildOptions.Minimized)
                        r2.Minimized = false;

                    // Clone all the peripheral sections for the client-rendering version
                    Div p_qrc = (Div)Browser.Document.GetById(RibbonBuildOptions.ClientID + "-" + RibbonPeripheralSection.QATRowCenter);
                    Div p_qrr = (Div)Browser.Document.GetById(RibbonBuildOptions.ClientID + "-" + RibbonPeripheralSection.QATRowRight);
                    Div p_trl = (Div)Browser.Document.GetById(RibbonBuildOptions.ClientID + "-" + RibbonPeripheralSection.TabRowLeft);
                    Div p_trr = (Div)Browser.Document.GetById(RibbonBuildOptions.ClientID + "-" + RibbonPeripheralSection.TabRowRight);

                    Div hiddenClonedPeripherals = new Div();
                    hiddenClonedPeripherals.Style.Display = "none";
                    Browser.Document.Body.AppendChild(hiddenClonedPeripherals);

                    Div clone;
                    if (null != p_qrc)
                    {
                        clone = (Div)p_qrc.CloneNode(true);
                        clone.Id = clone.Id.Replace(RibbonBuildOptions.ClientID, r2.ClientID);
                        hiddenClonedPeripherals.AppendChild(clone);
                    }
                    if (null != p_qrr)
                    {
                        clone = (Div)p_qrr.CloneNode(true);
                        clone.Id = clone.Id.Replace(RibbonBuildOptions.ClientID, r2.ClientID);
                        hiddenClonedPeripherals.AppendChild(clone);
                    }
                    if (null != p_trl)
                    {
                        clone = (Div)p_trl.CloneNode(true);
                        clone.Id = clone.Id.Replace(RibbonBuildOptions.ClientID, r2.ClientID);
                        hiddenClonedPeripherals.AppendChild(clone);
                    }
                    if (null != p_trr)
                    {
                        clone = (Div)p_trr.CloneNode(true);
                        clone.Id = clone.Id.Replace(RibbonBuildOptions.ClientID, r2.ClientID);
                        hiddenClonedPeripherals.AppendChild(clone);
                    }

                    r2.MakeTabSelectedInternal((Tab)r2.GetChild(rbc.InitialTabId));
                    r2.RefreshInternal();

                    if (!string.IsNullOrEmpty(RibbonBuildOptions.ShowQATId))
                        r2.BuildAndSetQAT(RibbonBuildOptions.ShowQATId, false, ds);
                    if (!string.IsNullOrEmpty(RibbonBuildOptions.ShowJewelId))
                        r2.BuildAndSetJewel(RibbonBuildOptions.ShowJewelId, false, ds);

                    r2.ScaleIndex(rbc.InitialScalingIndex - 1);
                    r2.CompleteConstruction();

                    // If this returns a message it means that it found some inconsistencies
                    // between the DOM Nodes
                    CompareNodes(Ribbon.ElementInternal, r2.ElementInternal);
                }
#endif
            }
            else
            {
                // Do the minimum amount of work necessary in order to be able to 
                // get the outer ribbon element and to be able to attach the Jewel and QAT.
                Ribbon.EnsureDOMElement();

                // Build the QAT and Jewel after the ribbon so that the placeholders
                // will have been created within the ribbon via Ribbon.RefreshInternal()
                if (!string.IsNullOrEmpty(RibbonBuildOptions.ShowQATId))
                    Ribbon.BuildAndSetQAT(RibbonBuildOptions.ShowQATId, false, DataSource);
                if (!string.IsNullOrEmpty(RibbonBuildOptions.ShowJewelId))
                    Ribbon.BuildAndSetJewel(RibbonBuildOptions.ShowJewelId, false, DataSource);

                // Remove anything else that is in the placeholder in case there is a temporary
                // animated gif or a static ribbon in there while the ribbon is loading.
                // We're doing this the slow way since partners might have a reference to this node
                Utility.RemoveChildNodesSlow(Placeholder);
                Placeholder.AppendChild(Ribbon.ElementInternal);
            }

            Ribbon.Scale();
            OnRootBuilt(Ribbon);
            BuildClient.OnComponentBuilt(Ribbon, Ribbon.Id);
            if (RibbonBuildOptions.LaunchedByKeyboard)
                Ribbon.SetFocusOnRibbon();

            PMetrics.PerfMark(PMarker.perfCUIRibbonInitPercvdEnd);
        }

        private void OnReturnTab(DataQueryResult res)
        {
            RibbonBuildContext rbc = (RibbonBuildContext)res.ContextData;

            if (res.Success)
            {
                JSObject ribbonNode =
                    DataNodeWrapper.GetFirstChildNodeWithName(res.QueryData,
                        DataNodeWrapper.RIBBON);
                JSObject tabsNode =
                    DataNodeWrapper.GetFirstChildNodeWithName(ribbonNode,
                        DataNodeWrapper.TABS);

                JSObject[] tabs = null;
                JSObject[] children = DataNodeWrapper.GetNodeChildren(tabsNode);
                if (children.Length == 0)
                {
                    JSObject ctxtabsNode =
                        DataNodeWrapper.GetFirstChildNodeWithName(ribbonNode,
                            DataNodeWrapper.CONTEXTUALTABS);
                    JSObject[] contextualGroups = DataNodeWrapper.GetNodeChildren(ctxtabsNode);
                    for (int i = 0; i < contextualGroups.Length; i++)
                    {
                        JSObject contextualGroup = contextualGroups[i];
                        tabs = DataNodeWrapper.GetNodeChildren(contextualGroup);
                        if (tabs.Length > 0)
                            break;
                    }
                }
                else
                {
                    tabs = DataNodeWrapper.GetNodeChildren(tabsNode);
                }

                JSObject templatesNode = DataNodeWrapper.GetFirstChildNodeWithName(res.QueryData,
                        DataNodeWrapper.TEMPLATES);

                // Apply any extensions to the template data.
                templatesNode = (JSObject)ApplyDataExtensions(templatesNode);

                TemplateManager.Instance.LoadTemplates(templatesNode);

                // Apply any extensions to the tab data.
                // In this case we do not want to apply the extensions to the whole hierarchy
                // including <CommandUI>, <Ribbon> etc because this query is really only for
                // a specific tab.
                object tabData = ApplyDataExtensions(tabs[0]);

                FillTab(rbc.InitializedTab, tabData, rbc);
                // This may need to be parametrized so that tabs can be inited 
                // without automatically getting selected when the initing is done.
                rbc.InitializedTab.Ribbon.MakeTabSelectedInternal(rbc.InitializedTab);
                rbc.InitializedTab.OnDelayedInitFinished(true);
            }
            // TODO: how to handle failures

#if PERF_METRICS
            PMetrics.PerfMark(PMarker.perfCUIRibbonTabSwitchWarmPercvdEnd);
#endif
        }

        private SPRibbon BuildRibbon(object data, RibbonBuildContext rbc)
        {
            JSObject ribbonElement = DataNodeWrapper.GetFirstChildNodeWithName(data,
                                                                             DataNodeWrapper.RIBBON);

            if (CUIUtility.IsNullOrUndefined(ribbonElement))
                throw new ArgumentNullException("No ribbon element was present in the data");

            Ribbon = new SPRibbon(DataNodeWrapper.GetAttribute(ribbonElement, "Id"),
                                  DataNodeWrapper.GetNodeAttributes(ribbonElement).To<RibbonProperties>());

            //REVIEW(josefl) Should this be configurable?
            Ribbon.UseDataCookie = true;

            // Handle the Tabs
            // The XML structure that we are looking at is <Ribbon><Tabs><Tab/><Tab/>...
            JSObject[] tabChildren = DataNodeWrapper.GetNodeChildren(
                DataNodeWrapper.GetFirstChildNodeWithName(ribbonElement, DataNodeWrapper.TABS));

            AddTabsToRibbon(tabChildren, "", rbc);

            // Handle the Contextual Tab Groups
            // The XML structure that we are looking at is <Ribbon><ContextualTabs><ContextualGroup>...
            object contextualTabs = DataNodeWrapper.GetFirstChildNodeWithName(ribbonElement, DataNodeWrapper.CONTEXTUALTABS);
            if (!CUIUtility.IsNullOrUndefined(contextualTabs))
            {
                JSObject[] cgChildren = DataNodeWrapper.GetNodeChildren(contextualTabs);
                bool shownContextualGroupsSpecified =
                    !CUIUtility.IsNullOrUndefined(RibbonBuildOptions.ShownContextualGroups);
                for (int j = 0; j < cgChildren.Length; j++)
                {
                    if (shownContextualGroupsSpecified)
                    {
                        // Show the contextual group if it has been explicitly requested/ made available
                        string cgId = DataNodeWrapper.GetAttribute(cgChildren[j], DataNodeWrapper.ID);
                        if (!string.IsNullOrEmpty(cgId))
                        {
                            if (!RibbonBuildOptions.ShownContextualGroups.ContainsKey(cgId) || 
                                    CUIUtility.IsNullOrUndefined(RibbonBuildOptions.ShownContextualGroups[cgId]))
                                continue;
                        }
                    }
                    AddContextualGroup(cgChildren[j], rbc);
                }
            }

            return Ribbon;
        }

        private void AddContextualGroup(JSObject data, RibbonBuildContext rbc)
        {
            ContextualColor color = ContextualColor.None;
            string contextualGroupId = DataNodeWrapper.GetAttribute(data, DataNodeWrapper.ID);

            // If the contextual tab group has been normalized, then we build the tabs as regular tabs
            bool normalized = !CUIUtility.IsNullOrUndefined(RibbonBuildOptions.NormalizedContextualGroups) &&
                              RibbonBuildOptions.NormalizedContextualGroups.ContainsKey(contextualGroupId) &&
                              RibbonBuildOptions.NormalizedContextualGroups[contextualGroupId];

            // If the contextual group has been normalized, it means that all of its tabs should
            // behave like regular tabs on this page so we do not need to create the contextual
            // group object in this case.
            if (!normalized)
            {
                switch (DataNodeWrapper.GetAttribute(data, DataNodeWrapper.COLOR))
                {
                    case DataNodeWrapper.DARKBLUE:
                        color = ContextualColor.DarkBlue;
                        break;
                    case DataNodeWrapper.LIGHTBLUE:
                        color = ContextualColor.LightBlue;
                        break;
                    case DataNodeWrapper.MAGENTA:
                        color = ContextualColor.Magenta;
                        break;
                    case DataNodeWrapper.GREEN:
                        color = ContextualColor.Green;
                        break;
                    case DataNodeWrapper.ORANGE:
                        color = ContextualColor.Orange;
                        break;
                    case DataNodeWrapper.PURPLE:
                        color = ContextualColor.Purple;
                        break;
                    case DataNodeWrapper.TEAL:
                        color = ContextualColor.Teal;
                        break;
                    case DataNodeWrapper.YELLOW:
                        color = ContextualColor.Yellow;
                        break;
                    default:
                        color = ContextualColor.None;
                        break;
                }

                Ribbon.AddContextualGroup(contextualGroupId,
                                          DataNodeWrapper.GetAttribute(data, DataNodeWrapper.TITLE),
                                          color,
                                          DataNodeWrapper.GetAttribute(data, DataNodeWrapper.COMMAND));
            }

            JSObject[] tabChildren = DataNodeWrapper.GetNodeChildren(data);
            if (!normalized)
            {
                // This array will usually have one or two entries and at the very most three
                // So we are not using an iterator or caching tabChildren.Length etc.
                for (int i = 0; i < tabChildren.Length; i++)
                {
                    // If the initially visible tabId is in a contextual group, then we make that contextual
                    // group initially visible.
                    string tabId = DataNodeWrapper.GetAttribute(tabChildren[i], DataNodeWrapper.ID);
                    if (tabId == rbc.InitialTabId)
                    {
                        if (CUIUtility.IsNullOrUndefined(RibbonBuildOptions.InitiallyVisibleContextualGroups))
                            RibbonBuildOptions.InitiallyVisibleContextualGroups = new Dictionary<string, bool>();
                        RibbonBuildOptions.InitiallyVisibleContextualGroups[contextualGroupId] = true;
                        break;
                    }
                }
            }

            AddTabsToRibbon(tabChildren, normalized ? "" : contextualGroupId, rbc);
        }

        private void AddTabsToRibbon(JSObject[] tabs, string contextualGroupId, RibbonBuildContext rbc)
        {
            bool shownTabsSpecified = !CUIUtility.IsNullOrUndefined(RibbonBuildOptions.ShownTabs);
            for (int j = 0; j < tabs.Length; j++)
            {
                if (shownTabsSpecified)
                {
                    // Only construct the tabs/tabheaders that have been made available
                    string tabId = DataNodeWrapper.GetAttribute(tabs[j], DataNodeWrapper.ID);
                    if (!string.IsNullOrEmpty(tabId))
                    {
                        if (!RibbonBuildOptions.ShownTabs.ContainsKey(tabId) ||
                                CUIUtility.IsNullOrUndefined(RibbonBuildOptions.ShownTabs[tabId]))
                            continue;
                    }
                }
                Tab tab = BuildTab(tabs[j], rbc, contextualGroupId);
                Ribbon.AddChild(tab);
            }
        }

        private Tab BuildTab(object data,
                             RibbonBuildContext rbc,
                             string contextualGroupId)
        {
            Tab tab;
            string id = DataNodeWrapper.GetAttribute(data, DataNodeWrapper.ID);
            string title = DataNodeWrapper.GetAttribute(data, DataNodeWrapper.TITLE);
            string description = DataNodeWrapper.GetAttribute(data, DataNodeWrapper.DESCRIPTION);
            string command = DataNodeWrapper.GetAttribute(data, DataNodeWrapper.COMMAND);
            string cssclass = DataNodeWrapper.GetAttribute(data, DataNodeWrapper.CSSCLASS);
            if (string.IsNullOrEmpty(contextualGroupId))
            {
                tab = Ribbon.CreateTab(id,
                                       title,
                                       description,
                                       command,
                                       cssclass);
            }
            else
            {
                tab = Ribbon.CreateContextualTab(id,
                                                 title,
                                                 description,
                                                 command,
                                                 contextualGroupId,
                                                 cssclass);
                // Make sure that the tabs that are in the initially shown contextual groups are visible
                if (!CUIUtility.IsNullOrUndefined(RibbonBuildOptions.InitiallyVisibleContextualGroups) &&
                        RibbonBuildOptions.InitiallyVisibleContextualGroups.ContainsKey(contextualGroupId) &&
                        RibbonBuildOptions.InitiallyVisibleContextualGroups[contextualGroupId])
                {
                    tab.VisibleInternal = true;
                }
            }

            // If the Tab is being inited in a shallow way, then we set the callback so that
            // the builder will be called if the Tab is selected.
            // We set up the Tab to be delay initialized and give it its own copy of the build context
            JSObject[] children = DataNodeWrapper.GetNodeChildren(data);
            if (children.Length == 0)
                tab.SetDelayedInitData(new DelayedInitHandler(DelayInitTab), data, rbc.Clone());
            else
                FillTab(tab, data, rbc);

            return tab;
        }

        private void FillTab(Tab tab, object data, RibbonBuildContext rbc)
        {
            JSObject groupsNode = DataNodeWrapper.GetFirstChildNodeWithName(data, DataNodeWrapper.GROUPS);
            JSObject[] groupChildren = DataNodeWrapper.GetNodeChildren(groupsNode);
            Dictionary<string, string> emptyTrimmedGroupIds = new Dictionary<string, string>();
            for (int i = 0; i < groupChildren.Length; i++)
            {
                if (IsNodeTrimmed(groupChildren[i]))
                    continue;

                Group group = BuildGroup(groupChildren[i], rbc);
                // If the build option TrimEmptyGroups is null, and the Group is empty
                // then null is returned by BuildGroup()
                if (!CUIUtility.IsNullOrUndefined(group))
                {
                    tab.AddChild(group);
                }
                else
                {
                    // If the group has an Id, then we store it so that we can ignore any scaling
                    // information that relates it it.  If it does not have an id, then any scaling info
                    // will not work anyways and it is an invalid node.  Groups must have ids.
                    string id = DataNodeWrapper.GetAttribute(groupChildren[i], DataNodeWrapper.ID);
                    if (!string.IsNullOrEmpty(id))
                        emptyTrimmedGroupIds[id] = id;
                }
            }

            JSObject scaling = DataNodeWrapper.GetFirstChildNodeWithName(data,
                                                                   DataNodeWrapper.SCALING);
            JSObject[] children = DataNodeWrapper.GetNodeChildren(scaling);
            string _scaleWarningMessage = "";
            bool _scaleWarning = false;

            for (int i = 0; i < children.Length; i++)
            {
                string name = DataNodeWrapper.GetNodeName(children[i]);
                string groupId = DataNodeWrapper.GetAttribute(children[i], DataNodeWrapper.GROUPID);

                if (name == DataNodeWrapper.MAXSIZE)
                {
                    // Don't include the scale step if the group that it refers to has been trimmed
                    if (IsIdTrimmed(groupId) || (emptyTrimmedGroupIds.ContainsKey(groupId) &&
                            !CUIUtility.IsNullOrUndefined(emptyTrimmedGroupIds[groupId])))
                        continue;

                    tab.Scaling.SetGroupMaxSize(groupId,
                                                DataNodeWrapper.GetAttribute(children[i], DataNodeWrapper.SIZE));
                }
                else if (name == DataNodeWrapper.SCALE)
                {
                    // Don't include the scale step if the group that it refers to has been trimmed
                    if (IsIdTrimmed(groupId) || (emptyTrimmedGroupIds.ContainsKey(groupId) &&
                            !CUIUtility.IsNullOrUndefined(emptyTrimmedGroupIds[groupId])))
                        continue;

                    tab.Scaling.AddScalingStep(new ScalingStep(groupId,
                                                               DataNodeWrapper.GetAttribute(children[i], DataNodeWrapper.SIZE),
                                                               DataNodeWrapper.GetAttribute(children[i], DataNodeWrapper.POPUPSIZE),
                                                               _scaleWarningMessage,
                                                               _scaleWarning));
                    _scaleWarningMessage = "";
                    _scaleWarning = false;
                }
                else if (name == DataNodeWrapper.LOWSCALEWARNING)
                {
                    _scaleWarningMessage = DataNodeWrapper.GetAttribute(children[i], DataNodeWrapper.MESSAGE);
                    _scaleWarning = true;
                }
                else
                {
                    throw new InvalidOperationException("Was expecting a node with name MaxSize or Scale.");
                }
            }

            // Start at the largest scale
            tab.ScaleMax();
        }

        private Component DelayInitTab(Component component,
                                       object data,
                                       object buildContext)
        {
            RibbonBuildContext rbc = (RibbonBuildContext)buildContext;
            Tab tab = (Tab)component;

            rbc.InitializedTab = (Tab)component;
            // If the data node does not have children, then it means that this tab
            // was shallowly fetched from the server.  In this case we need to run
            // a query to get the whole node with all of its controls from the server.
            JSObject[] children = DataNodeWrapper.GetNodeChildren(data);
            if (children.Length == 0)
            {
                // TODO: implement this so that the asynchronous part works
                // Find out if we even need to fetch the tabs asynchronously
                // or if we can get away with just initializing them asynchronously
                DataQuery query = new DataQuery();
                query.TabQuery = true;
                query.Id = rbc.InitializedTab.Id;
                query.QueryType = DataQueryType.RibbonTab;
                query.Handler = new DataReturnedEventHandler(this.OnReturnTab);
                query.Data = rbc;
                DataSource.RunQuery(query);
                return null;
            }

            FillTab(tab, data, rbc);
            tab.OnDelayedInitFinished(true);
            // TODO(josefl): this should later be an idle task registration instead of a hard call
            Ribbon.Refresh();

            return tab;
        }

        private Group BuildGroup(object data, RibbonBuildContext rbc)
        {
            string templateName = DataNodeWrapper.GetAttribute(data, DataNodeWrapper.TEMPLATE);

            Template template = TemplateManager.Instance.GetTemplate(templateName);
            if (CUIUtility.IsNullOrUndefined(template))
                throw new ArgumentOutOfRangeException("A template with name: " + templateName + " could not be loaded.");

            JSObject controlsData = null;
            JSObject[] dataChildren = DataNodeWrapper.GetNodeChildren(data);
            for (int i = 0; i < dataChildren.Length; i++)
            {
                if (DataNodeWrapper.GetNodeName(dataChildren[i]) == DataNodeWrapper.CONTROLS)
                {
                    controlsData = dataChildren[i];
                    break;
                }
            }

            if (CUIUtility.IsNullOrUndefined(controlsData))
                throw new InvalidOperationException("No Controls node found in this Group tag.");
            JSObject[] children = DataNodeWrapper.GetNodeChildren(controlsData);

            bool groupIsEmpty = true;
            Dictionary<string, List<Control>> controls = new Dictionary<string, List<Control>>();

            int len = children.Length;
            for (int i = 0; i < len; i++)
            {
                // Don't build controls that have been trimmed
                if (IsNodeTrimmed(children[i]))
                    continue;

                // The group has one or more controls in it
                groupIsEmpty = false;
                Control control = BuildControl(children[i], rbc);

                if (!controls.ContainsKey(control.TemplateAlias) || 
                        CUIUtility.IsNullOrUndefined(controls[control.TemplateAlias]))
                    controls[control.TemplateAlias] = new List<Control>();

                controls[control.TemplateAlias].Add(control);
            }

            if (RibbonBuildOptions.TrimEmptyGroups && groupIsEmpty)
                return null;

            string id = DataNodeWrapper.GetAttribute(data, DataNodeWrapper.ID);
            string title = DataNodeWrapper.GetAttribute(data, DataNodeWrapper.TITLE);
            string description = DataNodeWrapper.GetAttribute(data, DataNodeWrapper.DESCRIPTION);
            string command = DataNodeWrapper.GetAttribute(data, DataNodeWrapper.COMMAND);
            Group group = template.CreateGroup(Ribbon,
                                               id,
                                               DataNodeWrapper.GetNodeAttributes(data).To<GroupProperties>(),
                                               title,
                                               description,
                                               command,
                                               controls,
                                               null);
            return group;
        }

#if DEBUG

        private string CompareNodes(HtmlElement elm1, HtmlElement elm2)
        {
            // Compare the number of attributes
            // It is more usefull to get an error message with the name of the mismatched attributes
            // if (elm1.Attributes.Length != elm2.Attributes.Length)
            //     return ConstructCompareErrorMessage(elm1, elm2, "Nodes have different numbers of attributes.");

            // First compare the Attributes
            string message = CompareNodeAttributes(elm1, elm2);
            if (!string.IsNullOrEmpty(message))
                return message;

            message = CompareNodeAttributes(elm2, elm1);
            if (!string.IsNullOrEmpty(message))
                return message;

            // Now compare number of children
            if (elm1.ChildNodes.Length != elm2.ChildNodes.Length)
                return (ConstructCompareErrorMessage(elm1, elm2, "Nodes have different number of children: " + elm1.ChildNodes.Length + " and " + elm2.ChildNodes.Length));

            // Now recurse over the children
            for (int i = 0; i < elm1.ChildNodes.Length; i++)
            {
                message = CompareNodes((HtmlElement)elm1.ChildNodes[i], (HtmlElement)elm2.ChildNodes[i]);
                if (!string.IsNullOrEmpty(message))
                    return message;
            }

            return "";
        }

        private string CompareNodeAttributes(HtmlElement elm1, HtmlElement elm2)
        {
            if (CUIUtility.IsNullOrUndefined(elm1.Attributes))
            {
                if (elm1.InnerText != elm2.InnerText)
                    return ConstructCompareErrorMessage(elm1, elm2, "Text node text mismatched.");
                else
                    return "";
            }
            for (int i = 0; i < elm1.Attributes.Length; i++)
            {
                DomAttribute attr1 = elm1.Attributes[i];
                DomAttribute attr2 = elm2.Attributes.GetNamedItem(attr1.Name);

                // For the unselectable attribute, the client rendered ribbon adds this as an asynchronous
                // task so it does not appear in this attribute collection.
                if (CUIUtility.IsNullOrUndefined(attr2) && attr1.Name != "unselectable")
                {
                    // The exceptions to this:
                    // The "_events" attribute is used by Microsoft Ajax to attach events to the DOM node so it will
                    // not be present in the server rendered DOM.
                    if (attr1.Name != "_events")
                        return ConstructCompareErrorMessage(elm1, elm2, "Attribute: \"" + attr1.Name + "\" is only present in one node.");
                    else
                        continue;
                }
                else if (attr1.Name == "unselectable")
                {
                    string field = elm2.GetAttribute("unselectable");
                    if (string.IsNullOrEmpty(field) || field != attr1.Value)
                        return ConstructCompareErrorMessage(elm1, elm2, "Attribute values for \"unselectable\" does not match.");
                }

                if (attr1.Name.ToLower() != attr2.Name.ToLower())
                    return ConstructCompareErrorMessage(elm1, elm2, "Attributes at index " + i.ToString() + " have different names: \"" + attr1.Name + "\" and \"" + attr2.Name + "\"");

                if (attr1.Value.Trim() != attr2.Value.Trim())
                {
                    // Handle exceptions here
                    // We ignore the oncontextmenu attribute because this one is attached in the client
                    // via a MicrosoftAjax event handler.
                    if (attr1.Name == "oncontextmenu" &&
                        !string.IsNullOrEmpty(elm1.ClassName) &&
                        elm1.ClassName.IndexOf("ms-cui-ribbon") != -1)
                    {
                    }
                    else if (attr1.Name == "class" &&
                             attr1.Value.Replace("ms-cui-disabled", "").Trim() ==
                             attr2.Value.Replace("ms-cui-disabled", "").Trim())
                    {
                    }
                    // Some elements need unique ids to function correctly. If the only difference is "-client"
                    // then we can ignore the issue.
                    else if (attr1.Name == "id" &&
                             attr1.Value.Replace("-client", "").Trim() ==
                             attr2.Value.Replace("-client", "").Trim())
                    {
                    }
                    // We ignore the case where images are not the same size because the image sizes
                    // are not set until the <img> tag is actually rendered so one of these will be "0"
                    // because it has not been put into the DOM.
                    else if (elm1.TagName.ToLower() == "img" &&
                             (attr1.Name == "height" || attr1.Name == "width"))
                    {
                    }
                    else if (elm1.TagName.ToLower() == "label" && attr1.Name == "for")
                    {
                    }
                    else if (elm1.TagName.ToLower() == "input" && attr1.Name.ToLower() == "maxlength")
                    {
                    }
                    else
                    {
                        return ConstructCompareErrorMessage(elm1, elm2, "Attribute: \"" + attr1.Name + "\" has different values: \"" + attr1.Value + "\" and \"" + attr2.Value + "\"");
                    }
                }
            }

            return "";
        }

        private string ConstructCompareErrorMessage(HtmlElement elm1, HtmlElement elm2, string error)
        {
            string message = "COMPARE ERROR: " + error + "\n\n";
            message += "Closest Ancestor Id: \"" + GetClosestAncestorIdAndLevel(elm1) + "\"\n\n";
            message += "Element1 ClassName: " + elm1.ClassName + "\n";
            message += "Element1 InnerHTML: " + elm1.InnerHtml;
            message += "\n\n\n\n";
            message += "Element2 ClassName: " + elm2.ClassName + "\n";
            message += "Element2 InnerHTML: " + elm2.InnerHtml;

            // Give option to enter debugger at point of failure
            if (Browser.Window.Confirm(message + "\n\nDo you want to debug?"))
                Debugger.Break();
            
            return message;
        }

        private string GetClosestAncestorIdAndLevel(HtmlElement elm)
        {
            string message = "Found no id in ancestor chain";
            int level = 0;
            while (!CUIUtility.IsNullOrUndefined(elm))
            {
                if (!string.IsNullOrEmpty(elm.Id))
                {
                    message = elm.Id + " at level " + level;
                    break;
                }
                elm = (HtmlElement)elm.ParentNode;
                level++;
            }

            return message;
        }

#endif

    }
}
