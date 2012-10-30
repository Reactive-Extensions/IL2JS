using System;
using Microsoft.LiveLabs.Html;
using Microsoft.LiveLabs.JavaScript;

namespace Ribbon
{
    internal class QATBuildContext : BuildContext
    {
        public QAT QAT;
        public string QATId;
    }

    /// <summary>
    /// This class represents the build options for a QAT
    /// </summary>
    public class QATBuildOptions : BuildOptions
    {
        public QATBuildOptions() {}
    }

    /// <summary>
    /// Class that builds QAT specific Components.
    /// </summary>
    public class QATBuilder : Builder
    {
        /// <summary>
        /// Constructor for a QAT
        /// </summary>
        /// <param name="dataUrlBase">The XML data URL</param>
        /// <param name="version">The version of the XML data</param>
        /// <param name="lcid">The language code</param>
        /// <param name="options">The QATBuildOptions for this QAT. <see cref="QATBuildOptions"/></param>
        /// <param name="elmPlaceholder">The ID for the DOMElement that will enclose this QAT</param>
        /// <param name="rootBuildClient">The object using this builder</param>
        public QATBuilder(QATBuildOptions options,
                          HtmlElement elmPlaceholder,
                          IRootBuildClient rootBuildClient)
            : base(options, elmPlaceholder, rootBuildClient)
        {
            if (CUIUtility.IsNullOrUndefined(elmPlaceholder))
                throw new ArgumentNullException("QAT placeholder DOM element is null or undefined.");
        }

        /// <summary>
        /// The QAT that is built by this QATBuilder
        /// </summary>
        public QAT QAT
        {
            get 
            { 
                return (QAT)Root; 
            }
            private set 
            { 
                Root = value; 
            }
        }

        /// <summary>
        /// This method executes the build process
        /// </summary>
        /// <param name="qatId">The CUI ID for this QAT</param>
        /// <returns>The QAT object</returns>
        public bool BuildQAT(string qatId)
        {
            if (InQuery)
                return false;

            if (IsIdTrimmed(qatId))
                return true; /* no error, so return true */

            QATBuildContext qbc = new QATBuildContext();
            qbc.QATId = qatId;
            InQuery = true;
            DataQuery query = new DataQuery();
            query.TabQuery = false;
            query.Id = qbc.QATId;
            query.QueryType = DataQueryType.Root;
            query.Handler = new DataReturnedEventHandler(OnReturnQAT);
            query.Data = qbc;
            DataSource.RunQuery(query);
            return true;
        }

        internal void BuildQATFromData(object dataNode, QATBuildContext qbc)
        {
            DataQueryResult dqr = new DataQueryResult();
            dqr.Success = true;
            dqr.QueryData = dataNode;
            dqr.ContextData = qbc;
            OnReturnQAT(dqr);
        }

        private void OnReturnQAT(DataQueryResult dqr)
        {
            QATBuildContext qbc = (QATBuildContext)dqr.ContextData;

            // Apply any extensions to the data.
            dqr.QueryData = ApplyDataExtensions(dqr.QueryData);

            QAT = BuildQATInternal(DataNodeWrapper.GetFirstChildNodeWithName(dqr.QueryData, DataNodeWrapper.QAT), qbc);
            QAT.QATBuilder = this;
            BuildClient.OnComponentCreated(QAT, QAT.Id);

            if (QATBuildOptions.AttachToDOM)
            {
                QAT.AttachInternal(true);
            }
            else
            {
                QAT.RefreshInternal();
                Placeholder.AppendChild(QAT.ElementInternal);
                Utility.EnsureCSSClassOnElement(Placeholder, "loaded");
            }
            OnRootBuilt(QAT);
            BuildClient.OnComponentBuilt(QAT, QAT.Id);
        }

        private QAT BuildQATInternal(object data, QATBuildContext qbc)
        {
            if (CUIUtility.IsNullOrUndefined(data))
                throw new ArgumentNullException("No QAT element was present in the data");

            QAT = new QAT(DataNodeWrapper.GetAttribute(data, "Id"),
                          DataNodeWrapper.GetNodeAttributes(data).To<QATProperties>());

            // Handle the controls in the QAT
            // The XML structure looks like <QAT><Controls><Control></Control><Control></Control>...
            JSObject controlsParent = DataNodeWrapper.GetFirstChildNodeWithName(data, DataNodeWrapper.CONTROLS);
            JSObject[] controls = DataNodeWrapper.GetNodeChildren(controlsParent);
            for (int j = 0; j < controls.Length; j++)
            {
                if (!IsNodeTrimmed(controls[j]))
                {
                    Control control = BuildControl(controls[j], qbc);
                    QAT.AddChild(control.CreateComponentForDisplayMode("Small"));
                }
            }

            return QAT;
        }

        private QATBuildOptions QATBuildOptions
        {
            get 
            { 
                return (QATBuildOptions)base.Options; 
            }
        }
    }
}