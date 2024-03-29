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
using System.Globalization;
using System.Collections.Generic;
using System.Linq;

namespace Snowflake.GrantReport.ReportObjects
{
    public class Role
    {        
        public string AssignedUsers { get; set; }

        public List<Role> ChildRoles { get; set; } = new List<Role>();

        public string ChildRolesString 
        { 
            get
            {
                return String.Join(',', this.ChildRoles.Select(r => r.Name).ToArray());
            }
            set
            {
                //
            }
        }

        public string Comment { get; set; }

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

        private string _IsInheritedRaw;
        public string IsInheritedRaw
        {
            get
            {
                return this._IsInheritedRaw;
            }
            set
            {
                this._IsInheritedRaw = value;
                if (String.Compare(value, "Y", true, CultureInfo.InvariantCulture) == 0)
                {
                    this.IsInherited = true;
                }
                else
                {
                    this.IsInherited = false;
                }
            }
        }
        public bool IsInherited { get; set; }

        public bool IsObjectIdentifierSpecialCharacters { get; set; }

        public string Name { get; set; }

        public int NumAncestryPaths { get; set; }

        public int NumAssignedUsers { get; set; }

        public int NumChildRoles { get; set; }

        public int NumParentRoles { get; set; }

        public string Owner { get; set; }

        public List<Role> ParentRoles { get; set; } = new List<Role>();

        public string ParentRolesString 
        {
            get
            {
                return String.Join(',', this.ParentRoles.Select(r => r.Name).ToArray());
            }
            set
            {
                //
            }
        }

        public string AncestryPaths
        {
            get
            {
                List<string> allPaths = new List<string>();
                GetParentPaths(this, this.Name, allPaths, 25);
                string[] ancestryPathsArray = allPaths.Select(p => p.ToString()).ToArray();
                this.NumAncestryPaths = ancestryPathsArray.Length;
                return String.Join('\n', ancestryPathsArray);
            }
        }

        public RoleType Type { get; set; } = RoleType.Unknown;

        public override String ToString()
        {
            return String.Format(
                "Role: {0} [{1}], {2} children, {3} parents, {3} users",
                this.Name,
                this.Type,                
                this.NumChildRoles,
                this.NumParentRoles,
                this.NumAssignedUsers);
        }

        public bool RollsUpTo(Role potentialParent)
        {
            return doesRoleRollupToRole(this, potentialParent);
        }

        private bool doesRoleRollupToRole(Role thisRole, Role potentialParent)
        {
            if (thisRole.ParentRoles.Contains(potentialParent) == true)
            {
                return true;
            }
            else
            {
                if (thisRole.ParentRoles.Count == 0) 
                {
                    return false;
                }
                else
                {
                    foreach (Role parentRole in thisRole.ParentRoles)
                    {
                        if (doesRoleRollupToRole(parentRole, potentialParent) == true)
                        {
                            return true;
                        } 
                    }
                    return false;
                }
            }
        }

        public void GetParentPaths(Role thisRole, string pathSoFar, List<string> allPaths, int maxPathsToReturn)
        {
            if (thisRole.ParentRoles.Count == 0) 
            {
                allPaths.Add(pathSoFar);
            }
            else
            {
                foreach (Role parentRole in thisRole.ParentRoles)
                {
                    string pathSoFar1 = String.Format(@"{1}->{0}", pathSoFar, parentRole.Name);
                    GetParentPaths(parentRole, pathSoFar1, allPaths, maxPathsToReturn);
                    if (allPaths.Count >= maxPathsToReturn) 
                    {
                        return;
                    }
                }
                return;
            }
        }

        // public void GetParentPaths(Role thisRole, StringBuilder pathSoFar, List<string> allPaths, int maxPathsToReturn)
        // {
        //     if (thisRole.ParentRoles.Count == 0) 
        //     {
        //         allPaths.Add(pathSoFar.ToString());
        //     }
        //     else
        //     {
        //         foreach (Role parentRole in thisRole.ParentRoles)
        //         {
        //             pathSoFar.Insert(0, String.Format("{0}->", parentRole.Name));
        //             //string pathSoFar1 = String.Format(@"{1}->{0}", pathSoFar, parentRole.Name);
        //             GetParentPaths(parentRole, pathSoFar, allPaths, maxPathsToReturn);
        //             if (allPaths.Count >= maxPathsToReturn) 
        //             {
        //                 return;
        //             }
        //         }
        //         return;
        //     }
        // }

        public void GetAllParentRoles(Role thisRole, List<Role> listOfRoles)
        {
            if (thisRole.ParentRoles.Count == 0) 
            {
                return;
            }
            else
            {
                foreach (Role parentRole in thisRole.ParentRoles)
                {
                    listOfRoles.Add(parentRole);
                    GetAllParentRoles(parentRole, listOfRoles);
                }
                return;
            }
        }

        public void GetAllParentRoleHierarchies(Role thisRole, List<RoleHierarchy> listOfRoleHierarchies, int maxHierarchies)
        {
            if (thisRole.ParentRoles.Count == 0) 
            {
                return;
            }
            else
            {
                foreach (Role parentRole in thisRole.ParentRoles)
                {
                    RoleHierarchy roleHierarchy = new RoleHierarchy();
                    roleHierarchy.Name = thisRole.Name;
                    roleHierarchy.Type = thisRole.Type;
                    roleHierarchy.GrantedTo = parentRole.Name;
                    roleHierarchy.AncestryPaths = thisRole.AncestryPaths;
                    roleHierarchy.NumAncestryPaths = roleHierarchy.AncestryPaths.Split('\n').Count();
                    roleHierarchy.DirectAncestry = String.Format("{0}->{1}", roleHierarchy.GrantedTo, roleHierarchy.Name);
                    listOfRoleHierarchies.Add(roleHierarchy);
                    if (listOfRoleHierarchies.Count >= maxHierarchies)
                    {
                        return;
                    } 
                    GetAllParentRoleHierarchies(parentRole, listOfRoleHierarchies, maxHierarchies);
                }
                return;
            }
        }

        public void GetAllChildRoles(Role thisRole, List<Role> listOfRoles)
        {
            if (thisRole.ChildRoles.Count == 0) 
            {
                return;
            }
            else
            {
                foreach (Role childRole in thisRole.ChildRoles)
                {
                    listOfRoles.Add(childRole);
                    GetAllChildRoles(childRole, listOfRoles);
                }
                return;
            }
        }

        public void GetAllChildRoleHierarchies(Role thisRole, List<RoleHierarchy> listOfRoleHierarchies, int maxHierarchies)
        {
            if (thisRole.ChildRoles.Count == 0) 
            {
                return;
            }
            else
            {
                foreach (Role childRole in thisRole.ChildRoles)
                {
                    RoleHierarchy roleHierarchy = new RoleHierarchy();
                    roleHierarchy.Name = childRole.Name;
                    roleHierarchy.Type = childRole.Type;
                    roleHierarchy.GrantedTo = thisRole.Name;
                    roleHierarchy.AncestryPaths = childRole.AncestryPaths;
                    roleHierarchy.NumAncestryPaths = roleHierarchy.AncestryPaths.Split('\n').Count();
                    roleHierarchy.DirectAncestry = String.Format("{0}->{1}", roleHierarchy.GrantedTo, roleHierarchy.Name);
                    listOfRoleHierarchies.Add(roleHierarchy);
                    if (listOfRoleHierarchies.Count >= maxHierarchies)
                    {
                        return;
                    } 
                    GetAllChildRoleHierarchies(childRole, listOfRoleHierarchies, maxHierarchies);
                }
                return;
            }
        }        
    }
}
