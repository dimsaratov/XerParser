using System.Data;
using System.Globalization;
using static XerParser.XerElement;

namespace XerParser
{
    internal class DataSetter
    {
        internal DataSetter(DataColumn column)
        {
            Column = column;
        }

        #region Property
        public string Name => Column.ColumnName;

        public Type Type => Column.DataType;

        public ValueParse ValueParse => Column.DataType.Name switch
        {
            "DateTime" => new ValueParse(DateTimeParse),
            "Int32" => new ValueParse(IntParse),
            "Decimal" => new ValueParse(DecimalParse),
            _ => new ValueParse(StringParse),
        };

        public DataColumn Column { get; }

        #endregion

        #region Value Parsers
        private static object DecimalParse(string value)
        {
            return value.Length > 0 && decimal.TryParse(value, NumberStyles.Float, Parser.NumberFormat, out decimal n) ? n : DBNull.Value;
        }

        private static object DateTimeParse(string value)
        {
            return value.Length > 0 && DateTime.TryParse(value, out DateTime d) ? d : DBNull.Value;
        }

        private static string StringParse(string value)
        {
            return value;
        }

        private static object IntParse(string value)
        {
            return value.Length > 0 && int.TryParse(value, out int i) ? i : DBNull.Value;
        }
        #endregion
    }
}
