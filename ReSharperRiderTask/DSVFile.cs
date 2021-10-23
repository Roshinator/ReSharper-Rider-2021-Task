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
        public enum DateOrder { Slash_DDMMYYYY, Slash_MMDDYYYY, Slash_YYYYMMDD, Dot_DDMMYYYY, Dot_MMDDYYYY, Dot_YYYYMMDD };
        private Dictionary<Regex, DateOrder> _dateRegex = new Dictionary<Regex, DateOrder>()
        {
            {
                new Regex(@"^((([012]\d)|(3[01]))\/((0\d)|(1[012]))\/(\d{4}))",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase),
                DateOrder.Slash_DDMMYYYY
            },
            {
                new Regex(@"^(((0\d)|(1[012]))\/(([012]\d)|(3[01]))\/(\d{4}))",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase),
                DateOrder.Slash_MMDDYYYY
            },
            {
                new Regex(@"^((\d{4}))\/((0\d)|(1[012]))\/(([012]\d)|(3[01]))",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase),
                DateOrder.Slash_YYYYMMDD
            },
            {
                new Regex(@"^((([012]\d)|(3[01]))\.((0\d)|(1[012]))\.(\d{4}))",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase),
                DateOrder.Dot_DDMMYYYY
            },
            {
                new Regex(@"^(((0\d)|(1[012]))\.(([012]\d)|(3[01]))\.(\d{4}))",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase),
                DateOrder.Dot_MMDDYYYY
            },
            {
                new Regex(@"^((\d{4}))\.((0\d)|(1[012]))\.(([012]\d)|(3[01]))",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase),
                DateOrder.Dot_YYYYMMDD
            },
        };
        private SortedDictionary<Regex, char> _decRegex = new SortedDictionary<Regex, char>()
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
        private SortedDictionary<Regex, char> _thousandsRegex = new SortedDictionary<Regex, char>()
        {
            {
                new Regex(@"^\d{1,3}(\.\d{3})*",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase),
                _thousands[0] //"."
            },
            {
                new Regex(@"^\d{1,3}(,\d{3})*",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase),
                _thousands[1] //","
            },
            {
                new Regex(@"^\d{1,3}( \d{3})*",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase),
                _thousands[2] //" "
            },
        };
        private enum CellType { String, Number, Date };

        public char Delimiter;
        public char DecimalSeparator;
        public char ThousandsSeparator;
        private List<(string, CellType)> structure;

        public DateOrder DateFormat {get; private set;}

        public DSVFile(in string path)
        {
            structure = new List<(string, CellType)>();
            StreamReader inputFile = new StreamReader(path);

            string header = inputFile.ReadLine();
            Delimiter = FindDelimiter(header);
            IEnumerable<string> headerItems = SplitByDelimiter(header, Delimiter);

            string line2 = inputFile.ReadLine();
            structure = GetStructure(headerItems, SplitByDelimiter(line2, Delimiter));

            // We have now determined the structure of the file

            HashSet<char> possibleDecimals = new HashSet<char>(_decimals);
            HashSet<char> possibleThousands = new HashSet<char>(_thousands);
            HashSet<DateOrder> possibleDateOrders = new HashSet<DateOrder>(_dateRegex.Values);
            for (string line = line2; line != null; line = inputFile.ReadLine())
            {
                int column = 0;
                foreach (string item in SplitByDelimiter(line, Delimiter))
                {
                    CellType cType = structure[column].Item2;

                    if (cType == CellType.Number && (ThousandsSeparator == default || DecimalSeparator == default))
                    {
                        HashSet<char> itemPossibleDecimals = GetPossibleDecimalTypes(item);
                        HashSet<char> itemPossibleThousands = DetermineThousandsType(item);
                        //Process decimals here

                    }
                    else if (cType == CellType.Date)
                    {
                        HashSet<DateOrder> itemPossibleDateOrders = DetermineDateType(item);
                        possibleDateOrders.RemoveWhere((dateOrder) =>
                        {
                            return !itemPossibleDateOrders.Contains(dateOrder);
                        });
                        if (possibleDateOrders.Count == 1)
                        {
                            IEnumerator<DateOrder> e = possibleDateOrders.GetEnumerator();
                            e.MoveNext();
                            DateFormat = e.Current;
                        }
                    }

                    column++;
                }
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
                switch (c)
                {
                    case '\"':
                        withinQuote = !withinQuote;
                        break;
                    case ',':
                        if (!withinQuote) return c;
                        break;
                    case '\t':
                        if (!withinQuote) return c;
                        break;
                    case ';':
                        if (!withinQuote) return c;
                        break;
                }
            }
            return '\n'; // Only one item per row, so splits will simply return the line
        }

        private List<(string, CellType)> GetStructure(IEnumerable<string> headings, IEnumerable<string> items)
        {
            List<(string, CellType)> st = new List<(string, CellType)>();
            IEnumerator<string> headingsEnumerator = headings.GetEnumerator();
            foreach (string s in items)
            {
                headingsEnumerator.MoveNext();
                string title = headingsEnumerator.Current;
                string test = s.Replace("\"", "");
                foreach (Regex rx in _dateRegex.Keys)
                {
                    if (rx.IsMatch(test))
                    {
                        st.Add((title, CellType.Date));
                        continue;
                    }
                }

                if (Regex.IsMatch(test, @"[^\d\., ]")) // If there is a match for a non-number
                {
                    st.Add((title, CellType.String));
                }
                else
                {
                    st.Add((title, CellType.Number));
                }
            }
            return st;
        }

        private HashSet<char> GetPossibleDecimalTypes(string num)
        {
            HashSet<char> possibleDecimals = new HashSet<char>(_decimals);
            foreach (Regex key in _decRegex.Keys)
            {
                if (!key.IsMatch(num))
                {
                    possibleDecimals.Remove(_decRegex[key]);
                }
            }
            return possibleDecimals;
        }

        private HashSet<char> DetermineThousandsType(string num)
        {
            HashSet<char> possibleThousands = new HashSet<char>(_thousands);
            foreach (Regex key in _thousandsRegex.Keys)
            {
                if (!key.IsMatch(num))
                {
                    possibleThousands.Remove(_thousandsRegex[key]);
                }
            }
            return possibleThousands;
        }

        private HashSet<DateOrder> DetermineDateType(string date)
        {
            HashSet<DateOrder> possibleDateOrders = new HashSet<DateOrder>(_dateRegex.Values);
            foreach (Regex key in _dateRegex.Keys)
            {
                if (!key.IsMatch(date))
                {
                    possibleDateOrders.Remove(_dateRegex[key]);
                }
            }
            return possibleDateOrders;
        }

    }
}