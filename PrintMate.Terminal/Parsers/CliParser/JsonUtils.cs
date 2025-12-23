using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace ProjectParserTest.Parsers.CliParser;

public static class JsonUtils
{
    public static T GetParameterValue<T>(this JObject jObject, string path)
    {
        if (jObject == null)
            throw new ArgumentNullException(nameof(jObject));

        JToken token = jObject.SelectToken(path);

        if (token == null || token.Type == JTokenType.Null)
            throw new KeyNotFoundException($"Параметр '{path}' не найден в JSON.");

        try
        {
            return token.ToObject<T>();
        }
        catch (Exception ex)
        {
            throw new InvalidCastException($"Не удалось преобразовать значение '{token}' по пути '{path}' к типу {typeof(T).Name}.", ex);
        }
    }
}