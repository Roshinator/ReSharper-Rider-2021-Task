using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
namespace ReSharperRiderTask
{
    class Program
    {
        static async Task Main(string[] args)
        {
            List<string> files = GetFilesInDirectoryRecursive("/Users/roshansevalia/Desktop/ReSharperRiderTestCases", "*.txt");
            List<Task<DSVFile>> tasks = new List<Task<DSVFile>>(); // Use tasks to take advantage of thread pooling
            foreach (string file in files)
            {
                string fileCopy = file;
                tasks.Add(Task.Factory.StartNew<DSVFile>(() => GetData(fileCopy)));
            }

            DSVFile[] results = await Task.WhenAll(tasks);
            Dictionary<DSVStructure, int> structures = new Dictionary<DSVStructure, int>();
            Dictionary<DSVFormat, int> formats = new Dictionary<DSVFormat, int>();
            foreach (DSVFile res in results)
            {
                if (structures.ContainsKey(res.Structure))
                    structures[res.Structure]++;
                else
                    structures.Add(res.Structure, 1);

                if (formats.ContainsKey(res.Format))
                    formats[res.Format]++;
                else
                    formats.Add(res.Format, 1);
            }

            Console.WriteLine("STRUCTURE STATS:");
            foreach (DSVStructure strct in structures.Keys)
            {
                Console.WriteLine("Count: {0} => {1}", structures[strct], strct.ToString());
            }

            Console.WriteLine("FORMATS:");
            foreach (DSVFormat fmt in formats.Keys)
            {
                Console.WriteLine("Count: {0} => {1}", formats[fmt], fmt.ToString());
            }
        }

        static List<string> GetFilesInDirectoryRecursive(string path, string mask)
        {
            try
            {
                List<string> files = new List<string>();
                foreach (string file in Directory.GetFiles(path, mask))
                {
                    files.Add(file);
                }
                foreach (string directory in Directory.GetDirectories(path))
                {
                    files.AddRange(GetFilesInDirectoryRecursive(directory, mask));
                }
                return files;
            }
            catch (Exception)
            {
                Console.WriteLine("{0} is an invalid directory.", Path.GetFullPath(path));
                return null;
            }
        }

        static DSVFile GetData(string path)
        {
            return new DSVFile(path);
        }
    }
}

