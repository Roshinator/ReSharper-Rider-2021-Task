using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;
using System.Collections;

namespace ReSharperRiderTask
{
    public class DSVFile
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

            HashSet<char> possibleDecimals = new HashSet<char>(DSVFormat.s_Decimals);
            HashSet<char> possibleThousands = new HashSet<char>(DSVFormat.s_Thousands);
            HashSet<DSVDateFormat.DateOrder> possibleDateOrders = new HashSet<DSVDateFormat.DateOrder>(DSVDateFormat.s_DateRegex.Values);
            for (string line = line2;
                line != null && (Format.DecimalSeparator == default || Format.ThousandsSeparator == default || Format.DateFormat == DSVDateFormat.DateOrder.None);
                line = inputFile.ReadLine())
            {
                int column = 0;
                foreach (string item in SplitByDelimiter(line, Format.Delimiter))
                {
                    DSVStructure.CellType cType = Structure.GetTypeAtColumn(column);

                    if (cType == DSVStructure.CellType.Number && (Format.ThousandsSeparator == default || Format.DecimalSeparator == default))
                    {
                        HashSet<char> itemPossibleDecimals = GetPossibleDecimalTypes(item);
                        HashSet<char> itemPossibleThousands = DetermineThousandsType(item);
                        //Process decimals here
                        possibleDecimals.RemoveWhere((c) =>
                        {
                            return !itemPossibleDecimals.Contains(c);
                        });
                        possibleThousands.RemoveWhere((c) =>
                        {
                            return !itemPossibleThousands.Contains(c);
                        });
                        if (possibleDecimals.Count == 1)
                        {
                            Format.DecimalSeparator = GetFirst<char>(possibleDecimals);
                        }
                        if (possibleThousands.Count == 1)
                        {
                            Format.ThousandsSeparator = GetFirst<char>(possibleThousands);
                        }
                    }
                    else if (cType == DSVStructure.CellType.Date)
                    {
                        HashSet<DSVDateFormat.DateOrder> itemPossibleDateOrders = DetermineDateType(item);
                        possibleDateOrders.RemoveWhere((dateOrder) =>
                        {
                            return !itemPossibleDateOrders.Contains(dateOrder);
                        });
                        if (possibleDateOrders.Count == 1)
                        {
                            IEnumerator<DSVDateFormat.DateOrder> e = possibleDateOrders.GetEnumerator();
                            e.MoveNext();
                            Format.DateFormat = e.Current;
                        }
                    }

                    column++;
                }
            }
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
                Format.DateFormat = DSVDateFormat.DateOrder.Slash_DDMMYYYY;
            }
        }

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

        private char FindDelimiter(in string s)
        {
            bool withinQuote = false;
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
            if (possibleDecimals.Count == 0)
            {
                return new HashSet<char>(DSVFormat.s_Decimals);
            }
            return possibleDecimals;
        }

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
            if (possibleThousands.Count == 0)
            {
                return new HashSet<char>(DSVFormat.s_Thousands);
            }
            return possibleThousands;
        }

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
            return possibleDateOrders;
        }

        private T GetFirst<T>(IEnumerable<T> collection)
        {
            IEnumerator<T> enumerator = collection.GetEnumerator();
            enumerator.MoveNext();
            return enumerator.Current;
        }

    }
}