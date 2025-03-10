using DataAccess.DbAccess;
using Dapper;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace DataAccess.DbAccess;

public class SqlDataAccess : ISqlDataAccess
{
	private readonly IConfiguration _config;
	    
    public SqlDataAccess(IConfiguration config)
	{
		_config = config;
    }
    public async Task<IEnumerable<T>> LoadData<T, U>(string consulta, string tipoConsulta, U parameters, string connectionId = "Default")
    {
        try
        {
            using (IDbConnection connection = new SqlConnection(_config.GetConnectionString(connectionId)))
            {
                if (tipoConsulta.Equals("StoredProcedure"))
                {
                    return await connection.QueryAsync<T>(consulta, parameters, commandType: CommandType.StoredProcedure);
                }
                return await connection.QueryAsync<T>(consulta, parameters, commandType: CommandType.Text);
            };

        }
        catch (Exception ex)
        {
            string mensaje = ex.Message;
            return Enumerable.Empty<T>();
        }
    }

    public async Task<IEnumerable<T>> LoadData<T, U>(string storedProcedure, U parameters, string connectionId = "Default")
	{
		using IDbConnection connection = new SqlConnection(_config.GetConnectionString(connectionId));

		return await connection.QueryAsync<T>(storedProcedure, parameters, commandType: CommandType.StoredProcedure);
	}

    public async Task<string> LoadDataOutput<T>(string storedProcedure, DynamicParameters parameters, string connectionId = "Default")
    {
        using IDbConnection connection = new SqlConnection(_config.GetConnectionString(connectionId));

        try
        {
            var data = await connection.QueryAsync<string>(storedProcedure, parameters, commandType: CommandType.StoredProcedure);
            return parameters.Get<string>("Mensaje");
        }
        catch(Exception ex)
        {
            return ex.Message;
        }
        
    }

    public async Task<IEnumerable<T>> LoadData<T, U>(string storedProcedure, string connectionId = "Default")
    {
        using IDbConnection connection = new SqlConnection(_config.GetConnectionString(connectionId));

        return await connection.QueryAsync<T>(storedProcedure, commandType: CommandType.StoredProcedure);
    }

    public async Task<T> LoadMultiData<T, U>(string storedProcedure, U parameters, string connectionId = "Default")
    {
        
        var obj = (T)Activator.CreateInstance(typeof(T), false);
        try
        {
            using IDbConnection connection = new SqlConnection(_config.GetConnectionString(connectionId));
            var data = await connection.QueryMultipleAsync(storedProcedure, parameters, commandType: CommandType.StoredProcedure);

            foreach (PropertyInfo propertyInfo in obj.GetType().GetProperties())
            {
                propertyInfo.SetValue(obj, data.Read());
            }
            return obj;
        }
        catch(Exception ex)
        {
            return obj;
        }
    }

    public async Task SaveData<T>(string storedProcedure, T parameters, string connectionId = "Default")
	{
		using IDbConnection connection = new SqlConnection(_config.GetConnectionString(connectionId));

		await connection.ExecuteAsync(storedProcedure, parameters, commandType: CommandType.StoredProcedure);

	}



}
