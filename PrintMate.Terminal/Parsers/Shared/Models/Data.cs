using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json; // Убедитесь, что установлен пакет Newtonsoft.Json

namespace ProjectParserTest.Parsers.Shared.Models
{
    public class Parameter
    {
        [JsonProperty]
        public string Key { get; set; }

        [JsonProperty]
        public object Value { get; set; }

        // Конструктор по умолчанию — обязателен для десериализации
        public Parameter() { }

        public Parameter(string key, object value)
        {
            Key = key;
            Value = value;
        }

        public void SetValue(object value)
        {
            Value = value;
        }

        public object GetValue() => Value;

        public T? GetValue<T>() => Value is T t ? t : default(T?);

        public void GetValue<T>(ref T value)
        {
            value = GetValue<T>();
        }
    }

    public class Data
    {
        [JsonProperty]
        public List<Parameter> DataList { get; set; } = new List<Parameter>();

        public Parameter AddParameter(string key, bool onlyKeyFilter = true)
        {
            var param = new Parameter(key, null);
            DataList.Add(param);
            return param;
        }

        public Parameter? GetParameter(string key)
        {
            return DataList.FirstOrDefault(p => p.Key == key);
        }

        public T GetParameterValue<T>(string key)
        {
            var param = GetParameter(key);
            if (param == null) return default(T);

            // Безопасное преобразование
            if (param.Value == null && default(T) == null)
                return default(T);

            return (T)Convert.ChangeType(param.Value, typeof(T));
        }

        public bool ContainsKey(string key) => DataList.Any(p => p.Key == key);

        public List<Parameter> GetList() => DataList;
    }
}