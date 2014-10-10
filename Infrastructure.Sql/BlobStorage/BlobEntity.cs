namespace Infrastructure.Sql.BlobStorage {
    public class BlobEntity {
        public string Id { get; private set; }
        public string ContentType { get; set; }
        public byte[] Blob { get; set; }
        public string BlobString { get; set; }

        protected BlobEntity() {
        }

        public BlobEntity(string id, string contentType, byte[] blob, string blobString) {
            this.Id = id;
            this.ContentType = contentType;
            this.Blob = blob;
            this.BlobString = blobString;
        }
    }
}