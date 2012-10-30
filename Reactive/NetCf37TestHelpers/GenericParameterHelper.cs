//*****************************************************************************
// GenericParameterHelper.cs
// Owner: mkolt
//
// Types used by Unit Test Framework, Unit Test Object Model, etc
//
// Copyright(c) Microsoft Corporation, 2004
//*****************************************************************************

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Microsoft.VisualStudio.TestTools.UnitTesting
{    
    /// <summary>
    /// This class is designed to help user doing unit testing.
    /// GenericParameterHelper satisfies some comment generic type constraints
    /// such as:
    /// 1. public default constructor
    /// 2. implements common interface: IComparable, IEnumerable, ICloneable
    /// 
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
    [SuppressMessage("Microsoft.Design", "CA1036:OverrideMethodsOnComparableTypes")] // This next suppression could mask a problem, since Equals and CompareTo may not agree!
    public class GenericParameterHelper : IComparable, IEnumerable, ICloneable
    {
        #region Private Fields
        private int m_data;
        private IList m_ienumerableStore;

        #endregion

        #region Constructors
        /// <summary>
        /// public default constructor, satisfies the 'newable' constraint in C# generics.
        /// This constructor initializes the Data property to a random value.
        /// </summary>
        public GenericParameterHelper()
        {
            Random randomizer = new Random();
            this.Data = randomizer.Next();
        }

        /// <summary>
        /// This constructor initializes the Data proeprty to a user-supplied value
        /// </summary>
        /// <param name="data"></param>
        public GenericParameterHelper(int data)
        {
            this.Data = data;
        }
        #endregion

        #region Public Properties
        public int Data
        {
            get { return m_data; }
            set { m_data = value; }
        }
        #endregion

        #region Object Overrides
        /// <summary>
        /// Do the value comparison for two GenericParameterHelper object
        /// </summary>
        /// <param name="obj">object to do comparison with</param>
        /// <returns>true if obj has the same value as 'this' GenericParameterHelper object.
        /// false otherwise.</returns>
        public override bool Equals(object obj)
        {
            GenericParameterHelper other = obj as GenericParameterHelper;
            if (other == null) return false;

            return this.Data == other.Data;
        }

        /// <summary>
        /// Returns a hashcode for this object.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return this.Data.GetHashCode();
        }
        #endregion

        #region IComparable Members

        public int CompareTo(object obj)
        {
            GenericParameterHelper gpf = obj as GenericParameterHelper;
            if (gpf != null)
            {
                return this.Data.CompareTo(gpf.Data);
            }
            throw new NotSupportedException("GenericParameterHelper object is designed to compare to objects of GenericParameterHelper type only.");
        }

        #endregion

        #region IEnumerable Members
        /// <summary>
        /// Returns an IEnumerator object whose length is derived from
        /// the Data property.
        /// </summary>
        /// <returns></returns>
        public IEnumerator GetEnumerator()
        {
            int size = this.Data % 10;
            if (m_ienumerableStore == null)
            {
                m_ienumerableStore = new List<Object>(size);

                for (int i = 0; i < size; i++)
                {
                    m_ienumerableStore.Add(new Object());
                }
            } return m_ienumerableStore.GetEnumerator();
        }

        #endregion

        #region ICloneable Members
        /// <summary>
        /// Returns a GenericParameterHelper object that is equal to 
        /// 'this' one.
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            GenericParameterHelper clone = new GenericParameterHelper();
            clone.m_data = this.m_data;
            return clone;
        }

        #endregion
    }

    
}
