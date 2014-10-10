namespace Infrastructure {
    /// <summary>
    /// 长时任务处理器接口
    /// </summary>
    public interface IProcessor {
        void Start();
        void Stop();
    }
}