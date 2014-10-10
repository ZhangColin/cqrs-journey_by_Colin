using System;
using System.Data.Entity;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using Infrastructure.BlobStorage;
using Infrastructure.Serialization;
using Registration.Handlers;

namespace Registration.ReadModel.Implementation {
    public class OrderDao: IOrderDao {
        private readonly Func<ConferenceRegistrationDbContext> _contextFactory;
        private readonly IBlobStorage _blobStorage;
        private readonly ITextSerializer _serializer;

        public OrderDao(Func<ConferenceRegistrationDbContext> contextFactory, IBlobStorage blobStorage, ITextSerializer serializer) {
            this._contextFactory = contextFactory;
            this._blobStorage = blobStorage;
            this._serializer = serializer;
        }

        public DraftOrder FindDraftOrder(Guid orderId) {
            using (var context = this._contextFactory.Invoke()) {
                return context.Query<DraftOrder>().Include(x => x.Lines).FirstOrDefault(dto => dto.OrderId == orderId);
            }
        }

        public Guid? LocateOrder(string email, string accessCode) {
            using(var context = this._contextFactory.Invoke()) {
                var orderProjection = context.Query<DraftOrder>()
                    .Where(o => o.RegistrantEmail == email && o.AccessCode == accessCode)
                    .Select(o => new {o.OrderId}).FirstOrDefault();
                if(orderProjection!=null) {
                    return orderProjection.OrderId;
                }
                return null;
            }
        }

        public PricedOrder FindPricedOrder(Guid orderId) {
            using (var context = this._contextFactory.Invoke()) {
                return context.Query<PricedOrder>().Include(x => x.Lines).FirstOrDefault(dto => dto.OrderId == orderId);
            }
        }

        public OrderSeats FindOrderSeats(Guid assignmentsId) {
            return this.FindBlob<OrderSeats>(SeatAssignmentsViewModelGenerator.GetSeatAssignmentsBlobId(assignmentsId));
        }

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "By design")]
        private T FindBlob<T>(string id) where T: class {
            var dto = this._blobStorage.Find(id);
            if(dto==null) {
                return null;
            }

            using(var stream = new MemoryStream(dto)) {
                using(var reader = new StreamReader(stream, Encoding.UTF8)) {
                    return (T)this._serializer.Deserialize(reader);
                }
            }
        }
    }
}