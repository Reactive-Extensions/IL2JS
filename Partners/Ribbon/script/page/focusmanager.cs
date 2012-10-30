using System;
using System.Collections.Generic;

namespace Ribbon.Page
{
    public class FocusManager : CommandDispatcher, ICommandHandler
    {
        List<ICommandHandler> _activeComponents;
        Dictionary<string, int> _registeredCommands;
        PageManager _pageManager;
        Dictionary<ICommandHandler, PageComponent> _focusedComponents;

        /// <summary>
        /// Creates a new FocusManager
        /// </summary>
        /// <param name="pageManager">the PageManager for the page</param>
        internal FocusManager(PageManager pageManager)
        {
            _pageManager = pageManager;
            _components = new List<PageComponent>();
            _registeredCommands = new Dictionary<string, int>();
            _activeComponents = new List<ICommandHandler>();
            _focusedComponents = new Dictionary<ICommandHandler, PageComponent>();
        }

        internal override void Initialize()
        {
        }

        private void HandleQuickLookup()
        {
            _focusedComponents = new Dictionary<ICommandHandler, PageComponent>();
            int length = _activeComponents.Count;
            for (int i = 0; i < length; i++)
            {
                PageComponent comp = (PageComponent)_activeComponents[i];

                // We just want to have a normal javascript associated array with the object
                // as the key.  Script# does not have a object keyed Dictionary so we just
                // do it this way and it will compile correctly
                _focusedComponents[comp] = comp;
            }
        }

        /// <summary>
        /// Requests that the focus be put on the passed in component
        /// </summary>
        /// <param name="component">the component that focus is being requested for</param>
        /// <returns>true if the focus was successfully transferred to the passed in PageComponent</returns>
        public bool RequestFocusForComponent(PageComponent component)
        {
            if (CUIUtility.IsNullOrUndefined(component))
                return false;

            if (_activeComponents.Contains(component))
                return true;

            _activeComponents.Add(component);
            HandleQuickLookup();
            component.ReceiveFocus();
            return true;
        }

        /// <summary>
        /// Requests that the focus be released from the passed in component
        /// </summary>
        /// <param name="component">the component that focus is being requested for</param>
        /// <returns>true if the focus was successfully transferred to the passed in PageComponent</returns>
        public bool ReleaseFocusFromComponent(PageComponent component)
        {
            if (CUIUtility.IsNullOrUndefined(component))
                return false;

            if (!_activeComponents.Contains(component))
                return true;

            _activeComponents.Remove(component);
            HandleQuickLookup();
            component.YieldFocus();
            return true;
        }

        /// <summary>
        /// Removes the focus so that no PageComponent has the focus
        /// </summary>
        /// <returns>true if the focus was successfully removed</returns>
        public bool ReleaseAllFoci()
        {
            // We need to do this here in case some of the components execute commands in their "Yield()" method.
            // This is equivalent to a call to HandleQuickLookup()
            _focusedComponents = new Dictionary<ICommandHandler, PageComponent>();

            int length = _activeComponents.Count;
            for (int i = length - 1; i >= 0; i--)
            {
                PageComponent comp = (PageComponent)_activeComponents[i];
                _activeComponents.Remove(comp);
                comp.YieldFocus();
            }

            return true;
        }

        public List<ICommandHandler> GetFocusedComponents()
        {
            return new List<ICommandHandler>(_activeComponents);
        }

