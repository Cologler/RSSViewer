using Microsoft.Extensions.DependencyInjection;

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RSSViewer.Services
{
    public class JsonService
    {
        private readonly JsonSerializerOptions _jsonSerializerOptions;

        public JsonService(IServiceProvider serviceProvider)
        {
            this._jsonSerializerOptions = new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                IgnoreNullValues = true,
                WriteIndented = true
            };

            foreach (var converter in serviceProvider.GetServices<JsonConverter>())
            {
                this._jsonSerializerOptions.Converters.Add(converter);
            }
        }

        public string Serialize<TValue>(TValue value)
        {
            return JsonSerializer.Serialize(value, this._jsonSerializerOptions);
        }

        public TValue Deserialize<TValue>(string json)
        {
            return JsonSerializer.Deserialize<TValue>(json, this._jsonSerializerOptions);
        }
    }
}
