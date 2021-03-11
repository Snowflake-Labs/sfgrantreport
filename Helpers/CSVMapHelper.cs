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
