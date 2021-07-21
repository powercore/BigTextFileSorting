using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;

namespace BigTextFileSorting
{
    internal static class Program
    {
        private const string workPath = "/Users/Aquateca/";
        private const string processingPath = "/Users/Aquateca/tmp/";
        private const string testFileName = "testfile.txt";
        private const string resultFileName = "resultfile.txt";
        private const int bufferSize = 1000;
        private const int testFileSizeMb = 500;
        private const int magicQoeficient = 35000;

        // internal class for sorting things
        private class DataLine
        {
            public long Number { get; set; }
            public string Value { get; set; }

            public DataLine(long num, string val)
            {
                Number = num;
                Value = val;
            }
        }

        // sorter for DataLine objects
        private class MyDataLineComparer : IComparer<DataLine>
        {
            public int Compare(DataLine x, DataLine y)
            {
                if (ReferenceEquals(x, y)) return 0;
                if (ReferenceEquals(null, y)) return 1;
                if (ReferenceEquals(null, x)) return -1;
                var numberComparison = x.Number.CompareTo(y.Number);
                return numberComparison != 0
                    ? numberComparison
                    : string.Compare(x.Value, y.Value, StringComparison.Ordinal);
            }
        }

        // Helper
        private static bool Exists(this object obj)
        {
            return (obj != null);
        }

        // Test file generator
        private static void GenerateTestFile()
        {
            // get words for generation
            string[] words = File.ReadAllLines("coco.names");
            int WordsCount = words.Length;
            using StreamWriter file = new StreamWriter(workPath + testFileName);

            // generate number range based on linesCount
            long linesWritten = 0;

            //couple of vars for statistic
            long bytesWriten = 0;
            long bytesWritenBefore = 0;
            var st = DateTime.Now;
            var starttime = st;
            long linesCount = testFileSizeMb * magicQoeficient;
            int numberTrashhold = (linesCount > int.MaxValue) ? int.MaxValue : (int) linesCount;
            Random rand = new Random();
            var buffer = new StringBuilder();
            var sbuilder = new StringBuilder();
            while (linesWritten <= linesCount)
            {
                // generate random line
                // we need at least one word in the line
                var str1 = "";
                while (str1.Length == 0)
                    str1 = words[rand.Next(WordsCount)];

                // make first word with start from capital letter
                str1 = str1.First().ToString().ToUpper() + str1[1..];

                var str2 = words[rand.Next(WordsCount)];
                var str3 = words[rand.Next(WordsCount)];

                sbuilder.Append(rand.Next(numberTrashhold) + ". " + str1);
                if (str2.Length > 0)
                    sbuilder.Append(" " + str2);
                if (str3.Length > 0)
                    sbuilder.Append(" " + str3);
                    
                var line = sbuilder.ToString();
                sbuilder.Clear();

                // write line to file through buffer
                if (buffer.Length == 0)
                    buffer.Append(line);
                else
                    buffer.Append('\n' + line);

                if (buffer.Length > bufferSize)
                {
                    buffer.Append('\n');
                    file.Write(buffer.ToString());
                    buffer.Clear();
                }

                // calculate statistics
                bytesWriten += line.Length + 1;
                linesWritten++;

                // show statistic once per second
                var stt = DateTime.Now.Subtract(st).Seconds;
                if (stt <= 1) continue;
                st = DateTime.Now;
                Console.Write(
                    $"\rProgress: {(long) bytesWriten / 1000000} Mb, {(bytesWriten - bytesWritenBefore) / 1000000} Mb/s -> {(int) (((double) linesWritten / linesCount) * 100)}%");
                bytesWritenBefore = bytesWriten;
            }

            // flush buffer
            if (buffer.Length != 0)
                file.Write(buffer.ToString());

            file.Close();
            Console.WriteLine(
                $"\rProgress: {(long) bytesWriten / 1000000} Mb, {(bytesWriten - bytesWritenBefore) / 1000000} Mb/s -> {(int) (((double) linesWritten / linesCount) * 100)}%");

            Console.WriteLine(
                $"Job is done. Wrote {bytesWriten / 1000000} Mb, for a {DateTime.Now.Subtract(starttime).TotalSeconds} seconds");
        }

