using System.IO;

namespace Infrastructure.Serialization {
    /// <summary>
    /// 文本序列化器扩展
    /// </summary>
    public static class TextSerializerExtensions {
        public static string Serialize<T>(this ITextSerializer serializer, T data) {
            using(var writer = new StringWriter()) {
                serializer.Serialize(writer, data);
                return writer.ToString();
            }
        }

        public static T Deserialize<T>(this ITextSerializer serializer, string serialized) {
            using(var reader = new StringReader(serialized)) {
                return (T)serializer.Deserialize(reader);
            }
        }
    }
}