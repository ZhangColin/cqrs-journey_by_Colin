using System.Collections.Generic;
using Infrastructure.BlobStorage;

namespace Registration.IntegrationTests {
    public class MemoryBlobStorage: IBlobStorage {
        private Dictionary<string, byte[]> _blobs = new Dictionary<string, byte[]>();

        public byte[] Find(string id) {
            byte[] blob = null;
            this._blobs.TryGetValue(id, out blob);
            return blob;
        }

        public void Save(string id, string contentType, byte[] blob) {
            this._blobs[id] = blob;
        }

        public void Delete(string id) {
            this._blobs.Remove(id);
        }
    }
}