using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace 
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive
{
    /// <summary>
    /// Represents void.
    /// </summary>
    public struct Unit : IEquatable<Unit>
    {
        /// <summary>
        /// Always returns true.
        /// </summary>
        public bool Equals(Unit other)
        {
            return true;
        }

        /// <summary>
        /// Check equality between a unit value and other objects.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is Unit;
        }

        /// <summary>
        /// Gets the unit value's hash code.
        /// </summary>
        public override int GetHashCode()
        {
            return 0;
        }

        /// <summary>
        /// Always returns true.
        /// </summary>
        public static bool operator ==(Unit unit1, Unit unit2)
        {
            return true;
        }

        /// <summary>
        /// Always returns false.
        /// </summary>
        public static bool operator !=(Unit unit1, Unit unit2)
        {
            return false;
        }
    }
}
