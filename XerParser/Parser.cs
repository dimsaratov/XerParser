using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Security;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;

namespace XerParser

{
    /// <summary>
    /// Reading a Xer file to a DataTable and writing a DataTable to an Xer file
    /// </summary>
    public partial class Parser : IDisposable
    {
        #region Variable
        private enum State { outQuote, inQuote, mayBeOutQuote }
        private const char ASC_TAB_CHAR = '\t';
        private const char end1 = '\r';
        private const char end2 = '\n';
        private const char quote = '"';
        private const string rec = "%R";
        private const string tbl = "%T";
        private const string fld = "%F";
        private const string end = "%E";
        private static readonly string dblQuote = new('"', 2);
        private const string nonPrintablePattern = @"[\x00-\x09\x0B-\x1F]";
        private const string replacementChar = "#";
        private static readonly Regex nonPrintable = new(nonPrintablePattern, RegexOptions.Compiled);
        private DataSet dataSet;
        private NumberDecimalSeparator numberDecimalSeparator;
        private StreamReader reader;
        /// <summary>
        /// DataSet Schema Xer
        /// </summary>
        protected internal DataSet schemaXer;
        private bool disposedValue;
        private string pathSchemaXer;

        internal static NumberFormatInfo NumberFormat = new()
        {
            NumberDecimalSeparator = @".",
        };
        private Counter counter;
        #endregion

        /// <summary>
        /// Constructor with reading schema Xer
        /// </summary>
        /// <param name="pathSchemaXer">
        /// The path to the dataset schema file in
        /// the default xsd format Schumaxer.xsd
        /// </param>
        public Parser(string pathSchemaXer)
        {
#if NET6_0_OR_GREATER
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
#endif
            this.PathSchemaXER = pathSchemaXer;
        }

        /// <summary>
        /// Constructor with readed schema Xer
        /// </summary>
        /// <param name="schemaXer">
        /// Schema of the Xer format dataset
        /// </param>
        public Parser(DataSet schemaXer) : this("")
        {
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(schemaXer);
            this.schemaXer = schemaXer;
#else
            if (schemaXer == null)
            {
                throw new ArgumentNullException(nameof(schemaXer));
            }
#endif
        }
        #region Property
        /// <summary>
        /// Default ignored table names. Default "OBS", "POBS", "RISKTYPE"
        /// </summary>
        public string[] DefaultIgnoredTable { get; set; } = ["OBS", "POBS", "RISKTYPE"];

        /// <summary>
        /// Path to file schema of the Xer format dataset
        /// </summary>
        [Description("Путь к файлу схемы XER Примавера")]
        public string PathSchemaXER
        {
            get => pathSchemaXer;
            set
            {
                if (System.IO.File.Exists(value) && pathSchemaXer != value)
                {
                    pathSchemaXer = value;
                    schemaXer = new("dsXER");
                    schemaXer.ReadXmlSchema(pathSchemaXer);
                }
                else
                {
                    schemaXer = null;
                    pathSchemaXer = string.Empty;
                }
            }
        }

        /// <summary>
        /// Schema of the Xer format dataset
        /// </summary>
        public DataSet SchemaXer => schemaXer?.Clone();

        /// <summary>
        /// Hashset is the name of the tables that are ignored when reading the Xer file.
        /// Default value get and set in DefaultIgnoredTable
        /// </summary>
        public HashSet<string> IgnoredTable { get; } = [];

        /// <summary>
        /// Hashset are the names of the tables that will be loaded when reading the Xer file.
        /// </summary>
        public HashSet<string> LoadedTable { get; } = [];


        /// <summary>
        /// Xer file encoding, Windows 1251 by default
        /// </summary>
        public static Encoding Encoding { get; set; } = Encoding.GetEncoding(1251);

        /// <summary>
        ///An array of wrappers for readable elements of the Xer file
        /// </summary>
        public XerElement[] XerElements { get; private set; }

        private Stopwatch sw;

        /// <summary>
        /// The data set of the read Xer file
        /// </summary>
        public DataSet DataSetXer
        {
            get => dataSet;
            private set
            {
                dataSet = value;
                SetIgnoredTables();
            }
        }

