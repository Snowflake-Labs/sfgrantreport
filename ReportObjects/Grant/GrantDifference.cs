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
    public class GrantDifference
    {

        public string DBName { get; set; }

        public string EntityName { get; set; }

        public string GrantedTo { get; set; }

        public string ObjectName { get; set; }

        public string ObjectType { get; set; }

        public string Privilege { get; set; }

        public string SchemaName { get; set; }

        public string UniqueIdentifier { get; set; }

        public string ReportLeft { get; set; }
        
        public string ReportRight { get; set; }
        
        public string Difference { get; set; }

        public string DifferenceDetails { get; set; }

        public string? GrantedByLeft { get; set; }
        
        public string? GrantedByRight { get; set; }

        public bool? WithGrantOptionLeft { get; set; }
        
        public bool? WithGrantOptionRight { get; set; }

        public DateTime? CreatedOnUTCLeft { get; set; }
        
        public DateTime? CreatedOnUTCRight { get; set; }

        public string PrivilegeDisplayShort(Dictionary<string, string> privilegeNamesShortDict)
        { 
            string shortName = String.Empty;
            if (privilegeNamesShortDict.TryGetValue(this.Privilege, out shortName) == true)
            {
                if (this.WithGrantOptionLeft == true || this.WithGrantOptionRight == true == true)
                {
                    return String.Format("{0}+", shortName);
                }
                else
                {
                    return shortName;
                }
            }
            else
            {
                // Take first two characters
                string[] words = this.Privilege.Split(' ');
                List<string> shorterWords = new List<string>(words.Length);
                foreach (string word in words)
                {
                    shorterWords.Add(word.Substring(0, 2));

                }
                shortName = String.Join('_', shorterWords.ToArray());
                return shortName;
            }
        }

        public int PrivilegeOrder(Dictionary<string, int> privilegeOrderDict)
        { 
            int order = -1;
            if (privilegeOrderDict.TryGetValue(this.Privilege, out order) == true)
            {
                return order;
            }
            else
            {
                return 1000;
            }
        }

        public string PrivilegeDisplayLong
        { 
            get
            {
                if (this.WithGrantOptionLeft == true || this.WithGrantOptionRight == true)
                {
                    return String.Format("{0}+", this.Privilege);
                }
                else
                {
                    return this.Privilege;
                }
            }
        }

        public override String ToString()
        {
            return String.Format(
                "GrantDifference: {0}, {1}, left {2} <-> right {3} in {4}",
                this.Difference,
                this.UniqueIdentifier,
                this.ReportLeft,
                this.ReportRight,
                this.DifferenceDetails);
        }
    }
}
