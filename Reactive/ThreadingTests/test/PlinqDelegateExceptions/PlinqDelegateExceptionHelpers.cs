// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
//
// <OWNER>mikelid</OWNER>
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace plinq_devtests.PLinqDelegateExceptions
{
    /// <summary>
    /// Exception for simulating exceptions from user delegates.
    /// </summary>
    public class UserDelegateException : Exception
    {
        public static TOut Throw<TIn, TOut>(TIn input)
        {
            throw new UserDelegateException();
        }

        public static TOut Throw<TIn1, TIn2, TOut>(TIn1 input1, TIn2 input2)
        {
            throw new UserDelegateException();
        }

        public static void ThrowIf(bool predicate)
        {
            if (predicate)
                throw new UserDelegateException();
        }
    }

    /// <summary>
    /// Helper methods for writing tests about user-delegate exception handling.
    /// </summary>
    public static class PlinqDelegateExceptionHelpers
    {
        public static bool AggregateExceptionContains(AggregateException aggregateException, Type exceptionType)
        {
            foreach (Exception innerException in aggregateException.InnerExceptions)
            {
                if (innerException.GetType() == exceptionType)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
