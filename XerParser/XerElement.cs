﻿using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;

namespace XerParser
{

    /// <summary>
    /// Wrapper for readable element of the Xer file
    /// </summary>
    /// <remarks>
    /// Wrapper for readable element of the Xer file
    /// </remarks>
    public class XerElement
    {
        #region Variable

        private DataTable table;
        private string[] fields;
        private readonly Dictionary<string, DataSetter> setters = [];
        private readonly string table_name;
        private DataSet dsXer;
        private readonly Stopwatch stopwatch = Stopwatch.StartNew();
        internal BlockingCollection<string[]> records = [];
        #endregion

        /// <summary>
        /// Wrapper for readable element of the Xer file
        /// </summary>
        /// <remarks>
        /// Wrapper for readable element of the Xer file
        /// </remarks>
        /// <param name="tableName">Table name in Xer file</param>
        /// <param name="withFullLog">
        /// Fix all possible errors, running slower
        /// </param>
        public XerElement(string tableName, bool withFullLog)
        {
            this.table_name = tableName;
            TaskParsing = withFullLog ? ParseWithLog() : Parse();
        }

        #region Property

        /// <summary>
        /// A flag indicating whether the xer element has been read and converted
        /// </summary>
        public bool IsInicialized { get; private set; }

        /// <summary>
        ///  Error sheet when converting fields from XER element
        /// </summary>
        public List<string> ErrorLog { get; } = [];

        /// <summary>
        /// The number of rows of the Xer element table entries
        /// </summary>

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
                fields = [.. value];
                DefineSetters(fields);
            }
        }

        internal DataSet DataSetXer
        {
            set
            {
                dsXer = value;
                if (dsXer.TryGetTable(table_name, out table))
                {
                    table.BeginLoadData();
                }
            }
        }

        /// <summary>
        /// Link to the task for converting Xer element strings, supports waiting
        /// </summary>
        public Task TaskParsing { get; }
        #endregion

        #region ValueParser

        private static void WriteErrorLog(List<string> errorLog, string value, int row, string error, string cTable, string field)
        {
            string eString = $"Строка: {row}  Ошибка: {error} Таблица: {cTable} Поле: {field} Значение: {value}";
            errorLog.Add(eString);
        }

        private async Task ParseWithLog()
        {
            await Task.Run(() =>
            {
                int idx = 0;
                while (!records.IsCompleted)
                {
                    if (records.TryTake(out string[] rec))
                    {
                        idx++;
                        DataRow row = table.NewRow();
                        foreach (string f in fields)
                        {
                            DataSetter setter = setters[f];
                            {
                                string value = rec.TryGet(setter.Index);
                                try
                                {
                                    row[f] = setter.Value(value);
                                }
                                catch (Exception ex)
                                {
                                    WriteErrorLog(ErrorLog, value, idx, ex.Message, TableName, f);
                                }
                            }
                        }
                    }
                }
            });
            OnInitialised(new(stopwatch.Elapsed));
        }



        private async Task Parse()
        {
            await Task.Run(() =>
            {
                int idx = 0;
                while (!records.IsCompleted)
                {
                    if (records.TryTake(out string[] rec))
                    {
                        try
                        {
                            table.LoadDataRow([.. ParseRecord(rec)], true);
                        }
                        catch (Exception ex)
                        {
                            ErrorLog.Add($"{TableName} index row[{idx}] Error:{ex.Message}");
                        }
                        idx++;
                    }
                }
            });
            OnInitialised(new(stopwatch.Elapsed));
        }


        private IEnumerable<object> ParseRecord(string[] record)
        {
            foreach (KeyValuePair<string, DataSetter> setter in setters)
            {
                yield return setter.Value.Value(record.TryGet(setter.Value.Index));
            }
        }

        private void DefineSetters(string[] fields)
        {
            foreach (DataColumn column in table.Columns)
            {
                int idx = Array.IndexOf(fields, column.ColumnName);
                if (idx == -1)
                {
                    setters.Add(column.ColumnName, new(column));
                }
                else
                {
                    setters.Add(column.ColumnName, new(column, idx));
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
        internal virtual void OnInitialised(InitializeEventArgs e)
        {
            IsInicialized = true;
            table.EndLoadData();
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
