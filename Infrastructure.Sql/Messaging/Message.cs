using System;

namespace Infrastructure.Sql.Messaging {
    public class Message {
        public string Body { get; private set; }
        public DateTime? DeliveryDate { get; private set; }
        public string CorrelationId { get; private set; }

        public Message(string body, DateTime? deliveryDate=null, string correlationId = null) {
            this.Body = body;
            this.DeliveryDate = deliveryDate;
            this.CorrelationId = correlationId;
        }
    }
}