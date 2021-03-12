using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DirectoryTools
{
    class Program
    {
        static void Main(string[] args)
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
            if (TryGetSize(dir, out var totalSize, out var totalFiles, out var totalDirectories))
            {
                Console.WriteLine($"O tamanho total é {totalSize}, com {totalDirectories} diretórios e {totalFiles} arquivos.");
            }
            else
            {
                Console.WriteLine("Não foi possível computar esse diretório.");
            }

            Console.ReadLine();
        }

        static bool TryGetSize(string dir, out long totalSize, out int totalFiles, out int totalDirectories)
        {
            totalSize = 0;
            totalFiles = 0;
            totalDirectories = 0;

            try
            {
                (totalSize, totalFiles, totalDirectories) = Discovery(dir);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return false;
        }

        static (long totalSize, int totalFiles, int totalDirectories) Discovery(string dir)
        {
            var files = new List<string>();
            var directories = new List<string>();
            int totalDirectories = 0;

            var (totalSize, totalFiles) = ComputeSize(dir);

            directories.AddRange(Directory.GetDirectories(dir).Where(d => d != "." || d != ".."));
            totalDirectories = directories.Count;

            Parallel.ForEach(directories, dir =>
            {
                var (size, files, directories) = Discovery(dir);

                Interlocked.Add(ref totalSize, size);
                Interlocked.Add(ref totalFiles, files);
                Interlocked.Add(ref totalDirectories, directories);
            });

            return (totalSize, totalFiles, totalDirectories);
        }

        static (long TotalSize, int TotalFiles) ComputeSize(string dir)
        {
            var files = Directory.GetFiles(dir);
            long totalFiles = 0;

            Parallel.ForEach(files, file =>
            {
                Interlocked.Add(ref totalFiles, file.Length);
            });

            return (totalFiles, files.Length);
        }
    }
}
