using AutoUpdaterDotNET;
using System.Diagnostics;

namespace iLearn.Services
{
    public class AutoUpdateService : IDisposable
    {
        private readonly string _updateXmlUrl;
        private readonly TimeSpan _checkInterval;
        private CancellationTokenSource _cts;

        public AutoUpdateService(string updateXmlUrl, TimeSpan? checkInterval = null)
        {
            _updateXmlUrl = updateXmlUrl;
            _checkInterval = checkInterval ?? TimeSpan.FromHours(1);

            AutoUpdater.ApplicationExitEvent += AutoUpdater_ApplicationExitEvent;
        }
        public void Start()
        {
            _cts = new CancellationTokenSource();
            Task.Run(() => RunPeriodicCheck(_cts.Token));
        }

        public void Stop()
        {
            _cts?.Cancel();
        }

        public void CheckNow()
        {
            AutoUpdater.Start(_updateXmlUrl);
        }

        private async Task RunPeriodicCheck(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                AutoUpdater.Start(_updateXmlUrl);

                try
                {
                    await Task.Delay(_checkInterval, token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        private void AutoUpdater_ApplicationExitEvent()
        {
            try
            {
                // 关闭当前进程
                Process.GetCurrentProcess().Kill();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AutoUpdateService] Exit failed: {ex.Message}");
                Environment.Exit(0);
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
