namespace Infrastructure.BlobStorage {
    /// <summary>
    /// 二进制大对象存储接口
    /// </summary>
    public interface IBlobStorage {
        byte[] Find(string id);
        void Save(string id, string contentType, byte[] blob);
        void Delete(string id);
    }
}