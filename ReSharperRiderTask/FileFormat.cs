using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;

namespace ReSharperRiderTask
{
    public class FileFormat
    {
        //private readonly char[] _delimiters = { ',', '\t', ';' };
        //private readonly char[] _decimals = { '.', ',' };
        //private readonly char[] _thousands = { '.', ',', ' ' };
        //private readonly char[] _dateSeparator = { '/', '.' };
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
        private enum CellType { String, Number, Date };

        public char Delimiter;
        public char DecimalSeparator;
        public char ThousandsSeparator;
        private List<(string, CellType)> structure;

        public DateOrder DateFormat {get; private set;}

        public FileFormat(in string path)
        {
            structure = new List<(string, CellType)>();
            StreamReader inputFile = new StreamReader(path);

            string header = inputFile.ReadLine();
            Delimiter = FindDelimiter(header);
            IEnumerable<string> headerItems = SplitByDelimiter(header, Delimiter);

            string line2 = inputFile.ReadLine();
            structure = GetStructure(headerItems, SplitByDelimiter(line2, Delimiter));

            // We have now determined the structure of the file


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

    }
}

