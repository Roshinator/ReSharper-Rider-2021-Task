using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;
using System.Collections;

namespace ReSharperRiderTask
{
    /// <summary>
    /// This class parses the format and structure of the file
    /// </summary>
    public partial class DSVFile
    {
        public DSVStructure Structure { get; private set; }

        public DSVFormat Format { get; private set; }

        public DSVFile(in string path)
        {
            Format = new DSVFormat();
            StreamReader inputFile = new StreamReader(path);

            string header = inputFile.ReadLine();
            Format.Delimiter = FindDelimiter(header);

            IEnumerable<string> headerItems = SplitByDelimiter(header, Format.Delimiter);

            string line2 = inputFile.ReadLine();
            Structure = new DSVStructure(headerItems, SplitByDelimiter(line2, Format.Delimiter));

            // We have now determined the structure of the file
            // Analyze each line using the process of elimination on each data type
            HashSet<char> possibleDecimals = new HashSet<char>(DSVFormat.s_Decimals);
            HashSet<char> possibleThousands = new HashSet<char>(DSVFormat.s_Thousands);
            HashSet<DSVDateFormat.DateOrder> possibleDateOrders = new HashSet<DSVDateFormat.DateOrder>(DSVDateFormat.s_DateRegex.Values);
            // Run line by line until EOF or all data types are known
            for (string line = line2;
                line != null && (Format.DecimalSeparator == default || Format.ThousandsSeparator == default || Format.DateFormat == DSVDateFormat.DateOrder.None);
                line = inputFile.ReadLine())
            {
                // Run through each section separated by delimiter
                int column = 0;
                foreach (string item in SplitByDelimiter(line, Format.Delimiter))
                {
                    DSVStructure.CellType cType = Structure.GetTypeAtColumn(column);

                    // If we are analyzing a number, run the process of elimination on it
                    if (cType == DSVStructure.CellType.Number && (Format.ThousandsSeparator == default || Format.DecimalSeparator == default))
                    {
                        HashSet<char> itemPossibleDecimals = GetPossibleDecimalTypes(item);
                        HashSet<char> itemPossibleThousands = DetermineThousandsType(item);
                        //Process decimals here
                        possibleDecimals.IntersectWith(itemPossibleDecimals);
                        possibleThousands.IntersectWith(itemPossibleThousands);
                        if (possibleDecimals.Count == 1)
                        {
                            Format.DecimalSeparator = GetFirst<char>(possibleDecimals);
                        }
                        if (possibleThousands.Count == 1)
                        {
                            Format.ThousandsSeparator = GetFirst<char>(possibleThousands);
                        }
                    }
                    // If we are analyzing a date, run process of elimination
                    else if (cType == DSVStructure.CellType.Date)
                    {
                        HashSet<DSVDateFormat.DateOrder> itemPossibleDateOrders = DetermineDateType(item);
                        possibleDateOrders.IntersectWith(itemPossibleDateOrders);
                        if (possibleDateOrders.Count == 1)
                        {
                            Format.DateFormat = GetFirst<DSVDateFormat.DateOrder>(possibleDateOrders);
                        }
                    }

                    column++;
                }
            }

            // If we failed to perfectly eliminate everything, set defaults
            if (Format.DecimalSeparator == default)
            {
                Format.DecimalSeparator = GetFirst<char>(possibleDecimals);
            }
            if (Format.ThousandsSeparator == default)
            {
                Format.ThousandsSeparator = GetFirst<char>(possibleThousands);
            }
            if (Format.DateFormat == DSVDateFormat.DateOrder.None)
            {
                Format.DateFormat = GetFirst<DSVDateFormat.DateOrder>(possibleDateOrders);
            }
        }

