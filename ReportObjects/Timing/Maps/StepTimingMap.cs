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
    public class StepTimingReportMap : ClassMap<StepTiming>
    {
        public StepTimingReportMap()
        {
            int i = 0;
            Map(m => m.StepName).Index(i); i++;
            Map(m => m.StepID).Index(i); i++;

            Map(m => m.Connection).Index(i); i++;
            Map(m => m.Account).Index(i); i++;
            Map(m => m.NumEntities).Index(i); i++;

            CSVMapHelper.SetISO8601DateFormat(Map(m => m.StartTime), i); i++;
            CSVMapHelper.SetISO8601DateFormat(Map(m => m.EndTime), i); i++;
            Map(m => m.Duration).Index(i); i++;
            Map(m => m.DurationMS).Index(i); i++;

            Map(m => m.JobFileName).Index(i); i++;
        }
    }
}