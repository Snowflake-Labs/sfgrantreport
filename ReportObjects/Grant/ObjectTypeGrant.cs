using System;
using System.Globalization;

namespace Snowflake.GrantReport.ReportObjects
{
    public class ObjectTypeGrant
    {       
        public string DBName { get; set; }

        public string EntityName { get; set; }

        public string GrantedTo { get; set; }

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
                    this.EntityName = nameParts[1].Trim('"');
                }
                else if (nameParts.Length == 3)
                {
                    this.DBName = nameParts[0].Trim('"');
                    this.SchemaName = nameParts[1].Trim('"');
                    this.EntityName = nameParts[2].Trim('"');
                }
            }
        }

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
