namespace stock_tool.common;

class Retry
{

    // 同步重试方法
    public static T Run<T>(Func<T> func, int maxRetries, int retryDelay, Func<Exception, bool> shouldRetry = null)
    {
        int retryCount = 0;
        while (true)
        {
            try
            {
                T t = func();
                if (t == null)
                {
                    throw new Exception("null");
                }
                return t;
            }
            catch (Exception ex)
            {
                retryCount++;
                if (retryCount > maxRetries || (shouldRetry != null && !shouldRetry(ex)))
                {
                    throw;
                }

                Logger.Info($"重试【{retryCount}】，原因: {ex.Message}，等待 {retryDelay} 毫秒...");
                Thread.Sleep(retryDelay);
            }
        }
    }

}
