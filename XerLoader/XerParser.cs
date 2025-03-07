using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace XerLoader

{
    public class XerParser
    {
        #region Variable
        enum State { outQuote, inQuote, mayBeOutQuote }

        const char ASC_TAB_CHAR = '\t';
        const char end1 = '\r';
        const char end2 = '\n';
        const char quote = '"';
        const string rec = "%R";
        const string tbl = "%T";
        const string fld = "%F";
        const string end = "%E";
        static readonly string dblQuote = new('"', 2);
        const string nonPrintablePattern = @"[\x00-\x09\x0B-\x1F]";
        const string replacementChar = "#";

        readonly static Regex nonPrintable = new(nonPrintablePattern, RegexOptions.Compiled);
        private DataSet dataSet;
        private XerElement[] xerElements;
        private static readonly List<string> errLog = [];
        readonly DataSet schemaXer = new("dsXER");
        readonly string[] defIgnoredTable = ["OBS", "POBS", "RISKTYPE"];

        internal static readonly NumberFormatInfo NumberFormat = new()
        {
            NumberDecimalSeparator = @".",
        };
        private readonly HashSet<string> ignoredTable = [] ;
        private readonly HashSet<string> loadedTable = [];
        #endregion

        public XerParser(string pathSchemaXer)
        {
            schemaXer.ReadXmlSchema(pathSchemaXer);
        }


        #region Property
        private DataSet SchemaXer
        {
            get
            {
                return schemaXer.Clone();
            }
        }

        public HashSet<string> IgnoredTable { get => ignoredTable; }
        public HashSet<string> LoadedTable { get => loadedTable; }

        public static Encoding XEncoding { get; set; } = Encoding.GetEncoding(1251);

        public XerElement[] XerElements
        { get { return xerElements; } }

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
        /// Экранировать строковые значения по формату XML
        /// </summary>
        public static bool EscapeSpecialCharsXml { get; set; } = false;

        /// <summary>
        /// Удалять таблицы не имеющие записей, По умолчанию false;
        /// </summary>
        public static bool RemoveEmptyTables { get; set; } = false;

        public static List<string> ErrorLog { get => errLog; }

        public static string Currence { get; set; } = "RUB";

        static string FullName
        {
            get
            {
                string dir = @"WinNT://" +  System.Environment.UserDomainName + @"/" + System.Environment.UserName;
                try
                {
                    System.DirectoryServices.DirectoryEntry ADEntry = new(dir);
                    object value = ADEntry.Properties[nameof(FullName)].Value;
                    return value is null ? System.Environment.UserName : (string)value;
                }
                catch 
                {
                   return  System.Environment.UserName;
                }
            }
        }

        #endregion


        #region Parse
        public void LoadXer(string fileName)
        {
            SetIgnoredTables();
            Stopwatch sw = Stopwatch.StartNew();
            DataSetXer = SchemaXer;
            xerElements = InternalParse(fileName, dataSet).ToArray();
            sw.Stop();
            OnInitializationСompleted(new InitializeEventArgs(sw.Elapsed));
        }


        IEnumerable<XerElement> InternalParse(string fileName, DataSet dsXer)
        {
            XerElement e = null;
            List<List<string>> records = null;

            int remUpload = Math.Max(dsXer.Tables.Count - ignoredTable.Count,
                                                        loadedTable.Count);
            bool ignore = false;
            foreach (var line in Parse(ReadLines(fileName)))
            {
                string flag = line[0][..2];
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
                            if (remUpload == 0) yield break;
                        }
                        if (ignoredTable.Contains(line[1]))
                        {
                            ignore = true;
                            continue;
                        }
                        else
                            ignore = false;

                        e = new(line[1])
                        {
                            DataSetXer = dsXer
                        };
                        remUpload--;
                        e.Initialized += E_Initialised;

                        break;
                    case fld:
                        if (ignore) continue;
                        e.FieldNames = line.Skip(1);
                        records = [];
                        break;
                    case rec:
                        if (ignore) continue;
                        records.Add(line.Skip(1).ToList());
                        break;
                }
            }
        }

        private static IEnumerable<string> ReadLines(string fileName)
        {
            using StreamReader sr = new(fileName, Encoding.GetEncoding(1251));
            while (sr.Peek() >= 0)
                yield return sr.ReadLine();
        }

        private static IEnumerable<List<string>> Parse(IEnumerable<string> lines)
        {
            var e = lines.GetEnumerator();
            while (e.MoveNext())
                yield return ParseLine(e);
        }
 

        private static List<string> ParseLine(IEnumerator<string> e)
        {
            var items = new List<string>();

            foreach (string token in GetToken(e))
                items.Add(token);
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
                            state = State.inQuote;
                        else if (c == end1)
                            break;
                        else if (c == end2)
                            break;
                        else
                            token += c;
                        break;
                    case State.inQuote:
                        if (c == quote)
                            state = State.mayBeOutQuote;
                        else
                            token += c;
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
            if (state == State.inQuote && e.MoveNext()) goto again;

            yield return token;
        }
        #endregion

        #region Build
        public static bool BuildXerFile(DataSet dataSetXer, string path)
        {
            string userName = System.Environment.UserName;

            if (string.IsNullOrWhiteSpace(path)) return false;

            //builds an XER file from current datatable contents...
            string sLine;

            //On Error GoTo buildXerFile_Error
            using StreamWriter eXerFile = new(path, false, XEncoding);
            try
            {
                //xer header (TP/P3e)...
                string header = string.Join(ASC_TAB_CHAR, [ "ERMHDR",
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
                        if (RemoveEmptyTables && dTable.Rows.Count == 0) continue;
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


        static string GetRecordString(DataRow dRow, List<string> ArrColumn, List<string> badRecord = null)
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
                        val = SecurityElement.Escape(value.ToString());
                    else
                    {
                        val = value.ToString();
                        if (val.Contains('\n')) val = val.Replace("\x0A", "");          //\n
                        if (val.Contains('\x0D')) val = val.Replace("\x0D", "");        //\r
                        if (val.Contains('\x09')) val = val.Replace("\x09", "");        //\t
                        if (val.Contains('\x22')) val = val.Replace("\x22", dblQuote);  //Экранирование кавычек

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

        public void SetIgnoredTables(IEnumerable<string> ignoredTableNames)
        {
            foreach (string name in ignoredTableNames)
            {
                if (LoadedTable.Contains(name)) LoadedTable.Remove(name);
                ignoredTable.Add(name);
            }
        }

        void SetIgnoredTables()
        {          
            if (loadedTable.Count == 0 || dataSet == null) return;
            foreach (DataTable tbl in dataSet.Tables)
            {
                if (!loadedTable.Contains(tbl.TableName) &&
                    !ignoredTable.Contains(tbl.TableName)) ignoredTable.Add(tbl.TableName);
            }
        }

        public void ResetIgnoredTable()
        {
            loadedTable.Clear();
            ignoredTable.Clear();
            foreach(string str in defIgnoredTable) ignoredTable.Add(str);
        }

        #endregion


        #region Events

        private void E_Initialised(object sender, InitializeEventArgs e)
        {
            OnInitialization(new InitializingEventArgs(sender as XerElement, e.Elepsed));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Стили именования", Justification = "<Ожидание>")]
        private event EventHandler<InitializingEventArgs> onInitializing;
        protected internal virtual void OnInitialization(InitializingEventArgs e)
        {
            onInitializing?.Invoke(this, e);
        }

        public event EventHandler<InitializingEventArgs> Initialization
        {
            add { onInitializing += value; }
            remove { onInitializing -= value; }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Стили именования", Justification = "<Ожидание>")]
        private event EventHandler<InitializeEventArgs> onInitializationСompleted;
        protected internal virtual void OnInitializationСompleted(InitializeEventArgs e)
        {
            onInitializationСompleted?.Invoke(this, e);
        }

        public event EventHandler<InitializeEventArgs> InitializationСompleted
        {
            add { onInitializationСompleted += value; }
            remove { onInitializationСompleted -= value; }
        }

        #endregion

    }
}
