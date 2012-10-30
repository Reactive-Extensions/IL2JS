using System;
using System.Collections.Generic;

namespace Ribbon.Page
{
    /// <summary>
    /// Objects that can receive commands from the CommandDispatcher need to implement this.
    /// </summary>
    public interface ICommandHandler
    {
        /// <summary>
        /// Let the component handle the executed command.
        /// </summary>
        /// <param name="commandId">the id of the executed command</param>
        /// <param name="properties">the properties of the executed command</param>
        /// <returns></returns>
        bool HandleCommand(string commandId, Dictionary<string, string> properties, int sequenceNumber);
        /// <summary>
        /// Whether this object can handle a particular command or not.
        /// </summary>
        /// <param name="commandId">The command id that is being queried.</param>
        /// <returns></returns>
        bool CanHandleCommand(string commandId);
    }

    /// <summary>
    /// Dispatches commands between components on the page.
    /// </summary>
    public class CommandDispatcher
    {
        Dictionary<string, List<ICommandHandler>> _registrations;
        public CommandDispatcher()
        {
            _sequenceNumber = 0;
            _registrations = new Dictionary<string, List<ICommandHandler>>();
        }

        internal virtual void Initialize()
        {
        }

        int _sequenceNumber;
        /// <summary>
        /// Returns the next sequence number in the monotonically increasing
        /// </summary>
        /// <returns></returns>
        public virtual int GetNextSequenceNumber()
        {
            // Don't allow this to overflow into negative numbers
            if (_sequenceNumber + 1 < 0)
                throw new ArgumentOutOfRangeException("Command Dispatcher sequence numbers overflowed into negative numbers.");
            return ++_sequenceNumber;
        }

        public virtual int PeekNextSequenceNumber()
        {
            return _sequenceNumber + 1;
        }

        public virtual int GetLastSequenceNumber()
        {
            return _sequenceNumber;
        }

        // TODO: Should we fail if one handler throws or should we silently 
        // catch and go on?

        /// <summary>
        /// Executes a command against all the registered handlers for that command.
        /// </summary>
        /// <param name="commandId">The id of the command that is to be executed.</param>
        /// <param name="properties">The properties of the command that is to be executed.</param>
        /// <returns>Returns true if one or more command handlers processed the command successfully.</returns>
        public virtual bool ExecuteCommand(string commandId, Dictionary<string, string> properties)
        {
            return ExecuteCommandInternal(commandId, properties, GetNextSequenceNumber());
        }

        internal bool ExecuteCommandInternal(string commandId, Dictionary<string, string> properties, int sequenceNumber)
        {
            List<ICommandHandler> handlers = GetHandlerRecordForCommand(commandId);
            if (CUIUtility.IsNullOrUndefined(handlers))
                return false;

            bool success = false;
            for (int i = 0; i < handlers.Count; i++)
            {
                ICommandHandler handler = (ICommandHandler)handlers[i];

                // If at least one handler handles the command, then we return success
                if (CallCommandHandler(handler, commandId, properties, sequenceNumber))
                    success = true;
            }
            return success;
        }

        /// <summary>
        /// Checks whether any command handlers can accept the execution of a command.
        /// </summary>
        /// <param name="commandId">The id of the command that is being checked.</param>
        /// <returns>Returns true if one or more command handlers can execute the command.</returns>
        public bool IsCommandEnabled(string commandId)
        {
            List<ICommandHandler> handlers = GetHandlerRecordForCommand(commandId);
            if (CUIUtility.IsNullOrUndefined(handlers))
                return false;
            
            for (int i = 0; i < handlers.Count; i++)
            {
                ICommandHandler handler = (ICommandHandler)handlers[i];

                // If at least one handler handles the command, 
                // then we return that the command is enabled
                if (CallCommandHandlerForEnabled(handler, commandId))
                    return true;
            }
            return false;
        }

