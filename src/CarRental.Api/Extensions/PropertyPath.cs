using System.Text.Json;

namespace CarRental.Api.Extensions;

public static class PropertyPath
{
    public static string ToJsonName(string propertyName) =>
        string.IsNullOrEmpty(propertyName)
            ? propertyName
            : string.Join('.', propertyName.Split('.').Select(JsonNamingPolicy.CamelCase.ConvertName));
}
