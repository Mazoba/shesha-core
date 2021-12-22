using System;
using System.Collections.Generic;
using System.Text;

namespace Shesha.Metadata
{
    /// <summary>
    /// Base data types
    /// </summary>
    public static class BaseDataTypes
    {
        public const string String = "string";
        public const string Number = "number";
        public const string Date = "date";
        public const string Time = "time";
        public const string DateTime = "date-time";
        public const string Entity = "entity";
        public const string File = "file";
        public const string ReferenceListItem = "reference-list-item";
        public const string Boolean = "boolean";
        public const string Array = "array";
        public const string Object = "object";
    }

    /// <summary>
    /// Data formats
    /// </summary>
    public static class DataFormats
    {
        public const string Date = "date"; // full-date (see https://datatracker.ietf.org/doc/html/rfc3339#section-5.6)
        public const string DateTime = "date-time"; // date-time (see https://datatracker.ietf.org/doc/html/rfc3339#section-5.6)
        public const string Time = "time"; // full-time (see https://datatracker.ietf.org/doc/html/rfc3339#section-5.6)
        public const string Uuid = "uuid";
        public const string EntityReference = "entity-reference";
        public const string Float = "float";
        public const string Double = "double";
        public const string Int32 = "int32";
        public const string Int64 = "int64";
        public const string RefListValue = "ref-list-value";
    }

    /// <summary>
    /// Data types
    /// </summary>
    public static class DataTypes 
    {
        public const string String = BaseDataTypes.String;
        public static string Date => $"{BaseDataTypes.String}:{DataFormats.Date}";
        public static string DateTime => $"{BaseDataTypes.String}:{DataFormats.DateTime}";
        public static string Time => $"{BaseDataTypes.String}:{DataFormats.Time}";
        public static string Uuid => $"{BaseDataTypes.String}:{DataFormats.Uuid}";
        public static string EntityReference => $"{BaseDataTypes.String}:{DataFormats.EntityReference}";
        public static string Float => $"{BaseDataTypes.Number}:{DataFormats.Float}";
        public static string Double => $"{BaseDataTypes.Number}:{DataFormats.Double}";
        public static string Int32 => $"{BaseDataTypes.Number}:{DataFormats.Int32}";
        public static string Int64 => $"{BaseDataTypes.Number}:{DataFormats.Int64}";
        public static string Boolean => BaseDataTypes.Boolean;
        public static string RefListValue => $"{BaseDataTypes.Number}:{DataFormats.RefListValue}";
        public static string Array => BaseDataTypes.Array;
    }
}