        // Test file sorting procedure
        private static void SortFile()
        {
            // open test file
            System.Console.WriteLine("Stage 1 - preprocessing source file...");
            string path = Path.Combine(workPath + testFileName);
            FileInfo fi = new FileInfo(path);
            long fileSize = fi.Length;
            
            using var file = new StreamReader(path);
           
            // vars for statistic
            var st = DateTime.Now;
            var starttime = DateTime.Now;
            long linesCount = 0;
            long linesCountBefore = 0;
            long byteRead = 0;

            // preprocessing sorting dictionary
            var keywords = new Dictionary<string, string>();

            while (!file.EndOfStream)
            {
                var line = file.ReadLine();
                linesCount++;
                if (!line.Exists() || line.Length == 0)
                    throw new Exception("Error during processing the file - line is null or empty!");

                byteRead += line.Length;
                // split the line to number and string
                var parts = line.Split('.');
                parts[1] = parts[1].Trim(' ');
                int idx = parts[1].IndexOf(' ');
                string keyword = (idx != -1) ? parts[1][..idx] : parts[1];

                if (keywords.ContainsKey(keyword))
                {
                    // using dictionary's value as a buffer
                    keywords[keyword] += "\n" + line;
                    if (keywords[keyword].Length > bufferSize)
                    {
                        using var tempfile = new StreamWriter(processingPath + $"{keyword}", true);
                        tempfile.Write(keywords[keyword]);
                        keywords[keyword] = "";
                        tempfile.Close();
                    }
                }
                else
                    keywords.Add(keyword, line);

                // show statistic once per second
                var stt = DateTime.Now.Subtract(st).Seconds;
                if (stt <= 1) continue;
                st = DateTime.Now;
                Console.Write($"\rProgress: {linesCount} lines, {linesCount - linesCountBefore} lines/s -> {(int)(((float)byteRead/fileSize) * 100)}%");
                linesCountBefore = linesCount;
            }

            // flush all buffers
            foreach (var (key, value) in keywords)
            {
                if (value.Length <= 0) continue;
                using var tempfile = new StreamWriter(processingPath + $"{key}", true);
                tempfile.Write(value);
                tempfile.Close();
            }

            file.Close();
            
            Console.WriteLine($"\nPreprocessing 100% done.");

            System.Console.WriteLine("Stage 2 - sorting temp files...");

            // sorting dictionary
            var tempFilesList = new List<string>(keywords.OrderBy(k => k.Key).Select(x => x.Key));
            using var outputFile = new StreamWriter(workPath + resultFileName);

            // sorting every temp file
            for (int i = 0; i < tempFilesList.Count; i++)
            {
                var lines = File.ReadAllLines(processingPath + tempFilesList[i]);

                var tempList = new List<DataLine>();
                for (int j = 0; j < lines.Length; j++)
                {
                    if (lines[j].Length == 0) continue;
                    var parts = lines[j].Split('.');
                    var num = long.Parse(parts[0]);
                    tempList.Add(new DataLine(num, lines[j]));
                }

                tempList.Sort(new MyDataLineComparer());

                foreach (var dataLine in tempList)
                  outputFile.WriteLine(dataLine.Value);

                // show statistic once per second
                var stt = DateTime.Now.Subtract(st).Seconds;
                if (stt <= 1) continue;
                st = DateTime.Now;
                Console.Write($"\rProgress: {i} parts of {tempFilesList.Count}");
            }
            Console.WriteLine($"\rProgress: {tempFilesList.Count} parts of {tempFilesList.Count}");

            outputFile.Close();
            // delete temp files
            foreach (var name in tempFilesList)
                File.Delete(processingPath + name);

            Console.WriteLine($"Job is done for a {DateTime.Now.Subtract(starttime).TotalSeconds} seconds.");
        }

        static void Main(string[] args)
        {
            System.Console.WriteLine("BigTextFileSorter v1.0.1");
            System.Console.WriteLine(
                "Please enter \"1\" to generate test file, \"2\" to sort it, any other thing to quit.");
            string choise = System.Console.ReadLine();
            switch (choise)
            {
                case "1":
                    GenerateTestFile();
                    break;
                case "2":
                    SortFile();
                    break;
            }
        }
    }
}