
using Dapper;
using DataAccess.Models.ProductAndes;
using DataAccess.Models.Products;
using System.Reflection.Metadata;
using static DataAccess.Models.Products.CategoryModel;


namespace API_IntragracionJumpseller.Mapping;

public static class SqlMapping
{
    public static async void AddMappingDapper(this WebApplication app)
    {
        SqlMapper.AddTypeHandler(new JsonTypeHandler<List<Category>>());
        SqlMapper.AddTypeHandler(new JsonTypeHandler<List<Image>>());
        SqlMapper.AddTypeHandler(new JsonTypeHandler<List<Variant>>());
        SqlMapper.AddTypeHandler(new JsonTypeHandler<List<ImagenArticulo>>());
        SqlMapper.AddTypeHandler(new JsonTypeHandler<List<CategorizacionWebItemModel>>());
        SqlMapper.AddTypeHandler(new JsonTypeHandler<List<ProductsCategryModel>>());
       
    }
}
