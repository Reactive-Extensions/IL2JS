namespace Ribbon
{
    /// <summary>
    /// Contains valid section identifiers for Ribbon Peripheral Content
    /// </summary>
    /// <remarks>
    /// Keep this in sync with <see cref="Microsoft.Web.CommandUI.RibbonPeripheralSections"/>
    /// </remarks>
    /// <owner alias="JKern"/>
    public class RibbonPeripheralSection
    {
        /// <summary>
        /// In LTR languages, this location is on the left of the tab titles (opposite on RTL languages)
        /// </summary>
        public const string TabRowLeft = "TabRowLeft";

        /// <summary>
        /// In LTR languages, this location is on the right of the tab titles (opposite on RTL languages)
        /// </summary>
        public const string TabRowRight = "TabRowRight";

        /// <summary>
        /// This location is in the center of the row containing the QAT (if applicable)
        /// </summary>
        public const string QATRowCenter = "QATRowCenter";

        /// <summary>
        /// In LTR languages, this location is on the right of the tab titlesrow containing the QAT (if applicable) (opposite on RTL languages)
        /// </summary>
        public const string QATRowRight = "QATRowRight";
    }
}
