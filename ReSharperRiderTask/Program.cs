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
            foreach (DSVFile res in results)
            {
                Console.Write("{0}, ", res);
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

