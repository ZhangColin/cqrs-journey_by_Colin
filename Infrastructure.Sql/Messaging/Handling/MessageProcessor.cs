using System;
using System.Diagnostics;
using System.IO;
using Infrastructure.Serialization;

namespace Infrastructure.Sql.Messaging.Handling {
    public abstract class MessageProcessor: IProcessor, IDisposable {
        private readonly IMessageReceiver _receiver;
        private readonly ITextSerializer _serializer;
        private readonly object _lockObject = new object();

        private bool _disposed = false;
        private bool _started = false;

        protected MessageProcessor(IMessageReceiver receiver, ITextSerializer serializer) {
            this._receiver = receiver;
            this._serializer = serializer;
        }

        public void Start() {
            lock (this._lockObject) {
                if (!_started) {
                    this._receiver.MessageReceived += this.OnMessageReceived;
                    this._receiver.Start();
                    this._started = true;
                }
            }
        }

        public void Stop() {
            lock(this._lockObject) {
                if(_started) {
                    this._receiver.Stop();
                    this._receiver.MessageReceived -= this.OnMessageReceived;
                    this._started = false;
                }
            }
        }

        public void Dispose() {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) {
            if(!this._disposed) {
                if(disposing) {
                    this.Stop();
                    this._disposed = true;

                    using(this._receiver as IDisposable) {
                        // Dispose receiver if it's disposable
                    }
                }
            }
        }

        ~MessageProcessor() {
            this.Dispose(false);
        }

        protected abstract void ProcessMessage(object payload, string correlationId);

        private void OnMessageReceived(object sender, MessageReceivedEventArgs args) {
            Trace.WriteLine(new string('-', 100));

            try {
                var body = this.Deserialize(args.Message.Body);
                this.TracePayload(body);
                Trace.WriteLine("");

                this.ProcessMessage(body, args.Message.CorrelationId);

                Trace.WriteLine(new string('-', 100));
            }
            catch(Exception e) {
                Trace.TraceError("An exception happened while processing message through handler/s:\r\n{0}", e);
                Trace.TraceWarning("Error will be ignored and message receiving will continue.");
            }
        }

        protected object Deserialize(string serializedPayload) {
            using(var reader = new StringReader(serializedPayload)) {
                return this._serializer.Deserialize(reader);
            }
        }

        protected string Serialize(object payload) {
            using(var writer = new StringWriter()) {
                this._serializer.Serialize(writer, payload);
                return writer.ToString();
            }
        }

        private void ThrowIfDisposed() {
            if(this._disposed) {
                throw new ObjectDisposedException("MessageProcessor");
            }
        }

        [Conditional("TRACE")]
        private void TracePayload(object payload) {
            Trace.WriteLine(this.Serialize(payload));
        }
    }
}