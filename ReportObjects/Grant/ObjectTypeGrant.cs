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
    public class ObjectTypeGrant
    {       
        public string DBName { get; set; }

        public string EntityName { get; set; }

        public string GrantedTo { get; set; }

        public string ObjectName { get; set; }

        public string ObjectType { get; set; }

        public string Privilege0 { get; set; }
        public string Privilege1 { get; set; }
        public string Privilege2 { get; set; }
        public string Privilege3 { get; set; }
        public string Privilege4 { get; set; }
        public string Privilege5 { get; set; }
        public string Privilege6 { get; set; }
        public string Privilege7 { get; set; }
        public string Privilege8 { get; set; }
        public string Privilege9 { get; set; }
        public string Privilege10 { get; set; }
        public string Privilege11 { get; set; }
        public string Privilege12 { get; set; }
        public string Privilege13 { get; set; }
        public string Privilege14 { get; set; }
        public string Privilege15 { get; set; }
        public string Privilege16 { get; set; }
        public string Privilege17 { get; set; }
        public string Privilege18 { get; set; }
        public string Privilege19 { get; set; }

        public string SchemaName { get; set; }

        public override String ToString()
        {
            return String.Format(
                "ObjectGrants: {0} {1}",
                this.ObjectType,
                this.ObjectName);
        }
    }
}
