using System;
using System.Text;
using Infrastructure.Sql.BlobStorage;
using NUnit.Framework;

namespace Infrastructure.Sql.IntegrationTests {
    [TestFixture]
    public class SqlBlobStorageFixture: IDisposable {
        private readonly string _dbName = "SqlBlobStorageFixture_" + Guid.NewGuid();

        public SqlBlobStorageFixture() {
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
            var storage = new SqlBlobStorage(_dbName);

            storage.Save("test", "text/plain", Encoding.UTF8.GetBytes("Hello"));

            var data = Encoding.UTF8.GetString(storage.Find("test"));

            Assert.AreEqual("Hello", data);
        }

        [Test]
        public void WhenUpdatingExistingBlobThenCanReadChanges() {
            var storage = new SqlBlobStorage(_dbName);

            storage.Save("test", "text/plain", Encoding.UTF8.GetBytes("Hello"));
            storage.Save("test", "text/plain", Encoding.UTF8.GetBytes("World"));

            var data = Encoding.UTF8.GetString(storage.Find("test"));

            Assert.AreEqual("World", data);
        }
    }
}