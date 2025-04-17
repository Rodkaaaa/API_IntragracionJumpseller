using DataAccess.Data.Productos;
using DataAccess.Models.ProductAndes;
using DataAccess.Models.Products;
using DataAccess.Shared.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using static DataAccess.Models.Products.CategoryModel;

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
                string urlCategory = "v1/categories.json";
                string login = configuration["JumpSeller:LoginToken"] ?? "";
                string token = configuration["JumpSeller:AuthToken"] ?? "";
                string imgbbToken = configuration["JumpSeller:imgbbToken"] ?? "";
                List<ProductAndesModel> totalProductsList = new();
                List<ProductsModel> totalProductsListJumseller = new();
                List<ResponseCreacion> createdProducts = new();
                ProductsModel productPost = new() { };
                List<CategoryResponse> Categortias = new();



                var ProductosAndes = await data.GetProductosAndes();
                totalProductsList = ProductosAndes.ToList();
                var resultCount = await MainServices.JumpSeller.HttpClientInstance.GetAsync($"{urlCount}?login={login}&authtoken={token}");
                if (resultCount.IsSuccessStatusCode)
                {
                    string responseCount = await resultCount.Content.ReadAsStringAsync();
                    CountModel? productCount = JsonConvert.DeserializeObject<CountModel>(responseCount);
                    if (productCount != null && productCount.count != 0)
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
                                    totalProductsListJumseller.AddRange(response);
                                }
                            }
                        }
                    }


                    if (totalProductsList.Count > 0)
                    {
                        foreach (var product in totalProductsList)
                        {
                            if (!totalProductsListJumseller.Exists(x => x.product.sku == product.IDArticulo))
                            {

                                if (product.CategorizacionWeb != null)
                                {
                                    foreach (var categoria in product.CategorizacionWeb)
                                    {
                                        await PostCategory(urlCategory, login, token, categoria);
                                    }
                                }

                                productPost = new() { product = new Product { categories = new List<Category>() } };
                                productPost.product.name = product.Nombre;
                                productPost.product.page_title = product.NombreWeb;
                                productPost.product.meta_description = product.DescripcionComercial;
                                productPost.product.description = !String.IsNullOrEmpty(product.DescripcionComercial) ? product.DescripcionComercial : product.TextoWeb; // descripcion comercial == "" dejo descripcion web
                                productPost.product.type = "physical";
                                productPost.product.price = product.PrecioVenta;
                                productPost.product.sku = product.IDArticulo;
                                productPost.product.stock = product.Stock;
                                productPost.product.barcode = product.IDArticulo;
                                productPost.product.brand = product.Marca;
                                productPost.product.status = product.Stock > 0 ? "available" : "not-available";
                                productPost.product.google_product_category = product.Grupo;
                                if (product.CategorizacionWeb != null)
                                {
                                    foreach (var categoria in product.CategorizacionWeb)
                                    {
                                        Categortias = await GetCategory(urlCategory, login, token);
                                        if (categoria.IDGrupo > 0 && categoria.IDGrupo != null)
                                        {
                                            productPost.product.categories.Add(new Category
                                            {
                                                id = categoria.IDGrupo,
                                                name = categoria.Grupo,
                                                parent_id = 0,
                                                permalink = categoria.Grupo
                                            });

                                            if (categoria.IDCategoria > 0 && categoria.IDCategoria != null)
                                            {
                                                productPost.product.categories.Add(new Category
                                                {
                                                    id = categoria.IDCategoria,
                                                    name = categoria.Categoria,
                                                    parent_id = categoria.IDGrupo,
                                                    permalink = categoria.Categoria
                                                });

                                                if (categoria.IDSubCategoria > 0 && categoria.IDSubCategoria != null)
                                                {
                                                    productPost.product.categories.Add(new Category
                                                    {
                                                        id = categoria.IDSubCategoria,
                                                        name = categoria.SubCategoria,
                                                        parent_id = categoria.IDSubCategoria,
                                                        permalink = categoria.SubCategoria
                                                    });
                                                }
                                            }

                                        }
                                        else if (categoria.IDCategoria > 0 && categoria.IDCategoria != null)
                                        {
                                            productPost.product.categories.Add(new Category
                                            {
                                                id = categoria.IDCategoria,
                                                name = categoria.Categoria,
                                                parent_id = 0,
                                                permalink = categoria.Categoria
                                            });

                                            if (categoria.IDSubCategoria > 0 && categoria.IDSubCategoria != null)
                                            {
                                                productPost.product.categories.Add(new Category
                                                {
                                                    id = categoria.IDSubCategoria,
                                                    name = categoria.SubCategoria,
                                                    parent_id = categoria.IDSubCategoria,
                                                    permalink = categoria.SubCategoria
                                                });
                                            }
                                        }
                                        else if (categoria.IDSubCategoria > 0 && categoria.IDSubCategoria != null)
                                        {
                                            productPost.product.categories.Add(new Category
                                            {
                                                id = categoria.IDSubCategoria,
                                                name = categoria.SubCategoria,
                                                parent_id = 0,
                                                permalink = categoria.SubCategoria
                                            });
                                        }
                                    }
                                }
                                var resultPostProduct = await MainServices.JumpSeller.HttpClientInstance.PostAsJsonAsync<ProductsModel>($"{urlProducts}?login={login}&authtoken={token}", productPost);
                                if (resultPostProduct.IsSuccessStatusCode)
                                {

                                    int count = 0;
                                    //Creacion de imagenes en nueva lista
                                    foreach (var imagen in product.Imagen)
                                    {
                                        service = new MainServices();
                                        var formData = new MultipartFormDataContent();
                                        string imageBase64 = await ConvertImageUrlToBase64($"https://imgs.andesindustrial.cl/fotos/articulos/{imagen.Imagen}");
                                        if (imageBase64 != "error")
                                        {
                                            formData.Add(new StringContent(imageBase64), "image");
                                            var resultImgBB = await MainServices.ImgBB.HttpClientInstance.PostAsync($"{urlImgbbPost}?&key={imgbbToken}", formData);
                                            if (resultImgBB.IsSuccessStatusCode)
                                            {

                                                string responseImgbb = await resultImgBB.Content.ReadAsStringAsync();
                                                ResponseImgBBModel? responseImgbbData = JsonConvert.DeserializeObject<ResponseImgBBModel>(responseImgbb);
                                                string responseProductShimano = await resultPostProduct.Content.ReadAsStringAsync();
                                                ProductsModel? responseProductShimanoData = JsonConvert.DeserializeObject<ProductsModel>(responseProductShimano);
                                                string createArticleUrl = $"v1/products/{responseProductShimanoData?.product.id}/images.json";
                                                ImgJumpsellerModel imgPost = new ImgJumpsellerModel()
                                                {
                                                    image = new ImagePost
                                                    {
                                                        url = responseImgbbData.data.url,
                                                        position = count
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
                                                count++;
                                            }
                                            else
                                            {
                                                string responseImgbb = await resultImgBB.Content.ReadAsStringAsync();
                                                return Results.Problem($"Error Articulo {product.IDArticulo} - {responseImgbb}");

                                                //TODO: intentar arreglar el problemas de las imagenes
                                                productPost = new() { product = new Product { categories = new List<Category>() } };
                                                productPost.product.name = product.Nombre;
                                                productPost.product.page_title = product.NombreWeb;
                                                productPost.product.meta_description = product.DescripcionComercial;
                                                productPost.product.description = !String.IsNullOrEmpty(product.DescripcionComercial) ? product.DescripcionComercial : product.TextoWeb;
                                                productPost.product.type = "physical";
                                                productPost.product.price = product.PrecioVenta;
                                                productPost.product.sku = product.IDArticulo;
                                                productPost.product.stock = product.Stock;
                                                productPost.product.barcode = product.IDArticulo;
                                                productPost.product.brand = product.Marca;
                                                productPost.product.status = product.Stock > 0 ? "available" : "not-available";
                                                productPost.product.google_product_category = product.Grupo;
                                                if (product.CategorizacionWeb != null)
                                                {
                                                    foreach (var categoria in product.CategorizacionWeb)
                                                    {
                                                        if (categoria.IDGrupo > 0 && categoria.IDGrupo != null)
                                                        {
                                                            productPost.product.categories.Add(new Category
                                                            {
                                                                id = categoria.IDGrupo,
                                                                name = categoria.Grupo,
                                                                parent_id = 0,
                                                                permalink = categoria.Grupo
                                                            });

                                                            if (categoria.IDCategoria > 0 && categoria.IDCategoria != null)
                                                            {
                                                                productPost.product.categories.Add(new Category
                                                                {
                                                                    id = categoria.IDCategoria,
                                                                    name = categoria.Categoria,
                                                                    parent_id = categoria.IDGrupo,
                                                                    permalink = categoria.Categoria
                                                                });

                                                                if (categoria.IDSubCategoria > 0 && categoria.IDSubCategoria != null)
                                                                {
                                                                    productPost.product.categories.Add(new Category
                                                                    {
                                                                        id = categoria.IDSubCategoria,
                                                                        name = categoria.SubCategoria,
                                                                        parent_id = categoria.IDSubCategoria,
                                                                        permalink = categoria.SubCategoria
                                                                    });
                                                                }
                                                            }

                                                        }
                                                        else if (categoria.IDCategoria > 0 && categoria.IDCategoria != null)
                                                        {
                                                            productPost.product.categories.Add(new Category
                                                            {
                                                                id = categoria.IDCategoria,
                                                                name = categoria.Categoria,
                                                                parent_id = 0,
                                                                permalink = categoria.Categoria
                                                            });

                                                            if (categoria.IDSubCategoria > 0 && categoria.IDSubCategoria != null)
                                                            {
                                                                productPost.product.categories.Add(new Category
                                                                {
                                                                    id = categoria.IDSubCategoria,
                                                                    name = categoria.SubCategoria,
                                                                    parent_id = categoria.IDSubCategoria,
                                                                    permalink = categoria.SubCategoria
                                                                });
                                                            }
                                                        }
                                                        else if (categoria.IDSubCategoria > 0 && categoria.IDSubCategoria != null)
                                                        {
                                                            productPost.product.categories.Add(new Category
                                                            {
                                                                id = categoria.IDSubCategoria,
                                                                name = categoria.SubCategoria,
                                                                parent_id = 0,
                                                                permalink = categoria.SubCategoria
                                                            });
                                                        }
                                                    }
                                                }
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
                                            productPost.product.meta_description = product.DescripcionComercial;
                                            productPost.product.description = !String.IsNullOrEmpty(product.DescripcionComercial) ? product.DescripcionComercial : product.TextoWeb;
                                            productPost.product.type = "physical";
                                            productPost.product.price = product.PrecioVenta;
                                            productPost.product.sku = product.IDArticulo;
                                            productPost.product.stock = product.Stock;
                                            productPost.product.barcode = product.IDArticulo;
                                            productPost.product.brand = product.Marca;
                                            productPost.product.status = product.Stock > 0 ? "available" : "not-available";
                                            productPost.product.google_product_category = product.Grupo;
                                            if (product.CategorizacionWeb != null)
                                            {
                                                foreach (var categoria in product.CategorizacionWeb)
                                                {
                                                    if (categoria.IDGrupo > 0 && categoria.IDGrupo != null)
                                                    {
                                                        productPost.product.categories.Add(new Category
                                                        {
                                                            id = categoria.IDGrupo,
                                                            name = categoria.Grupo,
                                                            parent_id = 0,
                                                            permalink = categoria.Grupo
                                                        });

                                                        if (categoria.IDCategoria > 0 && categoria.IDCategoria != null)
                                                        {
                                                            productPost.product.categories.Add(new Category
                                                            {
                                                                id = categoria.IDCategoria,
                                                                name = categoria.Categoria,
                                                                parent_id = categoria.IDGrupo,
                                                                permalink = categoria.Categoria
                                                            });

                                                            if (categoria.IDSubCategoria > 0 && categoria.IDSubCategoria != null)
                                                            {
                                                                productPost.product.categories.Add(new Category
                                                                {
                                                                    id = categoria.IDSubCategoria,
                                                                    name = categoria.SubCategoria,
                                                                    parent_id = categoria.IDSubCategoria,
                                                                    permalink = categoria.SubCategoria
                                                                });
                                                            }
                                                        }

                                                    }
                                                    else if (categoria.IDCategoria > 0 && categoria.IDCategoria != null)
                                                    {
                                                        productPost.product.categories.Add(new Category
                                                        {
                                                            id = categoria.IDCategoria,
                                                            name = categoria.Categoria,
                                                            parent_id = 0,
                                                            permalink = categoria.Categoria
                                                        });

                                                        if (categoria.IDSubCategoria > 0 && categoria.IDSubCategoria != null)
                                                        {
                                                            productPost.product.categories.Add(new Category
                                                            {
                                                                id = categoria.IDSubCategoria,
                                                                name = categoria.SubCategoria,
                                                                parent_id = categoria.IDSubCategoria,
                                                                permalink = categoria.SubCategoria
                                                            });
                                                        }
                                                    }
                                                    else if (categoria.IDSubCategoria > 0 && categoria.IDSubCategoria != null)
                                                    {
                                                        productPost.product.categories.Add(new Category
                                                        {
                                                            id = categoria.IDSubCategoria,
                                                            name = categoria.SubCategoria,
                                                            parent_id = 0,
                                                            permalink = categoria.SubCategoria
                                                        });
                                                    }
                                                }
                                            }

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
                                    //Toma de errores de jumpseller
                                    string responseProductShimano = await resultPostProduct.Content.ReadAsStringAsync();
                                    ProductsModel? responseProductShimanoData = JsonConvert.DeserializeObject<ProductsModel>(responseProductShimano);
                                    createdProducts.Add(new ResponseCreacion
                                    {
                                        IDJumpseller = 0,
                                        Sku = product.IDArticulo,
                                        NombreArticulo = product.Nombre,
                                        SiImg = "No",
                                        Status = responseProductShimano
                                    });
                                }

                            }
                            else
                            {
                                createdProducts.Add(new ResponseCreacion
                                {
                                    IDJumpseller = 0,
                                    Sku = product.IDArticulo,
                                    NombreArticulo = product.Nombre,
                                    SiImg = "No",
                                    Status = "Existe"
                                });
                            }
                        }
                    }
                    else
                    {
                        return Results.BadRequest("No existen productos para crear");
                    }

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
                string urlCategoryPUT = "v1/categories";
                string urlCategory = "v1/categories.json";
                string login = configuration["JumpSeller:LoginToken"] ?? "";
                string token = configuration["JumpSeller:AuthToken"] ?? "";
                List<ProductsModel> totalProductsList = new();
                List<ResponseCreacion> createdProducts = new();
                List<ProductAndesModel> ListaAndes = new();
                List<CategoryResponse> Categortias = new();

                Categortias = await GetCategory(urlCategory, login, token);
                foreach (var categoria in Categortias)
                {
                    await putCategoryAsync(urlCategoryPUT, login, token, categoria.Category.name, categoria.Category.parent_id, categoria.Category.id);
                }


                var ProductosAndes = await data.GetProductosAndes();
                ListaAndes = ProductosAndes.ToList();
                if (ListaAndes.Count() > 0)
                {
                    var resultCount = await MainServices.JumpSeller.HttpClientInstance.GetAsync($"{urlCount}?login={login}&authtoken={token}");
                    if (resultCount.IsSuccessStatusCode)
                    {
                        string responseCount = await resultCount.Content.ReadAsStringAsync();
                        CountModel? productCount = JsonConvert.DeserializeObject<CountModel>(responseCount);
                        if (productCount != null && productCount.count != 0)
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
                        else
                        {
                            return Results.BadRequest("No hay articulos en Jumpseller para actualizar");
                        }
                        foreach (var item in totalProductsList)
                        {
                            if (ListaAndes.Any(x => x.IDArticulo == item.product.sku))
                            {
                                service = new MainServices();
                                var resultPostToAndes = await data.PostProductoToAndes(item.product.sku, item.product.id.ToString());
                                item.product.stock = ListaAndes.Find(x => x.IDArticulo == item.product.sku).Stock;
                                item.product.price = ListaAndes.Find(x => x.IDArticulo == item.product.sku).PrecioVenta;
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
                                        Status = "Actualizado",
                                        ActualizadoStatusAndes = resultPostToAndes
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
                                        Status = "No Actualizado",
                                        ActualizadoStatusAndes = resultPostToAndes

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
                                    Status = "No existe Lista Andes",
                                    ActualizadoStatusAndes = "NO"
                                });
                            }
                        }
                    }
                    else
                    {
                        return Results.BadRequest("No existen productos para crear");
                    }
                }
                else
                {
                    return Results.BadRequest("No Hay articulos de la lista andes");
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
            return Results.Ok(response);
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
        static async Task<List<CategoryResponse>> GetCategory(string urlCategory, string login, string token)
        {
            try
            {
                List<CategoryResponse> Categortias = new();
                var resultCategory = await MainServices.JumpSeller.HttpClientInstance.GetAsync($"{urlCategory}?login={login}&authtoken={token}");
                if (resultCategory.IsSuccessStatusCode)
                {
                    string responseContentCategory = await resultCategory.Content.ReadAsStringAsync();
                    Categortias = JsonConvert.DeserializeObject<List<CategoryResponse>>(responseContentCategory);
                    return Categortias;
                }
                else
                {
                    return new List<CategoryResponse>() { };
                }
            }
            catch (Exception e)
            {

                return new List<CategoryResponse>() { };
            }
        }
        static async Task PostCategory(string urlCategory, string login, string token, CategorizacionWebItemModel data)
        {
            try
            {
                List<CategoryResponse> existingCategories = await GetCategory(urlCategory, login, token);

                // Crear grupo si no existe
                if (data.IDGrupo != 0 && !existingCategories.Any(x => x.Category.name == data.Grupo))
                {
                    var groupResponse = await PostCategoryAsync(urlCategory, login, token, data.Grupo, null);
                    if (groupResponse != null && data.IDCategoria != 0)
                    {
                        // Crear categoría si no existe
                        var categoryResponse = await PostCategoryAsync(urlCategory, login, token, data.Categoria, groupResponse.Category.id);
                        if (categoryResponse != null && data.IDSubCategoria != 0)
                        {
                            // Crear subcategoría si no existe
                            await PostCategoryAsync(urlCategory, login, token, data.SubCategoria, categoryResponse.Category.id);
                        }
                    }
                }
                else if (data.IDCategoria != 0 && !existingCategories.Any(x => x.Category.name == data.Categoria))
                {
                    // Crear categoría si no existe
                    var parentId = existingCategories.FirstOrDefault(x => x.Category.name == data.Grupo)?.Category.id;
                    var categoryResponse = await PostCategoryAsync(urlCategory, login, token, data.Categoria, parentId);
                    if (categoryResponse != null && data.IDSubCategoria != 0)
                    {
                        // Crear subcategoría si no existe
                        await PostCategoryAsync(urlCategory, login, token, data.SubCategoria, categoryResponse.Category.id);
                    }
                }
                else if (data.IDSubCategoria != 0 && !existingCategories.Any(x => x.Category.name == data.SubCategoria))
                {
                    // Crear subcategoría si no existe
                    var parentId = existingCategories.FirstOrDefault(x => x.Category.name == data.Categoria)?.Category.id;
                    await PostCategoryAsync(urlCategory, login, token, data.SubCategoria, parentId);
                }
            }
            catch (Exception ex)
            {
                // Manejo de errores
                Console.Error.WriteLine($"Error al procesar la categoría: {ex.Message}");
            }
        }
        private static async Task<CategoryResponse?> PostCategoryAsync(string urlCategory, string login, string token, string name, int? parentId)
        {
            try
            {
                var categoryToPost = new CategoryResponse
                {
                    Category = new CategoryModel
                    {
                        name = name,
                        parent_id = parentId
                    }
                };

                var response = await MainServices.JumpSeller.HttpClientInstance.PostAsJsonAsync($"{urlCategory}?login={login}&authtoken={token}", categoryToPost);
                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<CategoryResponse>(responseContent);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error al crear la categoría '{name}': {ex.Message}");
            }

            return null;
        }
        private static async Task<CategoryResponse?> putCategoryAsync(string urlCategory, string login, string token, string name, int? parentId, int? IDCategory)
        {
            try
            {
                var categoryToPost = new CategoryResponse
                {
                    Category = new CategoryModel
                    {
                        name = name.ToUpper(),
                        parent_id = parentId
                    }
                };

                var response = await MainServices.JumpSeller.HttpClientInstance.PutAsJsonAsync($"{urlCategory}/{IDCategory}.json?login={login}&authtoken={token}", categoryToPost);
                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<CategoryResponse>(responseContent);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error al crear la categoría '{name}': {ex.Message}");
            }

            return null;
        }

    }
}
