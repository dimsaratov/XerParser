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
            parser.CreateRelationColumns = false;

        }

        private void Parser_CreatedRelationColumns(object sender, InitializingEventArgs e)
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
            parser.WithFullLog = true;
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

            if (parser.CreateRelationColumns)
            {
                ActivityDataTable table = (ActivityDataTable)parser.DataSetXer.Tables["TASK"];

                table.Columns.Add(new UdfDataColumn("user_field_131", typeof(string))
                {
                    ChildTypeId = 131,
                    ChildFieldValueName = "udf_text"
                });

                table.Columns.Add(new ActivityCodeDataColumn("actv_code_460", typeof(string))
                {
                    ChildTypeId = 460,
                    ChildFieldValueName = "short_name"
                });

                foreach (ActivityDataRow row in table.Rows.Cast<ActivityDataRow>())
                {
                    Console.WriteLine($"TaskCode:{row["task_code"]} {table.Columns[62].ColumnName}: {row.ItemArray[62]} {table.Columns[63].ColumnName}: {row.ItemArray[63]}");

                }
            }
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

        private void Parser_InitializationСompleted(object sender, InitializingEventArgs e)
        {
            parser.ParseCounter.PropertyChanged -= ParseCounter_PropertyChanged;
            PrintResult(sw, parser.XerElements);
        }


        private void Parser_Initialization(object sender, InitializedEventArgs e)
        {
            Console.WriteLine($"Parsed {e.XerElement.TableName,-12} " +
                              $"Время: {e.Elapsed.Seconds}.{e.Elapsed.Milliseconds,-7:0000} " +
                              $"Строк: {e.XerElement.RowsCount,-25}" +
                              $"::{parser.ParseCounter.Message}");
        }

        private void Parser_Readed(object sender, ReadedEventArgs e)
        {

            Console.WriteLine($"Readed {e.XerElement.TableName,-12} " +
                              $"Время: {e.Elapsed.Seconds}.{e.Elapsed.Milliseconds,-7:0000} " +
                              $"Полей: {e.XerElement.FieldCount,-7 } " +
                              $"Позиция: {parser.ParseCounter.Percent,-6:##0.00}% " +
                              $"::{parser.ParseCounter.Message}");


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
