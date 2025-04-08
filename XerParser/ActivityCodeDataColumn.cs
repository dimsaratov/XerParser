namespace XerParser
{
    public class ActivityCodeDataColumn(string columnName, Type type) : ChildDataColumn(columnName, type)
    {

        public override string ChildFieldTypeIdName => "actv_code_type_id";

        public override string DataRelationName => "rel_task_actv";

    }
}
