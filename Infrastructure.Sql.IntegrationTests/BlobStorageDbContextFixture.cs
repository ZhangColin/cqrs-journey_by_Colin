using System;
using System.Text;
using Infrastructure.Sql.BlobStorage;
using NUnit.Framework;

namespace Infrastructure.Sql.IntegrationTests {
    [TestFixture]
    public class BlobStorageDbContextFixture: IDisposable {
        private readonly string _dbName = "BlobStorageDbContextFixture_" + Guid.NewGuid();

        public BlobStorageDbContextFixture() {
            using(var context = new BlobStorageDbContext(_dbName)) {
                if(context.Database.Exists()) {
                    context.Database.Delete();
                }
                context.Database.Create();
            }
        }

        public void Dispose() {
            using(var context = new BlobStorageDbContext(_dbName)) {
                if (context.Database.Exists()) {
                    context.Database.Delete();
                }
            }
        }

        [Test]
        public void WhenSavingBlobThenCanReadIt() {
            using(var storage = new BlobStorageDbContext(_dbName)) {
                storage.Save("test", "text/plain", Encoding.UTF8.GetBytes("Hello"));
            }

            using(var storage = new BlobStorageDbContext(_dbName)) {
                var data = Encoding.UTF8.GetString(storage.Find("test"));

                Assert.AreEqual("Hello", data);
            }
        }
        
        [Test]
        public void WhenUpdatingExistingBlobThenCanReadChanges() {
            using(var storage = new BlobStorageDbContext(_dbName)) {
                storage.Save("test", "text/plain", Encoding.UTF8.GetBytes("Hello"));
            }
            
            using(var storage = new BlobStorageDbContext(_dbName)) {
                storage.Save("test", "text/plain", Encoding.UTF8.GetBytes("World"));
            }

            using(var storage = new BlobStorageDbContext(_dbName)) {
                var data = Encoding.UTF8.GetString(storage.Find("test"));

                Assert.AreEqual("World", data);
            }
        }

    }
}