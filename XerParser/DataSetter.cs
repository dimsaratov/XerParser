using System.Data;

namespace XerParser
{
    internal class DataSetter
    {

        internal DataSetter(DataColumn column)
        {
            Column = column;
            ValueParse = Parsers.DbNullParse;
        }

        internal DataSetter(DataColumn column, int index)
        {
            Column = column;
            this.Index = index;
            ValueParse = Parsers.ValueParse(column.DataType.Name);
        }

        #region Property
        public string ColumnName => Column.ColumnName;

        public Type Type => Column.DataType;

        public DataColumn Column { get; }

        public int Index { get; } = -1;

        public ValueParse ValueParse { get; }

        public dynamic Value(string value)
        {
            return ValueParse(value);
        }
        #endregion

    }
}
