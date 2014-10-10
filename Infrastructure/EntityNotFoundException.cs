using System;
using System.Collections;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace Infrastructure {
    /// <summary>
    /// 实体未找到异常
    /// </summary>
    public class EntityNotFoundException: Exception  {
        private readonly Guid _entityId;
        private readonly string _entityType;

        public EntityNotFoundException() {
        }

        public EntityNotFoundException(Guid entityId): base(entityId.ToString()) {
            this._entityId = entityId;
        }

        public EntityNotFoundException(Guid entityId, string entityType): base(entityType+": "+entityId) {
            this._entityId = entityId;
            this._entityType = entityType;
        }

        public EntityNotFoundException(Guid entityId, string entityType, string message, Exception innerException)
            : base(message, innerException) {
            this._entityId = entityId;
            this._entityType = entityType;
        }

        protected EntityNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context) {
            if(info==null) {
                throw new ArgumentNullException("info");
            }

            this._entityId = Guid.Parse(info.GetString("entityId"));
            this._entityType = info.GetString("entityType");
        }

        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context) {
            base.GetObjectData(info, context);

            info.AddValue("entityId", this._entityId.ToString());
            info.AddValue("entityType", this._entityType);
        }

        public Guid EntityId {
            get { return this._entityId; }
        }

        public string EntityType {
            get { return this._entityType; }
        }
    }
}