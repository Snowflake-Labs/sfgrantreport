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
    public class RoleMap : ClassMap<Role>
    {
        public RoleMap()
        {
            int i = 0;
            
            Map(m => m.Name).Index(i); i++;
            Map(m => m.Owner).Index(i); i++;
            Map(m => m.Type).Index(i); i++;
            Map(m => m.IsInherited).Index(i); i++;

            Map(m => m.NumAssignedUsers).Index(i); i++;
            Map(m => m.NumChildRoles).Index(i); i++;
            Map(m => m.NumParentRoles).Index(i); i++;

            Map(m => m.AssignedUsers).Index(i); i++;
            Map(m => m.ChildRolesString).Name("ChildRoles").Index(i); i++;
            Map(m => m.ParentRolesString).Name("ParentRoles").Index(i); i++;
            Map(m => m.AncestryPaths).Index(i); i++;
            Map(m => m.NumAncestryPaths).Index(i); i++;

            Map(m => m.Comment).Index(i); i++;
            
            Map(m => m.IsObjectIdentifierSpecialCharacters).Index(i); i++;

            CSVMapHelper.SetISO8601DateFormat(Map(m => m.CreatedOn), i); i++;
            CSVMapHelper.SetISO8601DateFormat(Map(m => m.CreatedOnUTC), i); i++;
        }
    }
}