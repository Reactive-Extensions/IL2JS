﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReactiveTests.Dummies
{
    static class DummyFunc<T>
    {
        public static readonly Func<T> Instance = () => { throw new NotImplementedException(); };
    }

    static class DummyFunc<T, U>
    {
        public static readonly Func<T, U> Instance = t => { throw new NotImplementedException(); };
    }

    static class DummyFunc<T, U, V>
    {
        public static readonly Func<T, U, V> Instance = (t, u) => { throw new NotImplementedException(); };
    }

    static class DummyAction
    {
        public static readonly Action Instance = () => { throw new NotImplementedException(); };
    }

    static class DummyAction<T>
    {
        public static readonly Action<T> Instance = t => { throw new NotImplementedException(); };
    }

    static class DummyAction<T, U>
    {
        public static readonly Action<T, U> Instance = (t, u) => { throw new NotImplementedException(); };
    }
}
