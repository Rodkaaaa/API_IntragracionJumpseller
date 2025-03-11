
using Dapper;
using DataAccess.Models.Products;
using System.Reflection.Metadata;


namespace API_IntragracionJumpseller.Mapping;

public static class SqlMapping
{
    public static async void AddMappingDapper(this WebApplication app)
    {
        SqlMapper.AddTypeHandler(new JsonTypeHandler<List<Category>>());
        SqlMapper.AddTypeHandler(new JsonTypeHandler<List<Image>>());
        SqlMapper.AddTypeHandler(new JsonTypeHandler<List<Variant>>());
       
    }
}
