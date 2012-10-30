namespace Ribbon
{
    /// <summary>
    /// A Component that can only live under a Ribbon Component Root.
    /// </summary>
    internal class RibbonComponent : Component
    {
        public RibbonComponent(SPRibbon ribbon, string id, string title, string description)
            : base(ribbon, id, title, description)
        {
        }

        /// <summary>
        /// The Ribbon Root that this Component belongs to.
        /// </summary>
        public SPRibbon Ribbon
        {
            get 
            { 
                return (SPRibbon)Root; 
            }
        }
    }
}
