namespace XerParser
{
    public class ActivityCodeDataColumn : ChildDataColumn
    {
#pragma warning disable CA1822 // Пометьте члены как статические
        public new bool ReadOnly => true;
#pragma warning restore CA1822 // Пометьте члены как статические

        public ActivityCodeDataColumn() : base()
        {
            base.ReadOnly = true;
        }

        public ActivityCodeDataColumn(string columnName, Type type) : this()
        {
            ColumnName = columnName;
            DataType = type;
        }

        public override string ChildFieldTypeIdName => "actv_code_type_id";

        public override string DataRelationName => "rel_task_actv";


        internal override object this[int record]
        {
            get => base[record];
            set { }
        }
    }
}
