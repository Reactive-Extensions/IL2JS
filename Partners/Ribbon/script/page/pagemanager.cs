using System;
using System.Collections.Generic;
using Commands;
using Microsoft.LiveLabs.Html;

namespace Ribbon.Page
{
    /// <summary>
    /// A Component of a page.  Implementing this will allow it to interact with the Page Framework.
    /// </summary>
    public class PageComponent : ICommandHandler
    {
        /// <summary>
        /// Allows the component to initialize itself.
        /// </summary>
        public virtual void Init()
        {
        }

        /// <summary>
        /// Gets a string[] of commandids that this component is interested in.  These commands will be executed on the component no matter if the component has the focus or not.
        /// </summary>
        /// <returns>a string[] of commandids</returns>
        public virtual string[] GetGlobalCommands()
        {
            return null;
        }

        /// <summary>
        /// Gets a string[] of commanids that this component is interest in.  These commands will only be executed on the component if it has the focus.
        /// </summary>
        /// <returns>a string[] of commandids</returns>
        public virtual string[] GetFocusedCommands()
        {
            return null;
        }

        /// <summary>
        /// Called in order to have this component handle a command that it has registered for.
        /// </summary>
        /// <param name="commandId">the id of the command</param>
        /// <param name="properties">the properties of the command</param>
        /// <param name="sequence">the sequence number of the command</param>
        /// <returns>true if the command was successfully handled by the component</returns>
        public virtual bool HandleCommand(string commandId, Dictionary<string, string> properties, int sequence)
        {
            return false;
        }

        /// <summary>
        /// Called to find out if this component can currently handle a command.
        /// </summary>
        /// <param name="commandId">the name of the command</param>
        /// <returns>true if this component can currently handle the command</returns>
        public virtual bool CanHandleCommand(string commandId)
        {
            return false;
        }

        /// <summary>
        /// Whether this component can have focus in the page or not.
        /// </summary>
        /// <returns>true if this component can have focus</returns>
        public virtual bool IsFocusable()
        {
            return false;
        }

        /// <summary>
        /// Called to notify this component that it now has the focus.
        /// </summary>
        /// <returns>true if this component successfully received the focus</returns>
        public virtual bool ReceiveFocus()
        {
            return false;
        }

        /// <summary>
        /// Called to cause this component to give up the focus.
        /// </summary>
        /// <returns>true if this component successfully gave up the focus.</returns>
        public virtual bool YieldFocus()
        {
            return true;
        }

        /// <summary>
        /// Gets the id of this PageComponent
        /// </summary>
        /// <returns>the id of this PageComponent</returns>
        public virtual string GetId()
        {
            return "PageComponent";
        }
    }

    /// <summary>
    /// This class is the overseer of the page.  It is a singleton and its instance can be gotten through PageManager.Instance.
    /// This class is responsible for bootstrapping the page runtime environment.  Page components register with it
    /// and it initizlizes them along with the FocusManager and the CommandDispatcher.
    /// </summary>
    public class PageManager : RootUser, ICommandHandler, IRootBuildClient
    {
        public PageManager()
        {
            _components = new List<PageComponent>();
            _componentIds = new Dictionary<string, PageComponent>();
            _commandDispatcher = new CommandDispatcher();
            _focusManager = new FocusManager(this);
            _undoManager = new UndoManager(this);
            _roots = new List<Root>();
            Browser.Window.Unload += OnPageUnload;
        }

        public static void Initialize()
        {
            if (!CUIUtility.IsNullOrUndefined(_instance))
            {
                return;

            }
            _instance = CreatePageManager();
            // REVIEW(josefl): should we call this here or in the callback?  Performance implications. 
            // This or parts of it should be defered.
            // Instance.InitializeInternal();
            _instance.InitializeInternal();
        }

        protected static PageManager CreatePageManager()
        {
            return new PageManager();
        }

        public void InitializeInternal()
        {
            _commandDispatcher.Initialize();
            _undoManager.Initialize();
            _focusManager.Initialize();
            _commandDispatcher.RegisterCommandHandler(CommandIds.ApplicationStateChanged, this);
        }

