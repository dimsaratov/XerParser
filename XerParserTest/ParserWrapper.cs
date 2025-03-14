using System.Diagnostics;
using XerParser;

namespace XerParserTest

{
    public class ParserWrapper
    {

        Stopwatch sw;
        readonly Parser parser;

        public ParserWrapper(string pathSchemaXer)
        {
            parser = new(pathSchemaXer);
            parser.InitializationСompleted += Parser_InitializationСompleted;
            parser.Initialization += Parser_Initialization;
        }

        private static void PrintStart(string operation, string filePath)
        {
            Console.WriteLine();
            Console.WriteLine($"Начало [{operation}]: {DateTime.Now}");
            Console.WriteLine(filePath);
            Console.WriteLine(new string('.', 75));
        }

        public async Task Parse(string filePath)
        {
            sw = Stopwatch.StartNew();
            PrintStart(nameof(Parse), filePath);
            parser.ResetIgnoredTable();
            Console.WriteLine();
            await parser.LoadXer(filePath);
            if (parser.ErrorLog.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine(new string('.', 75));
                Console.WriteLine("Errors:");
                foreach (string s in parser.ErrorLog)
                {
                    Console.WriteLine(s);
                }
                Console.WriteLine(new string('.', 75));
            }
        }

        public async Task ParseCustom(string filePath)
        {
            sw = Stopwatch.StartNew();
            PrintStart(nameof(ParseCustom), filePath);
            parser.ResetIgnoredTable();
            parser.ProgressCounter.Reset();
            parser.SetLoadedTable(["PROJECT","PROJWBS","TASK"]);        
            await parser.LoadXer(filePath);
        }


        public async Task ParseRsrc(string filePath)
        {
            sw = Stopwatch.StartNew();
            PrintStart(nameof(ParseCustom), filePath);
            parser.ResetIgnoredTable();
            parser.ProgressCounter.Reset();
            parser.SetLoadedTable(["CALENDAR", "RSRC", "UDFTYPE", "UDFVALUE", "UMEASURE"]);     
            await parser.LoadXer(filePath);
        }

        public async Task Save(string filePath)
        {
            PrintStart(nameof(Save), filePath);
            Stopwatch sw = Stopwatch.StartNew();
            parser.RemoveEmptyTables = true;
            await parser.BuildXerFile(filePath);
            sw.Stop();
            if (parser.ErrorLog.Count > 0)
            {
                Console.WriteLine("Произошли ошибки при записи в файл");
                Console.WriteLine(string.Join("\n", parser.ErrorLog));
            }
            else
            {
                Console.WriteLine("Построение Xer-файлa успешно завершено");
            }
            Console.WriteLine($"Время записи: {sw.Elapsed}");
        }
        private void Parser_InitializationСompleted(object sender, InitializeEventArgs e)
        {
            PrintResult(sw, parser.XerElements);
        }

        private void Parser_Initialization(object sender, InitializingEventArgs e)
        {
            decimal progress = Math.Round(parser.ProgressCounter.Percent(), 3);
            Console.WriteLine($"{e.XerElement.TableName,-10} Время: {e.Elapsed} Строк: {e.XerElement.RowsCount,-10} Позиция: {progress}%");
        }

        static void PrintResult(Stopwatch sw, XerElement[] res)
        {
            Console.WriteLine();
            Console.WriteLine($"Таблиц: {res.Length} Время: {sw.Elapsed}");
            Console.Write(new string('*', 75) + '\n');
            Console.WriteLine();
        }
    }
}
