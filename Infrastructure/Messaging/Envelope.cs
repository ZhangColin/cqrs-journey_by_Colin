using System;

namespace Infrastructure.Messaging {
    public abstract class Envelope {
        public static Envelope<T> Create<T>(T body) {
            return new Envelope<T>(body);
        }
    }

    /// <summary>
    /// 消息信封
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Envelope<T>: Envelope {
        public Envelope(T body) {
            this.Body = body;
        }

        /// <summary>
        /// 消息主体
        /// </summary>
        public T Body { private set; get; }

        /// <summary>
        /// 延时
        /// </summary>
        public TimeSpan Delay { get; set; }

        /// <summary>
        /// 存活时间
        /// </summary>
        public TimeSpan TimeToLive { get; set; }

        /// <summary>
        /// 相关Id
        /// </summary>
        public string CorrelationId { get; set; }

        /// <summary>
        /// 消息Id
        /// </summary>
        public string MessageId { get; set; }

        public static implicit operator Envelope<T>(T body) {
            return Create(body);
        }
    }
}