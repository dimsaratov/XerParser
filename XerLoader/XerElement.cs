using System.Data;
using System.Diagnostics;
using System.Globalization;

namespace XerLoader
{
    public class XerElement(string tableName)
    {
        #region Variable
        readonly HashSet<Tuple<int, ValueParse>> valueParsers = [];
        public delegate bool ValueParse(string value, out object parsedValue);

        private IEnumerable<string> fieldNames;
        DataTable table;
        private IEnumerable<List<string>> records;
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
            get => fieldNames;
            set
            {
                fieldNames = value;
                SetValueParser();
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
                    int row = 0;
                    foreach (var record in records)
                    {
                        string field = string.Empty;
                        string value = string.Empty;
                        int i = 0;
                        try
                        {
                            DataRow dr = table.NewRow();
                            string[] names = fieldNames.ToArray();
                            foreach (Tuple<int, ValueParse> v in valueParsers)
                            {
                                field = names[i];
                                value = record[i];
                                if (value.Length == 0)
                                    dr[v.Item1] = DBNull.Value;
                                else
                                {
                                    if (v.Item2(value, out object parsedValue))
                                        dr[v.Item1] = parsedValue;
                                    else
                                        WriteErrorLog(errorLog, value, row,
                                                      $"Ожидалось значение конвертируемое в {table.Columns[v.Item1].DataType}",
                                                      TableName, field);
                                }
                                i++;
                            }
                            table.Rows.Add(dr);
                            row++;
                        }
                        catch (Exception ex)
                        {
                            WriteErrorLog(errorLog, value, row, ex.Message, TableName, field);
                        }
                    }
                    records = null;
                    OnInitialised(new InitializeEventArgs(stopwatch.Elapsed));
                });
            await task;
            stopwatch.Stop();
            Debug.WriteLine($"{TableName,-10}: {stopwatch.Elapsed}");
            GC.Collect();
        }

        void SetValueParser()
        {
            foreach (string field in fieldNames)
            {
                int idx = table.Columns.IndexOf(field);
                if (idx < 0)
                    continue;
                else
                {
                    ValueParse valueParse = new(StringParse);
                    DataColumn column = table.Columns[idx];

                    valueParse = column.DataType.Name switch
                    {
                        "DateTime" => new ValueParse(DateTimeParse),
                        "Int32" => new ValueParse(IntParse),
                        "Decimal" => new ValueParse(DecimalParse),
                        "Boolean" => new ValueParse(BoolParse),
                        _ => new ValueParse(StringParse),
                    };
                    valueParsers.Add(new Tuple<int, ValueParse>(idx, valueParse));
                }
            }
        }


        static bool DecimalParse(string value, out object parsedValue)
        {
            parsedValue = DBNull.Value;
            if (decimal.TryParse(value, NumberStyles.Float, XerParser.NumberFormat, out decimal n))
            {
                parsedValue = n;
                return true;
            }
            return false;
        }

        static bool DateTimeParse(string value, out object parsedValue)
        {
            parsedValue = DBNull.Value;
            if (DateTime.TryParse(value, out DateTime d))
            {
                parsedValue = d;
                return true;
            }
            return false;
        }
        static bool StringParse(string value, out object parsedValue)
        {
            parsedValue = value;
            return true;
        }
        static bool IntParse(string value, out object parsedValue)
        {
            parsedValue = DBNull.Value;
            if (int.TryParse(value, out int i))
            {
                parsedValue = i;
                return true;
            }
            return false;
        }

        static bool BoolParse(string value, out object parsedValue)
        {
            parsedValue = DBNull.Value;
            if (bool.TryParse(value, out bool b))
            {
                parsedValue = b;
                return true;
            }
            return false;
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
