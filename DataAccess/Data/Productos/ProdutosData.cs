using DataAccess.DbAccess;
using DataAccess.Models.ProductAndes;
using Microsoft.Extensions.Configuration;
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
    }
}
