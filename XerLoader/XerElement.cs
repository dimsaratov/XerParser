using System.Data;
using System.Diagnostics;
using System.Globalization;
using static XerLoader.XerElement;

namespace XerLoader
{
    public class XerElement(string tableName)
    {
        #region Variable
        public delegate object ValueParse(string value);

     
        DataTable table;
        private IEnumerable<string> fields;
        private IEnumerable<List<string>> records;
        private List<DataSetter> parsers;
        private readonly string table_name = tableName;
        private DataSet dsXer;
        private bool isInitilized;
        private Task task;
        private readonly List<string> errorLog = [];
        private readonly Stopwatch stopwatch = Stopwatch.StartNew();
        #endregion

        #region Property
        public bool IsInicialized { get { return isInitilized; } }
        public List<string> ErrorLog { get => errorLog; }

        public string TableName
        {
            get => table?.TableName;
        }

        public int RowsCount
        {
            get
            {
                int? i = table?.Rows.Count;
                return i ?? 0;
            }
        }

        public bool IsErrors
        {
            get => errorLog.Count > 0;
        }

        public DataTable Table { get { return table; } }

        internal IEnumerable<string> FieldNames
        {
            get => fields;
            set
            {
                fields= value;
                SetValueParser(fields);
            }
        }
        internal IEnumerable<List<string>> Records
        {
            get => records ?? [];
            set
            {
                records = value;
                _ = ConvertValue();
            }
        }

        public DataSet DataSetXer
        {
            get => dsXer;
            set
            {
                dsXer = value;
                dsXer.TryGetTable(table_name, out table);
            }
        }
        public Task TaskParsing { get => task; }
        #endregion

        #region ValueParser
        static void WriteErrorLog(List<string> errorLog, string value, int row, string error, string cTable, string field)
        {
            string eString = $"Строка: {row}  Ошибка: {error} Таблица: {cTable} Поле: {field} Значение: {value}";
            errorLog.Add(eString);
        }

        async Task ConvertValue()
        {
            task = Task.Run(() =>
                {
                    int idx = 0;
                    foreach (var record in records)
                    {
                        string value = string.Empty;
                        string field = string.Empty;
                        try
                        {
                            DataRow row = table.NewRow();
                            for (int i =0; i<parsers.Count; i++)
                            {
                                value = record[i];
                                field = parsers[i].Name;
                                row[field] = parsers[i].ValueParse(value);
                            }
                            table.Rows.Add(row);                         
                        }
                        catch (Exception ex)
                        {
                            WriteErrorLog(errorLog, value, idx, ex.Message, TableName, field);
                        }
                        idx++;
                    }
                    records = null;
                    OnInitialised(new InitializeEventArgs(stopwatch.Elapsed));
                });
            await task;
            stopwatch.Stop();
            Debug.WriteLine($"{TableName,-10}: {stopwatch.Elapsed}");
            GC.Collect();
        }

        void SetValueParser(IEnumerable<string> fields)
        {
            parsers = [];
            foreach (string f in fields)
            {
                if (table.Columns[f] is DataColumn column) parsers.Add(new(column));                    
            }
        }
        #endregion

        #region Events

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Стили именования", Justification = "<Ожидание>")]
        private event EventHandler<InitializeEventArgs> onInitialized;
        protected virtual void OnInitialised(InitializeEventArgs e)
        {
            isInitilized = true;
            onInitialized?.Invoke(this, e);
        }

        public event EventHandler<InitializeEventArgs> Initialized
        {
            add { onInitialized += value; }
            remove { onInitialized -= value; }
        }


        #endregion 
    }
}
