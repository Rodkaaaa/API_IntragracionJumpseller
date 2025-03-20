using DataAccess.DbAccess;
using DataAccess.Models.ProductAndes;
using DataAccess.Models.Products;
using DataAccess.Shared.Services;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

        public async Task<CountModel> GetCountJumpseller(string login, string auth, string url = "v1/products/count.json")
        {
            var resultCount = await MainServices.JumpSeller.HttpClientInstance.GetAsync($"{url}?login={login}&authtoken={auth}");
            if (resultCount.IsSuccessStatusCode)
            {
                var content = await resultCount.Content.ReadAsStringAsync();
                CountModel count = JsonConvert.DeserializeObject<CountModel>(content);
                count.status = "success";
                return count;
            }
            else
            {
                var content = await resultCount.Content.ReadAsStringAsync();
                var response = JsonConvert.DeserializeObject(content) ?? "error";

                return new CountModel
                {
                    status = response.ToString() ?? "error"
                };


            }
        }

        public async Task<List<ProductsModel>> GetPaginatedProductsFromJumpSeller(string login, string auth, int totalPages, string url = "v1/products.json")
        {
            List<ProductsModel> totalProductsListJumseller = new();
            for (int i = 1; i <= totalPages; i++)
            {
                var result = await MainServices.JumpSeller.HttpClientInstance.GetAsync($"{url}?login={login}&authtoken={auth}&limit=100&page={i}");
                if (result.IsSuccessStatusCode)
                {
                    string responseContent = await result.Content.ReadAsStringAsync();
                    List<ProductsModel>? response = JsonConvert.DeserializeObject<List<ProductsModel>>(responseContent);
                    if (response != null && response.Count > 0)
                    {
                        totalProductsListJumseller.AddRange(response);
                    }
                }
                else
                {
                    string responseContent = await result.Content.ReadAsStringAsync();
                    List<ProductsModel>? response = JsonConvert.DeserializeObject<List<ProductsModel>>(responseContent);
                    if (response != null && response.Count > 0)
                    {
                        totalProductsListJumseller.AddRange(response);
                    }
                }
            }
            return totalProductsListJumseller;
        }

        public async Task<ResponseImgBBModel> PostImgByIDArticulo(string Token, string IDArticulo, string url = "1/upload")
        {
            MainServices service = new MainServices();
            var formData = new MultipartFormDataContent();
            string imageBase64 = await ConvertImageUrlToBase64($"https://imgs.andesindustrial.cl/fotos/articulos/{IDArticulo}.jpg");
            if (imageBase64 != "error")
            {
                formData.Add(new StringContent(imageBase64), "image");
                var resultImgBB = await MainServices.ImgBB.HttpClientInstance.PostAsync($"{url}?&key={Token}", formData);
                if (resultImgBB.IsSuccessStatusCode)
                {

                    string responseImgbb = await resultImgBB.Content.ReadAsStringAsync();
                    ResponseImgBBModel? responseImgbbData = JsonConvert.DeserializeObject<ResponseImgBBModel>(responseImgbb);
                    return responseImgbbData;
                }
            }
            return new ResponseImgBBModel() { };

        }
        static async Task<string> ConvertImageUrlToBase64(string imageUrl, long maxSizeInBytes = 32 * 1024 * 1024)
        {
            try
            {
                using (HttpClient client = new HttpClient())                {
                    byte[] imageBytes = await client.GetByteArrayAsync(imageUrl);
                    long imageSizeInBytes = imageBytes.Length;
                    if (imageSizeInBytes > maxSizeInBytes)
                    {
                        return "error";
                    }

                    string base64String = Convert.ToBase64String(imageBytes);
                    return base64String;
                }
            }
            catch (Exception ex)
            {
                return "error";
            }
        }
    }


}
