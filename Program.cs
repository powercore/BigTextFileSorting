using System;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace BigTextFileSorting
{
    class Program
    {
        private const string workPath = "/Users/Aquateca/";
        private const string processingPath = "/Users/Aquateca/tmp";
        private const string testFileName = "testfile.txt";
        

        private static void GenerateTestFile(string workpath, long linesCount = 100000000)
        {
            // get words for generation
            string[] words = File.ReadAllLines("coco.names");
            int WordsCount = words.Length;
            using StreamWriter file = new StreamWriter(workpath + testFileName);

            // generate number range based on linesCount
            long linesWritten = 0;

            //couple of vars for statistic
            long bytesWriten = 0;
            long bytesWritenBefore = 0;
            var st = DateTime.Now;
            var starttime = st;
            
            int numberTrashhold = (linesCount > int.MaxValue) ? int.MaxValue : (int) linesCount;
            Random rand = new Random(numberTrashhold);
            while (linesWritten < linesCount)
            {
                // generate random line
                var line =
                    $"{rand.Next()}. {words[rand.Next(WordsCount)]} {words[rand.Next(WordsCount)]} {words[rand.Next(WordsCount)]}\n";
               
                // write line to file
                file.WriteLine(line);
                
                // calculate statistics
                bytesWriten += line.Length;
                linesWritten++;

                // show statistic once per second
                var stt = DateTime.Now.Subtract(st).Seconds;
                if (stt <= 1) continue;
                st = DateTime.Now;
                Console.Write($"\r Progress: {(long) bytesWriten / 1000000} Mb, {(bytesWriten - bytesWritenBefore) / 1000000} Mb/s -> {(int)(((double)linesWritten/linesCount) * 100) }%");
                bytesWritenBefore = bytesWriten;
            }
            file.Close();
            Console.WriteLine();
            Console.WriteLine($"Job is done. Wrote {linesWritten} lines, {bytesWriten / 100000} Mb, for a {DateTime.Now.Subtract(starttime).Seconds} seconds");
        }

        private static void SortFile(string workpath, string processingPath)
        {
            // open test file
            System.Console.WriteLine("Stage 1 - preprocessing base file...");
            var file = new StreamReader(workpath + testFileName);
            
            // vars for statistic
            var st = DateTime.Now;
            var starttime = st;
            long linesCount = 0;
            long linesCountBefore = 0;

            while (!file.EndOfStream)
            {
                var line = file.ReadLine();
                linesCount++;

                // show statistic once per second
                var stt = DateTime.Now.Subtract(st).Seconds;
                if (stt <= 1) continue;
                st = DateTime.Now;
                Console.Write($"\rProgress: {linesCount} lines, {linesCount - linesCountBefore} lines/s.");
                linesCountBefore = linesCount;
            }

            file.Close();
            
            // readlines and store to separate files


            Console.WriteLine();
            Console.WriteLine($"Job is done for a {DateTime.Now.Subtract(starttime).Seconds} seconds.");
        }

        static void Main(string[] args)
        {
            // lifehack for string working speedup
            //CultureInfo.CurrentCulture = new CultureInfo("en-US");
            
            System.Console.WriteLine("BigTextFileSorter v1.0");
            System.Console.WriteLine("Please select 1 to generate test file, 2 to sort it, any other thing to quit.");
            string choise = System.Console.ReadLine();
            switch (choise)
            {
                case "1":
                    GenerateTestFile(workPath);
                    break;
                case "2":
                    SortFile(workPath, processingPath);
                    break;
            }
        }
    }
}