        /// <summary>
        /// Handle a command that will get routed to the PageComponent that has focus.
        /// </summary>
        /// <param name="commandId">the id of the command</param>
        /// <param name="properties">the properties of the command</param>
        /// <returns></returns>
        public bool HandleCommand(string commandId, Dictionary<string, string> properties, int sequenceNumber)
        {
            List<ICommandHandler> handlers = GetHandlerRecordForCommand(commandId);

            if (CUIUtility.IsNullOrUndefined(handlers))
                return false;

            for (int i = 0; i < handlers.Count; i++)
            {
                ICommandHandler handler = (ICommandHandler)handlers[i];

                // Only allow the command to go to the focused components
                if (!_focusedComponents.ContainsKey(handler) || CUIUtility.IsNullOrUndefined(_focusedComponents[handler]))
                    continue;

                // If at least one handler handles the command, 
                // then we return that the command is enabled
                if (CallCommandHandler(handler, commandId, properties, sequenceNumber))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Check if this command is enabled on the PageComponent that has focus.
        /// </summary>
        /// <param name="commandId">the id of the command</param>
        /// <param name="properties">the properties of the command</param>
        /// <returns></returns>
        public bool CanHandleCommand(string commandId)
        {
            List<ICommandHandler> handlers = GetHandlerRecordForCommand(commandId);
            if (CUIUtility.IsNullOrUndefined(handlers))
                return false;

            for (int i = 0; i < handlers.Count; i++)
            {
                ICommandHandler handler = (ICommandHandler)handlers[i];

                // Only allow the command to go to the focused components
                if (!_focusedComponents.ContainsKey(handler) || CUIUtility.IsNullOrUndefined(_focusedComponents[handler]))
                    continue;

                // If at least one handler handles the command, 
                // then we return that the command is enabled
                if (CallCommandHandlerForEnabled(handler, commandId))
                    return true;
            }
            return false;
        }

        List<PageComponent> _components;
        /// <summary>
        /// Adds a component that is capable of having focus to this FocusManager
        /// and registers for the appropriate events using PageComponent.GetFocusedCommands()
        /// </summary>
        /// <param name="component">the component that is to be added</param>
        internal void AddPageComponent(PageComponent component)
        {
            if (_components.Contains(component))
                return;

            RegisterMultipleCommandHandler((ICommandHandler)component,
                                           component.GetFocusedCommands());
            _components.Add(component);
        }

        /// <summary>
        /// Removes a component from the FocusManager and unregisters the commands that it is
        /// registerd for using PageComponent.GetFocusedCommands()
        /// </summary>
        /// <param name="component"></param>
        internal void RemovePageComponent(PageComponent component)
        {
            if (!_components.Contains(component))
                return;

            UnregisterMultipleCommandHandler((ICommandHandler)component,
                                             component.GetFocusedCommands());

            // Release the focus from the component in case it has it
            ReleaseFocusFromComponent(component);
            _components.Remove(component);
        }

        public override bool ExecuteCommand(string commandId, Dictionary<string, string> properties)
        {
            throw new InvalidOperationException("ExecuteCommand should not be called on the main CommandDispatcher of the page, not the FocusManager");
        }

        private int GetRegisterCommandCount(string commandId)
        {
            return !_registeredCommands.ContainsKey(commandId) ? 0 : _registeredCommands[commandId];
        }

        /// <summary>
        /// Registers a handler with the FocusManager for a particular id.
        /// </summary>
        /// <param name="commandId">the commandid that is to be registerd for</param>
        /// <param name="handler">the handler that will handle the passed in command id</param>
        /// <see cref="CommandDispatcher.RegisterCommandHandler"/>
        public override void RegisterCommandHandler(string commandId, ICommandHandler handler)
        {
            base.RegisterCommandHandler(commandId, handler);

            // Only register with the main command dispatcher if we haven't already
            if (!_registeredCommands.ContainsKey(commandId))
            {
                _pageManager.CommandDispatcher.RegisterCommandHandler(commandId, this);
                // Store the fact that we now have registered with the main command dispatch 
                // for this command
                _registeredCommands[commandId] = 0;
            }

            int regCount = GetRegisterCommandCount(commandId);
            _registeredCommands[commandId] = regCount + 1;
        }

        /// <summary>
        /// Unregisters a handler with the FocusManager
        /// </summary>
        /// <param name="commandId">the commandid that is to be unregisterd for</param>
        /// <param name="handler">the handler that is to be unregistered</param>
        public override void UnregisterCommandHandler(string commandId, ICommandHandler handler)
        {
            base.UnregisterCommandHandler(commandId, handler);
            int regCount = GetRegisterCommandCount(commandId);

            if (regCount > 0)
            {
                // Store the fact that we now have registered with the main command dispatch for this command
                _registeredCommands[commandId] = --regCount;

                // Only unregister with the main command dispatcher if there are no other components that are
                // registered for this command    
                if (regCount == 0)
                {
                    _pageManager.CommandDispatcher.UnregisterCommandHandler(commandId, this);
                    _registeredCommands.Remove(commandId);
                }
            }
        }

        public override int GetNextSequenceNumber()
        {
            throw new InvalidOperationException("The FocusManager does not issue command sequence numbers.  This is only done by the main CommandDispatcher of the page.");
        }

        public override int PeekNextSequenceNumber()
        {
            throw new InvalidOperationException("The FocusManager does not issue command sequence numbers.  This is only done by the main CommandDispatcher of the page.");
        }

        public override int GetLastSequenceNumber()
        {
            throw new InvalidOperationException("The FocusManager does not issue command sequence numbers.  This is only done by the main CommandDispatcher of the page.");
        }


        /// <summary>
        /// protected overrided method that makes sure that only the handler of the active page component
        /// is invoked.
        /// </summary>
        /// <param name="handler">a handler for a command</param>
        /// <param name="commandId">the command id</param>
        /// <param name="properties">the properties of the command</param>
        /// <returns>true if the handler is the active page component and if it can handle the passed in commandid</returns>
        protected override bool CallCommandHandler(ICommandHandler handler,
                                                   string commandId,
                                                   Dictionary<string, string> properties,
                                                   int sequenceNumber)
        {
            // If this handler is not the active PageComponent, then the command is not enabled
            // for this component because it does not have the focus
            if (!_activeComponents.Contains(handler))
                return false;

            return handler.HandleCommand(commandId, properties, sequenceNumber);
        }

        /// <summary>
        /// protected overriden method that makes sure that we only run the "IsEnabled" call on the active
        /// page component.
        /// </summary>
        /// <param name="handler">a handler the passed in command</param>
        /// <param name="commandId">the command id</param>
        /// <returns>true if this handler is the active component and it can handle the passed in commandid</returns>
        protected override bool CallCommandHandlerForEnabled(ICommandHandler handler, string commandId)
        {
            // If this handler is not the active PageComponent, then the command is not enabled
            // for this component because it does not have the focus
            if (!_activeComponents.Contains(handler))
                return false;

            return handler.CanHandleCommand(commandId);
        }
    }
}
