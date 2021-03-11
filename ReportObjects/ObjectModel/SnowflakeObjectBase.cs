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
using System.Collections.Generic;

namespace Snowflake.GrantReport.ReportObjects
{
    public class SnowflakeObjectBase
    {        
        public string ShortName { get; set; }
        
        public string FullName { get; set; }

        public virtual string EntityType { get; }

        public List<Grant> Grants { get; set; } = new List<Grant>();

        public override String ToString()
        {
            return String.Format(
                "{0}: {1} [{2}] with {3} grants",
                this.GetType().Name,
                this.FullName,
                this.EntityType, 
                this.Grants.Count);
        }        
    }
}
