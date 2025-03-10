using System.Diagnostics;
using XerParser;

namespace WrapperXerParser

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



        public async void Parse(string filePath)
        {
            parser.ResetIgnoredTable();
            sw = Stopwatch.StartNew();
            Console.WriteLine();
            Console.WriteLine($"Начало: {DateTime.Now}");
            await parser.LoadXer(filePath);
        }

        public async void ParseCustom(string filePath)
        {
            parser.LoadedTable.Add("PROJECT");
            parser.LoadedTable.Add("PROJWBS");
            parser.LoadedTable.Add("TASK");
            sw = Stopwatch.StartNew();
            Console.WriteLine();
            Console.WriteLine($"Начало: {DateTime.Now}");
            await parser.LoadXer(filePath);
        }

        private void Parser_InitializationСompleted(object sender, InitializeEventArgs e)
        {
            PrintResult(sw, parser.XerElements);
        }

        private void Parser_Initialization(object sender, InitializingEventArgs e)
        {
            Console.WriteLine($"{e.XerElement.TableName,-10} Время: {e.Elapsed} Строк: {e.XerElement.RowsCount}");
        }

        static void PrintResult(Stopwatch sw, XerElement[] res)
        {
            List<Task> tasks = [];
            foreach (var x in res)
            {
                if (!x.IsInicialized)
                {
                    Debug.WriteLine(x.TableName + " waiting...");
                    tasks.Add(x.TaskParsing);
                }
            }

            Task.WaitAll([.. tasks]);
            GC.Collect();

            Console.WriteLine();
            Console.WriteLine($"Таблиц: {res.Length} Время: {sw.Elapsed}");
            Console.Write(new string('*', 75) + '\n');
            Console.WriteLine();
        }
    }
}
