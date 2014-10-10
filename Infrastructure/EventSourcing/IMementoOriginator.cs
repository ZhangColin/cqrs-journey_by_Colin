namespace Infrastructure.EventSourcing {
    /// <summary>
    /// 快照
    /// </summary>
    public interface IMementoOriginator {
        IMemento SaveToMemento();
    }

    public interface IMemento {
        int Version { get; }
    }
}