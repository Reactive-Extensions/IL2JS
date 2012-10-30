namespace Ribbon
{
    /// <summary>
    /// A Component that can only live under a Jewel Component Root.
    /// </summary>
    internal class JewelComponent : Component
    {
        /// <summary>
        /// A Component that can only live under a Jewel Component Root.
        /// </summary>
        /// <param name="jewel">The Jewel that this component is in</param>
        /// <param name="id">The ID of this component</param>
        /// <param name="title">The title of this component</param>
        /// <param name="description">The description of this component</param>
        public JewelComponent(Jewel jewel, string id, string title, string description)
            : base(jewel, id, title, description)
        {
        }

        /// <summary>
        /// The Jewel Root that this Component belongs to.
        /// </summary>
        public Jewel Jewel
        {
            get
            {
                return (Jewel)Root;
            }
        }
    }
}
