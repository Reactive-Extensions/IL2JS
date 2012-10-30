using System.Collections.Generic;

namespace Ribbon
{
    /// <summary>
    /// An interface by which a Ribbon Group can be constructed according to a predefined template.
    /// </summary>
    internal abstract class Template
    {
        /// <summary>
        /// Creates a Group with this kind of template.
        /// </summary>
        /// <param name="ribbon">The Ribbon that this Group will be a part of.</param>
        /// <param name="id">Component id of the created Group</param>
        /// <param name="title">Title of the created Group</param>
        /// <param name="description">description of the created Group</param>
        /// <param name="command">command of the created Group(used for enabling/disabling through polling)</param>
        /// <param name="controls">array of Controls that will be used in the Temaplate(the order matters)</param>
        /// <param name="parameters">additional parameters for this Template</param>
        /// <returns>the created Group</returns>
        public abstract Group CreateGroup(SPRibbon ribbon,
                                          string id,
                                          GroupProperties properties,
                                          string title,
                                          string description,
                                          string command,
                                          Dictionary<string, List<Control>> controls,
                                          Dictionary<string, string> parameters);
    }
}
