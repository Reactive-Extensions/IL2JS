using System;
using Microsoft.LiveLabs.Html;
using Microsoft.LiveLabs.JavaScript;

using Ribbon.Controls;

namespace Ribbon
{
    internal class JewelBuildContext : BuildContext
    {
        public Jewel Jewel;
        public string JewelId;
    }

    /// <summary>
    /// This class represents the build options for a Jewel
    /// </summary>
    public class JewelBuildOptions : BuildOptions
    {
        public JewelBuildOptions() {}
    }

    /// <summary>
    /// Class that builds Jewel specific Components.
    /// </summary>
    public class JewelBuilder : Builder
    {
        /// <summary>
        /// Constructor for a Jewel
        /// </summary>
        /// <param name="dataUrlBase">The XML data URL</param>
        /// <param name="version">The version of the XML data</param>
        /// <param name="lcid">The language code</param>
        /// <param name="options">The JewelBuildOptions for this Jewel. <see cref="JewelBuildOptions"/></param>
        /// <param name="elmPlaceholder">The ID for the DOMElement that will enclose this Jewel</param>
        /// <param name="rootBuildClient">The object using this builder</param>
        public JewelBuilder(JewelBuildOptions options,
                            HtmlElement elmPlaceholder,
                            IRootBuildClient rootBuildClient)
            : base(options, elmPlaceholder, rootBuildClient)
        {
            if (CUIUtility.IsNullOrUndefined(elmPlaceholder))
                throw new ArgumentNullException("Jewel placeholder DOM element is null or undefined.");
        }

        /// <summary>
        /// The Jewel that is built by this JewelBuilder
        /// </summary>
        public Jewel Jewel
        {
            get 
            { 
                return (Jewel)Root; 
            }
            private set 
            { 
                Root = value; 
            }
        }

        /// <summary>
        /// This method executes the build process
        /// </summary>
        /// <param name="jewelId">The CUI ID for this Jewel</param>
        /// <returns>The Jewel object</returns>
        public bool BuildJewel(string jewelId)
        {
            if (InQuery)
                return false;

            if (IsIdTrimmed(jewelId))
                return true; /* no error, so return true */

            JewelBuildContext jbc = new JewelBuildContext();
            jbc.JewelId = jewelId;
            InQuery = true;
            DataQuery query = new DataQuery();
            query.TabQuery = false;
            query.Id = jbc.JewelId;
            query.QueryType = DataQueryType.Root;
            query.Handler = new DataReturnedEventHandler(OnReturnJewel);
            query.Data = jbc;
            this.DataSource.RunQuery(query);
            return true;
        }

        internal void BuildJewelFromData(object dataNode, JewelBuildContext jbc)
        {
            DataQueryResult dqr = new DataQueryResult();
            dqr.Success = true;
            dqr.QueryData = dataNode;
            dqr.ContextData = jbc;
            OnReturnJewel(dqr);
        }

        private void OnReturnJewel(DataQueryResult dqr)
        {
            JewelBuildContext jbc = (JewelBuildContext)dqr.ContextData;

            // Apply any extensions to the data.
            dqr.QueryData = ApplyDataExtensions(dqr.QueryData);

            JSObject jewelNode = DataNodeWrapper.GetFirstChildNodeWithName(dqr.QueryData, DataNodeWrapper.JEWEL);
            Jewel = BuildJewelInternal(jewelNode, jbc);
            Jewel.JewelBuilder = this;
            BuildClient.OnComponentCreated(Jewel, Jewel.Id);

            if (JewelBuildOptions.AttachToDOM)
            {
                Jewel.AttachInternal(true);
            }
            else
            {
                Jewel.RefreshInternal();
                Placeholder.AppendChild(Jewel.ElementInternal);
                Utility.EnsureCSSClassOnElement(Placeholder, "loaded");
            }
            OnRootBuilt(Jewel);
            BuildClient.OnComponentBuilt(Jewel, Jewel.Id);
        }

        private Jewel BuildJewelInternal(object data, JewelBuildContext jbc)
        {
            if (CUIUtility.IsNullOrUndefined(data))
                throw new ArgumentNullException("No Jewel element was present in the data");

            Jewel = new Jewel(DataNodeWrapper.GetAttribute(data, "Id"),
                              DataNodeWrapper.GetNodeAttributes(data).To<JewelProperties>());

            // Handle the Jewel Menu Launcher control
            JewelMenuLauncher jml = BuildJewelMenuLauncher(data, jbc);
            Jewel.AddChild(jml.CreateComponentForDisplayMode("Default"));
            Jewel.JewelMenuLauncher = jml;

            return Jewel;
        }

        private JewelMenuLauncher BuildJewelMenuLauncher(object data, JewelBuildContext jbc)
        {
            JewelMenuLauncherProperties properties =
                DataNodeWrapper.GetNodeAttributes(data).To<JewelMenuLauncherProperties>();

            JSObject[] children = DataNodeWrapper.GetNodeChildren(data);

            Menu menu = null;

            MenuLauncherControlProperties launcherProperties =
                DataNodeWrapper.GetNodeAttributes(data).To<MenuLauncherControlProperties>();

            if (!Utility.IsTrue(launcherProperties.PopulateDynamically))
                menu = BuildMenu(children[0], jbc, false);

            JewelMenuLauncher jml = new JewelMenuLauncher(Jewel,
                                                          properties.Id,
                                                          properties,
                                                          menu);
            return jml;
        }

        private JewelBuildOptions JewelBuildOptions
        {
            get 
            { 
                return (JewelBuildOptions)base.Options; 
            }
        }
    }
}