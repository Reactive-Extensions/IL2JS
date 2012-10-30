// <copyright file="IConfig.cs" company="Microsoft">
// Copyright © 2009 Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.Csa.SharedObjects.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// IConfig interface.  Represents the interface to a configuration system.
    /// </summary>
    public interface IConfig
    {
        /// <summary>
        /// Gets a string configuration setting.
        /// </summary>
        /// <param name="settingName">The name of the configuration setting to retrieve.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>A string containing the value of the configuration setting, or, the default value.</returns>
        string GetConfigurationSetting(string settingName, string defaultValue);

        /// <summary>
        /// Gets an integer configuration setting.
        /// </summary>
        /// <param name="settingName">The setting name.</param>
        /// <param name="defaultValue">The default value to return if the setting is missing, or, invalid..</param>
        /// <returns>The integer setting.</returns>
        int GetConfigurationSetting(string settingName, int defaultValue);

        /// <summary>
        /// Gets a boolean configuration setting.
        /// </summary>
        /// <param name="settingName">The setting name.</param>
        /// <param name="defaultValue">The default value to return if the setting is missing, or, invalid..</param>
        /// <returns>The boolean setting.</returns>
        bool GetConfigurationSetting(string settingName, bool defaultValue);

        /// <summary>
        /// Get a configuration setting representing a time span value
        /// </summary>
        /// <param name="settingName">The setting name.</param>
        /// <param name="defaultValue">The default value to return if the setting is missing, or, invalid..</param>
        /// <returns>The TimeSpan setting</returns>
        TimeSpan GetConfigurationSetting(string settingName, TimeSpan defaultValue);

        /// <summary>
        /// Returns a list of settings that match the provided filter criteria
        /// </summary>
        /// <param name="filter">A function to filter on key values</param>
        /// <returns>Setting values that match the query</returns>
        IEnumerable<string> QueryConfigurationSettings(Func<string, bool> filter);
    }
}
