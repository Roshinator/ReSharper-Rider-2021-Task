using System;
using System.Collections.Generic;

namespace ReSharperRiderTask
{
    public class FileStructure
    {
        private enum CellType { String, Number, Date };

        private List<(string, CellType)> structure;

        public FileStructure(string header, string analysis, char delimeter)
        {
            structure = new List<(string, CellType)>();
            string[] items = header.Split(delimeter);
            foreach (string s in items)
            {
                
            }
        }

        //private CellType MatchType(string s)
        //{

        //}
    }
}

