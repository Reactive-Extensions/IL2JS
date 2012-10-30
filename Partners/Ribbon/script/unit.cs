using System;
using Microsoft.LiveLabs.Html;

namespace Ribbon
{
    /// <summary>
    /// A class representing a unit for use within the Spinner control
    /// For example, inches, pixels, percent, etc.
    /// Takes a delimited string as a parameter and parses it into appropriate variables
    /// name - the long name of the unit
    /// abbreviations - an array of acceptable abbreviations for this unit
    /// min - the minimum acceptable value
    /// max - the maximum acceptable value
    /// decimaldigits - how many decimal digits should be shown/allowed
    /// step - the amount by which the value should be incremented/decremented on arrow button press
    /// </summary>
    internal class Unit
    {
        string _name;
        string[] _abbreviations;
        double _min;
        double _max;
        int _decimaldigits;
        double _step;

        // Validation result codes
        internal const int VALID = 0;
        internal const int INVALID = -1;
        internal const int NEEDSROUNDING = 1;
        internal const int BELOWMIN = 2;
        internal const int ABOVEMAX = 3;

        internal Unit(string name, string[] abbreviations, double min, double max, int decimaldigits, double step)
        {
            _name = name;
            _abbreviations = abbreviations;
            _min = min;
            _max = max;
            _decimaldigits = decimaldigits;
            _step = step;
        }

        internal bool ContainsAbbreviation(string abbrv)
        {
            if (string.IsNullOrEmpty(abbrv))
                return false;

            for (int i = 0; i < _abbreviations.Length; i++)
            {
                if (_abbreviations[i].ToLower() == abbrv.ToLower())
                    return true;
            }
            return false;
        }

        internal int ValidateNumber(double val)
        {
            if (val < Min)
                return BELOWMIN;
            if (val > Max)
                return ABOVEMAX;
            string valStr = val.ToString();
            int decimalLoc = valStr.IndexOf(".");
            if (decimalLoc > -1)
            {
                string decimalStr = valStr.Substring(decimalLoc + 1);
                if (decimalStr.Length > _decimaldigits)
                    return NEEDSROUNDING;
            }
            return VALID;
        }

        internal string Name
        {
            get
            {
                return _name;
            }
        }

        internal int DecimalDigits
        {
            get
            {
                return _decimaldigits;
            }
        }

        internal string DefaultAbbreviation
        {
            get
            {
                return _abbreviations[0];
            }
        }

        internal double Step
        {
            get
            {
                return _step;
            }
        }

        internal double Max
        {
            get
            {
                return _max;
            }
        }

        internal double Min
        {
            get
            {
                return _min;
            }
        }
    }
}
