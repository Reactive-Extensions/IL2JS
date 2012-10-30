//*****************************************************************************
// VSTestAttributes.cs
// Owner: marcelv
//
// VS Attributes for unit testing
//
// Copyright(c) Microsoft Corporation, 2003
//*****************************************************************************

namespace Microsoft.VisualStudio.TestTools.UnitTesting
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;

    [AttributeUsage(AttributeTargets.Class, AllowMultiple=false)]
    public sealed class TestClassAttribute : Attribute
    {
        public TestClassAttribute()
        {
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple=false)]
    public sealed class TestMethodAttribute : Attribute
    {
        public TestMethodAttribute()
        {
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple=false)]
    public sealed class TestInitializeAttribute : Attribute
    {
        public TestInitializeAttribute()
        {
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple=false)]
    public sealed class TestCleanupAttribute : Attribute
    {
        public TestCleanupAttribute()
        {
        }
    }

	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
	public sealed class IgnoreAttribute : Attribute
	{
		public IgnoreAttribute()
		{
		}
	}

	[AttributeUsage(AttributeTargets.Method, AllowMultiple=false)]
    public sealed class ExpectedExceptionAttribute : Attribute
    {
        private Type m_exceptionType;
        private string m_message;

        public ExpectedExceptionAttribute (Type exceptionType) : this (exceptionType, string.Empty)
        {
        }

        public ExpectedExceptionAttribute (Type exceptionType, string message)
        {
            Debug.Assert (exceptionType != null);
            Debug.Assert (message != null);

            m_exceptionType = exceptionType;
            m_message       = message;
        }

        public Type ExceptionType
        {
            get {return m_exceptionType;}
        }

        public string Message
        {
            get {return m_message;}
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class TestPropertyAttribute : Attribute
    {
        string m_name = string.Empty;
        string m_value= string.Empty;
        public string Name
        {
            get { return m_name; }
        }

        public string Value
        {
            get { return m_value; }
        }

        public TestPropertyAttribute(string name , string value)
        {
            // NOTE : DONT THROW EXCEPTIONS FROM HERE IT WILL CRASH GetCustomAttributes() call
            m_name = name;
            m_value = value;
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class ClassInitializeAttribute : Attribute
    {
        public ClassInitializeAttribute()
        {
        }
    }
    [AttributeUsage(AttributeTargets.Method , AllowMultiple = false)]
    public sealed class ClassCleanupAttribute : Attribute
    {
        public ClassCleanupAttribute()
        {
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class AssemblyInitializeAttribute : Attribute
    {
        public AssemblyInitializeAttribute()
        {
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class AssemblyCleanupAttribute : Attribute
    {
        public AssemblyCleanupAttribute()
        {
        }
    }

    /// <summary>
    /// Description of the test
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class DescriptionAttribute : Attribute
    {
        public DescriptionAttribute(string description)
        {
            m_description = description;
        }

        public string Description
        {
            get { return m_description; }
        }

        private string m_description;
    }

    /// <summary>
    /// Test Owner
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class OwnerAttribute : Attribute
    {
        public OwnerAttribute(string owner)
        {
            m_owner = owner;
        }

        public string Owner
        {
            get { return m_owner; }
        }

        private string m_owner;
    }

    /// <summary>
    /// CSS Project Structure URI
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class CssProjectStructureAttribute : Attribute
    {
        public CssProjectStructureAttribute(string cssProjectStructure)
        {
            m_cssProjectStructure = cssProjectStructure;
        }

        public string CssProjectStructure
        {
            get { return m_cssProjectStructure; }
        }

        private string m_cssProjectStructure;
    }


    /// <summary>
    /// CSS Iteration URI
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class CssIterationAttribute : Attribute
    {
        public CssIterationAttribute(string cssIteration)
        {
            m_cssIteration = cssIteration;
        }

        public string CssIteration
        {
            get { return m_cssIteration; }
        }

        private string m_cssIteration;
    }

    /// <summary>
    /// Priority attribute; used to specify the priority of a unit test.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class PriorityAttribute : Attribute
    {
        public PriorityAttribute(int priority)
        {
            m_priority = priority;
        }

        public int Priority
        {
            get { return m_priority; }
        }

        private int m_priority;
    }

    /// <summary>
    /// Timeout attribute; used to specify the timeout of a unit test.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class TimeoutAttribute : Attribute
    {
        public TimeoutAttribute(int timeout)
        {
            m_timeout = timeout;
        }

        public int Timeout
        {
            get { return m_timeout; }
        }

        private int m_timeout;
    }

    /// <summary>
    /// WorkItem attribute; used to specify a work item associated with this test.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class WorkItemAttribute : Attribute
    {
        public WorkItemAttribute(int id)
        {
            m_id = id;
        }

        public int Id
        {
            get { return m_id; }
        }

        private int m_id;
    }


#if DESKTOP
    /// <summary>
    /// HostType specifies the type of host that this unit test will
    /// run in.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class HostTypeAttribute : Attribute
    {
        public HostTypeAttribute(string hostType)
        {
            m_hostType = hostType;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="hostType">The type of the host.</param>
        /// <param name="hostData">Custom data for the host adapter.</param>
        public HostTypeAttribute(string hostType, string hostData)
        {
            m_hostType = hostType;
            m_hostData = hostData;
        }

        public string HostType
        {
            get { return m_hostType; }
        }

        public string HostData
        {
            get { return m_hostData; }
        }

        private string m_hostType;

        /// The reason this is string (and not object) is that currently CMI cannot parse arbitrary instances of object and we deprioritized changing CMI.
        private string m_hostData;
    }
#endif

    /// <summary>
    /// Used to specify deployment item (file or directory) for per-test deployment.
    /// Can be specified on test class or test method.
    /// Can have multiple instances of the attribute to specify more than one item.
    /// The item path can be absolute or relative, if relative, it is relative to RunConfig.RelativePathRoot.
    /// </summary>
    /// <example>
    /// [DeploymentItem("file1.xml")]
    /// [DeploymentItem("file2.xml", "DataFiles")]
    /// [DeploymentItem("bin\Debug")]
    /// </example>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public sealed class DeploymentItemAttribute : Attribute
    {
        private string m_path;
        private string m_outputDirectory;

        public DeploymentItemAttribute(string path)
        {
            m_path = path;
            m_outputDirectory = string.Empty;
        }

        public DeploymentItemAttribute(string path, string outputDirectory)
        {
            m_path = path;
            m_outputDirectory = outputDirectory;
        }

        public string Path
        {
            get { return m_path; }
        }

        public string OutputDirectory
        {
            get { return m_outputDirectory; }
        }
    }

#if DESKTOP
    /// <summary>
    /// Specifies connection string, table name and row access method for data driven testing.
    /// </summary>
    /// <example>
    /// [DataSource("Provider=SQLOLEDB.1;Data Source=mkolt;Integrated Security=SSPI;Initial Catalog=EqtCoverage;Persist Security Info=False", "MyTable")]
    /// [DataSource("dataSourceNameFromConfigFile")]
    /// </example>
	//"oleDbConnectionString" is more descriptive name for this parameter:
	[SuppressMessage("Microsoft.Design", "CA1019:DefineAccessorsForAttributeArguments")]
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class DataSourceAttribute: Attribute
    {
        // DefaultProviderName needs not to be constant so that clients do not need
        // to recompile if the value changes.
        [SuppressMessage("Microsoft.Performance", "CA1802:UseLiteralsWhereAppropriate")]
        public static readonly string DefaultProviderName = "System.Data.OleDb";
        public static readonly DataAccessMethod DefaultDataAccessMethod = DataAccessMethod.Random;

        /// <summary>
        /// Different providers use dfferent connection strings and provider itself is a part of connection string.
        /// </summary>
        private string m_invariantProviderName = DefaultProviderName;
        private string m_connectionString;
        private string m_tableName;
        private DataAccessMethod m_accessMethod;
        private string m_dataSourceSettingName;

        /// <summary>
        /// Specify data provider, connection string, data table and data access method to access the data source.
        /// </summary>
        /// <param name="providerInvariantName">Invariant data provider name, such as System.Data.SqlClient</param>
        /// <param name="connectionString">
        /// Data provider specific connection string. 
        /// WARNING: The connection string can contain sensitive data (for example, a password).
        /// The connection string is stored in plain text in source code and in the compiled assembly. 
        /// Restrict access to the source code and assembly to protect this sensitive information.
        /// </param>
        /// <param name="tableName">The name of the data table.</param>
        /// <param name="dataAccessMethod">Specifies the order to access data.</param>
        public DataSourceAttribute(string providerInvariantName, string connectionString, string tableName, DataAccessMethod dataAccessMethod)
        {
            m_invariantProviderName = providerInvariantName;
            m_connectionString = connectionString;
            m_tableName = tableName;
            m_accessMethod = dataAccessMethod;
        }

        /// <summary>
        /// Specify connection string and data table to access OLEDB data source.
        /// </summary>
        /// <param name="connectionString">
        /// Data provider specific connection string. 
        /// WARNING: The connection string can contain sensitive data (for example, a password).
        /// The connection string is stored in plain text in source code and in the compiled assembly. 
        /// Restrict access to the source code and assembly to protect this sensitive information.
        /// </param>
        /// <param name="tableName">The name of the data table.</param>
        public DataSourceAttribute(string connectionString, string tableName)
            : this(DefaultProviderName, connectionString, tableName, DefaultDataAccessMethod)
        {
        }

        public DataSourceAttribute(string dataSourceSettingName)
        {
            m_dataSourceSettingName = dataSourceSettingName;
        }

        public string ProviderInvariantName
        {
            get { return m_invariantProviderName; }
        }

        public string ConnectionString
        {
            get { return m_connectionString; }
        }

        public string TableName
        {
            get { return m_tableName; }
        }

        public DataAccessMethod DataAccessMethod
        {
            get { return m_accessMethod; }
        }

        /// <summary>
        /// The name of the data source from &lt;microsoft.visualstudio.qualitytools&gt\&lt;dataSources&gt section in config file;
        /// </summary>
        public string DataSourceSettingName
        {
            get { return m_dataSourceSettingName; }
        }
    }
#endif
}
