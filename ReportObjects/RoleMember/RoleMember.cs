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
    public class RoleMember
    {
        public DateTime CreatedOn { get; set; }

        public DateTime CreatedOnUTC
        {
            get
            {
                return this.CreatedOn.ToUniversalTime();
            }
            set
            {
                // Do nothing
            }
        }

        public DateTime? DeletedOn { get; set; }

        public string Name { get; set; }

        public string GrantedBy { get; set; }

        public string GrantedTo { get; set; }

        public string ObjectType { get; set; }

        public override String ToString()
        {
            return String.Format(
                "RoleMember: {0} is granted to {1} [{2}] by {4}",
                this.Name,
                this.GrantedTo,
                this.ObjectType,
                this.GrantedBy);
        }
    }
}
