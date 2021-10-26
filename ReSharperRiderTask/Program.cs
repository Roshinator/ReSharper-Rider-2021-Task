using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
namespace ReSharperRiderTask
{
    class Program
    {
        /// <summary>
        /// Main function
        /// </summary>
        /// <param name="args">Command line args</param>
        static async Task Main(string[] args)
        {
            Console.Write("Enter the directory:");
            string directory = Console.ReadLine();
            while (!Directory.Exists(directory))
            {
                Console.WriteLine("Invalid Directory.");
                Console.Write("Enter the directory:");
                directory = Console.ReadLine();
            }
            Console.WriteLine("Enter masks: [q to continue]");
            List<string> masks = new List<string>();
            for (string maskInput = Console.ReadLine(); maskInput != "q"; maskInput = Console.ReadLine())
            {
                masks.Add(maskInput);
            }
            if (masks.Count == 0)
            {
                masks.Add("*");
            }
            HashSet<string> files = GetFilesInDirectoryRecursive(directory, masks);
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

        /// <summary>
        /// Gets files in the directory and subdirectories using a mask
        /// </summary>
        /// <param name="path">Path to files to files</param>
        /// <param name="masks">A list of * and ? parameter filter masks</param>
        /// <returns>A set of file paths to the matching files</returns>
        static HashSet<string> GetFilesInDirectoryRecursive(string path, IEnumerable<string> masks)
        {
            try
            {
                HashSet<string> files = new HashSet<string>();
                foreach (string mask in masks)
                {
                    foreach (string file in Directory.GetFiles(path, mask))
                    {
                        if (!files.Contains(file))
                        {
                            files.Add(file);
                        }
                    }
                }
                foreach (string directory in Directory.GetDirectories(path))
                {
                    files.UnionWith(GetFilesInDirectoryRecursive(directory, masks));
                }
                return files;
            }
            catch (Exception)
            {
                Console.WriteLine("{0} is an invalid directory.", Path.GetFullPath(path));
                return null;
            }
        }

        /// <summary>
        /// Returns the file data using a path
        /// </summary>
        /// <param name="path">Path to the file</param>
        /// <returns>The data for the DSV file</returns>
        static DSVFile GetData(string path)
        {
            return new DSVFile(path);
        }
    }
}

