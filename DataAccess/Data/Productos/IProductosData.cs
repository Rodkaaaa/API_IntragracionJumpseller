using DataAccess.Models.ProductAndes;
using DataAccess.Models.Products;
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
        Task<CountModel> GetCountJumpseller( string login , string auth, string url = "v1/products/count.json");
        Task<List<ProductsModel>> GetPaginatedProductsFromJumpSeller(string login , string auth,int TotalPAges, string url = "v1/products.json");
        Task<string> PostImgByIDArticulo(string Token, string IDArticulo, string url = "1/upload");

    }
}
