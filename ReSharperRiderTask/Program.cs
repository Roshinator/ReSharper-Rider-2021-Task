using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
namespace ReSharperRiderTask
{
    class Program
    {
        static void Main(string[] args)
        {
            SpawnThreads();
        }

        static async Task SpawnThreads()
        {
            List<Task<int>> tasks = new List<Task<int>>();
            for (int i = 0; i < 1000000; i++)
            {
                int x = i;
                tasks.Add(Task.Factory.StartNew<int>(() =>
                {
                    Thread.Sleep(500000);
                    return x;
                }));
            }

            List<int> outputs = new List<int>();
            int c = 0;
            int[] results = await Task.WhenAll(tasks);
            foreach (int res in results)
            {
                Console.Write("{0}, ", res);
            }
            
        }

        DSVFile GetData(string path)
        {
            return new DSVFile(path);
        }
    }
}

