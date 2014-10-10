using System.IO;

namespace Infrastructure.Serialization {
    /// <summary>
    /// 文本序列化器接口
    /// </summary>
    public interface ITextSerializer {
        void Serialize(TextWriter writer, object graph);
        object Deserialize(TextReader reader);
    }
}