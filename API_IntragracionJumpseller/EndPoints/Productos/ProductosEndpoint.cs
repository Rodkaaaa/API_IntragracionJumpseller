using DataAccess.Models.Products;
using DataAccess.Shared.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;

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
                string UrlProducts = "v1/products.json";
                string UrlCount = "v1/products/count.json";
                string UrlImgbbPost = "1/upload";
                string loginPeru = "f23cb72f86246e387cd40d892a508f59";
                string tokenPeru = "edc68361f51feae4f871ae23eba581ea";
                string loginShimano = "b2096c5eda7370c1eee69c9de9c15883";
                string tokenShimano = "e854b7ca1b3877825d8ee522d70ab608";
                string IMGBBToken = "b52aa020152698d332d91b38d42654be";
                List<ProductsModel> ListaProductosTotales = new() { };
                var ResultCount = await service.JumpSeller.HttpClientInstance.GetAsync($"{UrlCount}?login={loginPeru}&authtoken={tokenPeru}");
                if (ResultCount.IsSuccessStatusCode)
                {
                    string responseCount = await ResultCount.Content.ReadAsStringAsync();
                    CountModel? CantidadArtiuculos = JsonConvert.DeserializeObject<CountModel>(responseCount);
                    if (CantidadArtiuculos != null)
                    {
                        int Cantidad = (int)Math.Ceiling((decimal)CantidadArtiuculos.count / 100);
                        for (int i = 1; i <= Cantidad + 1; i++)
                        {
                            var Result = await service.JumpSeller.HttpClientInstance.GetAsync($"{UrlProducts}?login={loginPeru}&authtoken={tokenPeru}&limit=100&page={i + 1}");
                            if (Result.IsSuccessStatusCode)
                            {

                                string responseContent = await Result.Content.ReadAsStringAsync();
                                List<ProductsModel>? response = JsonConvert.DeserializeObject<List<ProductsModel>>(responseContent);
                                if (response.Count > 0)
                                {
                                    ListaProductosTotales.AddRange(response.FindAll(x => x.product.sku != null).FindAll(x => x.product.sku.Contains("S") || x.product.sku.Contains("s")));
                                }
                            }
                        }
                        foreach (var item in ListaProductosTotales.FindAll(x => x.product.sku == "S60723"))
                        {
                            var formData = new MultipartFormDataContent();
                            string imageBase64 = await ConvertImageUrlToBase64(item.product.images.FirstOrDefault()?.url ?? string.Empty);
                            formData.Add(new StringContent(imageBase64), "image");
                            var ResultImgBB = await service.ImgBB.HttpClientInstance.PostAsync($"{UrlImgbbPost}?name={item.product.sku}&key={IMGBBToken}", formData);
                            if (ResultImgBB.IsSuccessStatusCode)
                            {
                                string responseImgbb = await ResultImgBB.Content.ReadAsStringAsync();
                                ResponseImgBBModel? responseImgbbData = JsonConvert.DeserializeObject<ResponseImgBBModel>(responseImgbb);
                                item.product.images.FirstOrDefault().url = responseImgbbData.data.url;
                                item.product.images.FirstOrDefault().id = 0;
                                item.product.id = 0;
                                var ResultPostPorduct = await service.JumpSeller.HttpClientInstance.PostAsJsonAsync<ProductsModel>($"{UrlProducts}?login={loginShimano}&authtoken={tokenShimano}", item);
                                if (ResultPostPorduct.IsSuccessStatusCode)
                                {
                                    string responseProductoShimano = await ResultPostPorduct.Content.ReadAsStringAsync();
                                    ProductsModel? responseProductoShimanoData = JsonConvert.DeserializeObject<ProductsModel>(responseProductoShimano);
                                    ImgJumpsellerModel imgpost = new ImgJumpsellerModel()
                                    {
                                        image = new ImagePost{ 
                                            url = item.product.images.FirstOrDefault().url,
                                            position = item.product.images.FirstOrDefault().position
                                        }
                                    };
                                    var ResultPostPorductImgShimano = await service.JumpSeller.HttpClientInstance.PostAsJsonAsync<ImgJumpsellerModel>($"v1/products/{responseProductoShimanoData.product.id}/images.json?login={loginShimano}&authtoken={tokenShimano}", imgpost);
                                    if (ResultPostPorductImgShimano.IsSuccessStatusCode)
                                    {
                                        string responseProductoShimanoImg = await ResultPostPorductImgShimano.Content.ReadAsStringAsync();
                                        ImgJumpsellerModel? responseProductoShimanoImgData = JsonConvert.DeserializeObject<ImgJumpsellerModel>(responseProductoShimanoImg);
                                        return Results.Ok(responseProductoShimanoImgData);
                                    }

                                }
                            }
                        }

                    }
                }
                return Results.BadRequest("sin articulos");
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ex.Message);
            }
        }

        static async Task<string> ConvertImageUrlToBase64(string imageUrl)
        {
            using (HttpClient client = new HttpClient())
            {
                // Descargar la imagen como un arreglo de bytes
                byte[] imageBytes = await client.GetByteArrayAsync(imageUrl);

                // Convertir los bytes a Base64
                string base64String = Convert.ToBase64String(imageBytes);

                return base64String;
            }
        }

    }
}
