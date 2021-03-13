using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DirectoryTools
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Informe um diretório para computar o tamanho em bytes");

            var dir = Console.ReadLine();

            if (string.IsNullOrEmpty(dir))
            {
                Console.WriteLine("Você precisa fornecer um diretório como parâmetro.");
                return;
            }

            if (!Directory.Exists(dir))
            {
                Console.WriteLine("Diretório não encontrado");
            }

            Console.WriteLine($"Computando {dir}");
            try
            {
                var (totalSize, totalFiles, totalDirectories) = await DiscoveryAsync(dir);
                Console.WriteLine($"O tamanho total é {totalSize}, com {totalDirectories} diretórios e {totalFiles} arquivos.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Não foi possível computar esse diretório. {ex.Message}");
            }

            Console.ReadLine();
        }

        static async Task<(long totalSize, int totalFiles, int totalDirectories)> DiscoveryAsync(string dir)
        {
            var files = new List<string>();
            var directories = new List<string>();
            int totalDirectories = 0;

            var (totalSize, totalFiles) = await ComputeSizeAsync(dir);

            directories.AddRange(await Task.Run(() => Directory.GetDirectories(dir).Where(d => d != "." || d != "..")));
            totalDirectories = directories.Count;

            //Parallel.ForEach(directories, async dir =>
            //{
            //    var (size, files, directories) = await DiscoveryAsync(dir);

            //    Interlocked.Add(ref totalSize, size);
            //    Interlocked.Add(ref totalFiles, files);
            //    Interlocked.Add(ref totalDirectories, directories);
            //});

            // Por conta do Parallel.ForEach ser síncrono, é preciso lidar na mão com o paralelismo assíncrono... ;/
            await Task.WhenAll(Partitioner
                .Create(directories)
                .GetPartitions(Environment.ProcessorCount)
                .Select(partition => Task.Run(async () =>
                {
                    using (partition)
                        while (partition.MoveNext())
                        {
                            var (size, files, directories) = await DiscoveryAsync(partition.Current);

                            Interlocked.Add(ref totalSize, size);
                            Interlocked.Add(ref totalFiles, files);
                            Interlocked.Add(ref totalDirectories, directories);
                        }
                })));

            return (totalSize, totalFiles, totalDirectories);
        }

        static async Task<(long TotalSize, int TotalFiles)> ComputeSizeAsync(string dir)
        {
            var files = await Task.Run(() => Directory.GetFiles(dir));
            long totalFiles = 0;

            Parallel.ForEach(files, file =>
            {
                Interlocked.Add(ref totalFiles, file.Length);
            });

            return (totalFiles, files.Length);
        }
    }
}
