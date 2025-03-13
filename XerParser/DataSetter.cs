using System.Data;

namespace XerParser
{
    internal class DataSetter
    {
        internal DataSetter(DataColumn column)
        {
            Column = column;
            ValueParse = Parsers.ValueParse(column.DataType.Name);
        }

        #region Property
        public string Name => Column.ColumnName;

        public Type Type => Column.DataType;

        public DataColumn Column { get; }

        public ValueParse ValueParse { get; }
        #endregion

    }
}
