using System.Collections.Generic;

namespace Infrastructure {
    /// <summary>
    /// 元数据提供器接口
    /// </summary>
    public interface IMetadataProvider {
        IDictionary<string, string> GetMetadata(object payload);
    }
}