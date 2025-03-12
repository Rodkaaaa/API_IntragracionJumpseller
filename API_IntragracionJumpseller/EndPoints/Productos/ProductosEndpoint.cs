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
            int count = 0;

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
                string IMGBBToken = "5badf53104d4acbe92cacf73cc8b381d";
                List<ProductsModel> ListaProductosTotales = new() { };
                var ResultCount = await MainServices.JumpSeller.HttpClientInstance.GetAsync($"{UrlCount}?login={loginPeru}&authtoken={tokenPeru}");
                if (ResultCount.IsSuccessStatusCode)
                {
                    string responseCount = await ResultCount.Content.ReadAsStringAsync();
                    CountModel? CantidadArtiuculos = JsonConvert.DeserializeObject<CountModel>(responseCount);
                    if (CantidadArtiuculos != null)
                    {
                        int Cantidad = (int)Math.Ceiling((decimal)CantidadArtiuculos.count / 100);
                        for (int i = 1; i <= Cantidad + 1; i++)
                        {
                            var Result = await MainServices.JumpSeller.HttpClientInstance.GetAsync($"{UrlProducts}?login={loginPeru}&authtoken={tokenPeru}&limit=100&page={i + 1}");
                            if (Result.IsSuccessStatusCode)
                            {

                                string responseContent = await Result.Content.ReadAsStringAsync();
                                List<ProductsModel>? response = JsonConvert.DeserializeObject<List<ProductsModel>>(responseContent);
                                if (response != null && response.Count > 0)
                                {
                                    ListaProductosTotales.AddRange(response.Where(x => x.product.sku != null).ToList().FindAll(x => x.product.sku.Contains("S") || x.product.sku.Contains("s")));
                                }
                            }
                        }
                        foreach (var item in ListaProductosTotales)
                        {
                            service = new MainServices();
                            var formData = new MultipartFormDataContent();
                            string imageBase64 = await ConvertImageUrlToBase64(item.product.images.FirstOrDefault()?.url ?? string.Empty);
                            if (imageBase64 != "error")
                            {
                                formData.Add(new StringContent(imageBase64), "image");
                                var ResultImgBB = await MainServices.ImgBB.HttpClientInstance.PostAsync($"{UrlImgbbPost}?&key={IMGBBToken}", formData) ;
                                if (ResultImgBB.IsSuccessStatusCode)
                                {
                                    string responseImgbb = await ResultImgBB.Content.ReadAsStringAsync();
                                    ResponseImgBBModel? responseImgbbData = JsonConvert.DeserializeObject<ResponseImgBBModel>(responseImgbb);
                                    item.product.images.FirstOrDefault().url = responseImgbbData.data.url;
                                    item.product.images.FirstOrDefault().id = 0;
                                    item.product.id = 0;
                                    var ResultPostPorduct = await MainServices.JumpSeller.HttpClientInstance.PostAsJsonAsync<ProductsModel>($"{UrlProducts}?login={loginShimano}&authtoken={tokenShimano}", item);
                                    if (ResultPostPorduct.IsSuccessStatusCode)
                                    {

                                        string responseProductoShimano = await ResultPostPorduct.Content.ReadAsStringAsync();
                                        ProductsModel? responseProductoShimanoData = JsonConvert.DeserializeObject<ProductsModel>(responseProductoShimano);
                                        string Creararticulo = $"v1/products/{responseProductoShimanoData?.product.id}/images.json";
                                        ImgJumpsellerModel imgpost = new ImgJumpsellerModel()
                                        {
                                            image = new ImagePost
                                            {
                                                url = item.product.images.FirstOrDefault().url,
                                                position = item.product.images.FirstOrDefault().position
                                            }
                                        };
                                        var ResultPostPorductImgShimano = await MainServices.JumpSeller.HttpClientInstance.PostAsJsonAsync<ImgJumpsellerModel>($"{Creararticulo}?login={loginShimano}&authtoken={tokenShimano}", imgpost);
                                        if (ResultPostPorductImgShimano.IsSuccessStatusCode)
                                        {
                                            string responseProductoShimanoImg = await ResultPostPorductImgShimano.Content.ReadAsStringAsync();
                                            ImgJumpsellerModel? responseProductoShimanoImgData = JsonConvert.DeserializeObject<ImgJumpsellerModel>(responseProductoShimanoImg);
                                        }

                                    }
                                }
                                else
                                {
                                    var ResultPostPorduct = await MainServices.JumpSeller.HttpClientInstance.PostAsJsonAsync<ProductsModel>($"{UrlProducts}?login={loginShimano}&authtoken={tokenShimano}", item);
                                 
                                }
                            }
                            else
                            {
                                var ResultPostPorduct = await MainServices.JumpSeller.HttpClientInstance.PostAsJsonAsync<ProductsModel>($"{UrlProducts}?login={loginShimano}&authtoken={tokenShimano}", item);
                            }
                        }

                    }
                }
                return Results.Ok("ARticulos creados");
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ex.Message);
            }
        }

        static async Task<string> ConvertImageUrlToBase64(string imageUrl, long maxSizeInBytes = 32 * 1024 * 1024) // 32 MB por defecto
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // Descargar la imagen como un arreglo de bytes
                    byte[] imageBytes = await client.GetByteArrayAsync(imageUrl);

                    // Verificar el tamaño de la imagen
                    long imageSizeInBytes = imageBytes.Length;

                    // Si la imagen es demasiado grande, devolver un error
                    if (imageSizeInBytes > maxSizeInBytes)
                    {
                        return "error";
                    }

                    // Convertir los bytes a Base64
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
