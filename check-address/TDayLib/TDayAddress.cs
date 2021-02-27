using System;
using System.Collections.Generic;
using System.Text;

namespace TDayLib
{
    public class TDayAddress
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        public string Note { get; set; }
        public string HomePhone { get; set; }
        public string CellPHone { get; set; }
        public string Code { get; set; }
        public float Lon { get; set; }
        public float Lat { get; set; }

        public override string ToString()
        {
            return $"{FirstName},{LastName},{Address1},{Address2},{City},{State},{Zip},{Note},{HomePhone},{CellPHone},{Code},{Lon},{Lat}";
        }

        public string ToAddress()
        {
            return $"{Address1},{Zip}";
        }
    }
}
