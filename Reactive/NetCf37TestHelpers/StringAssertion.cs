//*****************************************************************************
// StringAssertion.cs
// Owner: tmarsh
//
// String Assertion class for unit testing
//
// Copyright (c) Microsoft Corporation. All rights reserved.
//*****************************************************************************
namespace Microsoft.VisualStudio.TestTools.UnitTesting
{
    using System;
    using System.Collections;
    using System.Globalization;
    using System.Text.RegularExpressions;
    using Microsoft.VisualStudio.TestTools.Resources;

    public static class StringAssert
    {
        #region Substrings

        /// <summary>
        /// Tests whether the specified string contains the specified substring
        /// and throws an exception if the substring does not occur within the
        /// test string.
        /// </summary>
        /// <param name="value">
        /// The string that is expected to contain <paramref name="substring"/>.
        /// </param>
        /// <param name="substring">
        /// The string expected to occur within <paramref name="value"/>.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="substring"/> is not found in
        /// <paramref name="value"/>.
        /// </exception>
        public static void Contains(string value, string substring)
        {
            StringAssert.Contains(value, substring, string.Empty, null);
        }

        /// <summary>
        /// Tests whether the specified string contains the specified substring
        /// and throws an exception if the substring does not occur within the
        /// test string.
        /// </summary>
        /// <param name="value">
        /// The string that is expected to contain <paramref name="substring"/>.
        /// </param>
        /// <param name="substring">
        /// The string expected to occur within <paramref name="value"/>.
        /// </param>
        /// <param name="message">
        /// The message to include in the exception when <paramref name="substring"/>
        /// is not in <paramref name="value"/>. The message is shown in
        /// test results.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="substring"/> is not found in
        /// <paramref name="value"/>.
        /// </exception>
        public static void Contains(string value, string substring, string message)
        {
            StringAssert.Contains(value, substring, message, null);
        }

        /// <summary>
        /// Tests whether the specified string contains the specified substring
        /// and throws an exception if the substring does not occur within the
        /// test string.
        /// </summary>
        /// <param name="value">
        /// The string that is expected to contain <paramref name="substring"/>.
        /// </param>
        /// <param name="substring">
        /// The string expected to occur within <paramref name="value"/>.
        /// </param>
        /// <param name="message">
        /// The message to include in the exception when <paramref name="substring"/>
        /// is not in <paramref name="value"/>. The message is shown in
        /// test results.
        /// </param>
        /// <param name="parameters">
        /// An array of parameters to use when formatting <paramref name="message"/>.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="substring"/> is not found in
        /// <paramref name="value"/>.
        /// </exception>
        public static void Contains(string value, string substring, string message, params object[] parameters)
        {
            Assert.CheckParameterNotNull(value, "StringAssert.Contains", "value", string.Empty);
            Assert.CheckParameterNotNull(substring, "StringAssert.Contains", "substring", string.Empty);
            if (0 > value.IndexOf(substring, StringComparison.Ordinal))
            {
                string finalMessage = FrameworkMessages.ContainsFail(value, substring, message);
                Assert.HandleFail("StringAssert.Contains", finalMessage, parameters);
            }
        }

        /// <summary>
        /// Tests whether the specified string begins with the specified substring
        /// and throws an exception if the test string does not start with the
        /// substring.
        /// </summary>
        /// <param name="value">
        /// The string that is expected to begin with <paramref name="substring"/>.
        /// </param>
        /// <param name="substring">
        /// The string expected to be a prefix of <paramref name="value"/>.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="value"/> does not begin with
        /// <paramref name="substring"/>.
        /// </exception>
        public static void StartsWith(string value, string substring)
        {
            StringAssert.StartsWith(value, substring, string.Empty, null);
        }

        /// <summary>
        /// Tests whether the specified string begins with the specified substring
        /// and throws an exception if the test string does not start with the
        /// substring.
        /// </summary>
        /// <param name="value">
        /// The string that is expected to begin with <paramref name="substring"/>.
        /// </param>
        /// <param name="substring">
        /// The string expected to be a prefix of <paramref name="value"/>.
        /// </param>
        /// <param name="message">
        /// The message to include in the exception when <paramref name="value"/>
        /// does not begin with <paramref name="substring"/>. The message is
        /// shown in test results.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="value"/> does not begin with
        /// <paramref name="substring"/>.
        /// </exception>
        public static void StartsWith(string value, string substring, string message)
        {
            StringAssert.StartsWith(value, substring, message, null);
        }

