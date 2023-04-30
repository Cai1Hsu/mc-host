using System.Text;
using System.Diagnostics;

namespace mchost.Server
{
    public class ServerProcessManager
    {
        public Process java { get; set; } = null!;

        public ProcessStartInfo psi { get; set; } = null!;

        public List<string> ProcessLog { get; set; } = new();

        public bool IsDone { get; private set; } = false;

        public bool IsRunning
        {
            get
            {
                return java != null && !java.HasExited;
            }
        }

        public bool IsIntilized
        {
            get
            {
                return java != null && psi != null;
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
        }

        public void Terminate()
        {
            java.Kill();
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
                java.StandardInput.WriteLine(command);
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