        /// <summary>
        /// Escape string values using the RFC4180 format
        /// </summary>
        public static bool EscapeSpecialCharsXml { get; set; } = false;

        /// <summary>
        /// Delete tables with no records, false by default;
        /// </summary>
        public bool RemoveEmptyTables { get; set; } = false;

        /// <summary>
        /// Error sheet when converting fields from XER file
        /// </summary>
        public List<string> ErrorLog { get; } = [];

        /// <summary>
        /// The currency for building the file
        /// </summary>
        public static string Currence { get; set; } = "RUB";

        private static string FullName
        {
            get
            {
                fullName ??= WindowsIdentity.GetCurrent().Name;
                return fullName;
            }
            set => fullName = value;
        }

        /// <summary>
        /// Separator for decimal values
        /// </summary>
        public NumberDecimalSeparator NumberDecimalSeparator
        {
            get => numberDecimalSeparator;
            set
            {
                if (value != numberDecimalSeparator)
                {
                    numberDecimalSeparator = value;
                    string separator = numberDecimalSeparator == NumberDecimalSeparator.Point ? "." : ",";
                    NumberFormat = new NumberFormatInfo()
                    {
                        NumberDecimalSeparator = separator
                    };
                }
            }
        }

        /// <summary>
        /// A counter for tracking the process of loading a Xer file
        /// </summary>
        public Counter ProgressCounter
        {
            get
            {
                if (counter == null)
                {
                    counter = new Counter();
                    counter.PropertyChanged += Counter_PropertyChanged;
                }
                return counter;
            }
            set => counter = value;
        }

        /// <summary>
        /// Fix all possible errors, running slower
        /// </summary>  
        public bool WithFullLog { get; set; }

        private void Counter_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnCounterChanged(e);
        }

        #endregion


        #region Parse

        /// <summary>
        /// The function of reading a Xer file and converting it into a dataset
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="withFullLog">
        /// Fix all possible errors, running slower
        /// </param>
        public async Task LoadXer(string fileName, bool withFullLog)
        {
            WithFullLog = withFullLog;
            await LoadXer(fileName);
        }

        /// <summary>
        /// The function of reading a Xer file and converting it into a dataset,
        /// returns the presence of parsing errors
        /// </summary>
        /// <param name="fileName">path Xer file</param>
        /// <param name="errorLog">List error</param>         
        public async Task<bool> LoadXer(string fileName, List<string> errorLog)
        {
            await LoadXer(fileName);
            errorLog ??= [];
            errorLog.AddRange(ErrorLog);
            return errorLog.Count == 0;
        }

        /// <summary>
        /// The function of reading a Xer file and converting it into a dataset
        /// </summary>
        /// <param name="fileName">path Xer file</param>
        public async Task LoadXer(string fileName)
        {
            sw = Stopwatch.StartNew();
            DataSetXer = SchemaXer;
            SetIgnoredTables();

            await Task.Run(() => XerElements = [.. InternalParse(fileName, dataSet)]);

            IEnumerable<Task> tasks = from x in XerElements
                                      where !x.IsInicialized
                                      select x.TaskParsing;

            await Task.WhenAll(tasks);

            ErrorLog.AddRange(from x in XerElements
                              where x.IsErrors
                              select string.Join('\n', x.ErrorLog));

            sw.Stop();
            OnInitializationСompleted(new InitializeEventArgs(sw.Elapsed));
            GC.Collect();
        }


