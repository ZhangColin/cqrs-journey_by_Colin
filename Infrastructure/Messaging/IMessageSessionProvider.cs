namespace Infrastructure.Messaging {
    /// <summary>
    /// 消息会话提供器
    /// </summary>
    public interface IMessageSessionProvider {
        string SessionId { get; } 
    }
}