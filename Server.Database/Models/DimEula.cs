using System;

namespace Server.Database.Models
{
    public partial class DimEula
    {
        public int Id { get; set; }
        public string EulaTitle { get; set; }
        public string EulaBody { get; set; }
        public DateTime CreateDt { get; set; }
        public DateTime? ModifiedDt { get; set; }
        public DateTime FromDt { get; set; }
        public DateTime? ToDt { get; set; }
    }
}
