namespace XerParser
{
    public class UdfDataColumn(string columnName, Type type) : ChildDataColumn(columnName, type)
    {
        public override string ChildFieldTypeIdName => "udf_type_id";

        public override string DataRelationName => "rel_udf_task";

    }
}
