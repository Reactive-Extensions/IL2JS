namespace Ribbon
{
    /// <summary>
    /// A Component that can only live under a QAT Component Root.
    /// </summary>
    internal class QATComponent : Component
    {
        /// <summary>
        /// A Component that can only live under a QAT Component Root.
        /// </summary>
        /// <param name="qat">The QAT that this component is in</param>
        /// <param name="id">The ID of this component</param>
        /// <param name="title">The title of this component</param>
        /// <param name="description">The description of this component</param>
        public QATComponent(QAT qat, string id, string title, string description)
            : base(qat, id, title, description)
        {
        }

        /// <summary>
        /// The QAT Root that this Component belongs to.
        /// </summary>
        public QAT QAT
        {
            get 
            { 
                return (QAT)Root; 
            }
        }
    }
}
