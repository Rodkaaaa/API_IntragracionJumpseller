using DataAccess.Models.Products;
using DataAccess.Shared.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace API_IntragracionJumpseller.EndPoints.Productos
{
    public static class ProductosEndpoint
    {
        private static IConfiguration _configuration;

        public static async void Configurar_ProductosEndpoint(this WebApplication app, IConfiguration configuration, string versionApi)
        {
            _configuration = configuration;
            string Controller = "Produtos";

            // GET: 
            app.MapGet($"{versionApi}/{Controller}/GetProducts",
            async (IConfiguration configuration) =>
            await Get(configuration))
            .WithTags(Controller);
        }

        private static async Task<IResult> Get(IConfiguration configuration)
        {
            try
            {
                MainServices service;
                service = new MainServices();
                string UrlToken = "v1/products.json";
                string login = "0f6aecd9e622b9ac5893f41ca4052aab";
                string token = "a8b4539ea07e784623eb74acdc758cf0";
                var Result = await service.JumpSeller.HttpClientInstance.GetAsync($"{UrlToken}?login={login}&authtoken={token}");
                if (Result.IsSuccessStatusCode)
                {
                    string responseContent = await Result.Content.ReadAsStringAsync();
                    Console.WriteLine(responseContent); // Log the response content
                    List<ProductsModel> response = JsonConvert.DeserializeObject<List<ProductsModel>>(responseContent);
                    return Results.Ok(response);
                }
                return Results.BadRequest("bad");

            }
            catch (Exception ex)
            {
                return Results.BadRequest(ex.Message);
            }
        }

    }
}
