// Copyright (c) 2021 Snowflake Inc. All rights reserved.

// Licensed under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in compliance
// with the License.  You may obtain a copy of the License at

//   http://www.apache.org/licenses/LICENSE-2.0

// Unless required by applicable law or agreed to in writing,
// software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, either express or implied.  See the License for the
// specific language governing permissions and limitations
// under the License.

using CsvHelper.Configuration;

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