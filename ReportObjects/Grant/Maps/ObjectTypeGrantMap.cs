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
using System.Collections.Generic;

namespace Snowflake.GrantReport.ReportObjects
{
    public class ObjectTypeGrantMap : ClassMap<ObjectTypeGrant>
    {
        public ObjectTypeGrantMap(List<string> privilegeColumnNames)
        {
            int i = 0;
            Map(m => m.ObjectType).Index(i); i++;
            Map(m => m.ObjectName).Index(i); i++;
            Map(m => m.GrantedTo).Index(i); i++;
            
            Map(m => m.DBName).Index(i); i++;
            Map(m => m.SchemaName).Index(i); i++;
            Map(m => m.EntityName).Index(i); i++;

            for (int j = 0; j < privilegeColumnNames.Count; j++)
            {
                string privilegeColumnName = privilegeColumnNames[j];
                
                // This could probably be done with reflection but!
                switch (j)
                {
                    case 0:
                        Map(m => m.Privilege0).Name(privilegeColumnName).Index(i); i++;;
                        break;
                    case 1:
                        Map(m => m.Privilege1).Name(privilegeColumnName).Index(i); i++;;
                        break;
                    case 2:
                        Map(m => m.Privilege2).Name(privilegeColumnName).Index(i); i++;;
                        break;
                    case 3:
                        Map(m => m.Privilege3).Name(privilegeColumnName).Index(i); i++;;
                        break;
                    case 4:
                        Map(m => m.Privilege4).Name(privilegeColumnName).Index(i); i++;;
                        break;
                    case 5:
                        Map(m => m.Privilege5).Name(privilegeColumnName).Index(i); i++;;
                        break;
                    case 6:
                        Map(m => m.Privilege6).Name(privilegeColumnName).Index(i); i++;;
                        break;
                    case 7:
                        Map(m => m.Privilege7).Name(privilegeColumnName).Index(i); i++;;
                        break;
                    case 8:
                        Map(m => m.Privilege8).Name(privilegeColumnName).Index(i); i++;;
                        break;
                    case 9:
                        Map(m => m.Privilege9).Name(privilegeColumnName).Index(i); i++;;
                        break;
                    case 10:
                        Map(m => m.Privilege10).Name(privilegeColumnName).Index(i); i++;;
                        break;
                    case 11:
                        Map(m => m.Privilege11).Name(privilegeColumnName).Index(i); i++;;
                        break;
                    case 12:
                        Map(m => m.Privilege12).Name(privilegeColumnName).Index(i); i++;;
                        break;
                    case 13:
                        Map(m => m.Privilege13).Name(privilegeColumnName).Index(i); i++;;
                        break;
                    case 14:
                        Map(m => m.Privilege14).Name(privilegeColumnName).Index(i); i++;;
                        break;
                    case 15:
                        Map(m => m.Privilege15).Name(privilegeColumnName).Index(i); i++;;
                        break;
                    case 16:
                        Map(m => m.Privilege16).Name(privilegeColumnName).Index(i); i++;;
                        break;
                    case 17:
                        Map(m => m.Privilege17).Name(privilegeColumnName).Index(i); i++;;
                        break;
                    case 18:
                        Map(m => m.Privilege18).Name(privilegeColumnName).Index(i); i++;;
                        break;
                    case 19:
                        Map(m => m.Privilege19).Name(privilegeColumnName).Index(i); i++;;
                        break;
                    default:
                        // Can't fit more than 20 privileges
                        break;
                }

            }
        }
    }
}