        internal List<ICommandHandler> GetHandlerRecordForCommand(string commandId)
        {
            return _registrations.ContainsKey(commandId) ? _registrations[commandId] : null;
        }

        /// <summary>
        /// Register a command handler for the command with the passed in id.
        /// </summary>
        /// <param name="commandId">The command that the handler is registering for.</param>
        /// <param name="handler">The handler that will handle the command with this id.</param>
        public virtual void RegisterCommandHandler(string commandId, ICommandHandler handler)
        {
            if (string.IsNullOrEmpty(commandId) || CUIUtility.IsNullOrUndefined(handler))
                throw new ArgumentNullException("commandId and handler may not be null or undefined");

            List<ICommandHandler> handlers = GetHandlerRecordForCommand(commandId);
            if (CUIUtility.IsNullOrUndefined(handlers))
            {
                handlers = new List<ICommandHandler>();
                handlers.Add(handler);
                _registrations[commandId] = handlers;
            }
            else
            {
                // Add the handler to the vector of handlers for this command
                if (!handlers.Contains(handler))
                    handlers.Add(handler);
            }
        }

        /// <summary>
        /// Unregister a command handler for the command with the passed in id.
        /// </summary>
        /// <param name="commandId">The command that the handler is unregistering for.</param>
        /// <param name="handler">The handler that should be unregistered for the passed in command id.</param>
        public virtual void UnregisterCommandHandler(string commandId, ICommandHandler handler)
        {
            if (string.IsNullOrEmpty(commandId) || CUIUtility.IsNullOrUndefined(handler))
                throw new ArgumentNullException("commandId and handler may not be null or undefined");

            List<ICommandHandler> handlers = GetHandlerRecordForCommand(commandId);
            if (CUIUtility.IsNullOrUndefined(handlers))
                return;

            if (handlers.Contains(handler))
                handlers.Remove(handler);
        }

        /// <summary>
        /// Register a command handler for multiple commands at once.
        /// </summary>
        /// <param name="handler">the handler that is to be registered</param>
        /// <param name="commands">a string[] of the command ids that this handler should be registerd for</param>
        public virtual void RegisterMultipleCommandHandler(ICommandHandler component, string[] commands)
        {
            for (int i = 0; i < commands.Length; i++)
                this.RegisterCommandHandler((string)commands[i], (ICommandHandler)component);
        }

        /// <summary>
        /// Unregister a command handler for multiple commands at once.
        /// </summary>
        /// <param name="handler">the handler that is to be unregistered</param>
        /// <param name="commands">a string[] of the command ids that this handler should be unregistered for</param>
        public virtual void UnregisterMultipleCommandHandler(ICommandHandler component, string[] commands)
        {
            for (int i = 0; i < commands.Length; i++)
                this.UnregisterCommandHandler((string)commands[i], (ICommandHandler)component);
        }

        /// <summary>
        /// protected virtual method that is called for each handler that is registered for a command
        /// this can be overriden in subclasses that want to modify the behavior when a handler is found
        /// for a command.
        /// </summary>
        /// <param name="handler">the handler that should handle the command</param>
        /// <param name="commandId">the command id that that should be handled</param>
        /// <param name="properties">the properties of the command</param>
        /// <returns>true if the command was handled successfully</returns>
        protected virtual bool CallCommandHandler(ICommandHandler handler,
                                                  string commandId,
                                                  Dictionary<string, string> properties,
                                                  int sequenceNumber)
        {
            return handler.HandleCommand(commandId, properties, sequenceNumber);
        }

        /// <summary>
        /// protected virtual method that is called for each handler of a command to see whether that
        /// command is enabled for the handler.
        /// </summary>
        /// <param name="handler">the handler that is being queried</param>
        /// <param name="commandId">the command id that is being queried</param>
        /// <returns>true if the command is enabled for this handler</returns>
        protected virtual bool CallCommandHandlerForEnabled(ICommandHandler handler, string commandId)
        {
            return handler.CanHandleCommand(commandId);
        }
    }
}
