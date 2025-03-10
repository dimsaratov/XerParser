using System.Data;
using System.Diagnostics;

namespace XerParser
{

    /// <summary>
    /// Wrapper for readable element of the Xer file
    /// </summary>
    /// <param name="tableName">Table name in Xer file</param>
    public class XerElement(string tableName)
    {
        #region Variable
        internal delegate object ValueParse(string value);

        private DataTable table;
        private IEnumerable<string> fields;
        private IEnumerable<List<string>> records;
        private List<DataSetter> parsers;
        private readonly string table_name = tableName;
        private DataSet dsXer;
        private readonly Stopwatch stopwatch = Stopwatch.StartNew();
        private static readonly List<string> list = [];

        #endregion

        #region Property
        /// <summary>
        /// A flag indicating whether the xer element has been read and converted
        /// </summary>
        public bool IsInicialized { get; private set; }

        /// <summary>
        ///  Error sheet when converting fields from XER element
        /// </summary>
        public List<string> ErrorLog { get; } = list;

        /// <summary>
        /// The number of rows of the Xer element table entries
        /// </summary>
        public int RecordCount => records is null ? 0 : records.Count();

        /// <summary>
        /// Table name in Xer file
        /// </summary>
        public string TableName => table?.TableName;

        /// <summary>
        /// The number of rows of the table
        /// </summary>
        public int RowsCount
        {
            get
            {
                int? i = table?.Rows.Count;
                return i ?? 0;
            }
        }

        /// <summary>
        /// Flag for errors when reading and converting the Xer element
        /// </summary>
        public bool IsErrors => ErrorLog.Count > 0;

        /// <summary>
        /// Dataset table
        /// </summary>
        public DataTable Table => table;

        internal IEnumerable<string> FieldNames
        {
            get => fields;
            set
            {
                fields = value;
                SetValueParser(fields);
            }
        }
        internal IEnumerable<List<string>> Records
        {
            get => records;
            set
            {
                records = value;
                _ = ConvertValue();
            }
        }

        internal DataSet DataSetXer
        {
            set
            {
                dsXer = value;
                dsXer.TryGetTable(table_name, out table);
            }
        }

        /// <summary>
        /// Link to the task for converting Xer element strings, supports waiting
        /// </summary>
        public Task TaskParsing { get; private set; }
        #endregion

        #region ValueParser
        private static void WriteErrorLog(List<string> errorLog, string value, int row, string error, string cTable, string field)
        {
            string eString = $"Строка: {row}  Ошибка: {error} Таблица: {cTable} Поле: {field} Значение: {value}";
            errorLog.Add(eString);
        }

        private async Task ConvertValue()
        {
            TaskParsing = Task.Run(() =>
                {
                    int idx = 0;
                    foreach (List<string> record in records)
                    {
                        string value = string.Empty;
                        string field = string.Empty;
                        try
                        {
                            DataRow row = table.NewRow();
                            for (int i = 0; i < parsers.Count; i++)
                            {
                                value = record[i];
                                field = parsers[i].Name;
                                row[field] = parsers[i].ValueParse(value);
                            }
                            table.Rows.Add(row);
                        }
                        catch (Exception ex)
                        {
                            WriteErrorLog(ErrorLog, value, idx, ex.Message, TableName, field);
                        }
                        idx++;
                    }
                    records = null;
                    OnInitialised(new InitializeEventArgs(stopwatch.Elapsed));
                });
            await TaskParsing;
            stopwatch.Stop();
            Debug.WriteLine($"{TableName,-10}: {stopwatch.Elapsed}");
            GC.Collect();
        }

        private void SetValueParser(IEnumerable<string> fields)
        {
            parsers = [];
            foreach (string f in fields)
            {
                if (table.Columns[f] is DataColumn column)
                {
                    parsers.Add(new(column));
                }
            }
        }
        #endregion

        #region Events

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Стили именования", Justification = "<Ожидание>")]
        private event EventHandler<InitializeEventArgs> onInitialized;

        /// <summary>
        /// Internal method for event initiation event Initialized
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnInitialised(InitializeEventArgs e)
        {
            IsInicialized = true;
            onInitialized?.Invoke(this, e);
        }

        /// <summary>
        ///  The event occurs at the end of reading and converting current Xer Element.
        /// </summary>
        public event EventHandler<InitializeEventArgs> Initialized
        {
            add => onInitialized += value;
            remove => onInitialized -= value;
        }
        #endregion 
    }
}
