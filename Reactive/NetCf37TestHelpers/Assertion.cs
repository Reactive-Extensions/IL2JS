//*****************************************************************************
// Assertion.cs
// Owner: tmarsh
//
// Assertion class for unit testing
//
// Copyright (c) Microsoft Corporation. All rights reserved.
//*****************************************************************************

namespace Microsoft.VisualStudio.TestTools.UnitTesting
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Reflection;
    using System.Text;
    using Microsoft.VisualStudio.TestTools.Resources;

    /// <summary>
    /// A collection of helper classes to test various conditions within
    /// unit tests. If the condition being tested is not met, an exception
    /// is thrown.
    /// </summary>
    public static partial class Assert
    {
        #region Boolean

        /// <summary>
        /// Tests whether the specified condition is true and throws an exception
        /// if the condition is false.
        /// </summary>
        /// <param name="condition">The condition the test expects to be true.</param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="condition"/> is false.
        /// </exception>
        static public void IsTrue(bool condition)
        {
            Assert.IsTrue(condition, string.Empty, null);
        }

        /// <summary>
        /// Tests whether the specified condition is true and throws an exception
        /// if the condition is false.
        /// </summary>
        /// <param name="condition">The condition the test expects to be true.</param>
        /// <param name="message">
        /// The message to include in the exception when <paramref name="condition"/>
        /// is false. The message is shown in test results.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="condition"/> is false.
        /// </exception>
        static public void IsTrue(bool condition, string message)
        {
            Assert.IsTrue(condition, message, null);
        }

        /// <summary>
        /// Tests whether the specified condition is true and throws an exception
        /// if the condition is false.
        /// </summary>
        /// <param name="condition">The condition the test expects to be true.</param>
        /// <param name="message">
        /// The message to include in the exception when <paramref name="condition"/>
        /// is false. The message is shown in test results.
        /// </param>
        /// <param name="parameters">
        /// An array of parameters to use when formatting <paramref name="message"/>.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="condition"/> is false.
        /// </exception>
        static public void IsTrue(bool condition, string message, params object[] parameters)
        {
            if (!condition)
            {
                Assert.HandleFail("Assert.IsTrue", message, parameters);
            }
        }

        /// <summary>
        /// Tests whether the specified condition is false and throws an exception
        /// if the condition is true.
        /// </summary>
        /// <param name="condition">The condition the test expects to be false.</param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="condition"/> is true.
        /// </exception>
        static public void IsFalse(bool condition)
        {
            Assert.IsFalse(condition, string.Empty, null);
        }

        /// <summary>
        /// Tests whether the specified condition is false and throws an exception
        /// if the condition is true.
        /// </summary>
        /// <param name="condition">The condition the test expects to be false.</param>
        /// <param name="message">
        /// The message to include in the exception when <paramref name="condition"/>
        /// is true. The message is shown in test results.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="condition"/> is true.
        /// </exception>
        static public void IsFalse(bool condition, string message)
        {
            Assert.IsFalse(condition, message, null);
        }

        /// <summary>
        /// Tests whether the specified condition is false and throws an exception
        /// if the condition is true.
        /// </summary>
        /// <param name="condition">The condition the test expects to be false.</param>
        /// <param name="message">
        /// The message to include in the exception when <paramref name="condition"/>
        /// is true. The message is shown in test results.
        /// </param>
        /// <param name="parameters">
        /// An array of parameters to use when formatting <paramref name="message"/>.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="condition"/> is true.
        /// </exception>
        static public void IsFalse(bool condition, string message, params object[] parameters)
        {
            if (condition)
            {
                Assert.HandleFail("Assert.IsFalse", message, parameters);
            }
        }

        #endregion

        #region Null

        /// <summary>
        /// Tests whether the specified object is null and throws an exception
        /// if it is not.
        /// </summary>
        /// <param name="value">The object the test expects to be null.</param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="value"/> is not null.
        /// </exception>
        static public void IsNull(object value)
        {
            Assert.IsNull(value, string.Empty, null);
        }

        /// <summary>
        /// Tests whether the specified object is null and throws an exception
        /// if it is not.
        /// </summary>
        /// <param name="value">The object the test expects to be null.</param>
        /// <param name="message">
        /// The message to include in the exception when <paramref name="value"/>
        /// is not null. The message is shown in test results.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="value"/> is not null.
        /// </exception>
        static public void IsNull(object value, string message)
        {
            Assert.IsNull(value, message, null);
        }

        /// <summary>
        /// Tests whether the specified object is null and throws an exception
        /// if it is not.
        /// </summary>
        /// <param name="value">The object the test expects to be null.</param>
        /// <param name="message">
        /// The message to include in the exception when <paramref name="value"/>
        /// is not null. The message is shown in test results.
        /// </param>
        /// <param name="parameters">
        /// An array of parameters to use when formatting <paramref name="message"/>.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="value"/> is not null.
        /// </exception>
        static public void IsNull(object value, string message, params object[] parameters)
        {
            if (value != null)
            {
                Assert.HandleFail("Assert.IsNull", message, parameters);
            }
        }

        /// <summary>
        /// Tests whether the specified object is non-null and throws an exception
        /// if it is null.
        /// </summary>
        /// <param name="value">The object the test expects not to be null.</param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="value"/> is null.
        /// </exception>
        static public void IsNotNull(object value)
        {
            Assert.IsNotNull(value, string.Empty, null);
        }

        /// <summary>
        /// Tests whether the specified object is non-null and throws an exception
        /// if it is null.
        /// </summary>
        /// <param name="value">The object the test expects not to be null.</param>
        /// <param name="message">
        /// The message to include in the exception when <paramref name="value"/>
        /// is null. The message is shown in test results.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="value"/> is null.
        /// </exception>
        static public void IsNotNull(object value, string message)
        {
            Assert.IsNotNull(value, message, null);
        }

        /// <summary>
        /// Tests whether the specified object is non-null and throws an exception
        /// if it is null.
        /// </summary>
        /// <param name="value">The object the test expects not to be null.</param>
        /// <param name="message">
        /// The message to include in the exception when <paramref name="value"/>
        /// is null. The message is shown in test results.
        /// </param>
        /// <param name="parameters">
        /// An array of parameters to use when formatting <paramref name="message"/>.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="value"/> is null.
        /// </exception>
        static public void IsNotNull(object value, string message, params object[] parameters)
        {
            if (value == null)
            {
                Assert.HandleFail("Assert.IsNotNull", message, parameters);
            }
        }

        #endregion

        #region AreSame

        /// <summary>
        /// Tests whether the specified objects both refer to the same object and
        /// throws an exception if the two inputs do not refer to the same object.
        /// </summary>
        /// <param name="expected">
        /// The first object to compare. This is the value the test expects.
        /// </param>
        /// <param name="actual">
        /// The second object to compare. This is the value produced by the code under test.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="expected"/> does not refer to the same object
        /// as <paramref name="actual"/>.
        /// </exception>
        static public void AreSame(object expected, object actual)
        {
            Assert.AreSame(expected, actual, string.Empty, null);
        }

        /// <summary>
        /// Tests whether the specified objects both refer to the same object and
        /// throws an exception if the two inputs do not refer to the same object.
        /// </summary>
        /// <param name="expected">
        /// The first object to compare. This is the value the test expects.
        /// </param>
        /// <param name="actual">
        /// The second object to compare. This is the value produced by the code under test.
        /// </param>
        /// <param name="message">
        /// The message to include in the exception when <paramref name="actual"/>
        /// is not the same as <paramref name="expected"/>. The message is shown
        /// in test results.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="expected"/> does not refer to the same object
        /// as <paramref name="actual"/>.
        /// </exception>
        static public void AreSame(object expected, object actual, string message)
        {
            Assert.AreSame(expected, actual, message, null);
        }

        /// <summary>
        /// Tests whether the specified objects both refer to the same object and
        /// throws an exception if the two inputs do not refer to the same object.
        /// </summary>
        /// <param name="expected">
        /// The first object to compare. This is the value the test expects.
        /// </param>
        /// <param name="actual">
        /// The second object to compare. This is the value produced by the code under test.
        /// </param>
        /// <param name="message">
        /// The message to include in the exception when <paramref name="actual"/>
        /// is not the same as <paramref name="expected"/>. The message is shown
        /// in test results.
        /// </param>
        /// <param name="parameters">
        /// An array of parameters to use when formatting <paramref name="message"/>.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="expected"/> does not refer to the same object
        /// as <paramref name="actual"/>.
        /// </exception>
        static public void AreSame(object expected, object actual, string message, params object[] parameters)
        {
            if (!Object.ReferenceEquals(expected, actual))
            {
                string finalMessage = message;

                ValueType valExpected = expected as ValueType;
                if (valExpected != null)
                {
                    ValueType valActual = actual as ValueType;
                    if (valActual != null)
                    {
                        finalMessage = FrameworkMessages.AreSameGivenValues(
                            message == null ? String.Empty : ReplaceNulls(message));
                    }
                }

                Assert.HandleFail("Assert.AreSame", finalMessage, parameters);
            }
        }

        /// <summary>
        /// Tests whether the specified objects refer to different objects and
        /// throws an exception if the two inputs refer to the same object.
        /// </summary>
        /// <param name="notExpected">
        /// The first object to compare. This is the value the test expects not
        /// to match <paramref name="actual"/>.
        /// </param>
        /// <param name="actual">
        /// The second object to compare. This is the value produced by the code under test.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="notExpected"/> refers to the same object
        /// as <paramref name="actual"/>.
        /// </exception>
        static public void AreNotSame(object notExpected, object actual)
        {
            Assert.AreNotSame(notExpected, actual, string.Empty, null);
        }

        /// <summary>
        /// Tests whether the specified objects refer to different objects and
        /// throws an exception if the two inputs refer to the same object.
        /// </summary>
        /// <param name="notExpected">
        /// The first object to compare. This is the value the test expects not
        /// to match <paramref name="actual"/>.
        /// </param>
        /// <param name="actual">
        /// The second object to compare. This is the value produced by the code under test.
        /// </param>
        /// <param name="message">
        /// The message to include in the exception when <paramref name="actual"/>
        /// is the same as <paramref name="notExpected"/>. The message is shown in
        /// test results.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="notExpected"/> refers to the same object
        /// as <paramref name="actual"/>.
        /// </exception>
        static public void AreNotSame(object notExpected, object actual, string message)
        {
            Assert.AreNotSame(notExpected, actual, message, null);
        }

        /// <summary>
        /// Tests whether the specified objects refer to different objects and
        /// throws an exception if the two inputs refer to the same object.
        /// </summary>
        /// <param name="notExpected">
        /// The first object to compare. This is the value the test expects not
        /// to match <paramref name="actual"/>.
        /// </param>
        /// <param name="actual">
        /// The second object to compare. This is the value produced by the code under test.
        /// </param>
        /// <param name="message">
        /// The message to include in the exception when <paramref name="actual"/>
        /// is the same as <paramref name="notExpected"/>. The message is shown in
        /// test results.
        /// </param>
        /// <param name="parameters">
        /// An array of parameters to use when formatting <paramref name="message"/>.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="notExpected"/> refers to the same object
        /// as <paramref name="actual"/>.
        /// </exception>
        static public void AreNotSame(object notExpected, object actual, string message, params object[] parameters)
        {
            if (Object.ReferenceEquals(notExpected, actual))
            {
                Assert.HandleFail("Assert.AreNotSame", message, parameters);
            }
        }

        #endregion

        #region AreEqual

        /// <summary>
        /// Tests whether the specified values are equal and throws an exception
        /// if the two values are not equal. Different numeric types are treated
        /// as unequal even if the logical values are equal. 42L is not equal to 42.
        /// </summary>
        /// <typeparam name="T">The type of values to compare.</typeparam>
        /// <param name="expected">
        /// The first value to compare. This is the value the tests expects.
        /// </param>
        /// <param name="actual">
        /// The second value to compare. This is the value produced by the code under test.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="expected"/> is not equal to 
        /// <paramref name="actual"/>.
        /// </exception>
        static public void AreEqual<T>(T expected, T actual)
        {
            Assert.AreEqual(expected, actual, string.Empty, null);
        }

        /// <summary>
        /// Tests whether the specified values are equal and throws an exception
        /// if the two values are not equal. Different numeric types are treated
        /// as unequal even if the logical values are equal. 42L is not equal to 42.
        /// </summary>
        /// <typeparam name="T">The type of values to compare.</typeparam>
        /// <param name="expected">
        /// The first value to compare. This is the value the tests expects.
        /// </param>
        /// <param name="actual">
        /// The second value to compare. This is the value produced by the code under test.
        /// </param>
        /// <param name="message">
        /// The message to include in the exception when <paramref name="actual"/>
        /// is not equal to <paramref name="expected"/>. The message is shown in
        /// test results.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="expected"/> is not equal to
        /// <paramref name="actual"/>.
        /// </exception>
        static public void AreEqual<T>(T expected, T actual, string message)
        {
            Assert.AreEqual(expected, actual, message, null);
        }

        /// <summary>
        /// Tests whether the specified values are equal and throws an exception
        /// if the two values are not equal. Different numeric types are treated
        /// as unequal even if the logical values are equal. 42L is not equal to 42.
        /// </summary>
        /// <typeparam name="T">The type of values to compare.</typeparam>
        /// <param name="expected">
        /// The first value to compare. This is the value the tests expects.
        /// </param>
        /// <param name="actual">
        /// The second value to compare. This is the value produced by the code under test.
        /// </param>
        /// <param name="message">
        /// The message to include in the exception when <paramref name="actual"/>
        /// is not equal to <paramref name="expected"/>. The message is shown in
        /// test results.
        /// </param>
        /// <param name="parameters">
        /// An array of parameters to use when formatting <paramref name="message"/>.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="expected"/> is not equal to 
        /// <paramref name="actual"/>.
        /// </exception>
        static public void AreEqual<T>(T expected, T actual, string message, params object[] parameters)
        {
            if (!Object.Equals(expected, actual))
            {
                string finalMessage;
                if (actual != null && expected != null && !actual.GetType().Equals(expected.GetType()))
                {
                    // This is for cases like: Assert.AreEqual(42L, 42) -> Expected: <42>, Actual: <42>
                    finalMessage = FrameworkMessages.AreEqualDifferentTypesFailMsg(
                        message == null ? String.Empty : ReplaceNulls(message),
                        ReplaceNulls(expected),
                        expected.GetType().FullName,
                        ReplaceNulls(actual),
                        actual.GetType().FullName);
                }
                else
                {
                    finalMessage = FrameworkMessages.AreEqualFailMsg(
                        message == null ? String.Empty : ReplaceNulls(message),
                        ReplaceNulls(expected),
                        ReplaceNulls(actual));
                }
                Assert.HandleFail("Assert.AreEqual", finalMessage, parameters);
            }
        }

        /// <summary>
        /// Tests whether the specified values are unequal and throws an exception
        /// if the two values are equal. Different numeric types are treated
        /// as unequal even if the logical values are equal. 42L is not equal to 42.
        /// </summary>
        /// <typeparam name="T">The type of values to compare.</typeparam>
        /// <param name="notExpected">
        /// The first value to compare. This is the value the test expects not
        /// to match <paramref name="actual"/>.
        /// </param>
        /// <param name="actual">
        /// The second value to compare. This is the value produced by the code under test.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="notExpected"/> is equal to <paramref name="actual"/>.
        /// </exception>
        static public void AreNotEqual<T>(T notExpected, T actual)
        {
            Assert.AreNotEqual(notExpected, actual, string.Empty, null);
        }

        /// <summary>
        /// Tests whether the specified values are unequal and throws an exception
        /// if the two values are equal. Different numeric types are treated
        /// as unequal even if the logical values are equal. 42L is not equal to 42.
        /// </summary>
        /// <typeparam name="T">The type of values to compare.</typeparam>
        /// <param name="notExpected">
        /// The first value to compare. This is the value the test expects not
        /// to match <paramref name="actual"/>.
        /// </param>
        /// <param name="actual">
        /// The second value to compare. This is the value produced by the code under test.
        /// </param>
        /// <param name="message">
        /// The message to include in the exception when <paramref name="actual"/>
        /// is equal to <paramref name="notExpected"/>. The message is shown in
        /// test results.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="notExpected"/> is equal to <paramref name="actual"/>.
        /// </exception>
        static public void AreNotEqual<T>(T notExpected, T actual, string message)
        {
            Assert.AreNotEqual(notExpected, actual, message, null);
        }

        /// <summary>
        /// Tests whether the specified values are unequal and throws an exception
        /// if the two values are equal. Different numeric types are treated
        /// as unequal even if the logical values are equal. 42L is not equal to 42.
        /// </summary>
        /// <typeparam name="T">The type of values to compare.</typeparam>
        /// <param name="notExpected">
        /// The first value to compare. This is the value the test expects not
        /// to match <paramref name="actual"/>.
        /// </param>
        /// <param name="actual">
        /// The second value to compare. This is the value produced by the code under test.
        /// </param>
        /// <param name="message">
        /// The message to include in the exception when <paramref name="actual"/>
        /// is equal to <paramref name="notExpected"/>. The message is shown in
        /// test results.
        /// </param>
        /// <param name="parameters">
        /// An array of parameters to use when formatting <paramref name="message"/>.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="notExpected"/> is equal to <paramref name="actual"/>.
        /// </exception>
        static public void AreNotEqual<T>(T notExpected, T actual, string message, params object[] parameters)
        {
            if (Object.Equals(notExpected, actual))
            {
                string finalMessage = FrameworkMessages.AreNotEqualFailMsg(
                    message == null ? String.Empty : ReplaceNulls(message),
                    ReplaceNulls(notExpected),
                    ReplaceNulls(actual));
                Assert.HandleFail("Assert.AreNotEqual", finalMessage, parameters);
            }
        }

        /// <summary>
        /// Tests whether the specified objects are equal and throws an exception
        /// if the two objects are not equal. Different numeric types are treated
        /// as unequal even if the logical values are equal. 42L is not equal to 42.
        /// </summary>
        /// <param name="expected">
        /// The first object to compare. This is the object the tests expects.
        /// </param>
        /// <param name="actual">
        /// The second object to compare. This is the object produced by the code under test.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="expected"/> is not equal to
        /// <paramref name="actual"/>.
        /// </exception>
        static public void AreEqual(object expected, object actual)
        {
            Assert.AreEqual(expected, actual, String.Empty, null);
        }

        /// <summary>
        /// Tests whether the specified objects are equal and throws an exception
        /// if the two objects are not equal. Different numeric types are treated
        /// as unequal even if the logical values are equal. 42L is not equal to 42.
        /// </summary>
        /// <param name="expected">
        /// The first object to compare. This is the object the tests expects.
        /// </param>
        /// <param name="actual">
        /// The second object to compare. This is the object produced by the code under test.
        /// </param>
        /// <param name="message">
        /// The message to include in the exception when <paramref name="actual"/>
        /// is not equal to <paramref name="expected"/>. The message is shown in
        /// test results.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="expected"/> is not equal to
        /// <paramref name="actual"/>.
        /// </exception>
        static public void AreEqual(object expected, object actual, string message)
        {
            Assert.AreEqual(expected, actual, message, null);
        }

        /// <summary>
        /// Tests whether the specified objects are equal and throws an exception
        /// if the two objects are not equal. Different numeric types are treated
        /// as unequal even if the logical values are equal. 42L is not equal to 42.
        /// </summary>
        /// <param name="expected">
        /// The first object to compare. This is the object the tests expects.
        /// </param>
        /// <param name="actual">
        /// The second object to compare. This is the object produced by the code under test.
        /// </param>
        /// <param name="message">
        /// The message to include in the exception when <paramref name="actual"/>
        /// is not equal to <paramref name="expected"/>. The message is shown in
        /// test results.
        /// </param>
        /// <param name="parameters">
        /// An array of parameters to use when formatting <paramref name="message"/>.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="expected"/> is not equal to
        /// <paramref name="actual"/>.
        /// </exception>
        static public void AreEqual(object expected, object actual, string message, params object[] parameters)
        {
            Assert.AreEqual<object>(expected, actual, message, parameters);
        }

        /// <summary>
        /// Tests whether the specified objects are unequal and throws an exception
        /// if the two objects are equal. Different numeric types are treated
        /// as unequal even if the logical values are equal. 42L is not equal to 42.
        /// </summary>
        /// <param name="notExpected">
        /// The first object to compare. This is the value the test expects not
        /// to match <paramref name="actual"/>.
        /// </param>
        /// <param name="actual">
        /// The second object to compare. This is the object produced by the code under test.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="notExpected"/> is equal to <paramref name="actual"/>.
        /// </exception>
        static public void AreNotEqual(object notExpected, object actual)
        {
            Assert.AreNotEqual(notExpected, actual, string.Empty, null);
        }

        /// <summary>
        /// Tests whether the specified objects are unequal and throws an exception
        /// if the two objects are equal. Different numeric types are treated
        /// as unequal even if the logical values are equal. 42L is not equal to 42.
        /// </summary>
        /// <param name="notExpected">
        /// The first object to compare. This is the value the test expects not
        /// to match <paramref name="actual"/>.
        /// </param>
        /// <param name="actual">
        /// The second object to compare. This is the object produced by the code under test.
        /// </param>
        /// <param name="message">
        /// The message to include in the exception when <paramref name="actual"/>
        /// is equal to <paramref name="notExpected"/>. The message is shown in
        /// test results.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="notExpected"/> is equal to <paramref name="actual"/>.
        /// </exception>
        static public void AreNotEqual(object notExpected, object actual, string message)
        {
            Assert.AreNotEqual(notExpected, actual, message, null);
        }

        /// <summary>
        /// Tests whether the specified objects are unequal and throws an exception
        /// if the two objects are equal. Different numeric types are treated
        /// as unequal even if the logical values are equal. 42L is not equal to 42.
        /// </summary>
        /// <param name="notExpected">
        /// The first object to compare. This is the value the test expects not
        /// to match <paramref name="actual"/>.
        /// </param>
        /// <param name="actual">
        /// The second object to compare. This is the object produced by the code under test.
        /// </param>
        /// <param name="message">
        /// The message to include in the exception when <paramref name="actual"/>
        /// is equal to <paramref name="notExpected"/>. The message is shown in
        /// test results.
        /// </param>
        /// <param name="parameters">
        /// An array of parameters to use when formatting <paramref name="message"/>.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="notExpected"/> is equal to <paramref name="actual"/>.
        /// </exception>
        static public void AreNotEqual(object notExpected, object actual, string message, params object[] parameters)
        {
            Assert.AreNotEqual<object>(notExpected, actual, message, parameters);
        }

        /// <summary>
        /// Tests whether the specified floats are equal and throws an exception
        /// if they are not equal.
        /// </summary>
        /// <param name="expected">
        /// The first float to compare. This is the float the tests expects.
        /// </param>
        /// <param name="actual">
        /// The second float to compare. This is the float produced by the code under test.
        /// </param>
        /// <param name="delta">
        /// The required accuracy. An exception will be thrown only if
        /// <paramref name="actual"/> is different than <paramref name="expected"/>
        /// by more than <paramref name="delta"/>.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="expected"/> is not equal to
        /// <paramref name="actual"/>.
        /// </exception>
        static public void AreEqual(float expected, float actual, float delta)
        {
            Assert.AreEqual(expected, actual, delta, string.Empty, null);
        }

        /// <summary>
        /// Tests whether the specified floats are equal and throws an exception
        /// if they are not equal.
        /// </summary>
        /// <param name="expected">
        /// The first float to compare. This is the float the tests expects.
        /// </param>
        /// <param name="actual">
        /// The second float to compare. This is the float produced by the code under test.
        /// </param>
        /// <param name="delta">
        /// The required accuracy. An exception will be thrown only if
        /// <paramref name="actual"/> is different than <paramref name="expected"/>
        /// by more than <paramref name="delta"/>.
        /// </param>
        /// <param name="message">
        /// The message to include in the exception when <paramref name="actual"/>
        /// is different than <paramref name="expected"/> by more than
        /// <paramref name="delta"/>. The message is shown in test results.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="expected"/> is not equal to
        /// <paramref name="actual"/>.
        /// </exception>
        static public void AreEqual(float expected, float actual, float delta, string message)
        {
            Assert.AreEqual(expected, actual, delta, message, null);
        }

        /// <summary>
        /// Tests whether the specified floats are equal and throws an exception
        /// if they are not equal.
        /// </summary>
        /// <param name="expected">
        /// The first float to compare. This is the float the tests expects.
        /// </param>
        /// <param name="actual">
        /// The second float to compare. This is the float produced by the code under test.
        /// </param>
        /// <param name="delta">
        /// The required accuracy. An exception will be thrown only if
        /// <paramref name="actual"/> is different than <paramref name="expected"/>
        /// by more than <paramref name="delta"/>.
        /// </param>
        /// <param name="message">
        /// The message to include in the exception when <paramref name="actual"/>
        /// is different than <paramref name="expected"/> by more than
        /// <paramref name="delta"/>. The message is shown in test results.
        /// </param>
        /// <param name="parameters">
        /// An array of parameters to use when formatting <paramref name="message"/>.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="expected"/> is not equal to
        /// <paramref name="actual"/>.
        /// </exception>
        static public void AreEqual(float expected, float actual, float delta, string message, params object[] parameters)
        {
            if (Math.Abs(expected - actual) > delta)
            {
                string finalMessage = FrameworkMessages.AreEqualDeltaFailMsg(
                    message == null ? String.Empty : ReplaceNulls(message),
                    expected.ToString(CultureInfo.CurrentCulture.NumberFormat),
                    actual.ToString(CultureInfo.CurrentCulture.NumberFormat),
                    delta.ToString(CultureInfo.CurrentCulture.NumberFormat));
                Assert.HandleFail("Assert.AreEqual", finalMessage, parameters);
            }
        }

        /// <summary>
        /// Tests whether the specified floats are unequal and throws an exception
        /// if they are equal.
        /// </summary>
        /// <param name="notExpected">
        /// The first float to compare. This is the float the test expects not to
        /// match <paramref name="actual"/>.
        /// </param>
        /// <param name="actual">
        /// The second float to compare. This is the float produced by the code under test.
        /// </param>
        /// <param name="delta">
        /// The required accuracy. An exception will be thrown only if
        /// <paramref name="actual"/> is different than <paramref name="notExpected"/>
        /// by at most <paramref name="delta"/>.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="notExpected"/> is equal to <paramref name="actual"/>.
        /// </exception>
        static public void AreNotEqual(float notExpected, float actual, float delta)
        {
            Assert.AreNotEqual(notExpected, actual, delta, string.Empty, null);
        }

        /// <summary>
        /// Tests whether the specified floats are unequal and throws an exception
        /// if they are equal.
        /// </summary>
        /// <param name="notExpected">
        /// The first float to compare. This is the float the test expects not to
        /// match <paramref name="actual"/>.
        /// </param>
        /// <param name="actual">
        /// The second float to compare. This is the float produced by the code under test.
        /// </param>
        /// <param name="delta">
        /// The required accuracy. An exception will be thrown only if
        /// <paramref name="actual"/> is different than <paramref name="notExpected"/>
        /// by at most <paramref name="delta"/>.
        /// </param>
        /// <param name="message">
        /// The message to include in the exception when <paramref name="actual"/>
        /// is equal to <paramref name="notExpected"/> or different by less than
        /// <paramref name="delta"/>. The message is shown in test results.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="notExpected"/> is equal to <paramref name="actual"/>.
        /// </exception>
        static public void AreNotEqual(float notExpected, float actual, float delta, string message)
        {
            Assert.AreNotEqual(notExpected, actual, delta, message, null);
        }

        /// <summary>
        /// Tests whether the specified floats are unequal and throws an exception
        /// if they are equal.
        /// </summary>
        /// <param name="notExpected">
        /// The first float to compare. This is the float the test expects not to
        /// match <paramref name="actual"/>.
        /// </param>
        /// <param name="actual">
        /// The second float to compare. This is the float produced by the code under test.
        /// </param>
        /// <param name="delta">
        /// The required accuracy. An exception will be thrown only if
        /// <paramref name="actual"/> is different than <paramref name="notExpected"/>
        /// by at most <paramref name="delta"/>.
        /// </param>
        /// <param name="message">
        /// The message to include in the exception when <paramref name="actual"/>
        /// is equal to <paramref name="notExpected"/> or different by less than
        /// <paramref name="delta"/>. The message is shown in test results.
        /// </param>
        /// <param name="parameters">
        /// An array of parameters to use when formatting <paramref name="message"/>.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="notExpected"/> is equal to <paramref name="actual"/>.
        /// </exception>
        static public void AreNotEqual(float notExpected, float actual, float delta, string message, params object[] parameters)
        {
            if (Math.Abs(notExpected - actual) <= delta)
            {
                string finalMessage = FrameworkMessages.AreNotEqualDeltaFailMsg(
                    message == null ? String.Empty : ReplaceNulls(message),
                    notExpected.ToString(CultureInfo.CurrentCulture.NumberFormat),
                    actual.ToString(CultureInfo.CurrentCulture.NumberFormat),
                    delta.ToString(CultureInfo.CurrentCulture.NumberFormat));
                Assert.HandleFail("Assert.AreNotEqual", finalMessage, parameters);
            }
        }

        /// <summary>
        /// Tests whether the specified doubles are equal and throws an exception
        /// if they are not equal.
        /// </summary>
        /// <param name="expected">
        /// The first double to compare. This is the double the tests expects.
        /// </param>
        /// <param name="actual">
        /// The second double to compare. This is the double produced by the code under test.
        /// </param>
        /// <param name="delta">
        /// The required accuracy. An exception will be thrown only if
        /// <paramref name="actual"/> is different than <paramref name="expected"/>
        /// by more than <paramref name="delta"/>.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="expected"/> is not equal to
        /// <paramref name="actual"/>.
        /// </exception>
        static public void AreEqual(double expected, double actual, double delta)
        {
            Assert.AreEqual(expected, actual, delta, string.Empty, null);
        }

        /// <summary>
        /// Tests whether the specified doubles are equal and throws an exception
        /// if they are not equal.
        /// </summary>
        /// <param name="expected">
        /// The first double to compare. This is the double the tests expects.
        /// </param>
        /// <param name="actual">
        /// The second double to compare. This is the double produced by the code under test.
        /// </param>
        /// <param name="delta">
        /// The required accuracy. An exception will be thrown only if
        /// <paramref name="actual"/> is different than <paramref name="expected"/>
        /// by more than <paramref name="delta"/>.
        /// </param>
        /// <param name="message">
        /// The message to include in the exception when <paramref name="actual"/>
        /// is different than <paramref name="expected"/> by more than
        /// <paramref name="delta"/>. The message is shown in test results.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="expected"/> is not equal to 
        /// <paramref name="actual"/>.
        /// </exception>
        static public void AreEqual(double expected, double actual, double delta, string message)
        {
            Assert.AreEqual(expected, actual, delta, message, null);
        }

        /// <summary>
        /// Tests whether the specified doubles are equal and throws an exception
        /// if they are not equal.
        /// </summary>
        /// <param name="expected">
        /// The first double to compare. This is the double the tests expects.
        /// </param>
        /// <param name="actual">
        /// The second double to compare. This is the double produced by the code under test.
        /// </param>
        /// <param name="delta">
        /// The required accuracy. An exception will be thrown only if
        /// <paramref name="actual"/> is different than <paramref name="expected"/>
        /// by more than <paramref name="delta"/>.
        /// </param>
        /// <param name="message">
        /// The message to include in the exception when <paramref name="actual"/>
        /// is different than <paramref name="expected"/> by more than
        /// <paramref name="delta"/>. The message is shown in test results.
        /// </param>
        /// <param name="parameters">
        /// An array of parameters to use when formatting <paramref name="message"/>.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="expected"/> is not equal to 
        /// <paramref name="actual"/>.
        /// </exception>
        static public void AreEqual(double expected, double actual, double delta, string message, params object[] parameters)
        {
            if (Math.Abs(expected - actual) > delta)
            {
                string finalMessage = FrameworkMessages.AreEqualDeltaFailMsg(
                    message == null ? String.Empty : ReplaceNulls(message),
                    expected.ToString(CultureInfo.CurrentCulture.NumberFormat),
                    actual.ToString(CultureInfo.CurrentCulture.NumberFormat),
                    delta.ToString(CultureInfo.CurrentCulture.NumberFormat));
                Assert.HandleFail("Assert.AreEqual", finalMessage, parameters);
            }
        }

        /// <summary>
        /// Tests whether the specified doubles are unequal and throws an exception
        /// if they are equal.
        /// </summary>
        /// <param name="notExpected">
        /// The first double to compare. This is the double the test expects not to
        /// match <paramref name="actual"/>.
        /// </param>
        /// <param name="actual">
        /// The second double to compare. This is the double produced by the code under test.
        /// </param>
        /// <param name="delta">
        /// The required accuracy. An exception will be thrown only if
        /// <paramref name="actual"/> is different than <paramref name="notExpected"/>
        /// by at most <paramref name="delta"/>.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="notExpected"/> is equal to <paramref name="actual"/>.
        /// </exception>
        static public void AreNotEqual(double notExpected, double actual, double delta)
        {
            Assert.AreNotEqual(notExpected, actual, delta, string.Empty, null);
        }

        /// <summary>
        /// Tests whether the specified doubles are unequal and throws an exception
        /// if they are equal.
        /// </summary>
        /// <param name="notExpected">
        /// The first double to compare. This is the double the test expects not to
        /// match <paramref name="actual"/>.
        /// </param>
        /// <param name="actual">
        /// The second double to compare. This is the double produced by the code under test.
        /// </param>
        /// <param name="delta">
        /// The required accuracy. An exception will be thrown only if
        /// <paramref name="actual"/> is different than <paramref name="notExpected"/>
        /// by at most <paramref name="delta"/>.
        /// </param>
        /// <param name="message">
        /// The message to include in the exception when <paramref name="actual"/>
        /// is equal to <paramref name="notExpected"/> or different by less than
        /// <paramref name="delta"/>. The message is shown in test results.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="notExpected"/> is equal to <paramref name="actual"/>.
        /// </exception>
        static public void AreNotEqual(double notExpected, double actual, double delta, string message)
        {
            Assert.AreNotEqual(notExpected, actual, delta, message, null);
        }

        /// <summary>
        /// Tests whether the specified doubles are unequal and throws an exception
        /// if they are equal.
        /// </summary>
        /// <param name="notExpected">
        /// The first double to compare. This is the double the test expects not to
        /// match <paramref name="actual"/>.
        /// </param>
        /// <param name="actual">
        /// The second double to compare. This is the double produced by the code under test.
        /// </param>
        /// <param name="delta">
        /// The required accuracy. An exception will be thrown only if
        /// <paramref name="actual"/> is different than <paramref name="notExpected"/>
        /// by at most <paramref name="delta"/>.
        /// </param>
        /// <param name="message">
        /// The message to include in the exception when <paramref name="actual"/>
        /// is equal to <paramref name="notExpected"/> or different by less than
        /// <paramref name="delta"/>. The message is shown in test results.
        /// </param>
        /// <param name="parameters">
        /// An array of parameters to use when formatting <paramref name="message"/>.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="notExpected"/> is equal to <paramref name="actual"/>.
        /// </exception>
        static public void AreNotEqual(double notExpected, double actual, double delta, string message, params object[] parameters)
        {
            if (Math.Abs(notExpected - actual) <= delta)
            {
                string finalMessage = FrameworkMessages.AreNotEqualDeltaFailMsg(
                    message == null ? String.Empty : ReplaceNulls(message),
                    notExpected.ToString(CultureInfo.CurrentCulture.NumberFormat),
                    actual.ToString(CultureInfo.CurrentCulture.NumberFormat),
                    delta.ToString(CultureInfo.CurrentCulture.NumberFormat));
                Assert.HandleFail("Assert.AreNotEqual", finalMessage, parameters);
            }
        }

        /// <summary>
        /// Tests whether the specified strings are equal and throws an exception
        /// if they are not equal. The invariant culture is used for the comparison.
        /// </summary>
        /// <param name="expected">
        /// The first string to compare. This is the string the tests expects.
        /// </param>
        /// <param name="actual">
        /// The second string to compare. This is the string produced by the code under test.
        /// </param>
        /// <param name="ignoreCase">
        /// A Boolean indicating a case-sensitive or insensitive comparison. (true
        /// indicates a case-insensitive comparison.)
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="expected"/> is not equal to 
        /// <paramref name="actual"/>.
        /// </exception>
        public static void AreEqual(string expected, string actual, bool ignoreCase)
        {
            Assert.AreEqual(expected, actual, ignoreCase, string.Empty, null);
        }

        /// <summary>
        /// Tests whether the specified strings are equal and throws an exception
        /// if they are not equal. The invariant culture is used for the comparison.
        /// </summary>
        /// <param name="expected">
        /// The first string to compare. This is the string the tests expects.
        /// </param>
        /// <param name="actual">
        /// The second string to compare. This is the string produced by the code under test.
        /// </param>
        /// <param name="ignoreCase">
        /// A Boolean indicating a case-sensitive or insensitive comparison. (true
        /// indicates a case-insensitive comparison.)
        /// </param>
        /// <param name="message">
        /// The message to include in the exception when <paramref name="actual"/>
        /// is not equal to <paramref name="expected"/>. The message is shown in
        /// test results.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="expected"/> is not equal to 
        /// <paramref name="actual"/>.
        /// </exception>
        public static void AreEqual(string expected, string actual, bool ignoreCase, string message)
        {
            Assert.AreEqual(expected, actual, ignoreCase, message, null);
        }

        /// <summary>
        /// Tests whether the specified strings are equal and throws an exception
        /// if they are not equal. The invariant culture is used for the comparison.
        /// </summary>
        /// <param name="expected">
        /// The first string to compare. This is the string the tests expects.
        /// </param>
        /// <param name="actual">
        /// The second string to compare. This is the string produced by the code under test.
        /// </param>
        /// <param name="ignoreCase">
        /// A Boolean indicating a case-sensitive or insensitive comparison. (true
        /// indicates a case-insensitive comparison.)
        /// </param>
        /// <param name="message">
        /// The message to include in the exception when <paramref name="actual"/>
        /// is not equal to <paramref name="expected"/>. The message is shown in
        /// test results.
        /// </param>
        /// <param name="parameters">
        /// An array of parameters to use when formatting <paramref name="message"/>.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="expected"/> is not equal to 
        /// <paramref name="actual"/>.
        /// </exception>
        public static void AreEqual(string expected, string actual, bool ignoreCase, string message, params object[] parameters)
        {
            Assert.AreEqual(expected, actual, ignoreCase, CultureInfo.InvariantCulture, message, parameters);
        }

        /// <summary>
        /// Tests whether the specified strings are equal and throws an exception
        /// if they are not equal.
        /// </summary>
        /// <param name="expected">
        /// The first string to compare. This is the string the tests expects.
        /// </param>
        /// <param name="actual">
        /// The second string to compare. This is the string produced by the code under test.
        /// </param>
        /// <param name="ignoreCase">
        /// A Boolean indicating a case-sensitive or insensitive comparison. (true
        /// indicates a case-insensitive comparison.)
        /// </param>
        /// <param name="culture">
        /// A CultureInfo object that supplies culture-specific comparison information.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="expected"/> is not equal to 
        /// <paramref name="actual"/>.
        /// </exception>
        public static void AreEqual(string expected, string actual, bool ignoreCase, CultureInfo culture)
        {
            Assert.AreEqual(expected, actual, ignoreCase, culture, string.Empty, null);
        }

        /// <summary>
        /// Tests whether the specified strings are equal and throws an exception
        /// if they are not equal.
        /// </summary>
        /// <param name="expected">
        /// The first string to compare. This is the string the tests expects.
        /// </param>
        /// <param name="actual">
        /// The second string to compare. This is the string produced by the code under test.
        /// </param>
        /// <param name="ignoreCase">
        /// A Boolean indicating a case-sensitive or insensitive comparison. (true
        /// indicates a case-insensitive comparison.)
        /// </param>
        /// <param name="culture">
        /// A CultureInfo object that supplies culture-specific comparison information.
        /// </param>
        /// <param name="message">
        /// The message to include in the exception when <paramref name="actual"/>
        /// is not equal to <paramref name="expected"/>. The message is shown in
        /// test results.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="expected"/> is not equal to 
        /// <paramref name="actual"/>.
        /// </exception>
        public static void AreEqual(string expected, string actual, bool ignoreCase, CultureInfo culture, string message)
        {
            Assert.AreEqual(expected, actual, ignoreCase, culture, message, null);
        }

        /// <summary>
        /// Tests whether the specified strings are equal and throws an exception
        /// if they are not equal.
        /// </summary>
        /// <param name="expected">
        /// The first string to compare. This is the string the tests expects.
        /// </param>
        /// <param name="actual">
        /// The second string to compare. This is the string produced by the code under test.
        /// </param>
        /// <param name="ignoreCase">
        /// A Boolean indicating a case-sensitive or insensitive comparison. (true
        /// indicates a case-insensitive comparison.)
        /// </param>
        /// <param name="culture">
        /// A CultureInfo object that supplies culture-specific comparison information.
        /// </param>
        /// <param name="message">
        /// The message to include in the exception when <paramref name="actual"/>
        /// is not equal to <paramref name="expected"/>. The message is shown in
        /// test results.
        /// </param>
        /// <param name="parameters">
        /// An array of parameters to use when formatting <paramref name="message"/>.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="expected"/> is not equal to 
        /// <paramref name="actual"/>.
        /// </exception>
        public static void AreEqual(string expected, string actual, bool ignoreCase, CultureInfo culture, string message, params object[] parameters)
        {
            Assert.CheckParameterNotNull(culture, "Assert.AreEqual", "culture", string.Empty);
            if (0 != string.Compare(expected, actual, ignoreCase, culture))
            {
                string finalMessage;

                // Comparison failed. Check if it was a case-only failure.
                if (!ignoreCase &&
                    0 == string.Compare(expected, actual, true, culture))
                {
                    finalMessage = FrameworkMessages.AreEqualCaseFailMsg(
                        message == null ? String.Empty : ReplaceNulls(message),
                        ReplaceNulls(expected),
                        ReplaceNulls(actual));
                }
                else
                {
                    finalMessage = FrameworkMessages.AreEqualFailMsg(
                        message == null ? String.Empty : ReplaceNulls(message),
                        ReplaceNulls(expected),
                        ReplaceNulls(actual));
                }
                Assert.HandleFail("Assert.AreEqual", finalMessage, parameters);
            }
        }

        /// <summary>
        /// Tests whether the specified strings are unequal and throws an exception
        /// if they are equal. The invariant culture is used for the comparison.
        /// </summary>
        /// <param name="notExpected">
        /// The first string to compare. This is the string the test expects not to
        /// match <paramref name="actual"/>.
        /// </param>
        /// <param name="actual">
        /// The second string to compare. This is the string produced by the code under test.
        /// </param>
        /// <param name="ignoreCase">
        /// A Boolean indicating a case-sensitive or insensitive comparison. (true
        /// indicates a case-insensitive comparison.)
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="notExpected"/> is equal to <paramref name="actual"/>.
        /// </exception>
        public static void AreNotEqual(string notExpected, string actual, bool ignoreCase)
        {
            Assert.AreNotEqual(notExpected, actual, ignoreCase, string.Empty, null);
        }

        /// <summary>
        /// Tests whether the specified strings are unequal and throws an exception
        /// if they are equal. The invariant culture is used for the comparison.
        /// </summary>
        /// <param name="notExpected">
        /// The first string to compare. This is the string the test expects not to
        /// match <paramref name="actual"/>.
        /// </param>
        /// <param name="actual">
        /// The second string to compare. This is the string produced by the code under test.
        /// </param>
        /// <param name="ignoreCase">
        /// A Boolean indicating a case-sensitive or insensitive comparison. (true
        /// indicates a case-insensitive comparison.)
        /// </param>
        /// <param name="message">
        /// The message to include in the exception when <paramref name="actual"/>
        /// is equal to <paramref name="notExpected"/>. The message is shown in
        /// test results.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="notExpected"/> is equal to <paramref name="actual"/>.
        /// </exception>
        public static void AreNotEqual(string notExpected, string actual, bool ignoreCase, string message)
        {
            Assert.AreNotEqual(notExpected, actual, ignoreCase, message, null);
        }

        /// <summary>
        /// Tests whether the specified strings are unequal and throws an exception
        /// if they are equal. The invariant culture is used for the comparison.
        /// </summary>
        /// <param name="notExpected">
        /// The first string to compare. This is the string the test expects not to
        /// match <paramref name="actual"/>.
        /// </param>
        /// <param name="actual">
        /// The second string to compare. This is the string produced by the code under test.
        /// </param>
        /// <param name="ignoreCase">
        /// A Boolean indicating a case-sensitive or insensitive comparison. (true
        /// indicates a case-insensitive comparison.)
        /// </param>
        /// <param name="message">
        /// The message to include in the exception when <paramref name="actual"/>
        /// is equal to <paramref name="notExpected"/>. The message is shown in
        /// test results.
        /// </param>
        /// <param name="parameters">
        /// An array of parameters to use when formatting <paramref name="message"/>.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="notExpected"/> is equal to <paramref name="actual"/>.
        /// </exception>
        public static void AreNotEqual(string notExpected, string actual, bool ignoreCase, string message, params object[] parameters)
        {
            Assert.AreNotEqual(notExpected, actual, ignoreCase, CultureInfo.InvariantCulture, message, parameters);
        }

        /// <summary>
        /// Tests whether the specified strings are unequal and throws an exception
        /// if they are equal.
        /// </summary>
        /// <param name="notExpected">
        /// The first string to compare. This is the string the test expects not to
        /// match <paramref name="actual"/>.
        /// </param>
        /// <param name="actual">
        /// The second string to compare. This is the string produced by the code under test.
        /// </param>
        /// <param name="ignoreCase">
        /// A Boolean indicating a case-sensitive or insensitive comparison. (true
        /// indicates a case-insensitive comparison.)
        /// </param>
        /// <param name="culture">
        /// A CultureInfo object that supplies culture-specific comparison information.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="notExpected"/> is equal to <paramref name="actual"/>.
        /// </exception>
        public static void AreNotEqual(string notExpected, string actual, bool ignoreCase, CultureInfo culture)
        {
            Assert.AreNotEqual(notExpected, actual, ignoreCase, culture, string.Empty, null);
        }

        /// <summary>
        /// Tests whether the specified strings are unequal and throws an exception
        /// if they are equal.
        /// </summary>
        /// <param name="notExpected">
        /// The first string to compare. This is the string the test expects not to
        /// match <paramref name="actual"/>.
        /// </param>
        /// <param name="actual">
        /// The second string to compare. This is the string produced by the code under test.
        /// </param>
        /// <param name="ignoreCase">
        /// A Boolean indicating a case-sensitive or insensitive comparison. (true
        /// indicates a case-insensitive comparison.)
        /// </param>
        /// <param name="culture">
        /// A CultureInfo object that supplies culture-specific comparison information.
        /// </param>
        /// <param name="message">
        /// The message to include in the exception when <paramref name="actual"/>
        /// is equal to <paramref name="notExpected"/>. The message is shown in
        /// test results.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="notExpected"/> is equal to <paramref name="actual"/>.
        /// </exception>
        public static void AreNotEqual(string notExpected, string actual, bool ignoreCase, CultureInfo culture, string message)
        {
            Assert.AreNotEqual(notExpected, actual, ignoreCase, culture, message, null);
        }

        /// <summary>
        /// Tests whether the specified strings are unequal and throws an exception
        /// if they are equal.
        /// </summary>
        /// <param name="notExpected">
        /// The first string to compare. This is the string the test expects not to
        /// match <paramref name="actual"/>.
        /// </param>
        /// <param name="actual">
        /// The second string to compare. This is the string produced by the code under test.
        /// </param>
        /// <param name="ignoreCase">
        /// A Boolean indicating a case-sensitive or insensitive comparison. (true
        /// indicates a case-insensitive comparison.)
        /// </param>
        /// <param name="culture">
        /// A CultureInfo object that supplies culture-specific comparison information.
        /// </param>
        /// <param name="message">
        /// The message to include in the exception when <paramref name="actual"/>
        /// is equal to <paramref name="notExpected"/>. The message is shown in
        /// test results.
        /// </param>
        /// <param name="parameters">
        /// An array of parameters to use when formatting <paramref name="message"/>.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="notExpected"/> is equal to <paramref name="actual"/>.
        /// </exception>
        public static void AreNotEqual(string notExpected, string actual, bool ignoreCase, CultureInfo culture, string message, params object[] parameters)
        {
            Assert.CheckParameterNotNull(culture, "Assert.AreNotEqual", "culture", string.Empty);
            if (0 == string.Compare(notExpected, actual, ignoreCase, culture))
            {
                string finalMessage = FrameworkMessages.AreNotEqualFailMsg(
                    message == null ? String.Empty : ReplaceNulls(message),
                    ReplaceNulls(notExpected),
                    ReplaceNulls(actual));
                Assert.HandleFail("Assert.AreNotEqual", finalMessage, parameters);
            }
        }
      
        #endregion
        
        #region Type

        /// <summary>
        /// Tests whether the specified object is an instance of the expected
        /// type and throws an exception if the expected type is not in the
        /// inheritance hierarchy of the object.
        /// </summary>
        /// <param name="value">
        /// The object the test expects to be of the specified type.
        /// </param>
        /// <param name="expectedType">
        /// The expected type of <paramref name="value"/>.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="value"/> is null or
        /// <paramref name="expectedType"/> is not in the inheritance hierarchy
        /// of <paramref name="value"/>.
        /// </exception>
        static public void IsInstanceOfType(object value, Type expectedType)
        {
            Assert.IsInstanceOfType(value, expectedType, string.Empty, null);
        }

        /// <summary>
        /// Tests whether the specified object is an instance of the expected
        /// type and throws an exception if the expected type is not in the
        /// inheritance hierarchy of the object.
        /// </summary>
        /// <param name="value">
        /// The object the test expects to be of the specified type.
        /// </param>
        /// <param name="expectedType">
        /// The expected type of <paramref name="value"/>.
        /// </param>
        /// <param name="message">
        /// The message to include in the exception when <paramref name="value"/>
        /// is not an instance of <paramref name="expectedType"/>. The message is
        /// shown in test results.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="value"/> is null or
        /// <paramref name="expectedType"/> is not in the inheritance hierarchy
        /// of <paramref name="value"/>.
        /// </exception>
        static public void IsInstanceOfType(object value, Type expectedType, string message)
        {
            Assert.IsInstanceOfType(value, expectedType, message, null);
        }

        /// <summary>
        /// Tests whether the specified object is an instance of the expected
        /// type and throws an exception if the expected type is not in the
        /// inheritance hierarchy of the object.
        /// </summary>
        /// <param name="value">
        /// The object the test expects to be of the specified type.
        /// </param>
        /// <param name="expectedType">
        /// The expected type of <paramref name="value"/>.
        /// </param>
        /// <param name="message">
        /// The message to include in the exception when <paramref name="value"/>
        /// is not an instance of <paramref name="expectedType"/>. The message is
        /// shown in test results.
        /// </param>
        /// <param name="parameters">
        /// An array of parameters to use when formatting <paramref name="message"/>.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="value"/> is null or
        /// <paramref name="expectedType"/> is not in the inheritance hierarchy
        /// of <paramref name="value"/>.
        /// </exception>
        static public void IsInstanceOfType(object value, Type expectedType, string message, params object[] parameters)
        {
            if (expectedType == null)
            {
                Assert.HandleFail("Assert.IsInstanceOfType", message, parameters);
            }

            if (!expectedType.IsInstanceOfType(value))
            {
                string finalMessage = FrameworkMessages.IsInstanceOfFailMsg(
                    message == null ? String.Empty : ReplaceNulls(message),
                    expectedType.ToString(),
                    value == null ? FrameworkMessages.Common_NullInMessages : value.GetType().ToString());
                Assert.HandleFail("Assert.IsInstanceOfType", finalMessage, parameters);
            }
        }

        /// <summary>
        /// Tests whether the specified object is not an instance of the wrong
        /// type and throws an exception if the specified type is in the
        /// inheritance hierarchy of the object.
        /// </summary>
        /// <param name="value">
        /// The object the test expects not to be of the specified type.
        /// </param>
        /// <param name="wrongType">
        /// The type that <paramref name="value"/> should not be.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="value"/> is not null and
        /// <paramref name="wrongType"/> is in the inheritance hierarchy
        /// of <paramref name="value"/>.
        /// </exception>
        static public void IsNotInstanceOfType(object value, Type wrongType)
        {
            Assert.IsNotInstanceOfType(value, wrongType, string.Empty, null);
        }

        /// <summary>
        /// Tests whether the specified object is not an instance of the wrong
        /// type and throws an exception if the specified type is in the
        /// inheritance hierarchy of the object.
        /// </summary>
        /// <param name="value">
        /// The object the test expects not to be of the specified type.
        /// </param>
        /// <param name="wrongType">
        /// The type that <paramref name="value"/> should not be.
        /// </param>
        /// <param name="message">
        /// The message to include in the exception when <paramref name="value"/>
        /// is an instance of <paramref name="wrongType"/>. The message is shown
        /// in test results.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="value"/> is not null and
        /// <paramref name="wrongType"/> is in the inheritance hierarchy
        /// of <paramref name="value"/>.
        /// </exception>
        static public void IsNotInstanceOfType(object value, Type wrongType, string message)
        {
            Assert.IsNotInstanceOfType(value, wrongType, message, null);
        }

        /// <summary>
        /// Tests whether the specified object is not an instance of the wrong
        /// type and throws an exception if the specified type is in the
        /// inheritance hierarchy of the object.
        /// </summary>
        /// <param name="value">
        /// The object the test expects not to be of the specified type.
        /// </param>
        /// <param name="wrongType">
        /// The type that <paramref name="value"/> should not be.
        /// </param>
        /// <param name="message">
        /// The message to include in the exception when <paramref name="value"/>
        /// is an instance of <paramref name="wrongType"/>. The message is shown
        /// in test results.
        /// </param>
        /// <param name="parameters">
        /// An array of parameters to use when formatting <paramref name="message"/>.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="value"/> is not null and
        /// <paramref name="wrongType"/> is in the inheritance hierarchy
        /// of <paramref name="value"/>.
        /// </exception>
        static public void IsNotInstanceOfType(object value, Type wrongType, string message, params object[] parameters)
        {
            if (wrongType == null)
            {
                Assert.HandleFail("Assert.IsNotInstanceOfType", message, parameters);
            }

            if (value != null && wrongType.IsInstanceOfType(value))
            {
                string finalMessage = FrameworkMessages.IsNotInstanceOfFailMsg(
                    message == null ? String.Empty : ReplaceNulls(message),
                    wrongType.ToString(),
                    value.GetType().ToString());
                Assert.HandleFail("Assert.IsNotInstanceOfType", finalMessage, parameters);
            }
        }

        #endregion

        #region Fail

        /// <summary>
        /// Throws an AssertFailedException.
        /// </summary>
        /// <exception cref="AssertFailedException">
        /// Always thrown.
        /// </exception>
        static public void Fail()
        {
            Assert.Fail(string.Empty, null);
        }

        /// <summary>
        /// Throws an AssertFailedException.
        /// </summary>
        /// <param name="message">
        /// The message to include in the exception. The message is shown in
        /// test results.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Always thrown.
        /// </exception>
        static public void Fail(string message)
        {
            Assert.Fail(message, null);
        }

        /// <summary>
        /// Throws an AssertFailedException.
        /// </summary>
        /// <param name="message">
        /// The message to include in the exception. The message is shown in
        /// test results.
        /// </param>
        /// <param name="parameters">
        /// An array of parameters to use when formatting <paramref name="message"/>.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Always thrown.
        /// </exception>
        static public void Fail(string message, params object[] parameters)
        {
            Assert.HandleFail("Assert.Fail", message, parameters);
        }

        #endregion

        #region Inconclusive

        /// <summary>
        /// Throws an AssertInconclusiveException.
        /// </summary>
        /// <exception cref="AssertInconclusiveException">
        /// Always thrown.
        /// </exception>
        static public void Inconclusive()
        {
            Assert.Inconclusive(string.Empty, null);
        }

        /// <summary>
        /// Throws an AssertInconclusiveException.
        /// </summary>
        /// <param name="message">
        /// The message to include in the exception. The message is shown in
        /// test results.
        /// </param>
        /// <exception cref="AssertInconclusiveException">
        /// Always thrown.
        /// </exception>
        static public void Inconclusive(string message)
        {
            Assert.Inconclusive(message, null);
        }

        /// <summary>
        /// Throws an AssertInconclusiveException.
        /// </summary>
        /// <param name="message">
        /// The message to include in the exception. The message is shown in
        /// test results.
        /// </param>
        /// <param name="parameters">
        /// An array of parameters to use when formatting <paramref name="message"/>.
        /// </param>
        /// <exception cref="AssertInconclusiveException">
        /// Always thrown.
        /// </exception>
        static public void Inconclusive(string message, params object[] parameters)
        {
            string finalMessage = string.Empty;
            if (!string.IsNullOrEmpty(message))
            {
                if (parameters == null)
                {
                    finalMessage = ReplaceNulls(message);
                }
                else
                {
                    finalMessage = string.Format(CultureInfo.CurrentCulture, ReplaceNulls(message), parameters);
                }
            }
            throw new AssertInconclusiveException(FrameworkMessages.AssertionFailed("Assert.Inconclusive", finalMessage));
        }

        #endregion

        #region Equals Assertion
        /// <summary>
        /// Static equals overloads are used for comparing instances of two types for reference
        /// equality. This method should <b>not</b> be used for comparison of two instances for
        /// equality. This object will <b>always</b> throw with Assert.Fail. Please use
        /// Assert.AreEqual and associated overloads in your unit tests.
        /// </summary>
        /// <param name="objA"></param>
        /// <param name="objB"></param>
        /// <returns>False, always.</returns>
        static public new bool Equals(object objA, object objB)
        {
            Assert.Fail(FrameworkMessages.DoNotUseAssertEquals);
            return false;
        }
        #endregion Equals Assertion


        #region Helpers

        /// <summary>
        /// Helper function that creates and throws an AssertionFailedException
        /// </summary>
        /// <param name="assertionName">name of the assertion throwing an exception</param>
        /// <param name="message">message describing conditions for assertion failure</param>
        static internal void HandleFail(string assertionName, string message, params object[] parameters)
        {
            string finalMessage = string.Empty;
            if (!string.IsNullOrEmpty(message))
            {
                if (parameters == null)
                {
                    finalMessage = ReplaceNulls(message);
                }
                else
                {
                    finalMessage = string.Format(CultureInfo.CurrentCulture, ReplaceNulls(message), parameters);
                }
            }
			throw new AssertFailedException(FrameworkMessages.AssertionFailed(assertionName, finalMessage));
        }

        /// <summary>
        /// Checks the parameter for valid conditions
        /// </summary>
        /// <param name="condition">condition</param>
        /// <param name="parameterName">parameter name</param>
        /// <param name="message">message for the invalid parameter exception</param>
        static internal void CheckParameterNotNull(object param, string assertionName, string parameterName, string message, params object[] parameters)
        {
            if (param == null)
            {
                Assert.HandleFail(assertionName, FrameworkMessages.NullParameterToAssert(parameterName, message), parameters);
            }
        }

        // ReplaceNulls and ReplaceNullChars are duplicated from Common.StringHelper because
        // UnitTestFramework should not depend on Common.

        /// <summary>
        /// Safely converts an object to a string, handling null values and null characters.
        /// Null values are converted to "(null)". Null characters are converted to "\\0".
        /// </summary>
        /// <param name="input">The object to convert to a string.</param>
        /// <returns>The converted string.</returns>
        static internal string ReplaceNulls(object input)
        {
            // Use the localized "(null)" string for null values.
            if (null == input)
            {
                return FrameworkMessages.Common_NullInMessages.ToString();
            }
            else
            {
                // Convert it to a string.
                string inputString = input.ToString();

                // Make sure the class didn't override ToString and return null.
                if (inputString == null)
                {
                    return FrameworkMessages.Common_ObjectString.ToString();
                }

                return ReplaceNullChars(inputString);
            }
        }

        /// <summary>
        /// Replaces null characters ('\0') with "\\0".
        /// </summary>
        /// <param name="input">The string to search.</param>
        /// <returns>The converted string with null characters replaced by "\\0".</returns>
        public static string ReplaceNullChars(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            // Check for \0 in the middle of the display string.
            // Unfortunately we cannot use String.Replace or a regular expression
            // because both of those functions stop when they see a \0.

            // Count the zeros.
            List<int> zeroPos = new List<int>();
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == '\0')
                {
                    zeroPos.Add(i);
                }
            }

            if (zeroPos.Count > 0)
            {
                StringBuilder sb = new StringBuilder(input.Length + zeroPos.Count);

                // For each zero, add the string from the previous zero up to this one,
                // then add "\\0".
                int start = 0;
                foreach (int index in zeroPos)
                {
                    sb.Append(input.Substring(start, index - start));
                    sb.Append("\\0");
                    start = index + 1;
                }

                // Add the remainder of the string after the last zero
                sb.Append(input.Substring(start));
                return sb.ToString();
            }
            else
            {
                return input;
            }
        }

        #endregion
    }

    internal static class Helper
    {
        static internal void CheckParameterNotNull(object param, string parameterName, string message)
        {
            if (param == null)
            {
                throw new ArgumentNullException(parameterName, message);
            }
        }
        static internal void CheckParameterNotNullOrEmpty(string param, string parameterName, string message)
        {
            if (string.IsNullOrEmpty(param))
            {
                throw new ArgumentException(parameterName, message);
            }
        }

        // Don't use typeof - don't take a version dependancy on
        // the Adapter!
        private const string TestAdapterHelperName = @"Microsoft.VisualStudio.TestTools.TestTypes.Unit.UTAHelper,Microsoft.VisualStudio.QualityTools.Tips.UnitTest.Adapter";
        private const string ExceptionHelperMethod = @"AddStackTraceShadow";

        /// <summary>
        /// An exception from reflection will always be a TargetInvocationException - however
        /// the goal of Private Accessors is to be seemless to the original code.
        /// The only problem with throwing the inner exception is that the stack trace will
        /// be overwritten.  From here we register the stack trace of the inner exception
        /// and then throw it.  The Unit Test Adapter will then later rebuild the stack
        /// from the cached shadow information plus the remaining stack from this throw.
        /// </summary>
        /// <param name="outer"></param>
        [SuppressMessage("Microsoft.Globalization", "CA1304:SpecifyCultureInfo")]
        internal static void ThrowInnerException(TargetInvocationException outer)
        {
            Debug.Assert(outer.InnerException != null);
            Type testAdapter = Type.GetType(TestAdapterHelperName, false);
            if (testAdapter != null)
            {
                BindingFlags flags = BindingFlags.InvokeMethod |
                                     BindingFlags.NonPublic |
                                     BindingFlags.Public |
                                     BindingFlags.Static;

                object[] args = new object[1] { outer.InnerException };
                try
                {
                    testAdapter.InvokeMember(ExceptionHelperMethod, flags, null, null, args); // culture should not be interesting here
                }
                catch (ApplicationException e)
                {
                    Debug.Fail(e.Message);
                }
            }
            throw outer.InnerException ?? outer;
        }
    }
}
