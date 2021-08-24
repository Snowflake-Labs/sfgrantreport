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
    public class GrantDifferenceMap : ClassMap<GrantDifference>
    {
        public GrantDifferenceMap()
        {
            int i = 0;

            Map(m => m.Privilege).Index(i); i++;
            Map(m => m.ObjectType).Index(i); i++;
            Map(m => m.ObjectName).Index(i); i++;
            Map(m => m.GrantedTo).Index(i); i++;

            Map(m => m.UniqueIdentifier).Index(i); i++;

            Map(m => m.DBName).Index(i); i++;
            Map(m => m.SchemaName).Index(i); i++;
            Map(m => m.EntityName).Index(i); i++;

            Map(m => m.ReportLeft).Index(i); i++;
            Map(m => m.ReportRight).Index(i); i++;
            Map(m => m.Difference).Index(i); i++;
            Map(m => m.DifferenceDetails).Index(i); i++;

            Map(m => m.GrantedByLeft).Index(i); i++;
            Map(m => m.GrantedByRight).Index(i); i++;
            Map(m => m.WithGrantOptionLeft).Index(i); i++;
            Map(m => m.WithGrantOptionRight).Index(i); i++;
            CSVMapHelper.SetISO8601DateFormat(Map(m => m.CreatedOnUTCLeft), i); i++;
            CSVMapHelper.SetISO8601DateFormat(Map(m => m.CreatedOnUTCRight), i); i++;
        }
    }
}