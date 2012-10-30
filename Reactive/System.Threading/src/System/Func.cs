using System;

namespace System
{
    /// <summary>
    /// Encapsulates a method that has five parameters and returns a value of the type specified by the TResult parameter.
    /// </summary>
    public delegate TResult Func_<T1, T2, T3, T4, T5, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);
}
