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
            // Read in the directory and get the masks
            Console.Write("Enter the directory (must be an absolute path): ");
            string directory = Console.ReadLine();
            while (!Directory.Exists(directory))
            {
                Console.WriteLine("Invalid Directory.");
                Console.Write("Enter the directory: ");
                directory = Console.ReadLine();
            }
            Console.WriteLine("Enter masks: [q to continue]");
            List<string> masks = new List<string>();
            for (string maskInput = Console.ReadLine(); maskInput != "q"; maskInput = Console.ReadLine())
            {
                masks.Add(maskInput);
            }
            if (masks.Count == 0) // If there are no masks, search for everything
            {
                masks.Add("*");
            }

            Console.Write("Enter an output file path ex: \"/Users/me/Desktop/output.txt\": ");
            string outputFile = Console.ReadLine();

            // Retrieve all the files and add an analysis task
            HashSet<string> files = GetFilesInDirectoryRecursive(directory, masks);
            List<Task<DSVFile>> tasks = new List<Task<DSVFile>>(); // Use tasks to take advantage of thread pooling
            foreach (string file in files)
            {
                string fileCopy = file;
                tasks.Add(Task.Factory.StartNew<DSVFile>(() => GetData(fileCopy)));
            }

            // When results are finished, run the statistics analysis
            DSVFile[] results = await Task.WhenAll(tasks);
            Dictionary<DSVFile.DSVStructure, int> structures = new Dictionary<DSVFile.DSVStructure, int>();
            Dictionary<DSVFile.DSVFormat, int> formats = new Dictionary<DSVFile.DSVFormat, int>();
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

            // Output
            List<string> output = new List<string>();
            output.Add("STRUCTURE STATS:");
            foreach (DSVFile.DSVStructure strct in structures.Keys)
            {
                output.Add(String.Format("Count: {0} => {1}", structures[strct], strct.ToString()));
            }

            output.Add("FORMATS:");
            foreach (DSVFile.DSVFormat fmt in formats.Keys)
            {
                output.Add(String.Format("Count: {0} => {1}", formats[fmt], fmt.ToString()));
            }

            await File.WriteAllLinesAsync(outputFile, output);
            Console.WriteLine("Finished writing file to " + outputFile);
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

