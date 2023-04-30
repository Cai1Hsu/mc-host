using System.Text;
using System.Diagnostics;

namespace mchost.Server
{
    public class ServerProcessManager
    {
        public ServerHost? host { get; set; } = ServerHost.GetServerHost();

        public Process? java { get; set; }

        public ProcessStartInfo psi { get; set; } = null!;

        public List<string> ProcessLog { get; set; } = new();

        public bool IsDone { get; set; } = false;

        public bool IsRunning { get; set; } = false;

        public bool IsIntilized
        {
            get
            {
                return IsDone;
            }
        }

        public DateTime StartTime { get; set; } = DateTime.Now;

        public ServerProcessManager()
        {

        }

        public void StartServerProcess()
        {
            java = Process.Start(psi) ?? throw new Exception("Failed to start Minecraft Server. Are you sure you have Java installed?");
            StartTime = DateTime.Now;

            if (java.HasExited)
                throw new Exception("Java exited before we could handle it. Please check the output for more information.");

            ProcessLog = new List<string>();

            IsDone = false;
            IsRunning = true;

            java.Exited += (sender, e) =>
            {
                IsDone = false;
                IsRunning = false;

                host?.OnServerProcessExit();
            };
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