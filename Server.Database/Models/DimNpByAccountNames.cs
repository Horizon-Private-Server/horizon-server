using System;

namespace Server.Database.Models
{
    public partial class DimNpByAccountNames
    {
        public int Id { get; set; }

        public DateTime CreateDt { get; set; }
        public DateTime? ModifiedDt { get; set; }
        public DateTime FromDt { get; set; }
    }
}