        private IEnumerable<XerElement> InternalParse(string fileName, DataSet dsXer)
        {
            ProgressCounter.Reset();
            XerElement e = null;
            bool w_FullLog = WithFullLog;
            int remUpload = Math.Max(dsXer.Tables.Count - IgnoredTable.Count, LoadedTable.Count);
            bool ignore = false;

            foreach (IEnumerable<string> line in Parse(ReadLines(fileName)))
            {
                string flag = line.First();
                switch (flag)
                {
                    case tbl:
                        if (e is not null)
                        {
                            e.records.CompleteAdding();
                            yield return e;
                            e = null;
                        }
                        if (remUpload == 0)
                        {
                            yield break;
                        }
                        string tblName = line.Last();
                        if (IgnoredTable.Contains(tblName))
                        {
                            ignore = true;
                            continue;
                        }
                        else
                        {
                            ignore = false;
                        }
                        e = new(tblName, w_FullLog)
                        {
                            DataSetXer = dsXer
                        };
                        ProgressCounter.Message = $"чтение {tblName}";
                        remUpload--;
                        e.Initialized += E_Initialised;
                        break;
                    case fld:
                        if (ignore)
                        {
                            continue;
                        }
                        e.FieldNames = line.Skip(1);
                        break;
                    case rec:
                        if (reader.BaseStream.CanRead) { ProgressCounter.Value = reader.BaseStream.Position; }
                        if (ignore)
                        {
                            continue;
                        }
                        e.records.Add([.. line.Skip(1)]);
                        break;
                    case end:
                        if (e is not null)
                        {
                            e.records.CompleteAdding();
                            yield return e;
                            ProgressCounter.Message = $"парсинг {e.TableName}";
                        }
                        break;
                }
            }
        }

        private IEnumerable<string> ReadLines(string fileName)
        {
            FileInfo fileInfo = new(fileName);
            ProgressCounter.Maximum = fileInfo.Length;
            ProgressCounter.Message = fileInfo.Name;
            using StreamReader sr = new(fileName, Encoding.GetEncoding(1251));
            reader = sr;
            while (sr.Peek() >= 0)
            {
                yield return sr.ReadLine();
            }
        }

        private static IEnumerable<IEnumerable<string>> Parse(IEnumerable<string> lines)
        {
            IEnumerator<string> e = lines.GetEnumerator();
            while (e.MoveNext())
            {
                yield return ParseLine(e);
            }
        }

        private static IEnumerable<string> ParseLine(IEnumerator<string> e)
        {
            StringBuilder token = new();
            State state = State.outQuote;
        again:
            foreach (char c in e.Current)
            {
                switch (state)
                {
                    case State.outQuote:
                        if (c == ASC_TAB_CHAR)
                        {
                            yield return token.ToString();
                            token = new();
                        }
                        else if (c == quote)
                        {
                            state = State.inQuote;
                        }
                        else if (c == end1)
                        {
                            break;
                        }
                        else if (c == end2)
                        {
                            break;
                        }
                        else
                        {
                            token.Append(c);
                        }

                        break;
                    case State.inQuote:
                        if (c == quote)
                        {
                            state = State.mayBeOutQuote;
                        }
                        else
                        {
                            token.Append(c);
                        }

                        break;
                    case State.mayBeOutQuote:
                        if (c == quote)
                        {
                            //кавычки внутри кавычек
                            state = State.inQuote;
                            token.Append(c);
                        }
                        else
                        {
                            state = State.outQuote;
                            goto case State.outQuote;
                        }
                        break;
                }
            }
            //разрыв строки внутри кавычек
            if (state == State.inQuote && e.MoveNext())
            {
                goto again;
            }

            yield return token.ToString();
        }
        #endregion

        #region Build

        /// <summary>
        ///  Xer file building method
        /// </summary>
        /// <param name="path">
        /// The path where the Xer file will be created
        /// </param>
        /// <returns>
        /// Returns the result of successful recording of the Xer file
        /// </returns>
        public async Task<bool> BuildXerFile(string path)
        {
            return await Parser.BuildXerFile(DataSetXer, path, ErrorLog, RemoveEmptyTables, ProgressCounter);
        }

