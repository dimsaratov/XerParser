using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Security;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;

using XerParser.Enums;

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
        public const string NoExport = "NoExport";
        private static readonly string dblQuote = new('"', 2);
        private const string nonPrintablePattern = @"[\x00-\x09\x0B-\x1F]";
        private const string replacementChar = "#";
        private const string datetimeformat = @"yyyy-MM-dd HH:mm";
        private const string decformat = "0.########";
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
        private static string fullName;
        private ParsingTables table_names;
        private bool isLoading = false;

        internal static NumberFormatInfo NumberFormat = new()
        {
            NumberDecimalSeparator = @".",
        };
        #endregion

        /// <summary>
        /// Constructor with reading schema Xer
        /// </summary>
        public Parser()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            PathSchemaXER = Path.Combine(AppContext.BaseDirectory, "Schemas", "SchemaXer.xsd");
            ParseCounter = new Counter();
            ParseCounter.PropertyChanged += Counter_PropertyChanged;
        }

        /// <summary>
        /// Constructor with readed schema Xer
        /// </summary>
        /// <param name="schemaXer">
        /// Schema of the Xer format dataset
        /// </param>
        public Parser(DataSet schemaXer)
        {

            ArgumentNullException.ThrowIfNull(schemaXer);
            this.schemaXer = schemaXer;
            ParseCounter = new Counter();
            ParseCounter.PropertyChanged += Counter_PropertyChanged;
        }

        #region Property
        /// <summary>
        /// Default ignored table names. Default "OBS", "POBS", "RISKTYPE"
        /// </summary>
        public string[] DefaultIgnoredTable { get; set; } = ["OBS", "POBS", "RISKTYPE"];

        [DefaultValue(PrimaveraVersion.Primavera188)]
        public static PrimaveraVersion VersionPM { get; set; } = PrimaveraVersion.Primavera188;

        public static string Creator { get; set; } = "XerParser";

        /// <summary>
        /// Path to file schema of the Xer format dataset
        /// </summary>
        [Description(Messages.PathSchemaXER)]
        public string PathSchemaXER
        {
            get => pathSchemaXer;
            set
            {
                if (System.IO.File.Exists(value))
                {
                    if (pathSchemaXer != value || schemaXer is null)
                    {
                        pathSchemaXer = value;
                        schemaXer = new("dsXER");
                        try
                        {
                            schemaXer.ReadXmlSchema(pathSchemaXer);
                            schemaXer.ReplaceTableTask();
                            schemaXer.AcceptChanges();
                        }
                        catch (Exception ex)
                        {
                            ErrorLog.Add(ex.Message);
                        }
                    }
                }
                else
                {
                    ErrorLog.Add($"The xer schema file was not found: {value}");
                    schemaXer = null;
                    pathSchemaXer = string.Empty;
                }
            }
        }
        /// <summary>
        /// Return status loaded schema xer
        /// </summary>
        public bool IsSchemaLoaded => schemaXer is not null && schemaXer.Tables.Count > 0;

        /// <summary>
        /// Flag adding columns related to a value
        /// </summary>
        public bool CreateRelationColumns { get; set; } = false;

        /// <summary>
        /// Columns related to the value have been added
        /// </summary>
        public bool IsRelationColumnAdded { get; private set; } = false;

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
        public Counter ParseCounter { get; }

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
        /// The function of  reading a Xer file and converting it into a dataset
        /// </summary>
        /// <param name="fileName">path Xer file</param>
        public async Task LoadXer(string fileName)
        {
            ArgumentNullException.ThrowIfNull(schemaXer);
            if (isLoading)
            {
                return;
            }
            isLoading = true;
            table_names = new();
            sw = Stopwatch.StartNew();
            IsRelationColumnAdded = false;
            DataSetXer = SchemaXer;
            SetIgnoredTables();

            await Task.Run(() => XerElements = [.. InternalParse(fileName, dataSet)]);

            IEnumerable<Task> tasks = from x in XerElements
                                      where !x.IsInicialized
                                      select x.TaskParsing;
            Task p = Task.WhenAll(tasks);

            int noIni = tasks.Count();

            await Task.Run(() =>
            {
                int rem;
                ParseCounter.Maximum = (from x in XerElements
                                        where !x.IsInicialized && !x.IsErrors &&
                                              !(x.TaskParsing.Status == TaskStatus.RanToCompletion)
                                        select x).Sum(i => i.Remains);
                while (noIni > 0)
                {
                    Thread.Sleep(100);
                    rem = (from x in XerElements
                           where !x.IsInicialized
                           select x).Sum(i => i.Remains);
                    ParseCounter.Value = ParseCounter.Maximum - rem;
                    IEnumerable<XerElement> rem_xe = XerElements.Where(i => !i.IsInicialized && !i.IsErrors
                                                                         && !(i.TaskParsing.Status == TaskStatus.RanToCompletion));
                    noIni = rem_xe.Count();
                }
            });

            ErrorLog.AddRange(from x in XerElements
                              where x.IsErrors
                              select string.Join('\n', x.ErrorLog));

            if (RemoveEmptyTables)
            {
                RemoveEmpty();
            }

            if (CreateRelationColumns)
            {
                await SetParentDataColumn();
            }

            sw.Stop();
            OnInitializationСompleted(new InitializingEventArgs(sw.Elapsed));
            isLoading = false;
            GC.Collect();
        }

        public void RemoveEmpty()
        {
            DataTable[] et = [.. from t in DataSetXer.Tables.Cast<DataTable>()
                                 where t.Rows.Count == 0
                                 select t];

            foreach (DataTable t in et)
            {
                DataRelation[] rel = [.. DataSetXer.Relations.OfType<DataRelation>()
                                                   .Where(r => r.ParentTable.TableName == t.TableName
                                                         || r.ChildTable.TableName == t.TableName)];
                foreach (DataRelation r in rel)
                {
                    dataSet.Relations.Remove(r);
                }
                DataSetXer.Tables.Remove(t);
            }

        }

        private bool AddRelationColumns(string tableName, string columnName, Type T, string expression)
        {
            try
            {
                if (DataSetXer.Tables.Contains(tableName))
                {
                    DataColumnCollection collection = DataSetXer.Tables[tableName].Columns;
                    DataColumn column = collection.Add(columnName, T, expression);
                    column.ExtendedProperties.Add(NoExport, true);
                    column.ReadOnly = true;
                    return true;
                }
            }
            catch { }
            return false;
        }

        /// <summary>
        /// Adding columns related to a value
        /// </summary>
        public async Task SetParentDataColumn()
        {
            Stopwatch sw = Stopwatch.StartNew();
            await Task.Run(() =>
            {
                try
                {
                    AddRelationColumns("UDFVALUE", "table_name", typeof(string), "Parent(rel_udf_type).[table_name]");
                    AddRelationColumns("UDFVALUE", "udf_type_name", typeof(string), "Parent(rel_udf_type).[udf_type_name]");
                    AddRelationColumns("UDFVALUE", "udf_type_label", typeof(string), "Parent(rel_udf_type).[udf_type_label]");
                    //AddRelationColumns("UDFVALUE", "udf_value",
                    //                   "IIF(Parent(rel_udf_type).[logical_data_type]='FT_TEXT', udf_text, " +
                    //                   "IIF(Parent(rel_udf_type).[logical_data_type] LIKE '*DATE', udf_date, " +
                    //                   "IIF(Parent(rel_udf_type).[logical_data_type]= 'FT_STATICTYPE', udf_text, udf_number)))");


                    AddRelationColumns("DOCUMENT", "doc_catg_name", typeof(string), "Parent(rel_doc_catg).[doc_catg_name]");
                    AddRelationColumns("DOCUMENT", "doc_status_code", typeof(string), "Parent(rel_doc_stat).[doc_status_code]");

                    AddRelationColumns("TASKDOC", "doc_catg_name", typeof(string), "Parent(rel_doc_task).[doc_catg_name]");
                    AddRelationColumns("TASKDOC", "doc_status_code", typeof(string), "Parent(rel_doc_task).[doc_status_code]");
                    AddRelationColumns("TASKDOC", "doc_short_name", typeof(string), "Parent(rel_doc_task).[doc_short_name]");
                    AddRelationColumns("TASKDOC", "doc_name", typeof(string), "Parent(rel_doc_task).[doc_name]");
                    AddRelationColumns("TASKDOC", "doc_seq_num", typeof(int), "Parent(rel_doc_task).[doc_seq_num]");

                    AddRelationColumns("TASKACTV", "actv_code_type", typeof(string), "Parent(rel_actv_type_task).[actv_code_type]");
                    AddRelationColumns("TASKACTV", "short_name", typeof(string), "Parent(rel_actv_code_task).[short_name]");
                    AddRelationColumns("TASKACTV", "actv_code_name", typeof(string), "Parent(rel_actv_code_task).[actv_code_name]");

                    IsRelationColumnAdded = true;
                }
                catch (Exception ex)
                {
                    ErrorLog.Add("Создание связанных полей: " + ex.Message);
                }
            });
            OnCreatedRelationColumns(new InitializingEventArgs(sw.Elapsed));
        }

        private IEnumerable<XerElement> InternalParse(string fileName, DataSet dsXer)
        {
            ParseCounter.Reset();
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
                            OnReaded(new(e, false));
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
                        ParseCounter.Message = $"{Messages.Reading} {tblName}";
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
                        if (reader.BaseStream.CanRead) { ParseCounter.Value = reader.BaseStream.Position; }
                        if (ignore)
                        {
                            continue;
                        }
                        string[] record = [.. line.Skip(1)];
                        e.records.Add(record);
                        break;
                    case end:
                        if (e is not null)
                        {
                            e.records.CompleteAdding();
                            yield return e;
                            ParseCounter.Value = 0;
                            OnReaded(new(e, true));
                        }
                        break;
                }
            }
        }

        private IEnumerable<string> ReadLines(string fileName)
        {
            FileInfo fileInfo = new(fileName);
            ParseCounter.Maximum = fileInfo.Length;
            using StreamReader sr = new(fileName, Encoding);
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
            return await Parser.BuildXerFile(DataSetXer, path, ErrorLog, RemoveEmptyTables, ParseCounter);
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
                                                  VersionPM.GetDescription(),
                                                  DateTime.Now.ToString("yyyy-MM-dd"),
                                                  "Project" ,
                                                  userName,
                                                  FullName,
                                                  Creator,
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
                        IEnumerable<string> fields = from c in dTable.Columns.Cast<DataColumn>()
                                                     where (!c.ExtendedProperties.ContainsKey("NoExport") ||
                                                     !(bool)c.ExtendedProperties["NoExport"]) && !c.ReadOnly
                                                     select c.ColumnName;

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
                sb.Append(rec);
                foreach (string field in fields)
                {
                    object value = row[field];
                    if (value != DBNull.Value && value is DateTime time)
                    {
                        sb.Append(ASC_TAB_CHAR);
                        sb.Append(time.ToString(datetimeformat));
                    }
                    else if (value != DBNull.Value && value is int)
                    {
                        sb.Append(ASC_TAB_CHAR);
                        sb.Append(value.ToString());
                    }
                    else if (value != DBNull.Value && value is decimal v)
                    {
                        sb.Append(ASC_TAB_CHAR);
                        sb.Append(v.ToString(decformat));
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

        private void E_Initialised(object sender, InitializingEventArgs e)
        {
            XerElement el = sender as XerElement;
            table_names.Remove(el.TableName);
            if (table_names.TryPop(out string name))
            {
                ParseCounter.Message = $"{Messages.Parsing} {name}";
            }
            OnInitialization(new InitializedEventArgs(sender as XerElement));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Стили именования", Justification = "<Ожидание>")]
        private event EventHandler<InitializedEventArgs> onInitializing;

        /// <summary>
        /// The event occurs at the end of reading and converting a separate table from a Xer file.
        /// </summary>
        /// <param name="e">InitializingEventArgs</param>
        protected internal virtual void OnInitialization(InitializedEventArgs e)
        {
            onInitializing?.Invoke(this, e);
        }

        /// <summary>
        /// The event occurs at the end of reading and converting a separate table from a Xer file.
        /// </summary>
        public event EventHandler<InitializedEventArgs> Initialization
        {
            add => onInitializing += value;
            remove => onInitializing -= value;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Стили именования", Justification = "<Ожидание>")]
        private event EventHandler<InitializingEventArgs> onInitializationСompleted;

        /// <summary>
        /// The event occurs at the end of reading and converting the Xer file into a dataset.
        /// </summary>
        /// <param name="e">InitializeEventArgs</param>
        protected internal virtual void OnInitializationСompleted(InitializingEventArgs e)
        {
            onInitializationСompleted?.Invoke(this, e);
        }

        /// <summary>
        /// The event occurs at the end of reading and converting the Xer file into a dataset.
        /// </summary>
        public event EventHandler<InitializingEventArgs> InitializationСompleted
        {
            add => onInitializationСompleted += value;
            remove => onInitializationСompleted -= value;
        }

        private PropertyChangedEventHandler onCounterChanged = null;

        /// <summary>
        /// The event occurs at changed property.
        /// </summary>
        /// <param name="e">PropertyChangedEventArgs</param>
        protected internal virtual void OnCounterChanged(PropertyChangedEventArgs e)
        {
            onCounterChanged?.Invoke(this, e);
        }

        /// <summary>
        /// The event occurs at changed property.
        /// </summary>
        public event PropertyChangedEventHandler CounterChanged
        {
            add => onCounterChanged += value;
            remove => onCounterChanged -= value;
        }

        private EventHandler<ReadedEventArgs> onReaded = null;

        /// <summary>
        /// The event occurs at readed.
        /// </summary>
        /// <param name="e">InitializeEventArgs</param>
        protected internal virtual void OnReaded(ReadedEventArgs e)
        {
            table_names.Push(e.XerElement.TableName);
            onReaded?.Invoke(this, e);
        }

        /// <summary>
        /// The event occurs at changed property.
        /// </summary>
        public event EventHandler<ReadedEventArgs> Readed
        {
            add => onReaded += value;
            remove => onReaded -= value;
        }

        private event EventHandler<InitializingEventArgs> onCreatedRelationColumns;

        /// <summary>
        /// Internal method for event at the end of create Relation Data Columns.
        /// </summary>
        internal virtual void OnCreatedRelationColumns(InitializingEventArgs e)
        {
            onCreatedRelationColumns?.Invoke(this, e);
        }

        /// <summary>
        ///  The event occurs at the end of create Relation Data Columns.
        /// </summary>
        public event EventHandler<InitializingEventArgs> CreatedRelationColumns
        {
            add => onCreatedRelationColumns += value;
            remove => onCreatedRelationColumns -= value;
        }

        #endregion

        #region Dispose
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
