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
    /// <summary>
    /// Encapsulates DESCRIBE USER output
    /// </summary>
    public class UserProperty
    {
        public string PropName { get; set; }
           
        public string PropValue { get; set; }
        
        public bool IsDefault { get; set; }
        
        public string Description { get; set; }        

        public override String ToString()
        {
            return String.Format(
                "UserProperty: {0}={1}, IsDefault {2}",
                this.PropName,
                this.PropValue,
                this.IsDefault);
        }
    }
}
