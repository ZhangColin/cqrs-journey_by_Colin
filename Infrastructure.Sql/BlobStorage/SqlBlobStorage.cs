﻿using Infrastructure.BlobStorage;

namespace Infrastructure.Sql.BlobStorage {
    public class SqlBlobStorage: IBlobStorage {
        private readonly string _nameOrConnectionString;

        public SqlBlobStorage(string nameOrConnectionString) {
            this._nameOrConnectionString = nameOrConnectionString;
        }

        public byte[] Find(string id) {
            using(var context = new BlobStorageDbContext(_nameOrConnectionString)) {
                return context.Find(id);
            }
        }

        public void Save(string id, string contentType, byte[] blob) {
            using (var context = new BlobStorageDbContext(_nameOrConnectionString)) {
                context.Save(id, contentType, blob);
            }
        }

        public void Delete(string id) {
            using (var context = new BlobStorageDbContext(_nameOrConnectionString)) {
                context.Delete(id);
            }
        }
    }
}