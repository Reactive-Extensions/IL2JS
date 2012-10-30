using System.Collections.Generic;
using Microsoft.LiveLabs.Html;
using Ribbon.Page;

namespace Ribbon
{
    /// <summary>
    /// Ribbon commands requesting help for controls
    /// </summary>
    /// <owner alias="HillaryM" />
    public static class HelpCommandNames
    {
        public const string RequestContextualHelp = "RequestContextualHelp";
    }

    /// <summary>
    /// Page component handling request for help on controls in the ribbon.    
    /// </summary>
    /// <owner alias="HillaryM" />
    public class HelpPageComponent : PageComponent
    {
        private string[] m_focusedCommands;
        private string[] m_globalCommands;

        /// <summary>
        /// Component Constructor
        /// </summary>
        /// <owner alias="HillaryM" />
        private HelpPageComponent()
        {
        }

        private static HelpPageComponent s_instance;
        /// <summary>
        /// The singleton of the help management page component.
        /// </summary>
        /// <owner alias="HillaryM" />
        public static HelpPageComponent Instance
        {
            get
            {
                if (s_instance == null)
                {
                    s_instance = new HelpPageComponent();
                }
                return s_instance;
            }
        }

        /// <summary>
        /// Register the singleton instance with page manager.
        /// </summary>
        /// <owner alias="HillaryM" />
        internal static void RegisterWithPageManager()
        {
            PageManager.Instance.AddPageComponent(Instance);
        }

        /// <summary>
        /// Unregister the singleton instance from page manager.
        /// </summary>
        /// <owner alias="HillaryM" />
        internal static void UnregisterWithPageManager()
        {
            if (s_instance != null)
            {
                PageManager.Instance.RemovePageComponent(Instance);
            }
        }
        /// <summary>
        /// Init method. called by the page manager.
        /// </summary>
        /// <owner alias="HillaryM" />
        public override void Init()
        {
            m_focusedCommands = new string[] { HelpCommandNames.RequestContextualHelp };

            m_globalCommands = new string[] { HelpCommandNames.RequestContextualHelp };
        }

        /// <summary>
        /// Get focused command.
        /// </summary>
        /// <returns>The array of commands the Help Component handles when it has focus.</returns>
        /// <owner alias="HillaryM" />
        public override string[] GetFocusedCommands()
        {
            return m_focusedCommands;
        }

        /// <summary>
        /// Get global command.
        /// </summary>
        /// <returns>The array of comands the Help Component handles.</returns>
        /// <owner alias="HillaryM" />
        public override string[] GetGlobalCommands()
        {
            return m_globalCommands;
        }

        /// <summary>
        /// Indicates whether the Component can handle commands.
        /// </summary>
        /// <param name="commandId">The ID of the command to check for.</param>
        /// <returns>True if the Help Component can handle the given command.</returns>
        /// <owner alias="HillaryM" />
        public override bool CanHandleCommand(string commandId)
        {
            if (commandId == HelpCommandNames.RequestContextualHelp)
            {
                // check if core.js is loaded here             
                return true;
            }
            else
            {
                return false;
            }
        }


        /// <summary>
        /// The Id of this Component.
        /// </summary>
        /// <returns>The id of this PageComponent.</returns>
        /// <owner alias="JosefL" />
        public override string GetId()
        {
            return "HelpComponent";
        }

        private bool m_handlingCommand;
        /// <summary>
        /// Indicates whether the component is currently handling a commmand.
        /// </summary>        
        /// <owner alias="HillaryM" />
        internal bool HandlingCommand
        {
            get
            {
                return m_handlingCommand;
            }
            set
            {
                m_handlingCommand = value;
            }
        }

        /// <summary>
        /// Indicates whether the component is currently handling a commmand.
        /// </summary>
        /// <owner alias="HillaryM" />
        internal static bool IsHandlingCommand
        {
            get
            {
                if (s_instance == null)
                {
                    return false;
                }
                return s_instance.HandlingCommand;
            }
        }

        /// <summary>
        /// Method called when a Help Command is raised.
        /// </summary>
        /// <remarks>
        /// This function checks that the raised command is indeed a help command and if so takes the 
        /// appropriate action. 
        /// </remarks>
        /// <param name="commandId">The id of the executed command.</param>
        /// <param name="properties">The properties of the executed command.</param>
        /// <param name="sequence">The sequence number for the executed command.</param>
        /// <returns>True if the command is handled.False otherwise.</returns>
        /// <owner alias="HillaryM" />
        public override bool HandleCommand(string commandId, Dictionary<string, string> properties, int sequence)
        {
            if (string.IsNullOrEmpty(commandId) ||
                CUIUtility.IsNullOrUndefined(properties))
            {
                return false;
            }

            if (commandId == HelpCommandNames.RequestContextualHelp)
            {
                HandlingCommand = true;
                string helpKeyord = properties["HelpKeyword"];
                Browser.Window.Alert("Unable to navigate to help at this time");
                HandlingCommand = false;
            }            
            else
            {
                // not recognized command
                return false;
            }
            return true;
        }

        /// <summary>        
        /// Indicates whether the component is can recieve focus.
        /// </summary>        
        /// <returns>True if the component can receive focus.</returns>
        /// <owner alias="HillaryM" />
        public override bool IsFocusable()
        {
            return true;
        }

        /// <summary>
        /// Recieves focus from the Page FocusManager.
        /// </summary>
        /// <returns>True if component recieved focus.</returns>
        /// <owner alias="HillaryM" />
        public override bool ReceiveFocus()
        {
            return true;
        }

        /// <summary>
        /// Yields focus.
        /// </summary>
        /// <returns>True if component yields focus.</returns>
        /// <owner alias="HillaryM" />
        public override bool YieldFocus()
        {
            return true;
        }
    }
}