        private void OnPageUnload(HtmlEvent args)
        {
            Dispose();
        }

        protected virtual void Dispose()
        {
            // In case there is any pending state in any controls that has not been
            // sent up to the appliation yet.
            if (!CUIUtility.IsNullOrUndefined(Ribbon))
                Ribbon.FlushPendingChanges();

            _focusManager = null;
            _undoManager = null;
            _commandDispatcher = null;
            _roots = null;
            _components = null;
            Browser.Window.Unload -= OnPageUnload;
        }

        protected static PageManager _instance;
        public static PageManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    PageManager.Initialize();
                }
                return _instance;
            }
        }

        CommandDispatcher _commandDispatcher = null;
        public CommandDispatcher CommandDispatcher
        {
            get
            {
                return _commandDispatcher;
            }
        }

        FocusManager _focusManager;
        public FocusManager FocusManager
        {
            get
            {
                return _focusManager;
            }
        }

        UndoManager _undoManager;
        public UndoManager UndoManager
        {
            get
            {
                return _undoManager;
            }
        }

        private Dictionary<string, EventHandler> _eventList;
        private Dictionary<string, EventHandler> Events
        {
            get
            {
                if (_eventList == null)
                {
                    _eventList = new Dictionary<string, EventHandler>();
                }
                return _eventList;
            }
        }

        SPRibbon _ribbon;
        public SPRibbon Ribbon
        {
            get
            {
                return _ribbon;
            }
            set
            {
                if (value == _ribbon)
                    return;

                // If the ribbon is being set to null, then we remove it from 
                // the list of roots.
                if (CUIUtility.IsNullOrUndefined(value) &&
                    !CUIUtility.IsNullOrUndefined(_ribbon))
                {
                    RemoveRoot(_ribbon);
                    _ribbon = null;
                }
                // Also add the ribbon to the list of roots
                else if (!_roots.Contains(value))
                {
                    AddRoot(value);
                    _ribbon = value;
                }
            }
        }

        public event EventHandler RibbonInited
        {
            add
            {
                Events["RibbonInited"] = value;
            }
            remove
            {
                Events.Remove("RibbonInited");
            }
        }
        /// <summary>
        /// Part of IRootBuildClient.  Called when the root has been build and a reference to is is available.
        /// This adds the roots and ribbons to this PageManager so that they can be managed :-)
        /// </summary>
        /// <param name="root"></param>
        public virtual void OnComponentBuilt(Root root, string componentId)
        {
            PollRootState(root);
            // If the ribbon just got built then we send out the appropriate event
            if (typeof(SPRibbon).IsInstanceOfType(root))
            {
                EventHandler handler = Events.ContainsKey("RibbonInited") ? Events["RibbonInited"] : null;
                if (handler != null)
                {
                    handler(this, EventArgs.Empty);
                }
            }
        }

        public virtual void OnComponentCreated(Root root, string componentId)
        {
            // Set THE Ribbon for the page if it has not already been set.
            // REVIEW(josefl): Revisit this if we somehow allow more than one ribbon per page.
            if (typeof(SPRibbon).IsInstanceOfType(root) && CUIUtility.IsNullOrUndefined(Ribbon))
                Ribbon = (SPRibbon)root;
            else
                AddRoot(root);
        }

        List<Root> _roots;
        /// <summary>
        /// Adds a commandui Root to this PageManager
        /// </summary>
        /// <param name="root">the Root that is to be added</param>
        public void AddRoot(Root root)
        {
            if (_roots.Contains(root))
                throw new IndexOutOfRangeException("This Root has already been added to the PageManager");
            _roots.Add(root);
            root.RootUser = this;
        }

        /// <summary>
        /// Removes a root from this PageManger.
        /// </summary>
        /// <param name="root">the Root that is to be removed</param>
        public void RemoveRoot(Root root)
        {
            if (!_roots.Contains(root))
                throw new IndexOutOfRangeException("This Root has not been added to the PageManager.");
            _roots.Remove(root);
            root.RootUser = null;
        }

        List<PageComponent> _components;
        Dictionary<string, PageComponent> _componentIds;

        public PageComponent GetPageComponentById(string id)
        {
            if (_componentIds.ContainsKey(id))
                return (PageComponent)_componentIds[id];

            throw new IndexOutOfRangeException("Unable to find PageComponent with id: " + id);
        }

        /// <summary>
        /// Add a page component to this PageManager.
        /// </summary>
        /// <param name="component">the component that is to be added</param>
        public void AddPageComponent(PageComponent component)
        {
            // If a component with this id has already been added, we throw
            string compId = component.GetId();
            if (_componentIds.ContainsKey(compId) && !CUIUtility.IsNullOrUndefined(_componentIds[compId]))
                throw new ArgumentNullException("A PageComponent with id: " + component.GetId() + " has already been added to the PageManger.");


            // REVIEW(josefl): for performance reasons, we may want to move this into an Init() method
            if (!CUIUtility.IsNullOrUndefined(_components) && !_components.Contains(component))
            {
                _componentIds[component.GetId()] = component;
                component.Init();
                _commandDispatcher.RegisterMultipleCommandHandler((ICommandHandler)component, 
                                                                   component.GetGlobalCommands());
                _components.Add(component);
                if (component.IsFocusable())
                    _focusManager.AddPageComponent(component);
            }
        }

        /// <summary>
        /// Removes a page component from this PageManager.
        /// </summary>
        /// <param name="component">the component that is to be removed</param>
        public void RemovePageComponent(PageComponent component)
        {
            if (CUIUtility.IsNullOrUndefined(_components) || !_components.Contains(component))
                return;

            _commandDispatcher.UnregisterMultipleCommandHandler((ICommandHandler)component,
                                                                 component.GetGlobalCommands());
            _components.Remove(component);
            // Add this component to the focus manager if it is focusable
            if (component.IsFocusable())
                _focusManager.RemovePageComponent(component);

            // Remove the component from the hash of their ids
            _componentIds[component.GetId()] = null;
        }

        #region IRootUser Implementation
        public override bool ExecuteRootCommand(string commandId,
                                                Dictionary<string, string> properties,
                                                CommandInformation commandInfo,
                                                Root root)
        {
            return CommandDispatcher.ExecuteCommand(commandId, properties);
        }

        public override bool IsRootCommandEnabled(string commandId, Root root)
        {
            return CommandDispatcher.IsCommandEnabled(commandId);
        }
        public override void OnRootRefreshed(Root root)
        {
            if (!CUIUtility.IsNullOrUndefined(root))
                PollRootState(root);
        }
        #endregion

        #region ICommandHandler Implementation
        public bool HandleCommand(string commandId, Dictionary<string, string> properties, int sequenceNumber)
        {
            if (commandId == CommandIds.ApplicationStateChanged)
            {
                // TODO: this should be done asynchronously with idle time
                for (int i = 0; i < _roots.Count; i++)
                {
                    Root root = (Root)_roots[i];
                    PollRootState(root);
                    if (root.Dirty)
                        root.RefreshInternal();
                }
                return true;
            }
            return false;
        }

        // TODO(josefl): consider removing this after alpha and using a more
        // explicit way like a ExecuteRootQueryCommand method.
        bool _rootPollingInProgress = false;
        public bool RootPollingInProgress
        {
            get
            {
                return _rootPollingInProgress;
            }
        }

        public void PollRootState(Root root)
        {
            try
            {
                _rootPollingInProgress = true;
                root.PollForStateAndUpdate();
            }
            finally
            {
                _rootPollingInProgress = false;
            }
        }


        public bool ChangeCommandContext(string commandContextId)
        {
            if (!CUIUtility.IsNullOrUndefined(Ribbon))
                return Ribbon.SelectTabByCommand(commandContextId);
            return false;
        }

        public bool CanHandleCommand(string commandId)
        {
            return commandId == CommandIds.ApplicationStateChanged;
        }
        #endregion

        public void RestoreFocusToRibbon()
        {
            if (!Ribbon.RestoreFocus())
                Ribbon.SetFocus();
        }
    }
}
