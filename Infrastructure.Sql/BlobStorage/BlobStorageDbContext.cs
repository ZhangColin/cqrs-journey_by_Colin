using System.Data.Entity;
using System.IO;

namespace Infrastructure.Sql.BlobStorage {
    public class BlobStorageDbContext: DbContext {
        public const string SchemaName = "BlobStorage";

        public BlobStorageDbContext(string nameOrConnectionString)
            : base(nameOrConnectionString) {
        }

        public byte[] Find(string id) {
            var blob = this.Set<BlobEntity>().Find(id);
            if(blob==null) {
                return null;
            }

            return blob.Blob;
        }

        public void Save(string id, string contentType, byte[] blob) {
            var existing = this.Set<BlobEntity>().Find(id);
            string blobString = "";
            if(contentType=="text/plain") {
                Stream stream = null;
                try {
                    stream = new MemoryStream(blob);
                    using(var reader = new StreamReader(stream)) {
                        stream = null;
                        blobString = reader.ReadToEnd();
                    }
                }
                finally {
                    if(stream!=null) {
                        stream.Dispose();
                    }
                }
            }

            if(existing!=null) {
                existing.Blob = blob;
                existing.BlobString = blobString;
            }
            else {
                this.Set<BlobEntity>().Add(new BlobEntity(id, contentType, blob, blobString));
            }

            this.SaveChanges();
        }

        public void Delete(string id) {
            var blob = this.Set<BlobEntity>().Find(id);
            if(blob==null) {
                return;
            }
            this.Set<BlobEntity>().Remove(blob);
            this.SaveChanges();
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<BlobEntity>().ToTable("Blobs", SchemaName);
        }
    }
}