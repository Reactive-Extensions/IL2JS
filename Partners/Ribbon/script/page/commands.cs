namespace Commands
{
    public static class CommandIds
    {
        public const string ApplicationStateChanged = "appstatechanged";

        /// <summary>
        /// This command is sent by command ui when a "redo" button has been clicked.
        /// It is listened to and handled by the UndoManager.
        /// </summary>
        public const string GlobalRedo = "GlobalRedo";

        /// <summary>
        /// This command is sent out by the undo manager and should be listened to 
        /// by all the components on the page that implement undo functionality.
        /// </summary>
        public const string Redo = "Redo";

        /// <summary>
        /// This command is sent by command ui when a "undo" button has been clicked.
        /// It is listened to and handled by the UndoManager.
        /// </summary>
        public const string GlobalUndo = "GlobalUndo";

        /// <summary>
        /// This command is sent out by the undo manager and should be listened to 
        /// by all the components on the page that implement undo functionality.
        /// </summary>
        public const string Undo = "Undo";
    }
    public static class GlobalRedoProperties
    {
        public const string SequenceNumber = "SequenceNumber";
    }
    public static class RedoProperties
    {
        public const string SequenceNumber = "SequenceNumber";
    }
    public static class GlobalUndoProperties
    {
        public const string SequenceNumber = "SequenceNumber";
    }
    public static class UndoProperties
    {
        public const string SequenceNumber = "SequenceNumber";
    }
}