        /// <summary>
        /// Splits a string by delimiter and accounts for quotes in cells
        /// </summary>
        /// <param name="str">String to parse</param>
        /// <param name="delimiter">Delimiter character</param>
        /// <returns>Returns a collection of strings that have been separated by the delimiter with quotes removed</returns>
        public static IEnumerable<string> SplitByDelimiter(string str, char delimiter)
        {
            bool inQuote = false;
            int beginning = 0;
            int length = 0;
            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];
                if (c == '\"')
                {
                    inQuote = !inQuote;
                    if (inQuote)
                    {
                        beginning = i + 1; // Leave out the quote if we entered a quote
                    }
                }
                else if ((c == delimiter || c == '\n') && !inQuote)
                {
                    string output = str.Substring(beginning, length);
                    beginning = i + 1;
                    length = 0;
                    if (output.Length == 0 && beginning + length == str.Length) // If we find a 0 length string at the end, we can ignore it
                    {
                        yield break;
                    }
                    yield return output;
                }
                else
                {
                    length++;
                }
            }
            if (length > 0)
            {
                string output = str.Substring(beginning, length);
                yield return output;
            }
            yield break;
        }

        /// <summary>
        /// Finds the delimiter in a string
        /// </summary>
        /// <param name="s">String to parse the delimiter from</param>
        /// <returns>The delimiter character</returns>
        private char FindDelimiter(in string s)
        {
            bool withinQuote = false; // Used to ignore delimiters within quotes
            foreach (char c in s)
            {
                if (c == '\"')
                {
                    withinQuote = !withinQuote;
                }
                else if (Array.IndexOf(DSVFormat.s_Delimiters, c) != -1)
                {
                    if (!withinQuote)
                        return c;
                }
            }
            return DSVFormat.s_Delimiters[0]; // Only one item per row, so use a default
        }

        /// <summary>
        /// Gets the possible decimal point types for a number string
        /// </summary>
        /// <param name="num">The number input</param>
        /// <returns>A set of possible decimal types (all if none could be eliminated)</returns>
        private HashSet<char> GetPossibleDecimalTypes(string num)
        {
            HashSet<char> possibleDecimals = new HashSet<char>(DSVFormat.s_Decimals);
            foreach (Regex key in DSVFormat.s_DecRegex.Keys)
            {
                if (!key.IsMatch(num))
                {
                    possibleDecimals.Remove(DSVFormat.s_DecRegex[key]);
                }
            }
            if (possibleDecimals.Count == 0) // If all failed, all options are possibilities
            {
                return new HashSet<char>(DSVFormat.s_Decimals);
            }
            return possibleDecimals;
        }

        /// <summary>
        /// Gets the possible thousand point types for a number string
        /// </summary>
        /// <param name="num">The number input</param>
        /// <returns>A set of possible thousand types (all if none could be eliminated)</returns>
        private HashSet<char> DetermineThousandsType(string num)
        {
            HashSet<char> possibleThousands = new HashSet<char>(DSVFormat.s_Thousands);
            foreach (Regex key in DSVFormat.s_ThousandsRegex.Keys)
            {
                if (!key.IsMatch(num))
                {
                    possibleThousands.Remove(DSVFormat.s_ThousandsRegex[key]);
                }
            }
            if (possibleThousands.Count == 0) // If all failed, all options are possibilities
            {
                return new HashSet<char>(DSVFormat.s_Thousands);
            }
            return possibleThousands;
        }

        /// <summary>
        /// Gets the possible date format types for a number string
        /// </summary>
        /// <param name="num">The number input</param>
        /// <returns>A set of possible decimal types (all if none could be eliminated)</returns>
        private HashSet<DSVDateFormat.DateOrder> DetermineDateType(string date)
        {
            HashSet<DSVDateFormat.DateOrder> possibleDateOrders = new HashSet<DSVDateFormat.DateOrder>(DSVDateFormat.s_DateRegex.Values);
            foreach (Regex key in DSVDateFormat.s_DateRegex.Keys)
            {
                if (!key.IsMatch(date))
                {
                    possibleDateOrders.Remove(DSVDateFormat.s_DateRegex[key]);
                }
            }
            if (possibleDateOrders.Count == 0) // If all failed, all options are possibilities
            {
                return new HashSet<DSVDateFormat.DateOrder>(DSVDateFormat.s_DateRegex.Values);
            }
            return possibleDateOrders;
        }

        /// <summary>
        /// Gets the first element in a set according to its enumerator. Used to get the only item from a count = 1 unordered set.
        /// </summary>
        /// <typeparam name="T">Type in the collection</typeparam>
        /// <param name="collection">The collection to get the first item from</param>
        /// <returns>The first item in the collection according to its iterator</returns>
        private T GetFirst<T>(ISet<T> collection)
        {
            IEnumerator<T> enumerator = collection.GetEnumerator();
            enumerator.MoveNext();
            return enumerator.Current;
        }

    }
}