        /// <summary>
        /// Xer file building method
        /// </summary>
        /// <param name="dataSetXer">
        /// A set of data to write to a Xer file
        /// </param>
        /// <param name="path">
        /// The path where the Xer file will be created
        /// </param>
        /// <param name="counter">
        /// A counter for tracking the process of building a Xer file
        /// </param>
        /// <param name="errorLog">
        /// A list of error lines for writing the XER file
        /// </param>
        /// <param name="removeEmptyTables">
        /// Don't save empty tables
        /// </param>
        /// <returns>
        /// Returns the result of successful recording of the Xer file
        /// </returns>
        public static async Task<bool> BuildXerFile(DataSet dataSetXer, string path, List<string> errorLog, bool removeEmptyTables, Counter counter = null)
        {
            return !string.IsNullOrWhiteSpace(path) && dataSetXer is not null
            && await Task.Run(() =>
            {
                string userName = Environment.UserName;
                counter ??= new Counter();
                counter.Reset();
                errorLog ??= [];

                using StreamWriter eXerFile = new(path, false, Encoding);
                try
                {
                    //xer header (TP/P3e)...
                    string header = string.Join(ASC_TAB_CHAR.ToString(),
                                                [ "ERMHDR",
                                              "19.12" ,
                                              DateTime.Now.ToString("yyyy-MM-dd"),
                                              "Project" ,
                                              userName,
                                              FullName,
                                              "XerBuilder",
                                              "Project Management",
                                              Currence]);
                    eXerFile.WriteLine(header);

                    foreach (DataTable dTable in dataSetXer.Tables)
                    {
                        //make sure sheet has data...
                        if (removeEmptyTables && dTable.Rows.Count == 0)
                        {
                            continue;
                        }
                        dTable.AcceptChanges();
                        //builds an XER file from current datatable contents...
                        counter.Reset();
                        counter.Message = dTable.TableName;
                        counter.Maximum = dTable.Rows.Count;

                        //write table header...
                        eXerFile.WriteLine($"{tbl}\t{dTable.TableName}");

                        //write field header...
                        IEnumerable<string> fields = dTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName);
                        string f = $"{fld}\t{string.Join(ASC_TAB_CHAR.ToString(), fields)}";
                        eXerFile.WriteLine(f);

                        //write data rows...
                        IEnumerable<DataRow> records = dTable.Rows.OfType<DataRow>();
                        List<string> bedRecord = [];
                        foreach (string r in GetRecordString(records, fields, bedRecord))
                        {
                            eXerFile.WriteLine(r);
                            counter.Value++;
                        }
                        if (bedRecord.Count > 0) { errorLog.AddRange(bedRecord); }
                    }
                    counter.Reset();
                    //xer footer...
                    eXerFile.WriteLine("%E");
                    return true;
                }
                catch (Exception ex)
                {
                    errorLog.Add(ex.Message + "\n" + ex.StackTrace);
                    return false;
                }
            });
        }

        private static IEnumerable<string> GetRecordString(IEnumerable<DataRow> rows, IEnumerable<string> fields, List<string> badRecord)
        {
            foreach (DataRow row in rows)
            {
                StringBuilder sb = new();
                sb.Append("%R");
                foreach (string field in fields)
                {
                    object value = row[field];
                    if (value != DBNull.Value && value is DateTime time)
                    {
                        sb.Append(ASC_TAB_CHAR);
                        sb.Append(time.ToString(@"yyyy-MM-dd HH:mm"));
                    }
                    else if (value != DBNull.Value && value is int)
                    {
                        sb.Append(ASC_TAB_CHAR);
                        sb.Append(value.ToString());
                    }
                    else if (value != DBNull.Value && value is decimal v)
                    {
                        sb.Append(ASC_TAB_CHAR);
                        sb.Append(v.ToString("0.########"));
                    }
                    else
                    {
                        string val;
                        if (EscapeSpecialCharsXml)
                        {
                            val = SecurityElement.Escape(value.ToString());
                        }
                        else
                        {
                            val = value.ToString();
                            if (val.Contains('\n'))
                            {
                                val = val.Replace("\x0A", "");
                            }
                            //\n
                            if (val.Contains('\x0D'))
                            {
                                val = val.Replace("\x0D", "");
                            }
                            //\r
                            if (val.Contains('\x09'))
                            {
                                val = val.Replace("\x09", "");
                            }
                            //\t
                            if (val.Contains('\x22'))
                            {
                                val = val.Replace("\x22", dblQuote);
                            }
                            //Escaping quotation marks

                            if (nonPrintable.IsMatch(val) && badRecord != null)
                            {
                                val = nonPrintable.Replace(val, replacementChar);
                                badRecord.Add($"{row.Table.TableName}/{field}: {value}/{val}");
                            }
                        }
                        sb.Append(ASC_TAB_CHAR);
                        sb.Append(val);
                    }
                }
                yield return sb.ToString();
            }
        }
        #endregion

        #region Metods

        /// <summary>
        /// A method that sets a list of tables that are ignored when reading a Xer file
        /// </summary>
        /// <param name="ignoredTableNames">
        /// The name of the tables ignored when reading the Xer file
        /// </param>
        public void SetIgnoredTables(IEnumerable<string> ignoredTableNames)
        {
            foreach (string name in ignoredTableNames)
            {
                LoadedTable.Remove(name);
                IgnoredTable.Add(name);
            }
        }

        /// <summary>
        /// Set loaded table
        /// </summary>
        /// <param name="tableNames"></param>
        public void SetLoadedTable(IEnumerable<string> tableNames)
        {
            foreach (string name in tableNames) { LoadedTable.Add(name); }
        }

        private void SetIgnoredTables()
        {
            if (LoadedTable.Count == 0 || dataSet == null)
            {
                return;
            }
            foreach (DataTable tbl in dataSet.Tables)
            {
                if (!LoadedTable.Contains(tbl.TableName) &&
                    !IgnoredTable.Contains(tbl.TableName))
                {
                    IgnoredTable.Add(tbl.TableName);
                }
            }
        }

        /// <summary>
        /// The procedure for resetting table names that are ignored when loading to the default value
        /// </summary>
        public void ResetIgnoredTable()
        {
            LoadedTable.Clear();
            IgnoredTable.Clear();
            foreach (string str in DefaultIgnoredTable)
            {
                IgnoredTable.Add(str);
            }
        }

        #endregion

        #region Events

        private void E_Initialised(object sender, InitializeEventArgs e)
        {
            OnInitialization(new InitializingEventArgs(sender as XerElement, e.Elapsed));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Стили именования", Justification = "<Ожидание>")]
        private event EventHandler<InitializingEventArgs> onInitializing;

        /// <summary>
        /// The event occurs at the end of reading and converting a separate table from a Xer file.
        /// </summary>
        /// <param name="e">InitializingEventArgs</param>
        protected internal virtual void OnInitialization(InitializingEventArgs e)
        {
            onInitializing?.Invoke(this, e);
        }

        /// <summary>
        /// The event occurs at the end of reading and converting a separate table from a Xer file.
        /// </summary>
        public event EventHandler<InitializingEventArgs> Initialization
        {
            add => onInitializing += value;
            remove => onInitializing -= value;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Стили именования", Justification = "<Ожидание>")]
        private event EventHandler<InitializeEventArgs> onInitializationСompleted;

        /// <summary>
        /// The event occurs at the end of reading and converting the Xer file into a dataset.
        /// </summary>
        /// <param name="e">InitializeEventArgs</param>
        protected internal virtual void OnInitializationСompleted(InitializeEventArgs e)
        {
            onInitializationСompleted?.Invoke(this, e);
        }

        /// <summary>
        /// The event occurs at the end of reading and converting the Xer file into a dataset.
        /// </summary>
        public event EventHandler<InitializeEventArgs> InitializationСompleted
        {
            add => onInitializationСompleted += value;
            remove => onInitializationСompleted -= value;
        }

        private PropertyChangedEventHandler onCounterChanged = null;
        private static string fullName;

        /// <summary>
        /// The event occurs at changed property.
        /// </summary>
        /// <param name="e">PropertyChangedEventArgs</param>
        protected internal virtual void OnCounterChanged(PropertyChangedEventArgs e)
        {
            onCounterChanged?.Invoke(counter, e);
        }


        /// <summary>
        /// The event occurs at changed property.
        /// </summary>
        public event PropertyChangedEventHandler CounterChanged
        {
            add => onCounterChanged += value;
            remove => onCounterChanged -= value;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposing">
        /// true to release both managed and unmanaged resources; false to release only unmanaged
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (!disposing)
                {
                    schemaXer?.Dispose();
                }
            }
            disposedValue = true;
        }

        #endregion

    }
}
