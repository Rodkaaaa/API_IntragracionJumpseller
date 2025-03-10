using Dapper;

namespace DataAccess.DbAccess
{
    public interface ISqlDataAccess
    {
        Task<IEnumerable<T>> LoadData<T, U>(string consulta, string tipoConsulta, U parameters, string connectionId = "Default");
        //Task<string> LoadDataOutput<U>(string storedProcedure, string tipoConsulta, U parameters, string connectionId = "Default");
        Task<IEnumerable<T>> LoadData<T, U>(string storedProcedure, U parameters, string connectionId = "Default");
        Task<string> LoadDataOutput<T>(string storedProcedure, DynamicParameters parameters, string connectionId = "Default");
        Task<IEnumerable<T>> LoadData<T, U>(string storedProcedure, string connectionId = "Default");
        Task SaveData<T>(string storedProcedure, T parameters, string connectionId = "Default");
        Task<T> LoadMultiData<T,U>(string storedProcedure, U parameters, string connectionId = "Default");
    }
}