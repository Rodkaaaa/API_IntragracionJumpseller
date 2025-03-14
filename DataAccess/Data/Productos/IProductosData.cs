using DataAccess.Models.ProductAndes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Data.Productos
{
    public interface IProductosData
    {
        Task<IEnumerable<ProductAndesModel>> GetProductosAndes();
    }
}
