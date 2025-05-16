using System.ComponentModel;

namespace XerParser.Enums
{
    /// <summary>
    /// Версия индикатора файла Primavera
    /// </summary>
    public enum PrimaveraVersion
    {
        /// <summary>
        /// Primavera version 6.2
        /// </summary>
        [Description("6.2")]
        Primavera62,

        /// <summary>
        /// Primavera version 18.8
        /// </summary>
        [Description("18.8")]
        Primavera188,

        /// <summary>
        /// Primavera version 19.12
        /// </summary>
        [Description("19.12")]
        Primavera1912,
    }

    /// <summary>
    /// 
    /// </summary>
    public enum NumberDecimalSeparator
    {
        /// <summary>
        /// Point separator
        /// </summary>
        [Description("Точка")]
        Point,
        /// <summary>
        /// Comma separator
        /// </summary>
        [Description("Запятая")]
        Comma
    }

    public static class Extensions
    {
        /// <summary>
        /// Получить атрибут "Описание"
        /// </summary>
        public static string GetDescription(this Enum value)
        {
            System.Reflection.FieldInfo fi = value.GetType().GetField(value.ToString());
            DescriptionAttribute[] attributes = (DescriptionAttribute[])fi?.GetCustomAttributes(typeof(DescriptionAttribute), false);
            return attributes != null && attributes.Length > 0 ? attributes[0].Description : value.ToString();
        }
    }
}
