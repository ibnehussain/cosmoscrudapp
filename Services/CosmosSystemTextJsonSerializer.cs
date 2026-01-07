using Microsoft.Azure.Cosmos;
using System.Text.Json;

namespace CosmosApp.Services
{
    public class CosmosSystemTextJsonSerializer : CosmosSerializer
    {
        private readonly JsonSerializerOptions _serializerOptions;

        public CosmosSystemTextJsonSerializer(JsonSerializerOptions serializerOptions)
        {
            _serializerOptions = serializerOptions;
        }

        public override T FromStream<T>(Stream stream)
        {
            using (stream)
            {
                if (stream.CanSeek && stream.Length == 0)
                {
                    return default!;
                }

                if (typeof(Stream).IsAssignableFrom(typeof(T)))
                {
                    return (T)(object)stream;
                }

                return JsonSerializer.Deserialize<T>(stream, _serializerOptions)!;
            }
        }

        public override Stream ToStream<T>(T input)
        {
            var streamPayload = new MemoryStream();
            JsonSerializer.Serialize(streamPayload, input, _serializerOptions);
            streamPayload.Position = 0;
            return streamPayload;
        }
    }
}