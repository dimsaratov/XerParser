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
        private readonly DataSet schemaXer = new("dsXER");
        internal static readonly NumberFormatInfo NumberFormat = new()
        {
            NumberDecimalSeparator = @".",
        };
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
            schemaXer.ReadXmlSchema(pathSchemaXer);
        }

        /// <summary>
        /// Constructor with readed schema Xer
        /// </summary>
        /// <param name="schemaXer">
        /// Schema of the Xer format dataset
        /// </param>
        public Parser(DataSet schemaXer)
        {
            this.schemaXer = schemaXer;
        }


        #region Property
        /// <summary>
        /// Default ignored table names. Default "OBS", "POBS", "RISKTYPE"
        /// </summary>
        public string[] DefaultIgnoredTable { get; set; } = ["OBS", "POBS", "RISKTYPE"];

        private DataSet SchemaXer => schemaXer.Clone();

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
        public static bool RemoveEmptyTables { get; set; } = false;

        /// <summary>
        /// Error sheet when converting fields from XER file
        /// </summary>
        public static List<string> ErrorLog { get; } = [];

        /// <summary>
        /// 
        /// </summary>
        public static string Currence { get; set; } = "RUB";

        private static string FullName => WindowsIdentity.GetCurrent().Name;

        #endregion


        #region Parse
        /// <summary>
        /// The function of reading a Xer file and converting it into a dataset
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns>The path to the Xer file</returns>
        public async Task LoadXer(string fileName)
        {
            SetIgnoredTables();
            Stopwatch sw = Stopwatch.StartNew();
            DataSetXer = SchemaXer;
            await Task.Run(() => XerElements = InternalParse(fileName, dataSet).ToArray());

            IEnumerable<Task> tasks = from x in XerElements
                                      where !x.IsInicialized
                                      select x.TaskParsing;
            await Task.WhenAll(tasks);
            sw.Stop();
            OnInitializationСompleted(new InitializeEventArgs(sw.Elapsed));
            GC.Collect();
        }


        private IEnumerable<XerElement> InternalParse(string fileName, DataSet dsXer)
        {
            XerElement e = null;
            List<List<string>> records = null;

            int remUpload = Math.Max(dsXer.Tables.Count - IgnoredTable.Count,
                                                        LoadedTable.Count);
            bool ignore = false;
            foreach (List<string> line in Parse(ReadLines(fileName)))
            {
                string flag = line[0];
                switch (flag)
                {
                    case end:
                        e.Records = records;
                        yield return e;
                        break;
                    case tbl:
                        if (e != null)
                        {
                            e.Records = records;
                            yield return e;
                            if (remUpload == 0)
                            {
                                yield break;
                            }
                        }
                        if (IgnoredTable.Contains(line[1]))
                        {
                            ignore = true;
                            continue;
                        }
                        else
                        {
                            ignore = false;
                        }

                        e = new(line[1])
                        {
                            DataSetXer = dsXer
                        };
                        remUpload--;
                        e.Initialized += E_Initialised;
                        break;
                    case fld:
                        if (ignore)
                        {
                            continue;
                        }

                        e.FieldNames = line.Skip(1);
                        records = [];
                        break;
                    case rec:
                        if (ignore)
                        {
                            continue;
                        }
                        records.Add(line.Skip(1).ToList());
                        break;
                }
            }
        }

        private static IEnumerable<string> ReadLines(string fileName)
        {
            using StreamReader sr = new(fileName, Encoding.GetEncoding(1251));
            while (sr.Peek() >= 0)
            {
                yield return sr.ReadLine();
            }
        }

        private static IEnumerable<List<string>> Parse(IEnumerable<string> lines)
        {
            IEnumerator<string> e = lines.GetEnumerator();
            while (e.MoveNext())
            {
                yield return ParseLine(e);
            }
        }


        private static List<string> ParseLine(IEnumerator<string> e)
        {
            List<string> items = [.. GetToken(e)];
            return items;
        }

        private static IEnumerable<string> GetToken(IEnumerator<string> e)
        {
            string token = "";
            State state = State.outQuote;
        again:
            foreach (char c in e.Current)
            {
                switch (state)
                {
                    case State.outQuote:
                        if (c == ASC_TAB_CHAR)
                        {
                            yield return token;
                            token = "";
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
                            token += c;
                        }

                        break;
                    case State.inQuote:
                        if (c == quote)
                        {
                            state = State.mayBeOutQuote;
                        }
                        else
                        {
                            token += c;
                        }

                        break;
                    case State.mayBeOutQuote:
                        if (c == quote)
                        {
                            //кавычки внутри кавычек
                            state = State.inQuote;
                            token += c;
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

            yield return token;
        }
        #endregion

        #region Build
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataSetXer">
        /// A set of data to write to a Xer file
        /// </param>
        /// <param name="path">
        /// The path where the Xer file will be created
        /// </param>
        /// <returns>
        /// Returns the result of successful recording of the Xer file
        /// </returns>
        public static bool BuildXerFile(DataSet dataSetXer, string path)
        {
            string userName = Environment.UserName;

            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            //builds an XER file from current datatable contents...
            string sLine;

            //On Error GoTo buildXerFile_Error
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
                dataSetXer.AcceptChanges();


                foreach (DataTable dTable in dataSetXer.Tables)
                {
                    //make sure sheet has data...

                    if (dTable != null)
                    {
                        if (RemoveEmptyTables && dTable.Rows.Count == 0)
                        {
                            continue;
                        }
                        dTable.AcceptChanges();
                        //write table header...
                        sLine = "%T" + ASC_TAB_CHAR + dTable.TableName;
                        eXerFile.WriteLine(sLine);
                        sLine = "%F";

                        //write field header...
                        List<string> ArrColumn = [];

                        foreach (DataColumn dColumn in dTable.Columns)
                        {
                            sLine += ASC_TAB_CHAR + dColumn.ColumnName;
                            ArrColumn.Add(dColumn.ColumnName);
                        }

                        eXerFile.WriteLine(sLine);

                        //write data rows...
                        foreach (DataRow dRow in dTable.Rows)
                        {
                            string record = GetRecordString(dRow, ArrColumn);
                            eXerFile.WriteLine(record);
                        }
                    }
                }
                //xer footer...
                eXerFile.WriteLine("%E");
                return true;
            }
            catch (Exception ex)
            {
                ErrorLog.Add(ex.Message + "\n" + ex.StackTrace);
                return false;
            }
        }

        private static string GetRecordString(DataRow dRow, List<string> ArrColumn, List<string> badRecord = null)
        {
            StringBuilder sb = new();
            sb.Append("%R");

            foreach (string iColumn in ArrColumn)
            {
                object value = dRow[iColumn];
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
                            StringBuilder sb1 = new();
                            sb1.Append(dRow.Table.TableName);
                            sb1.Append('/');
                            sb1.Append(iColumn);
                            sb1.Append(": ");
                            sb1.Append(value.ToString());
                            sb1.Append(" / ");
                            sb1.Append(val);
                            badRecord.Add(sb1.ToString());
                        }
                    }
                    sb.Append(ASC_TAB_CHAR);
                    sb.Append(val);
                }
            }
            return sb.ToString();
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

        /// <inheritdoc/>
        public void Dispose()
        {
            schemaXer?.Dispose();
            GC.SuppressFinalize(this);
        }
        #endregion

    }
}
