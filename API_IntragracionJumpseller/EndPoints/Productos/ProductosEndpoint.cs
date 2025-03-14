using DataAccess.Data.Productos;
using DataAccess.Models.Products;
using DataAccess.Shared.Services;
using Newtonsoft.Json;

namespace API_IntragracionJumpseller.EndPoints.Productos
{
    public static class ProductosEndpoint
    {
        private static IConfiguration _configuration;

        public static void ConfigurarProductosEndpoint(this WebApplication app, IConfiguration configuration, string versionApi)
        {
            _configuration = configuration;
            string controller = "Productos";

            app.MapGet($"{versionApi}/{controller}/MigrateProducts",
            async (IConfiguration configuration) =>
            await MigrateProductos(configuration))
            .WithTags(controller);

            app.MapGet($"{versionApi}/{controller}/AndestoJumpProductos",
            async (IConfiguration configuration, IProductosData data) =>
            await AndestoJumpProductos(configuration, data))
            .WithTags(controller);

            app.MapGet($"{versionApi}/{controller}/UpdateProducts",
            async (IConfiguration configuration, IProductosData data) =>
            await UpdateProductos(configuration, data))
            .WithTags(controller);
        }

        private static async Task<IResult> MigrateProductos(IConfiguration configuration)
        {
            try
            {
                MainServices service = new MainServices();
                string urlProducts = "v1/products.json";
                string urlCount = "v1/products/count.json";
                string urlImgbbPost = "1/upload";
                string loginPeru = "f23cb72f86246e387cd40d892a508f59";
                string tokenPeru = "edc68361f51feae4f871ae23eba581ea";
                string loginShimano = "b2096c5eda7370c1eee69c9de9c15883";
                string tokenShimano = "e854b7ca1b3877825d8ee522d70ab608";
                string imgbbToken = "5badf53104d4acbe92cacf73cc8b381d";
                List<ProductsModel> totalProductsList = new();
                List<ResponseCreacion> createdProducts = new();

                var resultCount = await MainServices.JumpSeller.HttpClientInstance.GetAsync($"{urlCount}?login={loginPeru}&authtoken={tokenPeru}");
                if (resultCount.IsSuccessStatusCode)
                {
                    string responseCount = await resultCount.Content.ReadAsStringAsync();
                    CountModel? productCount = JsonConvert.DeserializeObject<CountModel>(responseCount);
                    if (productCount != null)
                    {
                        int totalPages = (int)Math.Ceiling((decimal)productCount.count / 100);
                        for (int i = 1; i <= totalPages; i++)
                        {
                            var result = await MainServices.JumpSeller.HttpClientInstance.GetAsync($"{urlProducts}?login={loginPeru}&authtoken={tokenPeru}&limit=100&page={i + 1}");
                            if (result.IsSuccessStatusCode)
                            {
                                string responseContent = await result.Content.ReadAsStringAsync();
                                List<ProductsModel>? response = JsonConvert.DeserializeObject<List<ProductsModel>>(responseContent);
                                if (response != null && response.Count > 0)
                                {
                                    totalProductsList.AddRange(response.Where(x => x.product.sku != null).ToList().FindAll(x => x.product.sku.Contains("S") || x.product.sku.Contains("s")));
                                }
                            }
                        }

                        foreach (var product in totalProductsList)
                        {
                            service = new MainServices();
                            var formData = new MultipartFormDataContent();
                            string imageBase64 = await ConvertImageUrlToBase64(product.product.images.FirstOrDefault()?.url ?? string.Empty);
                            if (imageBase64 != "error")
                            {
                                formData.Add(new StringContent(imageBase64), "image");
                                var resultImgBB = await MainServices.ImgBB.HttpClientInstance.PostAsync($"{urlImgbbPost}?&key={imgbbToken}", formData);
                                if (resultImgBB.IsSuccessStatusCode)
                                {
                                    string responseImgbb = await resultImgBB.Content.ReadAsStringAsync();
                                    ResponseImgBBModel? responseImgbbData = JsonConvert.DeserializeObject<ResponseImgBBModel>(responseImgbb);
                                    product.product.images.FirstOrDefault().url = responseImgbbData.data.url;
                                    product.product.images.FirstOrDefault().id = 0;
                                    product.product.id = 0;
                                    var resultPostProduct = await MainServices.JumpSeller.HttpClientInstance.PostAsJsonAsync<ProductsModel>($"{urlProducts}?login={loginShimano}&authtoken={tokenShimano}", product);
                                    if (resultPostProduct.IsSuccessStatusCode)
                                    {
                                        string responseProductShimano = await resultPostProduct.Content.ReadAsStringAsync();
                                        ProductsModel? responseProductShimanoData = JsonConvert.DeserializeObject<ProductsModel>(responseProductShimano);
                                        string createArticleUrl = $"v1/products/{responseProductShimanoData?.product.id}/images.json";
                                        ImgJumpsellerModel imgPost = new ImgJumpsellerModel()
                                        {
                                            image = new ImagePost
                                            {
                                                url = product.product.images.FirstOrDefault().url,
                                                position = product.product.images.FirstOrDefault().position
                                            }
                                        };
                                        var resultPostProductImgShimano = await MainServices.JumpSeller.HttpClientInstance.PostAsJsonAsync<ImgJumpsellerModel>($"{createArticleUrl}?login={loginShimano}&authtoken={tokenShimano}", imgPost);
                                        if (resultPostProductImgShimano.IsSuccessStatusCode)
                                        {
                                            string responseProductShimanoImg = await resultPostProductImgShimano.Content.ReadAsStringAsync();
                                            ImgJumpsellerModel? responseProductShimanoImgData = JsonConvert.DeserializeObject<ImgJumpsellerModel>(responseProductShimanoImg);
                                            createdProducts.Add(new ResponseCreacion
                                            {
                                                IDJumpseller = responseProductShimanoData.product.id,
                                                Sku = responseProductShimanoData.product.sku,
                                                NombreArticulo = responseProductShimanoData.product.name,
                                                SiImg = "si",
                                                Status = "Creado"
                                            });
                                        }
                                    }
                                }
                                else
                                {
                                    var resultPostProduct = await MainServices.JumpSeller.HttpClientInstance.PostAsJsonAsync<ProductsModel>($"{urlProducts}?login={loginShimano}&authtoken={tokenShimano}", product);
                                    if (resultPostProduct.IsSuccessStatusCode)
                                    {
                                        string responseProductShimano = await resultPostProduct.Content.ReadAsStringAsync();
                                        ProductsModel? responseProductShimanoData = JsonConvert.DeserializeObject<ProductsModel>(responseProductShimano);
                                        createdProducts.Add(new ResponseCreacion
                                        {
                                            IDJumpseller = responseProductShimanoData.product.id,
                                            Sku = responseProductShimanoData.product.sku,
                                            NombreArticulo = responseProductShimanoData.product.name,
                                            SiImg = "No",
                                            Status = "Creado"
                                        });
                                    }
                                    else
                                    {
                                        createdProducts.Add(new ResponseCreacion
                                        {
                                            IDJumpseller = 0,
                                            Sku = product.product.sku,
                                            NombreArticulo = product.product.name,
                                            SiImg = "No",
                                            Status = "No Creado"
                                        });
                                    }
                                }
                            }
                            else
                            {
                                var resultPostProduct = await MainServices.JumpSeller.HttpClientInstance.PostAsJsonAsync<ProductsModel>($"{urlProducts}?login={loginShimano}&authtoken={tokenShimano}", product);
                                if (resultPostProduct.IsSuccessStatusCode)
                                {
                                    string responseProductShimano = await resultPostProduct.Content.ReadAsStringAsync();
                                    ProductsModel? responseProductShimanoData = JsonConvert.DeserializeObject<ProductsModel>(responseProductShimano);
                                    createdProducts.Add(new ResponseCreacion
                                    {
                                        IDJumpseller = responseProductShimanoData.product.id,
                                        Sku = responseProductShimanoData.product.sku,
                                        NombreArticulo = responseProductShimanoData.product.name,
                                        SiImg = "No",
                                        Status = "Creado"
                                    });
                                }
                                else
                                {
                                    createdProducts.Add(new ResponseCreacion
                                    {
                                        IDJumpseller = 0,
                                        Sku = product.product.sku,
                                        NombreArticulo = product.product.name,
                                        SiImg = "No",
                                        Status = "No Creado"
                                    });
                                }
                            }
                        }
                    }
                }
                else
                {
                    return Results.BadRequest("No existen productos para crear");
                }
                return Results.Ok(createdProducts);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ex.Message);
            }
        }
        private static async Task<IResult> AndestoJumpProductos(IConfiguration configuration, IProductosData data)
        {
            try
            {
                MainServices service = new MainServices();
                string urlProducts = "v1/products.json";
                string urlCount = "v1/products/count.json";
                string urlImgbbPost = "1/upload";
                string login = "b2096c5eda7370c1eee69c9de9c15883";
                string token = "e854b7ca1b3877825d8ee522d70ab608";
                string imgbbToken = "5badf53104d4acbe92cacf73cc8b381d";
                List<dynamic> totalProductsList = new();
                List<ResponseCreacion> createdProducts = new();

                if (totalProductsList.Count > 0)
                {
                    foreach (var product in totalProductsList)
                    {
                        service = new MainServices();
                        var formData = new MultipartFormDataContent();
                        string imageBase64 = await ConvertImageUrlToBase64($"https://imgs.andesindustrial.cl/fotos/articulos/{product.IDartiuclo}.jpg");
                        if (imageBase64 != "error")
                        {
                            formData.Add(new StringContent(imageBase64), "image");
                            var resultImgBB = await MainServices.ImgBB.HttpClientInstance.PostAsync($"{urlImgbbPost}?&key={imgbbToken}", formData);
                            if (resultImgBB.IsSuccessStatusCode)
                            {
                                string responseImgbb = await resultImgBB.Content.ReadAsStringAsync();
                                ResponseImgBBModel? responseImgbbData = JsonConvert.DeserializeObject<ResponseImgBBModel>(responseImgbb);
                                product.product.images.FirstOrDefault().url = responseImgbbData.data.url;
                                product.product.images.FirstOrDefault().id = 0;
                                product.product.id = 0;
                                var resultPostProduct = await MainServices.JumpSeller.HttpClientInstance.PostAsJsonAsync<ProductsModel>($"{urlProducts}?login={login}&authtoken={token}", (ProductsModel)product);
                                if (resultPostProduct.IsSuccessStatusCode)
                                {
                                    string responseProductShimano = await resultPostProduct.Content.ReadAsStringAsync();
                                    ProductsModel? responseProductShimanoData = JsonConvert.DeserializeObject<ProductsModel>(responseProductShimano);
                                    string createArticleUrl = $"v1/products/{responseProductShimanoData?.product.id}/images.json";
                                    ImgJumpsellerModel imgPost = new ImgJumpsellerModel()
                                    {
                                        image = new ImagePost
                                        {
                                            url = product.product.images.FirstOrDefault().url,
                                            position = product.product.images.FirstOrDefault().position
                                        }
                                    };
                                    var resultPostProductImgShimano = await MainServices.JumpSeller.HttpClientInstance.PostAsJsonAsync<ImgJumpsellerModel>($"{createArticleUrl}?login={login}&authtoken={token}", imgPost);
                                    if (resultPostProductImgShimano.IsSuccessStatusCode)
                                    {
                                        string responseProductShimanoImg = await resultPostProductImgShimano.Content.ReadAsStringAsync();
                                        ImgJumpsellerModel? responseProductShimanoImgData = JsonConvert.DeserializeObject<ImgJumpsellerModel>(responseProductShimanoImg);
                                        createdProducts.Add(new ResponseCreacion
                                        {
                                            IDJumpseller = responseProductShimanoData.product.id,
                                            Sku = responseProductShimanoData.product.sku,
                                            NombreArticulo = responseProductShimanoData.product.name,
                                            SiImg = "si",
                                            Status = "Creado"
                                        });
                                    }
                                }
                            }
                            else
                            {
                                var resultPostProduct = await MainServices.JumpSeller.HttpClientInstance.PostAsJsonAsync($"{urlProducts}?login={login}&authtoken={token}", (ProductsModel)product);
                                if (resultPostProduct.IsSuccessStatusCode)
                                {
                                    string responseProductShimano = await resultPostProduct.Content.ReadAsStringAsync();
                                    ProductsModel? responseProductShimanoData = JsonConvert.DeserializeObject<ProductsModel>(responseProductShimano);
                                    createdProducts.Add(new ResponseCreacion
                                    {
                                        IDJumpseller = responseProductShimanoData.product.id,
                                        Sku = responseProductShimanoData.product.sku,
                                        NombreArticulo = responseProductShimanoData.product.name,
                                        SiImg = "No",
                                        Status = "Creado"
                                    });
                                }
                                else
                                {
                                    createdProducts.Add(new ResponseCreacion
                                    {
                                        IDJumpseller = 0,
                                        Sku = product.product.sku,
                                        NombreArticulo = product.product.name,
                                        SiImg = "No",
                                        Status = "No Creado"
                                    });
                                }
                            }
                        }
                        else
                        {
                            var resultPostProduct = await MainServices.JumpSeller.HttpClientInstance.PostAsJsonAsync<ProductsModel>($"{urlProducts}?login={login}&authtoken={token}", (ProductsModel)product);
                            if (resultPostProduct.IsSuccessStatusCode)
                            {
                                string responseProductShimano = await resultPostProduct.Content.ReadAsStringAsync();
                                ProductsModel? responseProductShimanoData = JsonConvert.DeserializeObject<ProductsModel>(responseProductShimano);
                                createdProducts.Add(new ResponseCreacion
                                {
                                    IDJumpseller = responseProductShimanoData.product.id,
                                    Sku = responseProductShimanoData.product.sku,
                                    NombreArticulo = responseProductShimanoData.product.name,
                                    SiImg = "No",
                                    Status = "Creado"
                                });
                            }
                            else
                            {
                                createdProducts.Add(new ResponseCreacion
                                {
                                    IDJumpseller = 0,
                                    Sku = product.product.sku,
                                    NombreArticulo = product.product.name,
                                    SiImg = "No",
                                    Status = "No Creado"
                                });
                            }
                        }
                    }
                }
                else
                {
                    return Results.BadRequest("No existen productos para crear");
                }
                return Results.Ok(createdProducts);

            }
            catch (Exception ex)
            {
                return Results.BadRequest(ex.Message);
            }
        }
        private static async Task<IResult> UpdateProductos(IConfiguration configuration, IProductosData data)
        {
            try
            {
                MainServices service = new MainServices();
                string urlProducts = "v1/products.json";
                string urlUpdateProducts = "v1/products/";
                string urlCount = "v1/products/count.json";
                string login = "f23cb72f86246e387cd40d892a508f59";
                string token = "edc68361f51feae4f871ae23eba581ea";
                List<ProductsModel> totalProductsList = new();
                List<ResponseCreacion> createdProducts = new();
                List<dynamic> ListaAndes = new();

                var resultCount = await MainServices.JumpSeller.HttpClientInstance.GetAsync($"{urlCount}?login={login}&authtoken={token}");
                if (resultCount.IsSuccessStatusCode)
                {
                    string responseCount = await resultCount.Content.ReadAsStringAsync();
                    CountModel? productCount = JsonConvert.DeserializeObject<CountModel>(responseCount);
                    if (productCount != null)
                    {
                        int totalPages = (int)Math.Ceiling((decimal)productCount.count / 100);
                        for (int i = 1; i <= totalPages; i++)
                        {
                            var result = await MainServices.JumpSeller.HttpClientInstance.GetAsync($"{urlProducts}?login={login}&authtoken={token}&limit=100&page={i + 1}");
                            if (result.IsSuccessStatusCode)
                            {
                                string responseContent = await result.Content.ReadAsStringAsync();
                                List<ProductsModel>? response = JsonConvert.DeserializeObject<List<ProductsModel>>(responseContent);
                                if (response != null && response.Count > 0)
                                {
                                    totalProductsList.AddRange(response);
                                }
                            }
                        }
                    }
                    foreach (var item in totalProductsList)
                    {
                        service = new MainServices();

                        if (ListaAndes.Find(x => x.sku == item.product.sku).stock > 0)
                        {
                            item.product.stock = ListaAndes.Find(x => x.sku == item.product.sku).stock;
                            item.product.status = "available";
                        }
                        else if (ListaAndes.Find(x => x.sku == item.product.sku).stock == 0)
                        {
                            item.product.stock = ListaAndes.Find(x => x.sku == item.product.sku).stock;
                            item.product.status = "disabled";
                        }
                        var ResponsePutPRoducto = await MainServices.JumpSeller.HttpClientInstance.PutAsJsonAsync($"{urlUpdateProducts}{item.product.id}.json?login={login}&authtoken={token}", item);
                        if (ResponsePutPRoducto.IsSuccessStatusCode)
                        {
                            createdProducts.Add(new ResponseCreacion
                            {
                                IDJumpseller = item.product.id,
                                Sku = item.product.sku,
                                NombreArticulo = item.product.name,
                                SiImg = "",
                                Status = "Actualizado"
                            });

                        }
                        else
                        {
                            createdProducts.Add(new ResponseCreacion
                            {
                                IDJumpseller = item.product.id,
                                Sku = item.product.sku,
                                NombreArticulo = item.product.name,
                                SiImg = "",
                                Status = "No Actualizado"
                            });
                        }
                    }
                }
                else
                {
                    return Results.BadRequest("No existen productos para crear");
                }

                return Results.Ok("Updated");
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ex.Message);
            }
        }
        static async Task<string> ConvertImageUrlToBase64(string imageUrl, long maxSizeInBytes = 32 * 1024 * 1024)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
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
