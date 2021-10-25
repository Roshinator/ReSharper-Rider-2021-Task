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
        private static readonly char[] _delimiters = { ',', '\t', ';' };
        private static readonly char[] _decimals = { '.', ',' };
        private static readonly char[] _thousands = { '.', ',', ' ' };
        
        private static Dictionary<Regex, char> s_DecRegex = new Dictionary<Regex, char>()
        {
            {
                new Regex(@"\d{1,3}\.\d*$",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase),
                _decimals[0] //"."
            },
            {
                new Regex(@"\d{1,3},\d*$",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase),
                _decimals[1] //","
            }
        };
        private static Dictionary<Regex, char> s_ThousandsRegex = new Dictionary<Regex, char>()
        {
            {
                new Regex(@"^\d{1,3}(\.\d{3})+",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase),
                _thousands[0] //"."
            },
            {
                new Regex(@"^\d{1,3}(,\d{3})+",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase),
                _thousands[1] //","
            },
            {
                new Regex(@"^\d{1,3}( \d{3})+",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase),
                _thousands[2] //" "
            },
        };

        public DSVStructure structure { get; private set; }

        public char Delimiter;
        public char DecimalSeparator;
        public char ThousandsSeparator;

        public DSVDateFormat.DateOrder DateFormat { get; private set; } = DSVDateFormat.DateOrder.None;

        public DSVFile(in string path)
        {
            StreamReader inputFile = new StreamReader(path);

            string header = inputFile.ReadLine();
            Delimiter = FindDelimiter(header);

            IEnumerable<string> headerItems = SplitByDelimiter(header, Delimiter);

            string line2 = inputFile.ReadLine();
            structure = new DSVStructure(headerItems, SplitByDelimiter(line2, Delimiter));

            // We have now determined the structure of the file

            HashSet<char> possibleDecimals = new HashSet<char>(_decimals);
            HashSet<char> possibleThousands = new HashSet<char>(_thousands);
            HashSet<DSVDateFormat.DateOrder> possibleDateOrders = new HashSet<DSVDateFormat.DateOrder>(DSVDateFormat.s_DateRegex.Values);
            for (string line = line2;
                line != null && (DecimalSeparator == default || ThousandsSeparator == default || DateFormat == DSVDateFormat.DateOrder.None);
                line = inputFile.ReadLine())
            {
                int column = 0;
                foreach (string item in SplitByDelimiter(line, Delimiter))
                {
                    DSVStructure.CellType cType = structure.GetTypeAtColumn(column);

                    if (cType == DSVStructure.CellType.Number && (ThousandsSeparator == default || DecimalSeparator == default))
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
                            DecimalSeparator = GetFirst<char>(possibleDecimals);
                        }
                        if (possibleThousands.Count == 1)
                        {
                            ThousandsSeparator = GetFirst<char>(possibleThousands);
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
                            DateFormat = e.Current;
                        }
                    }

                    column++;
                }
            }
            if (DecimalSeparator == default)
            {
                DecimalSeparator = GetFirst<char>(possibleDecimals);
            }
            if (ThousandsSeparator == default)
            {
                ThousandsSeparator = GetFirst<char>(possibleThousands);
            }
            if (DateFormat == DSVDateFormat.DateOrder.None)
            {
                DateFormat = DSVDateFormat.DateOrder.Slash_DDMMYYYY;
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
                else if (Array.IndexOf(_delimiters, c) != -1)
                {
                    if (!withinQuote)
                        return c;
                }
            }
            return _delimiters[0]; // Only one item per row, so use a default
        }

        private HashSet<char> GetPossibleDecimalTypes(string num)
        {
            HashSet<char> possibleDecimals = new HashSet<char>(_decimals);
            foreach (Regex key in s_DecRegex.Keys)
            {
                if (!key.IsMatch(num))
                {
                    possibleDecimals.Remove(s_DecRegex[key]);
                }
            }
            if (possibleDecimals.Count == 0)
            {
                return new HashSet<char>(_decimals);
            }
            return possibleDecimals;
        }

        private HashSet<char> DetermineThousandsType(string num)
        {
            HashSet<char> possibleThousands = new HashSet<char>(_thousands);
            foreach (Regex key in s_ThousandsRegex.Keys)
            {
                if (!key.IsMatch(num))
                {
                    possibleThousands.Remove(s_ThousandsRegex[key]);
                }
            }
            if (possibleThousands.Count == 0)
            {
                return new HashSet<char>(_thousands);
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