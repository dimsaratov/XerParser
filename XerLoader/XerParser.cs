using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Text;


namespace XerLoader

{
    public class XerParser
    {

        public char separator = '\t';
        public char quote = '"';
        public const string rec = "%R";
        internal readonly static string dblQuote = new('"', 2);
        public char end1 = '\r';
        public char end2 = '\n';
        enum State { outQuote, inQuote, mayBeOutQuote }
        private DataSet dataSet;
        private XerElement[] xerElements;
        readonly DataSet schemaXer = new("dsXER");


        internal static readonly NumberFormatInfo NumberFormat = new()
        {
            NumberDecimalSeparator = @".",
        };

        public XerParser(string pathSchemaXer)
        {
            schemaXer.ReadXmlSchema(pathSchemaXer);
        }

        private DataSet SchemaXer
        {
            get
            {
                return schemaXer.Clone();
            }
        }

        public XerElement[] XerElements
        { get { return xerElements; } }

        public DataSet DataSetXer => dataSet;

        private void E_Initialised(object sender, InitializeEventArgs e)
        {
            OnInitialization(new InitializingEventArgs(sender as XerElement, e.Elepsed));
        }

        #region Parse
        public void LoadXer(string fileName)
        {
            Stopwatch sw = Stopwatch.StartNew();
            dataSet = SchemaXer;
            xerElements = InternalParse(fileName, dataSet).ToArray();
            sw.Stop();
            OnInitializationСompleted(new InitializeEventArgs(sw.Elapsed));
        }


        IEnumerable<XerElement> InternalParse(string fileName, DataSet dsXer)
        {
            XerElement e = null;
            List<List<string>> records = null;
            foreach (var line in Parse(ReadLines(fileName)))
            {
                string flag = line[0][..2];
                switch (flag)
                {
                    case "%E":
                        e.Records = records;
                        yield return e;
                        break;
                    case "%T":
                        if (e != null)
                        {
                            e.Records = records;
                            yield return e;
                        }
                        e = new(line[1])
                        {
                            DataSetXer = dsXer
                        };
                        e.Initialized += E_Initialised;
                        break;
                    case "%F":

                        e.FieldNames = line.Skip(1).ToList();
                        records = [];
                        break;
                    case "%R":
                        records.Add(line.Skip(1).ToList());
                        break;
                }
            }
        }

        static IEnumerable<string> ReadLines(string fileName)
        {
            using StreamReader sr = new(fileName, Encoding.GetEncoding(1251));
            while (sr.Peek() >= 0)
                yield return sr.ReadLine();
        }

        private IEnumerable<List<string>> Parse(IEnumerable<string> lines)
        {
            var e = lines.GetEnumerator();
            while (e.MoveNext())
                yield return ParseLine(e);
        }
        #endregion

        private List<string> ParseLine(IEnumerator<string> e)
        {
            var items = new List<string>();

            foreach (string token in GetToken(e))
                items.Add(token);
            return items;
        }

        private IEnumerable<string> GetToken(IEnumerator<string> e)
        {
            string token = "";
            State state = State.outQuote;
            again:
            foreach (char c in e.Current)
            {
                switch (state)
                {
                    case State.outQuote:
                        if (c == separator)
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

        #region Events

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

    public class InitializingEventArgs(XerElement xerElement, TimeSpan time) : InitializeEventArgs(time)
    {
        public XerElement XerElement { get; private set; } = xerElement;

    }

    public class InitializeEventArgs(TimeSpan time) : EventArgs
    {
        public TimeSpan Elepsed { get; private set; } = time;
    }
}