        /// <summary>
        /// Tests whether the specified string begins with the specified substring
        /// and throws an exception if the test string does not start with the
        /// substring.
        /// </summary>
        /// <param name="value">
        /// The string that is expected to begin with <paramref name="substring"/>.
        /// </param>
        /// <param name="substring">
        /// The string expected to be a prefix of <paramref name="value"/>.
        /// </param>
        /// <param name="message">
        /// The message to include in the exception when <paramref name="value"/>
        /// does not begin with <paramref name="substring"/>. The message is
        /// shown in test results.
        /// </param>
        /// <param name="parameters">
        /// An array of parameters to use when formatting <paramref name="message"/>.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="value"/> does not begin with
        /// <paramref name="substring"/>.
        /// </exception>
        public static void StartsWith(string value, string substring, string message, params object[] parameters)
        {
            Assert.CheckParameterNotNull(value, "StringAssert.StartsWith", "value", string.Empty);
            Assert.CheckParameterNotNull(substring, "StringAssert.StartsWith", "substring", string.Empty);
            if (!value.StartsWith(substring, StringComparison.Ordinal))
            {
                string finalMessage = FrameworkMessages.StartsWithFail(value, substring, message);
                Assert.HandleFail("StringAssert.StartsWith", finalMessage, parameters);
            }
        }

        /// <summary>
        /// Tests whether the specified string ends with the specified substring
        /// and throws an exception if the test string does not end with the
        /// substring.
        /// </summary>
        /// <param name="value">
        /// The string that is expected to end with <paramref name="substring"/>.
        /// </param>
        /// <param name="substring">
        /// The string expected to be a suffix of <paramref name="value"/>.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="value"/> does not end with
        /// <paramref name="substring"/>.
        /// </exception>
        public static void EndsWith(string value, string substring)
        {
            StringAssert.EndsWith(value, substring, string.Empty, null);
        }

        /// <summary>
        /// Tests whether the specified string ends with the specified substring
        /// and throws an exception if the test string does not end with the
        /// substring.
        /// </summary>
        /// <param name="value">
        /// The string that is expected to end with <paramref name="substring"/>.
        /// </param>
        /// <param name="substring">
        /// The string expected to be a suffix of <paramref name="value"/>.
        /// </param>
        /// <param name="message">
        /// The message to include in the exception when <paramref name="value"/>
        /// does not end with <paramref name="substring"/>. The message is
        /// shown in test results.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="value"/> does not end with
        /// <paramref name="substring"/>.
        /// </exception>
        public static void EndsWith(string value, string substring, string message)
        {
            StringAssert.EndsWith(value, substring, message, null);
        }

        /// <summary>
        /// Tests whether the specified string ends with the specified substring
        /// and throws an exception if the test string does not end with the
        /// substring.
        /// </summary>
        /// <param name="value">
        /// The string that is expected to end with <paramref name="substring"/>.
        /// </param>
        /// <param name="substring">
        /// The string expected to be a suffix of <paramref name="value"/>.
        /// </param>
        /// <param name="message">
        /// The message to include in the exception when <paramref name="value"/>
        /// does not end with <paramref name="substring"/>. The message is
        /// shown in test results.
        /// </param>
        /// <param name="parameters">
        /// An array of parameters to use when formatting <paramref name="message"/>.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="value"/> does not end with
        /// <paramref name="substring"/>.
        /// </exception>
        public static void EndsWith(string value, string substring, string message, params object[] parameters)
        {
            Assert.CheckParameterNotNull(value, "StringAssert.EndsWith", "value", string.Empty);
            Assert.CheckParameterNotNull(substring, "StringAssert.EndsWith", "substring", string.Empty);
            if (!value.EndsWith(substring, StringComparison.Ordinal))
            {
                string finalMessage = FrameworkMessages.EndsWithFail(value, substring, message);
                Assert.HandleFail("StringAssert.EndsWith", finalMessage, parameters);
            }
        }

        #endregion Substrings

        #region Regular Expresssions

        /// <summary>
        /// Tests whether the specified string matches a regular expression and
        /// throws an exception if the string does not match the expression.
        /// </summary>
        /// <param name="value">
        /// The string that is expected to match <paramref name="pattern"/>.
        /// </param>
        /// <param name="pattern">
        /// The regular expression that <paramref name="value"/> is
        /// expected to match.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="value"/> does not match
        /// <paramref name="pattern"/>.
        /// </exception>
        public static void Matches(string value, Regex pattern)
        {
            StringAssert.Matches(value, pattern, string.Empty, null);
        }

