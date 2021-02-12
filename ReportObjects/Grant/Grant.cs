using System;
using System.Collections.Generic;
using System.Linq;

namespace Snowflake.GrantReport.ReportObjects
{
    public class Grant
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

        public string DisplaySettingWithGrantOption
        {
            get
            {
                if (this.WithGrantOption == true)
                {
                    return "X+";
                }
                else
                {
                    return "X";
                }

            }
        }

        public string DBName { get; set; }

        public string EntityName { get; set; }

        public string GrantedBy { get; set; }

        public string GrantedTo { get; set; }

        public bool WithGrantOption { get; set; }

        private string _objectName = String.Empty;
        public string ObjectName 
        { 
            get
            {
                return this._objectName;
            }
            set
            {
                this._objectName = value;

                if (this._objectName.Contains('"') == false)
                {
                    string[] nameParts = this._objectName.Split('.');
                    if (nameParts.Length == 0)
                    {
                        this.EntityName = this._objectName.Trim('"');
                    }
                    else if (nameParts.Length == 1)
                    {
                        this.EntityName = this._objectName.Trim('"');
                    }
                    else if (nameParts.Length == 2)
                    {
                        this.DBName = nameParts[0].Trim('"');
                        this.SchemaName = nameParts[1].Trim('"');
                        this.EntityName =  this.SchemaName;
                    }
                    else if (nameParts.Length == 3)
                    {
                        this.DBName = nameParts[0].Trim('"');
                        this.SchemaName = nameParts[1].Trim('"');
                        this.EntityName = nameParts[2].Trim('"');
                    }
                }
                else
                {
                    string[] nameParts = this._objectName.Split('.');

                    // The name must contain periods.
                    // Periods are quoted in double quotes "
                    // That calls for better parsing
                    
                    List<string> firstPartParts = new List<string>();
                    List<string> secondPartParts = new List<string>();
                    List<string> thirdPartParts = new List<string>();

                    List<string> currentPart = firstPartParts;

                    bool inQuotedPart = false;

                    foreach (string namePart in nameParts)
                    {
                        currentPart.Add(namePart);

                        if (namePart.StartsWith('"') == false && namePart.EndsWith('"') == false)
                        {
                            // Unquoted part and therefore without periods
                            
                            if (inQuotedPart == false)
                            {
                                // Move to next part
                                if (currentPart == firstPartParts) 
                                {
                                    currentPart = secondPartParts;
                                }
                                else if (currentPart == secondPartParts) 
                                {
                                    currentPart = thirdPartParts;
                                }
                            }
                        }
                        else if (namePart.StartsWith('"'))
                        {
                            // Begin of the quoted part, therefore with periods
                            
                            // Keep going

                            inQuotedPart = true;
                        }
                        else if (namePart.EndsWith('"'))
                        {
                            // End of the quoted part, therefore with periods

                            inQuotedPart = false;

                            if (firstPartParts.Count > 0)
                            {
                                currentPart = secondPartParts;
                            }
                            if (secondPartParts.Count > 0)
                            {
                                currentPart = thirdPartParts;
                            }
                        }
                    }
                    
                    if (secondPartParts.Count == 0)
                    {
                        this.EntityName = String.Join(".", firstPartParts.ToArray()).Trim('"');

                    }
                    else if (thirdPartParts.Count == 0)
                    {
                        this.DBName = String.Join(".", firstPartParts.ToArray()).Trim('"');
                        this.SchemaName = String.Join(".", secondPartParts.ToArray()).Trim('"');
                        this.EntityName =  this.SchemaName;
                    }
                    else
                    {
                        this.DBName = String.Join(".", firstPartParts.ToArray()).Trim('"');
                        this.SchemaName = String.Join(".", secondPartParts.ToArray()).Trim('"');
                        this.EntityName = String.Join(".", thirdPartParts.ToArray()).Trim('"');
                    }
                }
            }
        }

        public string ObjectNameUnquoted
        {
            get 
            {
                if (this.DBName.Length == 0)
                {
                    return this.EntityName;
                }
                else
                {
                    if (this.ObjectType == "DATABASE")
                    {
                        return this.DBName;
                    }
                    else if (this.ObjectType == "SCHEMA")
                    {
                        return String.Format("{0}.{1}", this.DBName, this.EntityName);
                    }
                    else
                    {
                        return String.Format("{0}.{1}.{2}", this.DBName, this.SchemaName, this.EntityName);
                    }
                }
            }
        }

        public string ObjectType { get; set; }

        public string Privilege { get; set; }

        public string PrivilegeDisplayShort(Dictionary<string, string> privilegeNamesShortDict)
        { 
            string shortName = String.Empty;
            if (privilegeNamesShortDict.TryGetValue(this.Privilege, out shortName) == true)
            {
                if (this.WithGrantOption == true)
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
                if (this.WithGrantOption == true)
                {
                    return String.Format("{0}+", this.Privilege);
                }
                else
                {
                    return this.Privilege;
                }
            }
        }


        public string SchemaName { get; set; }

        public string UniqueIdentifier
        { 
            get
            {
                return String.Format("{0}-{1}-{2}-{3}-{4}", this.Privilege, this.ObjectType, this.ObjectName, this.GrantedTo, this.GrantedBy);
            }        
        }

        public override String ToString()
        {
            return String.Format(
                "Grant: {0} on {1} [{2}] to {3} is granted by {4} with grant option {5}",
                this.Privilege,
                this.ObjectName,
                this.ObjectType,
                this.GrantedTo,
                this.GrantedBy,
                this.WithGrantOption ? "On" : "Off");
        }
    }
}
