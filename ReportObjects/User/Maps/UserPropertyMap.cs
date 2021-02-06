using Snowflake.GrantReport;
using Snowflake.GrantReport.ReportObjects;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace Snowflake.GrantReport.ReportObjects
{
    public class UserPropertyMap: ClassMap<UserProperty>
    {
        public UserPropertyMap()
        {
            int i = 0;
            
            Map(m => m.PropName).Name("property").Index(i); i++;
            Map(m => m.PropValue).Name("value").Index(i); i++;
            setUserDefaultBooleanFormat(Map(m => m.IsDefault), i); i++;
            Map(m => m.Description).Name("description").Index(i); i++;
        }

        private static void setUserDefaultBooleanFormat(MemberMap map, int index)
        {
            map.Name("default");
            map.TypeConverterOption.BooleanValues(false, false, new string[] {"null", "FALSE"});
            map.TypeConverterOption.BooleanValues(true, false, new string[] {"TRUE"});
            map.Index(index);
            
            return;
        }        

    }
}