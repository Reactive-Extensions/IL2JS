using Microsoft.LiveLabs.JavaScript;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace Ribbon
{
    sealed class QueryRecord
    {
        public string id;
        public DataQueryType queryType;
        public DataReturnedEventHandler handler;
        public object data;
    }

    public sealed class DataQueryResult
    {
        public bool Success;
        public string Id;
        public object QueryData;
        public object ContextData;
    }

    /// <summary>
    /// The type of a UI Data Query to the server.
    /// </summary>
    public enum DataQueryType
    {
        None = 0,
        All = 1,
        RibbonVisibleTabDeep = 2,
        RibbonShallow = 3,
        RibbonTab = 4,
        Root = 5
    }

    public class DataQuery
    {
        public bool TabQuery;
        public string DataUrl;
        public string Version;
        public string Lcid;
        public string Id;
        public DataQueryType QueryType;
        public DataReturnedEventHandler Handler;
        public object Data;
    }

    public delegate void DataReturnedEventHandler(DataQueryResult result);

    /// <summary>
    /// Makes the UI hierarchy available in the browser.  
    /// Understands how to build up the hierarchy a piece at a time on demand.
    /// </summary>
    public class DataSource
    {
        string _dataUrl;
        string _version;
        string _lcid;

        public DataSource(string dataUrl, string version, string lcid)
        {
            _dataUrl = dataUrl;
            _version = version;
            _lcid = lcid;
        }

        /// <summary>
        /// The URL of the data file used to make this Data object
        /// </summary>
        public string DataUrl
        {
            get
            {
                return _dataUrl;
            }
        }

        /// <summary>
        /// The version of this data
        /// </summary>
        public string Version
        {
            get
            {
                return _version;
            }
        }

        /// <summary>
        /// The lcid of this data
        /// </summary>
        public string Lcid
        {
            get
            {
                return _lcid;
            }
        }

        /*
        /// <summary>
        /// Subclassers can hook in here if they want to run their own logic to 
        /// get the sought after cui data.
        /// </summary>
        /// <param name="query">The cui query that is to be run.</param>
        public virtual void RunQuery(DataQuery query)
        {
            string version = _version;
            if (!string.IsNullOrEmpty(query.Version))
                version = query.Version;

            string lcid = _lcid;
            if (!string.IsNullOrEmpty(query.Lcid))
                lcid = query.Lcid;

            string dataUrl = _dataUrl;
            if (!string.IsNullOrEmpty(query.DataUrl))
                dataUrl = query.DataUrl;

            string url;
            string type = null;

            // The dataUrl passed in might already have parameters
            if (dataUrl.IndexOf('?') == -1)
                url = dataUrl + "?ver=";
            else
                url = dataUrl + "&ver=";

            url = url + version + "&id=" + query.Id + "&lcid=" + lcid + "&qt=";

            switch (query.QueryType)
            {
                case DataQueryType.All:
                    type = "all";
                    break;
                case DataQueryType.RibbonTab:
                    type = "ribbontab";
                    break;
                case DataQueryType.RibbonShallow:
                    type = "ribbonshallow";
                    break;
                case DataQueryType.Root:
                    type = "root";
                    break;
                case DataQueryType.RibbonVisibleTabDeep:
                    type = "ribbonvisibletabdeep";
                    break;
            }

            url += type;
#if PERF_METRICS
            PMetrics.PerfMark(PMarker.perfCUIRibbonQueryDataStart);
#endif
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "GET";
            req.ContentType = "text/html";

            QueryRecord rec = new QueryRecord();
            rec.id = query.Id;
            rec.queryType = query.QueryType;
            rec.data = query.Data;
            rec.handler = query.Handler;

            object data= new object();
            RequestState state = new RequestState(req, data, rec);
            IAsyncResult result = req.BeginGetResponse(new AsyncCallback(OnDataReturned),state);

            //Register the timeout callback
            ThreadPool.RegisterWaitForSingleObject(result.AsyncWaitHandle, 
                new WaitOrTimerCallback(ScanTimeoutCallback), state, (30 * 1000), true);
        }

        /// <summary>
        /// Subsclassers could hook in here if they wanted to subprocess the
        /// resulting JSON.
        /// </summary>
        /// <param name="executor">the web executor that has returned from async request</param>
        protected virtual void OnDataReturned(IAsyncResult result)
        {
#if PERF_METRICS
            PMetrics.PerfMark(PMarker.perfCUIRibbonQueryDataEnd);
#endif
            RequestState state = (RequestState)result.AsyncState;
            WebRequest request = (WebRequest)state.Request;

            HttpWebResponse response =(HttpWebResponse)request.EndGetResponse(result);
            Stream s = (Stream)response.GetResponseStream();
            StreamReader readStream = new StreamReader(s);

            // Get the complete contents of the message
            string dataString = readStream.ReadToEnd();
            response.Close();
            s.Close();
            readStream.Close();

            QueryRecord rec = state.Query;
            DataQueryResult res = new DataQueryResult();
            res.ContextData = rec.data;
            res.Id = rec.id;

            // If the request succeeded
            // TODO(josefl) figure out right way to find out if it succeeded
            if (!string.IsNullOrEmpty(dataString))
            {
                res.Success = true;
                res.QueryData = dataString;
                rec.handler(res);
            }
            else
            {
                res.Success = false;
                // Return that the data retrieval failed
                rec.handler(res);
            }
        }

        private static void ScanTimeoutCallback (object state, bool timedOut) 
        {
            if (timedOut) 
            {
                RequestState reqState = (RequestState)state;
                if (reqState != null) 
                    reqState.Request.Abort();
            }
        }
        */

        /// <summary>
        /// Subclassers can hook in here if they want to run their own logic to 
        /// get the sought after cui data.
        /// </summary>
        /// <param name="query">The cui query that is to be run.</param>
        public virtual void RunQuery(DataQuery query)
        {
            QueryRecord rec = new QueryRecord();
            rec.id = query.Id;
            rec.queryType = query.QueryType;
            rec.data = query.Data;
            rec.handler = query.Handler;

            RibbonData rData = new RibbonData();

            JSObject dataBlock;
            if (query.TabQuery)
                dataBlock = rData.GetTabQueryData(query.Id);
            else
                dataBlock = rData.GetQueryData(query.Id);

            DataQueryResult res = new DataQueryResult();
            res.ContextData = rec.data;
            res.Id = rec.id;

            if (!CUIUtility.IsNullOrUndefined(dataBlock))
            {
                res.Success = true;
                res.QueryData = dataBlock;
                rec.handler(res);
            }
            else
            {
                // Return that the data retrieval failed
                res.Success = false;
                rec.handler(res);
            }
        }
    }

    [Import(Qualification = Qualification.Type)]
    public class RibbonData
    {
        extern public RibbonData();
        extern public JSObject GetQueryData(string queryId);
        extern public JSObject GetTabQueryData(string queryId);
    }
}