        /// <summary>
        /// Tests whether the specified string matches a regular expression and
        /// throws an exception if the string does not match the expression.
        /// </summary>
        /// <param name="value">
        /// The string that is expected to match <paramref name="pattern"/>.
        /// </param>
        /// <param name="pattern">
        /// The regular expression that <paramref name="value"/> is
        /// expected to match.
        /// </param>
        /// <param name="message">
        /// The message to include in the exception when <paramref name="value"/>
        /// does not match <paramref name="pattern"/>. The message is shown in
        /// test results.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="value"/> does not match
        /// <paramref name="pattern"/>.
        /// </exception>
        public static void Matches(string value, Regex pattern, string message)
        {
            StringAssert.Matches(value, pattern, message, null);
        }

        /// <summary>
        /// Tests whether the specified string matches a regular expression and
        /// throws an exception if the string does not match the expression.
        /// </summary>
        /// <param name="value">
        /// The string that is expected to match <paramref name="pattern"/>.
        /// </param>
        /// <param name="pattern">
        /// The regular expression that <paramref name="value"/> is
        /// expected to match.
        /// </param>
        /// <param name="message">
        /// The message to include in the exception when <paramref name="value"/>
        /// does not match <paramref name="pattern"/>. The message is shown in
        /// test results.
        /// </param>
        /// <param name="parameters">
        /// An array of parameters to use when formatting <paramref name="message"/>.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="value"/> does not match
        /// <paramref name="pattern"/>.
        /// </exception>
        public static void Matches(string value, Regex pattern, string message, params object[] parameters)
        {
            Assert.CheckParameterNotNull(value, "StringAssert.Matches", "value", string.Empty);
            Assert.CheckParameterNotNull(pattern, "StringAssert.Matches", "pattern", string.Empty);

            if (!pattern.IsMatch(value))
            {
                string finalMessage = FrameworkMessages.IsMatchFail(value, pattern, message);
                Assert.HandleFail("StringAssert.Matches", finalMessage, parameters);
            }
        }

        /// <summary>
        /// Tests whether the specified string does not match a regular expression
        /// and throws an exception if the string matches the expression.
        /// </summary>
        /// <param name="value">
        /// The string that is expected not to match <paramref name="pattern"/>.
        /// </param>
        /// <param name="pattern">
        /// The regular expression that <paramref name="value"/> is
        /// expected to not match.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="value"/> matches <paramref name="pattern"/>.
        /// </exception>
        public static void DoesNotMatch(string value, Regex pattern)
        {
            StringAssert.DoesNotMatch(value, pattern, string.Empty, null);
        }

        /// <summary>
        /// Tests whether the specified string does not match a regular expression
        /// and throws an exception if the string matches the expression.
        /// </summary>
        /// <param name="value">
        /// The string that is expected not to match <paramref name="pattern"/>.
        /// </param>
        /// <param name="pattern">
        /// The regular expression that <paramref name="value"/> is
        /// expected to not match.
        /// </param>
        /// <param name="message">
        /// The message to include in the exception when <paramref name="value"/>
        /// matches <paramref name="pattern"/>. The message is shown in test
        /// results.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="value"/> matches <paramref name="pattern"/>.
        /// </exception>
        public static void DoesNotMatch(string value, Regex pattern, string message)
        {
            StringAssert.DoesNotMatch(value, pattern, message, null);
        }

        /// <summary>
        /// Tests whether the specified string does not match a regular expression
        /// and throws an exception if the string matches the expression.
        /// </summary>
        /// <param name="value">
        /// The string that is expected not to match <paramref name="pattern"/>.
        /// </param>
        /// <param name="pattern">
        /// The regular expression that <paramref name="value"/> is
        /// expected to not match.
        /// </param>
        /// <param name="message">
        /// The message to include in the exception when <paramref name="value"/>
        /// matches <paramref name="pattern"/>. The message is shown in test
        /// results.
        /// </param>
        /// <param name="parameters">
        /// An array of parameters to use when formatting <paramref name="message"/>.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="value"/> matches <paramref name="pattern"/>.
        /// </exception>
        public static void DoesNotMatch(string value, Regex pattern, string message, params object[] parameters)
        {
            Assert.CheckParameterNotNull(value, "StringAssert.DoesNotMatch", "value", string.Empty);
            Assert.CheckParameterNotNull(pattern, "StringAssert.DoesNotMatch", "pattern", string.Empty);

            if (pattern.IsMatch(value))
            {
                string finalMessage = FrameworkMessages.IsNotMatchFail(value, pattern, message);
                Assert.HandleFail("StringAssert.DoesNotMatch", finalMessage, parameters);
            }
        }

        #endregion Regular Expresssions
    }
}
