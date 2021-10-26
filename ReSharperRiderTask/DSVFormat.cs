using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace ReSharperRiderTask
{
    /// <summary>
    /// This class represents a DSV file's format as per the problem statement.
    /// </summary>
    public class DSVFormat : IEquatable<DSVFormat>
    {
        public static readonly char[] s_Decimals = { '.', ',' };
        public static readonly char[] s_Thousands = { '.', ',', ' ' };
        public static readonly char[] s_Delimiters = { ',', '\t', ';' };

        public static Dictionary<Regex, char> s_DecRegex = new Dictionary<Regex, char>()
        {
            {
                new Regex(@"\d{1,3}\.\d*$",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase),
                s_Decimals[0] //"."
            },
            {
                new Regex(@"\d{1,3},\d*$",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase),
                s_Decimals[1] //","
            }
        };

        public static Dictionary<Regex, char> s_ThousandsRegex = new Dictionary<Regex, char>()
        {
            {
                new Regex(@"^\d{1,3}(\.\d{3})+",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase),
                s_Thousands[0] //"."
            },
            {
                new Regex(@"^\d{1,3}(,\d{3})+",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase),
                s_Thousands[1] //","
            },
            {
                new Regex(@"^\d{1,3}( \d{3})+",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase),
                s_Thousands[2] //" "
            },
        };

        public char Delimiter { get; set; }
        public char ThousandsSeparator { get; set; }
        public char DecimalSeparator { get; set; }

        public DSVDateFormat.DateOrder DateFormat = DSVDateFormat.DateOrder.None;

        public bool Equals([AllowNull] DSVFormat other)
        {
            return other.Delimiter == Delimiter
                && other.ThousandsSeparator == ThousandsSeparator
                && other.DecimalSeparator == DecimalSeparator
                && other.DateFormat == DateFormat;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Delimiter, ThousandsSeparator, DecimalSeparator, DateFormat);
        }

        public override string ToString()
        {
            return String.Format("Delimiter: \'{0}\'\tThousands Separator: \"{1}\"\tDecimal Separator: \'{2}\'\tDateFormat: {3}", Delimiter, ThousandsSeparator, DecimalSeparator, DateFormat.ToString());
        }
    }
}

