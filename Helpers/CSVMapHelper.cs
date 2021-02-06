using CsvHelper.Configuration;

namespace Snowflake.GrantReport
{
    /// <summary>
    /// Helper functions for writing CSV files
    /// </summary>
    public class CSVMapHelper
    {
        /// <summary>
        /// The "O" or "o" standard format specifier represents a custom date and time format string using a pattern that preserves time zone information and emits a result string that complies with ISO 8601. 
        /// https://docs.microsoft.com/en-us/dotnet/standard/base-types/standard-date-and-time-format-strings#the-round-trip-o-o-format-specifier
        /// </summary>
        /// <param name="map"></param>
        /// <param name="index"></param>
        public static void SetISO8601DateFormat(MemberMap map, int index)
        {
            map.TypeConverterOption.Format("O");
            map.Index(index);
            
            return;
        }
    }
}
