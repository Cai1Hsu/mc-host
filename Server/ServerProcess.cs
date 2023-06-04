using System.Diagnostics;

namespace Parallel.Server
{
    public class ServerProcessManager
    {
        public ServerHost? host { get; set; } = ServerHost.GetServerHost();

        public Process? java { get; set; }

        public ProcessStartInfo psi { get; set; } = null!;

        public bool IsDone { get; set; } = false;

        public bool IsRunning { get; set; } = false;

        public DateTime StartTime { get; set; } = DateTime.Now;

        public ServerProcessManager()
        {
        }

        public void StartServerProcess()
        {
            java = Process.Start(psi) ?? throw new Exception("Failed to start Minecraft Server. Are you sure you have Java installed?");
            java.Exited += (sender, e) =>
            {
                IsDone = false;
                IsRunning = false;

                host?.OnServerProcessExit();
            };
            StartTime = DateTime.Now;

            if (java.HasExited)
                throw new Exception("Java exited before we could handle it. Please check the output for more information.");

            IsDone = false;
            IsRunning = true;
        }

        public void Terminate()
        {
            java?.Kill();
        }

        public void StopServer()
        {
            SendCommand("/stop", true);
        }

        public bool SendCommand(string command, bool force = false)
        {
            if (!IsDone && !force) return false;

            try
            {
                // TODO Remove this on release.
                Logging.Logger.Log($"Sending command: {command}");
                java?.StandardInput.WriteLine(command);
                return true;
            }
            catch (Exception) { }

            return false;
        }

        public void SetDone()
        {
            IsDone = true;
        }
    }
}