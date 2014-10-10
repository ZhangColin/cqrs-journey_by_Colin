using System;
using System.IO;
using Infrastructure.BlobStorage;
using Infrastructure.Serialization;
using Moq;
using NUnit.Framework;
using Registration.ReadModel;
using Registration.ReadModel.Implementation;

namespace Registration.Tests.ReadModel {
    [TestFixture]
    public class OrderDaoFixture {
        [Test]
        public void WhenFindingNonExistingAssignmentThenReturnsNull() {
            var storage = new Mock<IBlobStorage>();
            storage.SetReturnsDefault<byte[]>(null);
            var dao = new OrderDao(() => new ConferenceRegistrationDbContext("OrderDaoFixture"), storage.Object,
                Mock.Of<ITextSerializer>());

            var dto = dao.FindOrderSeats(Guid.NewGuid());

            Assert.Null(dto);
        }

        [Test]
        public void When_finding_existing_dao_Then_deserializes_blob_and_returns_instance() {
            var dto = new OrderSeats();
            var storage = Mock.Of<IBlobStorage>(x => x.Find(It.IsAny<string>()) == new byte[0]);
            var serializer = Mock.Of<ITextSerializer>(x => x.Deserialize(It.IsAny<TextReader>()) == dto);
            var dao = new OrderDao(() => new ConferenceRegistrationDbContext("OrderDaoFixture"), storage, serializer);

            var result = dao.FindOrderSeats(Guid.NewGuid());

            Assert.AreSame(result, dto);
        }
    }
}