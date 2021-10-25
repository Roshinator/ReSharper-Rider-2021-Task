using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace ReSharperRiderTask
{
    public class DSVStructure : IEquatable<DSVStructure>
    {
        public enum CellType { String, Number, Date };
        private List<(string, CellType)> _structure;

        public DSVStructure(IEnumerable<string> headings, IEnumerable<string> items)
        {
            _structure = new List<(string, CellType)>();
            IEnumerator<string> headingsEnumerator = headings.GetEnumerator();
            foreach (string s in items)
            {
                headingsEnumerator.MoveNext();
                string title = headingsEnumerator.Current;
                string test = s.Replace("\"", "");
                bool foundDate = false;
                foreach (Regex rx in DSVDateFormat.s_DateRegex.Keys)
                {
                    if (rx.IsMatch(test))
                    {
                        _structure.Add((title, CellType.Date));
                        foundDate = true;
                        break;
                    }
                }

                if (!foundDate && Regex.IsMatch(test, @"[^\d\., ]")) // If there is a match for a non-number
                {
                    _structure.Add((title, CellType.String));
                }
                else if (!foundDate)
                {
                    _structure.Add((title, CellType.Number));
                }
            }
        }

        public CellType GetTypeAtColumn(int column)
        {
            return _structure[column].Item2;
        }

        public bool Equals([AllowNull] DSVStructure other)
        {
            return other._structure.Equals(_structure);
        }
    }
}

