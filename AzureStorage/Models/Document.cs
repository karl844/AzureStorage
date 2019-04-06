using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AzureStorage.Models
{
    public class Document
    {
        public int DocumentId { get; set; }

        public string Directory { get; set; }
        public string FileShare { get; set; }
        public DateTime Created { get; set; }
        public string Name { get; set; }
        public int Size { get; set; }
       
    }
}
