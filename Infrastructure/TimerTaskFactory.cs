using System;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure {
    /// <summary>
    /// 定时任务工厂
    /// </summary>
    public static class TimerTaskFactory {
        private static readonly TimeSpan DoNotRepeat = TimeSpan.FromMilliseconds(-1);

        /// <summary>
        /// 开始执行一个新任务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="getResult">获取结果的回调函数</param>
        /// <param name="isResultValid">结果验证函数</param>
        /// <param name="pollInterval">轮询间隔</param>
        /// <param name="timeout">超时</param>
        /// <returns></returns>
        public static Task<T> StartNew<T>(Func<T> getResult, Func<T, bool> isResultValid, TimeSpan pollInterval,
            TimeSpan timeout) {
            Timer timer = null;
            TaskCompletionSource<T> taskCompletionSource = null;
            DateTime expirationTime = DateTime.UtcNow.Add(timeout);

            timer = new Timer(_ => {
                try {
                    // 过期，释放定时器，返回默认值
                    if(DateTime.UtcNow>expirationTime) {
                        timer.Dispose();
                        taskCompletionSource.SetResult(default(T));
                    }

                    // 获取结果
                    var result = getResult();

                    // 结果通过验证，释放定时器，返回默认值
                    if(isResultValid(result)) {
                        timer.Dispose();
                        taskCompletionSource.SetResult(result);
                    }
                    // 验证未通过，间隔pollInterval之后继续执行
                    else {
                        timer.Change(pollInterval, DoNotRepeat);
                    }
                }
                catch(Exception e) {
                    // 发生异常，释放定时器，返回异常
                    timer.Dispose();
                    taskCompletionSource.SetException(e);
                }
            });

            taskCompletionSource = new TaskCompletionSource<T>(timer);
            timer.Change(pollInterval, DoNotRepeat);

            return taskCompletionSource.Task;
        }
    }
}