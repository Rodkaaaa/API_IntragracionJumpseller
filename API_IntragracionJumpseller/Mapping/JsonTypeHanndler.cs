using System.Data;
using Dapper;
using Newtonsoft.Json;

namespace API_IntragracionJumpseller.Mapping;

public class JsonTypeHandler<T> : SqlMapper.TypeHandler<T>
{
    public override void SetValue(IDbDataParameter parameter, T value)
    {
        parameter.Value = JsonConvert.SerializeObject(value, JsonSettings.Settings);
    }

    public override T Parse(object value)
    {
        if (value is string json)
        {
            return JsonConvert.DeserializeObject<T>(json, JsonSettings.Settings);
        }

        return default;
    }
}

public static class JsonSettings
{
    public static JsonSerializerSettings Settings { get; set; } = new JsonSerializerSettings();
}
public class Json<T>
{
    public Json(T? value)
    {
        Value = value;
    }

    public T? Value { get; }
}


