using DataAccess.DbAccess;
using DataAccess.Models.ProductAndes;
using DataAccess.Models.Products;
using DataAccess.Shared.Services;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Data.Productos
{
    public class ProductosData : IProductosData
    {

        public readonly ISqlDataAccess _db;
        public readonly IConfiguration _configuration;
        private readonly int IDAllGestEmpresa;
        private readonly int IDEmpresa;
        private readonly string CompanyDBSAP;
        private readonly string CompanyDBFULL;
        private readonly string UsuarioActivo;

        public ProductosData(ISqlDataAccess db, IConfiguration configuration)
        {
            _db = db;
            _configuration = configuration;

            IDAllGestEmpresa = Convert.ToInt32(_configuration["AGSettings:IDAllgestEmpresa"]);
            IDEmpresa = Convert.ToInt32(_configuration["AGSettings:IDEmpresa"]);
            CompanyDBSAP = _configuration["AGSettings:CompanyDBSAP"];
            CompanyDBFULL = _configuration["AGSettings:CompanyDBFULL"];
            UsuarioActivo = _configuration["AGSettings:UsuarioActivo"];
        }

        public Task<IEnumerable<ProductAndesModel>> GetProductosAndes() =>
         _db.LoadData<ProductAndesModel, dynamic>(storedProcedure: "sp_Test_Articulos",
           new
           {
               IDAllGestEmpresa = IDAllGestEmpresa,
               IDEmpresa = IDEmpresa
           });

        public async Task<string> PostProductoToAndes(string IDArticulo, string IDJumpSeller)
        {
            var result = await _db.LoadData<string, dynamic>(
                storedProcedure: "sp_ActualizaArticulosBikesBagels",
                new
                {
                    IDAllGestEmpresa,
                    IDEmpresa,
                    IDArticulo,
                    IDJumpSeller
                });

            return result.FirstOrDefault() ?? string.Empty;
        }



        public async Task<CountModel> GetCountJumpseller(string login, string auth, string url = "v1/products/count.json")
        {
            var resultCount = await MainServices.JumpSeller.HttpClientInstance.GetAsync($"{url}?login={login}&authtoken={auth}");
            if (resultCount.IsSuccessStatusCode)
            {
                var content = await resultCount.Content.ReadAsStringAsync();
                CountModel count = JsonConvert.DeserializeObject<CountModel>(content);
                count.status = "Success";
                return count;
            }
            else
            {
                var content = await resultCount.Content.ReadAsStringAsync();
                string response = JsonConvert.DeserializeObject<string>(content) ?? "Error";

                return new CountModel
                {
                    status = response
                };


            }
        }

    }
}
