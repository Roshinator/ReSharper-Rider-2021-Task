using System;

namespace ReSharperRiderTask
{
    class Program
    {
        static void Main(string[] args)
        {
            // string str = "\"thing, 2\",thing,\"this\",that,\"wow\"\n";
            string str = "thing,,thing,\n";
            foreach (string s in FileFormat.SplitByDelimiter(str, ','))
            {
                Console.WriteLine("<" + s + ">");
            }
        }
    }
}

