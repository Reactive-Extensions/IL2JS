using System;

namespace Rx
{
    /// <summary>
    /// Represents required and optional arguments passed into Observable.XmlHttpRequest.
    /// </summary>
    public class XmlHttpRequestDetails
    {
        /// <summary>
        /// Creates a new XmlHttpRequestDetails object.
        /// </summary>
        public XmlHttpRequestDetails()
        {
        }

        /// <summary>
        ///Required. String that specifies either the absolute or a relative URL of the XML data or server-side XML Web services.
        /// </summary>
        [PreserveCase]
        [IntrinsicProperty]
        public string Url
        {
            get;
            set;
        }

        /// <summary>
        /// Required. String that specifies the HTTP method used to open the connection: such as GET, POST, or HEAD. This parameter is not case-sensitive.
        /// </summary>
        [PreserveCase]
        [IntrinsicProperty]
        public string Method
        {
            get;
            set;
        }
        /// <summary>
        /// Optional. String that specifies the name of the user for authentication. If this parameter is null ("") or missing and the site requires authentication, the component displays a logon window
        /// </summary>
        [PreserveCase]
        [IntrinsicProperty]
        public string User
        {
            get;
            set;
        }

        /// <summary>
        /// Optional. String that specifies the password for authentication. This parameter is ignored if the user parameter is null ("") or missing.
        /// </summary>
        [PreserveCase]
        [IntrinsicProperty]
        public string Password
        {
            get;
            set;
        }

        /// <summary>
        /// Adds custom HTTP headers to the request.
        /// </summary>
        [PreserveCase]
        [IntrinsicProperty]
        public Dictionary Headers
        {
            get;
            set;
        }
    }
}
