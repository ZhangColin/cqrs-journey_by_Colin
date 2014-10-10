using System;
using System.Runtime.Serialization;

namespace Infrastructure {
    /// <summary>
    /// 并发异常
    /// </summary>
    [Serializable]
    public class ConcurrencyException: Exception {
        public ConcurrencyException() {
        }

        public ConcurrencyException(string message)
            : base(message) {
        }

        public ConcurrencyException(string message, Exception innerException)
            : base(message, innerException) {
        }

        protected ConcurrencyException(SerializationInfo info, StreamingContext context)
            : base(info, context) {
        }
    }
}