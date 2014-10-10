using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using Newtonsoft.Json;

namespace Infrastructure.Serialization {
    /// <summary>
    /// Json序列化器
    /// </summary>
    public class JsonTextSerializer: ITextSerializer {
        private readonly JsonSerializer _serializer;

        public JsonTextSerializer(): this(JsonSerializer.Create(new JsonSerializerSettings() {
            TypeNameHandling = TypeNameHandling.All,
            TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple
        })) {
        }

        public JsonTextSerializer(JsonSerializer serializer) {
            this._serializer = serializer;
        }

        public void Serialize(TextWriter writer, object graph) {
            var jsonWriter = new JsonTextWriter(writer);
#if DEBUG
            jsonWriter.Formatting=Formatting.Indented;
#endif
            this._serializer.Serialize(jsonWriter, graph);

            writer.Flush();
        }

        public object Deserialize(TextReader reader) {
            var jsonReader = new JsonTextReader(reader);

            try {
                return this._serializer.Deserialize(jsonReader);
            }
            catch(JsonSerializationException e) {
                throw new SerializationException(e.Message, e);
            }
        }
    }
}