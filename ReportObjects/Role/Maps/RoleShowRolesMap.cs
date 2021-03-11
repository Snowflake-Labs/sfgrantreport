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
    public class RoleShowRolesMap : ClassMap<Role>
    {
        public RoleShowRolesMap()
        {
            int i = 0;
            
            Map(m => m.Name).Name("name").Index(i); i++;
            Map(m => m.Comment).Name("comment").Index(i); i++;
            Map(m => m.Owner).Name("owner").Index(i); i++;

            Map(m => m.IsInheritedRaw).Name("is_inherited").Index(i); i++;

            Map(m => m.NumAssignedUsers).Name("assigned_to_users").Index(i); i++;
            Map(m => m.NumChildRoles).Name("granted_roles").Index(i); i++;
            Map(m => m.NumParentRoles).Name("granted_to_roles").Index(i); i++;

            Map(m => m.CreatedOn).Name("created_on").Index(i); i++;
        }
    }
}