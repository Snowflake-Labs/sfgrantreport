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

using System;

namespace Snowflake.GrantReport.ReportObjects
{
    public class StepTiming
    {
        public string Connection { get; set; }

        public string Account { get; set; }

        public string JobFileName { get; set; }

        public string StepName { get; set; }
        public int StepID { get; set; }
        public int NumEntities { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public long DurationMS { get; set; }

        public override String ToString()
        {
            return String.Format(
                "StepTiming: {0}({1}):{2} {3}/{4}",
                this.StepName,
                this.StepID,
                this.NumEntities,
                this.Duration,
                this.DurationMS);
        }
    }
}
