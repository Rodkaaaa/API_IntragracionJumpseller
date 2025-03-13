using DataAccess.Models.Products;
using DataAccess.Shared.Services;
using Newtonsoft.Json;

namespace API_IntragracionJumpseller.EndPoints.Productos
{
    public static class ProductosEndpoint
    {
        private static IConfiguration _configuration;

        public static async void Configurar_ProductosEndpoint(this WebApplication app, IConfiguration configuration, string versionApi)
        {
            _configuration = configuration; // Asigna la configuración a la variable estática.
            string controller = "Productos"; // Define el nombre del controlador.

            // Configura la ruta GET para obtener productos.
            app.MapGet($"{versionApi}/{controller}/GetProductos",
            async (IConfiguration configuration) =>
            await GetProducts(configuration))
            .WithTags(controller);

            // Configura la ruta GET para obtener productos.
            app.MapGet($"{versionApi}/{controller}/UpdateProductos",
            async (IConfiguration configuration) =>
            await UpdateProductos(configuration))
            .WithTags(controller);
        }

        private static async Task<IResult> GetProducts(IConfiguration configuration)
        {
            try
            {
                MainServices service = new MainServices(); // Crea una instancia de MainServices.
                string urlProducts = "v1/products.json"; // Define la URL para obtener productos.
                string urlCount = "v1/products/count.json"; // Define la URL para obtener el conteo de productos.
                string urlImgbbPost = "1/upload"; // Define la URL para subir imágenes a ImgBB.
                string loginPeru = "f23cb72f86246e387cd40d892a508f59"; // Define el login para Perú.
                string tokenPeru = "edc68361f51feae4f871ae23eba581ea"; // Define el token para Perú.
                string loginShimano = "b2096c5eda7370c1eee69c9de9c15883"; // Define el login para Shimano.
                string tokenShimano = "e854b7ca1b3877825d8ee522d70ab608"; // Define el token para Shimano.
                string imgbbToken = "5badf53104d4acbe92cacf73cc8b381d"; // Define el token para ImgBB.
                List<ProductsModel> totalProductsList = new(); // Crea una lista para almacenar todos los productos.
                List<ResponseCreacion> createdProducts = new(); // Crea una lista para almacenar los productos creados.

                var resultCount = await MainServices.JumpSeller.HttpClientInstance.GetAsync($"{urlCount}?login={loginPeru}&authtoken={tokenPeru}"); // Realiza una solicitud GET para obtener el conteo de productos.
                if (resultCount.IsSuccessStatusCode) // Verifica si la solicitud fue exitosa.
                {
                    string responseCount = await resultCount.Content.ReadAsStringAsync(); // Lee el contenido de la respuesta.
                    CountModel? productCount = JsonConvert.DeserializeObject<CountModel>(responseCount); // Deserializa el conteo de productos.
                    if (productCount != null) // Verifica si el conteo de productos no es nulo.
                    {
                        int totalPages = (int)Math.Ceiling((decimal)productCount.count / 100); // Calcula el número total de páginas.
                        for (int i = 1; i <= totalPages + 1; i++) // Itera sobre cada página.
                        {
                            var result = await MainServices.JumpSeller.HttpClientInstance.GetAsync($"{urlProducts}?login={loginPeru}&authtoken={tokenPeru}&limit=100&page={i + 1}"); // Realiza una solicitud GET para obtener productos.
                            if (result.IsSuccessStatusCode) // Verifica si la solicitud fue exitosa.
                            {
                                string responseContent = await result.Content.ReadAsStringAsync(); // Lee el contenido de la respuesta.
                                List<ProductsModel>? response = JsonConvert.DeserializeObject<List<ProductsModel>>(responseContent); // Deserializa la lista de productos.
                                if (response != null && response.Count > 0) // Verifica si la respuesta no es nula y contiene productos.
                                {
                                    totalProductsList.AddRange(response.Where(x => x.product.sku != null).ToList().FindAll(x => x.product.sku.Contains("S") || x.product.sku.Contains("s"))); // Agrega los productos a la lista total.
                                }
                            }
                        }

                        foreach (var product in totalProductsList) // Itera sobre cada producto en la lista total.
                        {
                            service = new MainServices(); // Crea una nueva instancia de MainServices.
                            var formData = new MultipartFormDataContent(); // Crea un nuevo contenido de formulario multipart.
                            string imageBase64 = await ConvertImageUrlToBase64(product.product.images.FirstOrDefault()?.url ?? string.Empty); // Convierte la URL de la imagen a Base64.
                            if (imageBase64 != "error") // Verifica si la conversión fue exitosa.
                            {
                                formData.Add(new StringContent(imageBase64), "image"); // Agrega la imagen en Base64 al formulario.
                                var resultImgBB = await MainServices.ImgBB.HttpClientInstance.PostAsync($"{urlImgbbPost}?&key={imgbbToken}", formData); // Realiza una solicitud POST para subir la imagen a ImgBB.
                                if (resultImgBB.IsSuccessStatusCode) // Verifica si la solicitud fue exitosa.
                                {
                                    string responseImgbb = await resultImgBB.Content.ReadAsStringAsync(); // Lee el contenido de la respuesta.
                                    ResponseImgBBModel? responseImgbbData = JsonConvert.DeserializeObject<ResponseImgBBModel>(responseImgbb); // Deserializa la respuesta de ImgBB.
                                    product.product.images.FirstOrDefault().url = responseImgbbData.data.url; // Actualiza la URL de la imagen del producto.
                                    product.product.images.FirstOrDefault().id = 0; // Establece el ID de la imagen a 0.
                                    product.product.id = 0; // Establece el ID del producto a 0.
                                    var resultPostProduct = await MainServices.JumpSeller.HttpClientInstance.PostAsJsonAsync<ProductsModel>($"{urlProducts}?login={loginShimano}&authtoken={tokenShimano}", product); // Realiza una solicitud POST para crear el producto en JumpSeller.
                                    if (resultPostProduct.IsSuccessStatusCode) // Verifica si la solicitud fue exitosa.
                                    {
                                        string responseProductShimano = await resultPostProduct.Content.ReadAsStringAsync(); // Lee el contenido de la respuesta.
                                        ProductsModel? responseProductShimanoData = JsonConvert.DeserializeObject<ProductsModel>(responseProductShimano); // Deserializa la respuesta del producto.
                                        string createArticleUrl = $"v1/products/{responseProductShimanoData?.product.id}/images.json"; // Define la URL para crear la imagen del producto.
                                        ImgJumpsellerModel imgPost = new ImgJumpsellerModel() // Crea un nuevo modelo de imagen para JumpSeller.
                                        {
                                            image = new ImagePost
                                            {
                                                url = product.product.images.FirstOrDefault().url,
                                                position = product.product.images.FirstOrDefault().position
                                            }
                                        };
                                        var resultPostProductImgShimano = await MainServices.JumpSeller.HttpClientInstance.PostAsJsonAsync<ImgJumpsellerModel>($"{createArticleUrl}?login={loginShimano}&authtoken={tokenShimano}", imgPost); // Realiza una solicitud POST para crear la imagen del producto en JumpSeller.
                                        if (resultPostProductImgShimano.IsSuccessStatusCode) // Verifica si la solicitud fue exitosa.
                                        {
                                            string responseProductShimanoImg = await resultPostProductImgShimano.Content.ReadAsStringAsync(); // Lee el contenido de la respuesta.
                                            ImgJumpsellerModel? responseProductShimanoImgData = JsonConvert.DeserializeObject<ImgJumpsellerModel>(responseProductShimanoImg); // Deserializa la respuesta de la imagen del producto.
                                            createdProducts.Add(new ResponseCreacion // Agrega el producto creado a la lista de productos creados.
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
                                else // Si la solicitud para subir la imagen a ImgBB falla.
                                {
                                    var resultPostProduct = await MainServices.JumpSeller.HttpClientInstance.PostAsJsonAsync<ProductsModel>($"{urlProducts}?login={loginShimano}&authtoken={tokenShimano}", product); // Realiza una solicitud POST para crear el producto en JumpSeller sin imagen.
                                    if (resultPostProduct.IsSuccessStatusCode) // Verifica si la solicitud fue exitosa.
                                    {
                                        string responseProductShimano = await resultPostProduct.Content.ReadAsStringAsync(); // Lee el contenido de la respuesta.
                                        ProductsModel? responseProductShimanoData = JsonConvert.DeserializeObject<ProductsModel>(responseProductShimano); // Deserializa la respuesta del producto.
                                        createdProducts.Add(new ResponseCreacion // Agrega el producto creado a la lista de productos creados.
                                        {
                                            IDJumpseller = responseProductShimanoData.product.id,
                                            Sku = responseProductShimanoData.product.sku,
                                            NombreArticulo = responseProductShimanoData.product.name,
                                            SiImg = "No",
                                            Status = "Creado"
                                        });
                                    }
                                    else // Si la solicitud para crear el producto falla.
                                    {
                                        createdProducts.Add(new ResponseCreacion // Agrega el producto no creado a la lista de productos creados.
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
                            else // Si la conversión de la imagen a Base64 falla.
                            {
                                var resultPostProduct = await MainServices.JumpSeller.HttpClientInstance.PostAsJsonAsync<ProductsModel>($"{urlProducts}?login={loginShimano}&authtoken={tokenShimano}", product); // Realiza una solicitud POST para crear el producto en JumpSeller sin imagen.
                                if (resultPostProduct.IsSuccessStatusCode) // Verifica si la solicitud fue exitosa.
                                {
                                    string responseProductShimano = await resultPostProduct.Content.ReadAsStringAsync(); // Lee el contenido de la respuesta.
                                    ProductsModel? responseProductShimanoData = JsonConvert.DeserializeObject<ProductsModel>(responseProductShimano); // Deserializa la respuesta del producto.
                                    createdProducts.Add(new ResponseCreacion // Agrega el producto creado a la lista de productos creados.
                                    {
                                        IDJumpseller = responseProductShimanoData.product.id,
                                        Sku = responseProductShimanoData.product.sku,
                                        NombreArticulo = responseProductShimanoData.product.name,
                                        SiImg = "No",
                                        Status = "Creado"
                                    });
                                }
                                else // Si la solicitud para crear el producto falla.
                                {
                                    createdProducts.Add(new ResponseCreacion // Agrega el producto no creado a la lista de productos creados.
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
                return Results.Ok(createdProducts); // Devuelve la lista de productos creados.
            }
            catch (Exception ex) // Captura cualquier excepción que ocurra.
            {
                return Results.BadRequest(ex.Message); // Devuelve un mensaje de error.
            }
        }
        private static async Task<IResult> UpdateProductos(IConfiguration configuration)
        {
            try
            {
                MainServices service = new MainServices(); // Crea una instancia de MainServices.
                string urlProducts = "v1/products.json"; // Define la URL para obtener productos.
                string urlCount = "v1/products/count.json"; // Define la URL para obtener el conteo de productos.
                string urlImgbbPost = "1/upload"; // Define la URL para subir imágenes a ImgBB.
                string loginPeru = "f23cb72f86246e387cd40d892a508f59"; // Define el login para Perú.
                string tokenPeru = "edc68361f51feae4f871ae23eba581ea"; // Define el token para Perú.
                string loginShimano = "b2096c5eda7370c1eee69c9de9c15883"; // Define el login para Shimano.
                string tokenShimano = "e854b7ca1b3877825d8ee522d70ab608"; // Define el token para Shimano.
                string imgbbToken = "5badf53104d4acbe92cacf73cc8b381d"; // Define el token para ImgBB.
                List<ProductsModel> totalProductsList = new(); // Crea una lista para almacenar todos los productos.
                List<ResponseCreacion> createdProducts = new(); // Crea una lista para almacenar los productos creados.


                return Results.Ok("Updated"); // Devuelve un mensaje de error.

            }
            catch (Exception ex) // Captura cualquier excepción que ocurra.
            {
                return Results.BadRequest(ex.Message); // Devuelve un mensaje de error.
            }
        }
        static async Task<string> ConvertImageUrlToBase64(string imageUrl, long maxSizeInBytes = 32 * 1024 * 1024) // Método para convertir una URL de imagen a Base64.
        {
            try
            {
                using (HttpClient client = new HttpClient()) // Crea una instancia de HttpClient.
                {
                    // Descargar la imagen como un arreglo de bytes
                    byte[] imageBytes = await client.GetByteArrayAsync(imageUrl); // Descarga la imagen como un arreglo de bytes.

                    // Verificar el tamaño de la imagen
                    long imageSizeInBytes = imageBytes.Length; // Obtiene el tamaño de la imagen en bytes.

                    // Si la imagen es demasiado grande, devolver un error
                    if (imageSizeInBytes > maxSizeInBytes) // Verifica si la imagen es demasiado grande.
                    {
                        return "error"; // Devuelve un error si la imagen es demasiado grande.
                    }

                    // Convertir los bytes a Base64
                    string base64String = Convert.ToBase64String(imageBytes); // Convierte los bytes a Base64.

                    return base64String; // Devuelve la cadena en Base64.
                }
            }
            catch (Exception ex) // Captura cualquier excepción que ocurra.
            {
                return "error"; // Devuelve un error si ocurre una excepción.
            }
        }
    }
}