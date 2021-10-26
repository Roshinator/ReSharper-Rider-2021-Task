using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ReSharperRiderTask
{
    /// <summary>
    /// This class provides the Date formats
    /// </summary>
    public class DSVDateFormat
    {
        public enum DateOrder { None, Slash_DDMMYYYY, Slash_MMDDYYYY, Slash_YYYYMMDD, Dot_DDMMYYYY, Dot_MMDDYYYY, Dot_YYYYMMDD };
        public static readonly Dictionary<Regex, DateOrder> s_DateRegex = new Dictionary<Regex, DateOrder>()
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

        /// <summary>
        /// Constructor override since this class should not be constructed.
        /// </summary>
        private DSVDateFormat() { }
    }
}

