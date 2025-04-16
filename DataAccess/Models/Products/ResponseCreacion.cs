using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Models.Products
{
    public class ResponseCreacion
    {
        public int IDJumpseller { get; set; }
        public string Sku { get; set; }
        public string NombreArticulo { get; set; }
        public string Status { get; set; }
        public string SiImg { get; set; }
        public string ActualizadoStatusAndes { get; set; }
    }
}
