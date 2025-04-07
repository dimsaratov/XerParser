using System.Data;
using System.Diagnostics;
using System.Text.RegularExpressions;

using XerParser;

namespace XerParserTest

{
    public class ParserWrapper
    {

        Stopwatch sw;
        readonly Parser parser;
        int cursorTop;
        private static readonly Lock ConsoleWriterLock = new();
        public bool WithFullLog { get => parser.WithFullLog; set => parser.WithFullLog = value; }

        public ParserWrapper()
        {
            parser = new();
            parser.InitializationСompleted += Parser_InitializationСompleted;
            parser.Initialization += Parser_Initialization;
            parser.CreatedRelationColumns += Parser_CreatedRelationColumns;
            parser.Readed += Parser_Readed;
            parser.CreateRelationColumns = true;

        }

        private void Parser_CreatedRelationColumns(object sender, InitializeEventArgs e)
        {
            Console.WriteLine("Связанные колонки добавлены: " + e.Elapsed);
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
            await LoadXer(filePath);

            if (parser.ErrorLog.Count > 0)
            {
                Console.WriteLine("Errors occurred when parsing the file (Произошли ошибки): ");
                foreach (string s in parser.ErrorLog)
                {
                    string s2 = SplitToLines(s, 75);
                    Console.WriteLine(s2);
                }               
            }
            else
            {
                Console.WriteLine("No mistakes / Без ошибок");
            }
            Console.WriteLine(new string('.', 75));
        }

        public async Task ParseCustom(string filePath)
        {
            sw = Stopwatch.StartNew();
            PrintStart(nameof(ParseCustom), filePath);
            parser.ResetIgnoredTable();
            parser.SetLoadedTable(["PROJECT","PROJWBS","TASK"]);        
            await LoadXer(filePath);
        }


        public async Task ParseRsrc(string filePath)
        {
            sw = Stopwatch.StartNew();
            PrintStart(nameof(ParseCustom), filePath);
            parser.ResetIgnoredTable();
            parser.SetLoadedTable(["CALENDAR", "RSRC", "UDFTYPE", "UDFVALUE", "UMEASURE"]);     
            await LoadXer(filePath);
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
      
        private async Task LoadXer(string filePath)
        {           
            await parser.LoadXer(filePath);
        }

        private void Parser_InitializationСompleted(object sender, InitializeEventArgs e)
        {
            parser.ParseCounter.PropertyChanged -= ParseCounter_PropertyChanged;
            PrintResult(sw, parser.XerElements);
        }


        private void Parser_Initialization(object sender, InitializingEventArgs e)
        {
            Console.WriteLine($"Parsed {e.XerElement.TableName,-10} Время: {e.Elapsed} Строк: {e.XerElement.RowsCount}");
        }

        private void Parser_Readed(object sender, ReadingEventArgs e)
        {

            decimal progress = Math.Round(parser.ReadCounter.Percent, 3);
            Console.WriteLine($"Readed {e.XerElement.TableName,-10} Время: {e.Elapsed} Полей: {e.XerElement.FieldCount,-5 } Позиция: {progress}%");
           
            if (e.IsCompleted)
            {
                Console.WriteLine($"Reading complited: {sw.Elapsed}");
                Console.WriteLine(new string('.', 75));
                Console.WriteLine($"Parsing: ");
                cursorTop = Console.CursorTop - 1;
                parser.ParseCounter.PropertyChanged += ParseCounter_PropertyChanged;
            }
        }

        private void ParseCounter_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            lock (ConsoleWriterLock)
            {
                (int, int) curr = Console.GetCursorPosition();
                Console.SetCursorPosition(11, cursorTop);
                Console.WriteLine($"{(sender as Counter).Percent:0.##}%    ");
                Console.SetCursorPosition(curr.Item1, curr.Item2);
            }
        }

        private static void PrintResult(Stopwatch sw, XerElement[] res)
        {     
            Console.WriteLine();
            Console.WriteLine($"Таблиц: {res.Length} Время: {sw.Elapsed}");
            Console.Write(new string('*', 75) + '\n');
        }

        private static string SplitToLines(string str, int n)
        {
            return Regex.Replace(str, ".{" + n + "}(?!$)", "$0\n");
        }
    }
}
