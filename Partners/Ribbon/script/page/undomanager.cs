using System;
using System.Collections.Generic;
using Commands;

namespace Ribbon.Page
{
    public class UndoManager : ICommandHandler
    {
        PageManager _pageManager;
        int _RedoSequenceNumber = NULL_SEQUENCE;
        Dictionary<int, int> _sequenceNumbers;
        List<int> _undoStack;
        List<int> _redoStack;

        internal UndoManager(PageManager pageManager)
        {
            _pageManager = pageManager;
            _undoStack = new List<int>();
            _redoStack = new List<int>();
            _sequenceNumbers = new Dictionary<int, int>();
        }

        internal void Initialize()
        {
            _pageManager.CommandDispatcher.RegisterCommandHandler(CommandIds.GlobalUndo, this);
            _pageManager.CommandDispatcher.RegisterCommandHandler(CommandIds.GlobalRedo, this);
            _pageManager.CommandDispatcher.RegisterCommandHandler("grpedit", this);
        }

        /// <summary>
        /// Pushes a command sequence number onto the undo stack.
        /// </summary>
        /// <param name="sequenceNumber">the sequence number that is to be added to the undo stack</param>
        public void AddUndoSequenceNumber(int sequenceNumber)
        {
            PushUndoStack(sequenceNumber);
            // We always empty the redo stack unless this undo sequence number is actually
            // the sequence number of a command that we just issued.
            if (sequenceNumber != _RedoSequenceNumber)
                EmptyRedoStack();
        }

        /// <summary>
        /// Pushes a sequence number onto the redo stack.
        /// </summary>
        /// <param name="sequenceNumber"></param>
        public void AddRedoSequenceNumber(int sequenceNumber)
        {
            PushRedoStack(sequenceNumber);
        }

        /// <summary>
        /// Returns the oldest undo command sequence number on the undo or the redo stack.
        /// </summary>
        public int OldestSequenceNumber
        {
            get
            {
                if (_undoStack.Count == 0)
                    return NULL_SEQUENCE;

                int oldestUndo = NULL_SEQUENCE;
                int oldestRedo = NULL_SEQUENCE;

                if (_undoStack.Count > 0)
                    oldestUndo = _undoStack[_undoStack.Count - 1];
                if (_redoStack.Count > 0)
                    oldestRedo = _redoStack[0];

                if (oldestUndo == NULL_SEQUENCE)
                    return oldestUndo;
                else
                    return oldestUndo;
            }
        }

        private void DoUndo()
        {
            int sequenceNumber = PopUndoStack();
            if (sequenceNumber == NULL_SEQUENCE)
                return;

            Dictionary<string, string> properties = new Dictionary<string, string>();
            properties[UndoProperties.SequenceNumber] = sequenceNumber.ToString();
            _pageManager.CommandDispatcher.ExecuteCommand(CommandIds.Undo, properties);
        }

        private void DoRedo()
        {
            int sequenceNumber = PopRedoStack();
            if (sequenceNumber == NULL_SEQUENCE)
                return;

            Dictionary<string, string> properties = new Dictionary<string, string>();
            properties[GlobalRedoProperties.SequenceNumber] = sequenceNumber.ToString();

            // Store the sequence number that the redo command will have so that we can know
            // whether we need to empty the redo stack or not if components add this as an undo command.
            _RedoSequenceNumber = _pageManager.CommandDispatcher.PeekNextSequenceNumber();
            _pageManager.CommandDispatcher.ExecuteCommand(CommandIds.Redo, properties);
        }

        private int PopRedoStack()
        {
            if (_redoStack.Count == 0)
                return NULL_SEQUENCE;

            int sequenceNumber = _redoStack[0];
            _redoStack.RemoveAt(0);
            _sequenceNumbers[sequenceNumber] = NULL_SEQUENCE;
            return sequenceNumber;
        }

        private void PushRedoStack(int sequenceNumber)
        {
            // If this sequence number is already on the redo stack then we don't need to push it again
            if (_sequenceNumbers.ContainsKey(sequenceNumber) && _sequenceNumbers[sequenceNumber] != NULL_SEQUENCE)
            {
                // If this sequence number is already on the stack, then it better be on top
                if (_undoStack[0] != sequenceNumber)
                    throw new IndexOutOfRangeException("This command sequence number is already on the undo or the redo stack but it is not ontop of the redo stack.  Pushing it would result in out of sequence redo and undo stacks.");
                return;
            }

            _redoStack.Insert(0, sequenceNumber);
            _sequenceNumbers[sequenceNumber] = sequenceNumber;
        }

        private static int NULL_SEQUENCE = -1;
        private int PopUndoStack()
        {
            if (_undoStack.Count == 0)
                return NULL_SEQUENCE;

            int sequenceNumber = _undoStack[0];
            _undoStack.RemoveAt(0);
            _sequenceNumbers[sequenceNumber] = NULL_SEQUENCE;
            return sequenceNumber;
        }

        private void PushUndoStack(int sequenceNumber)
        {
            // If this sequence number is already on the undo stack then we don't need to push it again
            if (_sequenceNumbers.ContainsKey(sequenceNumber) && _sequenceNumbers[sequenceNumber] != NULL_SEQUENCE)
            {
                // If this sequence number is already on the stack, then it better be on top
                if (_undoStack[0] != sequenceNumber)
                    throw new IndexOutOfRangeException("This command sequence number is already on the stack and not on top.  Pushing it would result in an out of sequence undo stack.");
                return;
            }

            _undoStack.Insert(0, sequenceNumber);
            _sequenceNumbers[sequenceNumber] = sequenceNumber;
        }

        private void EmptyRedoStack()
        {
            for (int i = 0; i < _redoStack.Count; i++)
            {
                _sequenceNumbers[_redoStack[i]] = NULL_SEQUENCE;
                _redoStack.RemoveAt(_redoStack[i]);
            }
            _redoStack.Clear();
        }

        /// <summary>
        /// Invalidate sequence numbers that are older than and equal to the passed in one.
        /// This is called by applications when they are invalidating their undo stacks.
        /// </summary>
        /// <param name="sequenceNumber"></param>
        public void InvalidateUndoSequenceNumber(int sequenceNumber)
        {
            // Go through the undo stack and remove all sequence numbers that are older than
            // sequenceNumber (inclusive)
            for (int i = _undoStack.Count - 1; i > -1; i--)
            {
                int sn = _undoStack[i];
                if (sn <= sequenceNumber)
                {
                    _undoStack.RemoveAt(i);
                    _sequenceNumbers[sn] = NULL_SEQUENCE;
                }
            }

            // Now go through the redo stack and remove all sequence number older than
            // sequenceNumber(inclusive)
            while (_redoStack.Count > 0 && _redoStack[0] <= sequenceNumber)
            {
                _sequenceNumbers[_redoStack[0]] = NULL_SEQUENCE;
                _redoStack.RemoveAt(0);
            }
        }

        public bool CanHandleCommand(string commandId)
        {
            // We can return true because we only register for two commands and we can always
            // handle them.
            if (commandId == CommandIds.GlobalUndo)
                return _undoStack.Count > 0;
            else if (commandId == CommandIds.GlobalRedo)
                return _redoStack.Count > 0;
            else if (commandId == "grpedit")
                return true;

            return false;
        }

        public bool HandleCommand(string commandId, Dictionary<string, string> properties, int sequenceNumber)
        {
            switch (commandId)
            {
                case CommandIds.GlobalUndo:
                    DoUndo();
                    return true;
                case CommandIds.GlobalRedo:
                    DoRedo();
                    return true;
            }
            return false;
        }
    }
}
