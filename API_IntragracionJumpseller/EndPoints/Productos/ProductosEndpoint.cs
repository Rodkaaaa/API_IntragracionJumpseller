using DataAccess.Data.Productos;
using DataAccess.Models.ProductAndes;
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

            //app.MapGet($"{versionApi}/{controller}/MigrateProducts",
            //async (IConfiguration configuration) =>
            //await MigrateProductos(configuration))
            //.WithTags(controller);

            app.MapGet($"{versionApi}/{controller}/AndestoJumpProductos",
            async (IConfiguration configuration, IProductosData data) =>
            await AndestoJumpProductos(configuration, data))
            .WithTags(controller);

            app.MapGet($"{versionApi}/{controller}/UpdateProducts",
            async (IConfiguration configuration, IProductosData data) =>
            await UpdateProductos(configuration, data))
            .WithTags(controller);

            app.MapGet($"{versionApi}/{controller}/test",
            async (IConfiguration configuration, IProductosData data) =>
            await GetCountJumpseller(configuration, data))
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
                string loginPeru = configuration["TestJumpSeller:loginPeru"] ?? "";
                string tokenPeru = configuration["TestJumpSeller:tokenPeru"] ?? "";
                string loginShimano = configuration["TestJumpSeller:loginShimano"] ?? "";
                string tokenShimano = configuration["TestJumpSeller:tokenShimano"] ?? "";
                string imgbbToken = configuration["TestJumpSeller:imgbbToken"] ?? "";
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
                            var result = await MainServices.JumpSeller.HttpClientInstance.GetAsync($"{urlProducts}?login={loginPeru}&authtoken={tokenPeru}&limit=100&page={i}");
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
                                        string responseProductCreado = await resultPostProduct.Content.ReadAsStringAsync();
                                        ProductsModel? responseProductCreadoData = JsonConvert.DeserializeObject<ProductsModel>(responseProductCreado);
                                        string createArticleUrl = $"v1/products/{responseProductCreadoData?.product.id}/images.json";
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
                                                IDJumpseller = responseProductCreadoData.product.id,
                                                Sku = responseProductCreadoData.product.sku,
                                                NombreArticulo = responseProductCreadoData.product.name,
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
                                        string responseProductCreado = await resultPostProduct.Content.ReadAsStringAsync();
                                        ProductsModel? responseProductCreadoData = JsonConvert.DeserializeObject<ProductsModel>(responseProductCreado);
                                        createdProducts.Add(new ResponseCreacion
                                        {
                                            IDJumpseller = responseProductCreadoData.product.id,
                                            Sku = responseProductCreadoData.product.sku,
                                            NombreArticulo = responseProductCreadoData.product.name,
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
                string login = configuration["JumpSeller:LoginToken"] ?? "";
                string token = configuration["JumpSeller:AuthToken"] ?? "";
                string imgbbToken = configuration["JumpSeller:imgbbToken"] ?? "";
                List<ProductAndesModel> totalProductsList = new();
                List<ResponseCreacion> createdProducts = new();
                ProductsModel productPost = new() { };


                var ProductosAndes = await data.GetProductosAndes();
                totalProductsList = ProductosAndes.ToList();
                if (totalProductsList.Count > 0)
                {
                    foreach (var product in totalProductsList)
                    {
                        service = new MainServices();
                        var formData = new MultipartFormDataContent();
                        string imageBase64 = await ConvertImageUrlToBase64($"https://imgs.andesindustrial.cl/fotos/articulos/{product.IDArticulo}.jpg");
                        if (imageBase64 != "error")
                        {
                            formData.Add(new StringContent(imageBase64), "image");
                            var resultImgBB = await MainServices.ImgBB.HttpClientInstance.PostAsync($"{urlImgbbPost}?&key={imgbbToken}", formData);
                            if (resultImgBB.IsSuccessStatusCode)
                            {

                                string responseImgbb = await resultImgBB.Content.ReadAsStringAsync();
                                ResponseImgBBModel? responseImgbbData = JsonConvert.DeserializeObject<ResponseImgBBModel>(responseImgbb);

                                productPost = new() { product = new Product { categories = new List<Category>() } };
                                productPost.product.name = product.Nombre;
                                productPost.product.page_title = product.NombreWeb;
                                productPost.product.meta_description = product.Descripcion;
                                productPost.product.description = String.IsNullOrEmpty(product.TextoWeb) ? product.Descripcion : product.TextoWeb;
                                productPost.product.type = "physical";
                                productPost.product.price = product.PrecioVenta > 0 ? (float)((product.PrecioVenta * 2) * 0.85) : 1;
                                productPost.product.sku = product.IDArticulo;
                                productPost.product.stock = product.Stock;
                                productPost.product.barcode = product.IDArticulo;
                                productPost.product.brand = product.Marca;
                                productPost.product.status = product.Stock > 0 ? "available" : "not-available";
                                productPost.product.google_product_category = product.Grupo;
                                if (product.IDGrupoAgrupa > 0)
                                {
                                    productPost.product.categories.Add(new Category
                                    {
                                        id = product.IDGrupoAgrupa,
                                        name = product.GrupoAgrupa,
                                        parent_id = 0,
                                        permalink = product.GrupoAgrupa
                                    });
                                }
                                if (product.IDCategoriaAgrupa > 0)
                                {
                                    productPost.product.categories.Add(new Category
                                    {
                                        id = product.IDCategoriaAgrupa,
                                        name = product.CategoriaAgrupa,
                                        parent_id = 0,
                                        permalink = product.CategoriaAgrupa
                                    });
                                }
                                var resultPostProduct = await MainServices.JumpSeller.HttpClientInstance.PostAsJsonAsync<ProductsModel>($"{urlProducts}?login={login}&authtoken={token}", productPost);
                                if (resultPostProduct.IsSuccessStatusCode)
                                {
                                    string responseProductShimano = await resultPostProduct.Content.ReadAsStringAsync();
                                    ProductsModel? responseProductShimanoData = JsonConvert.DeserializeObject<ProductsModel>(responseProductShimano);
                                    string createArticleUrl = $"v1/products/{responseProductShimanoData?.product.id}/images.json";
                                    ImgJumpsellerModel imgPost = new ImgJumpsellerModel()
                                    {
                                        image = new ImagePost
                                        {
                                            url = responseImgbbData.data.url,
                                            position = 0
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
                                else
                                {
                                    string responseProductShimano = await resultPostProduct.Content.ReadAsStringAsync();
                                    ProductsModel? responseProductShimanoData = JsonConvert.DeserializeObject<ProductsModel>(responseProductShimano);
                                }
                            }
                            else
                            {
                                productPost = new() { product = new Product { categories = new List<Category>() } };
                                productPost.product.name = product.Nombre;
                                productPost.product.page_title = product.NombreWeb;
                                productPost.product.meta_description = product.Descripcion;
                                productPost.product.description = String.IsNullOrEmpty(product.TextoWeb) ? product.Descripcion : product.TextoWeb;
                                productPost.product.type = "physical";
                                productPost.product.price = product.PrecioVenta > 0 ? (float)((product.PrecioVenta * 2) * 0.85) : 1;
                                productPost.product.sku = product.IDArticulo;
                                productPost.product.stock = product.Stock;
                                productPost.product.barcode = product.IDArticulo;
                                productPost.product.brand = product.Marca;
                                productPost.product.status = product.Stock > 0 ? "available" : "not-available";
                                productPost.product.google_product_category = product.Grupo;
                                if (product.IDGrupoAgrupa > 0)
                                {
                                    productPost.product.categories.Add(new Category
                                    {
                                        id = product.IDGrupoAgrupa,
                                        name = product.GrupoAgrupa,
                                        parent_id = 0,
                                        permalink = product.GrupoAgrupa
                                    });
                                }
                                if (product.IDCategoriaAgrupa > 0)
                                {
                                    productPost.product.categories.Add(new Category
                                    {
                                        id = product.IDCategoriaAgrupa,
                                        name = product.CategoriaAgrupa,
                                        parent_id = 0,
                                        permalink = product.CategoriaAgrupa
                                    });
                                }

                                var resultPostProduct = await MainServices.JumpSeller.HttpClientInstance.PostAsJsonAsync($"{urlProducts}?login={login}&authtoken={token}", productPost);
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
                                        Sku = product.IDArticulo,
                                        NombreArticulo = product.Nombre,
                                        SiImg = "No",
                                        Status = "No Creado"
                                    });
                                }
                            }
                        }
                        else
                        {
                            productPost = new() { product = new Product { categories = new List<Category>() } };
                            productPost.product.name = product.Nombre;
                            productPost.product.page_title = product.NombreWeb;
                            productPost.product.meta_description = product.Descripcion;
                            productPost.product.description = String.IsNullOrEmpty(product.TextoWeb) ? product.Descripcion : product.TextoWeb;
                            productPost.product.type = "physical";
                            productPost.product.price = product.PrecioVenta > 0 ? (float)((product.PrecioVenta * 2) * 0.85) : 1;
                            productPost.product.sku = product.IDArticulo;
                            productPost.product.stock = product.Stock;
                            productPost.product.barcode = product.IDArticulo;
                            productPost.product.brand = product.Marca;
                            productPost.product.status = product.Stock > 0 ? "available" : "not-available";
                            productPost.product.google_product_category = product.Grupo;
                            if (product.IDGrupoAgrupa > 0)
                            {
                                productPost.product.categories.Add(new Category
                                {
                                    id = product.IDGrupoAgrupa,
                                    name = product.GrupoAgrupa,
                                    parent_id = 0,
                                    permalink = product.GrupoAgrupa
                                });
                            }
                            if (product.IDCategoriaAgrupa > 0)
                            {
                                productPost.product.categories.Add(new Category
                                {
                                    id = product.IDCategoriaAgrupa,
                                    name = product.CategoriaAgrupa,
                                    parent_id = 0,
                                    permalink = product.CategoriaAgrupa
                                });
                            }

                            var resultPostProduct = await MainServices.JumpSeller.HttpClientInstance.PostAsJsonAsync<ProductsModel>($"{urlProducts}?login={login}&authtoken={token}", productPost);
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
                                    Sku = product.IDArticulo,
                                    NombreArticulo = product.Nombre,
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
                string login = configuration["JumpSeller:LoginToken"] ?? "";
                string token = configuration["JumpSeller:AuthToken"] ?? "";
                List<ProductsModel> totalProductsList = new();
                List<ResponseCreacion> createdProducts = new();
                List<ProductAndesModel> ListaAndes = new();

                var ProductosAndes = await data.GetProductosAndes();
                ListaAndes = ProductosAndes.ToList();

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
                            var result = await MainServices.JumpSeller.HttpClientInstance.GetAsync($"{urlProducts}?login={login}&authtoken={token}&limit=100&page={i}");
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
                        if (ListaAndes.Any(x => x.IDArticulo == item.product.sku))
                        {
                            service = new MainServices();


                            item.product.stock = ListaAndes.Find(x => x.IDArticulo == item.product.sku).Stock;
                            item.product.status = ListaAndes.Find(x => x.IDArticulo == item.product.sku).Stock > 0 ? "available" : "not-available";



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
                        else
                        {
                            createdProducts.Add(new ResponseCreacion
                            {
                                IDJumpseller = item.product.id,
                                Sku = item.product.sku,
                                NombreArticulo = item.product.name,
                                SiImg = "",
                                Status = "No existe Lista Andes"
                            });
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

        private static async Task<IResult> GetCountJumpseller(IConfiguration configuration, IProductosData data)
        {
            string login = configuration["JumpSeller:LoginToken"] ?? "";
            string auth = configuration["JumpSeller:AuthToken"] ?? "";
            var response = await data.GetCountJumpseller(login, auth);
            return Results.Ok(response.count);
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
