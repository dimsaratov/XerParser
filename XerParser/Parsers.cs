using System.Globalization;

namespace XerParser
{

    /// <summary>
    /// Delegate for parse string value
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public delegate object ValueParse(string value);

    /// <summary>
    /// Present parsers for string values
    /// </summary>
    public static class Parsers
    {
        /// <summary>
        /// Parse to Decimal
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static object DecimalParse(string value)
        {
            return value.Length > 0 && decimal.TryParse(value, NumberStyles.Float, Parser.NumberFormat, out decimal n) ? n : DBNull.Value;
        }

        /// <summary>
        /// Parse to DateTime
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static object DateTimeParse(string value)
        {
            return value.Length > 0 && DateTime.TryParse(value, out DateTime d) ? d : DBNull.Value;
        }

        /// <summary>
        /// Parse to boolean
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static object BoolParse(string value)
        {
            return value.Length > 0 && bool.TryParse(value, out bool d) ? d : DBNull.Value;
        }

        /// <summary>
        /// Required for api returns an unchanged string
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string StringParse(string value)
        {
            return value;
        }

        /// <summary>
        /// Parse to Int
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static object IntParse(string value)
        {
            return value.Length > 0 && int.TryParse(value, out int i) ? i : DBNull.Value;
        }

        /// <summary>
        /// Switch delegate value parse
        /// </summary>
        /// <param name="dataTypeName">Data type name for select delegate value parse </param>
        /// <returns>Delegate value parse</returns>
        public static ValueParse ValueParse(string dataTypeName)
        {
            return dataTypeName switch
            {
                "DateTime" => new ValueParse(DateTimeParse),
                "Int32" => new ValueParse(IntParse),
                "Decimal" => new ValueParse(DecimalParse),
                "Boolean" => new ValueParse(BoolParse),
                _ => new ValueParse(StringParse),
            };
        }
    }
}
