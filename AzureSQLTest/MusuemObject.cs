using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureSQLTest
{
    public class MuseumObject
    {
        public int Id { get; set; }
        public int ExternalId { get; set; }
        public string Title { get; set; }
        public string? Artist { get; set; }
        public string? Culture { get; set; }

        public string? PrimaryImageURL { get; set; }
        public string? ObjectURL { get; set; }   

    }
}
