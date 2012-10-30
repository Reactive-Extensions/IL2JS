using System;
using System.Collections.Generic;

namespace Ribbon
{
    /// <summary>
    /// The type of a Command.
    /// </summary>
    public enum CommandType
    {
        /// <summary>
        /// A General Command.  Used for all Commands that are not of a specific type.
        /// </summary>
        General = 1,
        /// <summary>
        /// The Command generated when a Tab is selected through the UI.
        /// </summary>
        TabSelection = 2,
        /// <summary>
        /// A Command generated when one option in an option group is selected.
        /// This could be one of the valid values for a combo box or dropdown.
        /// </summary>
        OptionSelection = 3,
        /// <summary>
        /// A Command issued when a menu is launched by a Control int the Ribbon.
        /// </summary>
        MenuCreation = 4,

        /// <summary>
        /// Command used for live previewing.  Usually fired on mouseover and mouseout of some controls.
        /// </summary>
        Preview = 5,
        PreviewRevert = 6,

        /// <summary>
        /// Command used for live previewing when the control is a selectable control within an DropDown-based control's menu
        /// </summary>
        OptionPreview = 7,
        OptionPreviewRevert = 8,

        /// <summary>
        /// Command used for controls that should not close the menu when clicked.  For example, multiple selection
        /// or buttons on a spinner
        /// </summary>
        IgnoredByMenu = 9,

        /// <summary>
        /// Command issued when a menu is closed - Fired if user clicks somewhere to close menu or selects menu option
        /// </summary>
        MenuClose = 10,

        /// <summary>
        /// An event concerning something about the root.  For example Ribbon Maximize and Minimize.
        /// </summary>
        RootEvent = 11
    }

    /// <summary>
    /// A Command carries information about events that occurr in CommandUI Roots
    /// up the hierarchy of CommandUI components.
    /// </summary>
    internal class CommandEventArgs : EventArgs
    {
        private string _id;
        private int _sequence;
        private Dictionary<string, string> _params;
        private Component _source;
        private CommandType _type;
        private CommandInformation _commandInfo;

        internal CommandEventArgs(string id,
                                  CommandType type,
                                  Component source,
                                  Dictionary<string, string> pars)
        {
            _id = id;
            _params = pars;
            _source = source;
            _type = type;
        }

        /// <summary>
        /// A uniqe identifier for this Command.  For example, if the the "Paste" button may issue a Command with Id "paste".
        /// </summary>
        public string Id
        {
            get
            {
                return _id;
            }
        }

        /// <summary>
        /// The parameters included with this command
        /// </summary>
        public Dictionary<string, string> Parameters
        {
            get
            {
                return _params;
            }
        }

        /// <summary>
        /// A unique identifier an instance of a Command.  This id is based on a monotonically increasing sequence that allows for a complete history of all Commands issued in the lifetime of a Ribbon.
        /// </summary>
        public int SequenceNumber
        {
            get
            {
                return _sequence;
            }
            set
            {
                _sequence = value;
            }
        }

        /// <summary>
        /// The Component that the Command came from.
        /// </summary>
        public Component Source
        {
            get
            {
                return _source;
            }
        }

        /// <summary>
        /// The Control that executed the Command.
        /// </summary>
        public Control SourceControl
        {
            get
            {
                if (!CUIUtility.IsNullOrUndefined(_source) &&
                        typeof(ControlComponent).IsInstanceOfType(_source))
                {
                    return ((ControlComponent)_source).Control;
                }

                return null;
            }
        }

        /// <summary>
        /// The type of the executed Command.
        /// </summary>
        public CommandType Type
        {
            get
            {
                return _type;
            }
        }

        public Dictionary<string, string> Properties
        {
            get
            {
                return _params;
            }
        }

        public CommandInformation CommandInfo
        {
            get
            {
                if (CUIUtility.IsNullOrUndefined(_commandInfo))
                    _commandInfo = new CommandInformation();
                return _commandInfo;
            }
        }
    }
}

