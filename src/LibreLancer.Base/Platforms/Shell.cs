using System;
using System.Diagnostics;

namespace LibreLancer
{
    public static class Shell
    {
        public static bool HasCommand(string cmd)
        {
            var startInfo = new ProcessStartInfo("/bin/sh")
            {
                UseShellExecute = false,
                Arguments = $" -c \"command -v {cmd} >/dev/null 2>&1\""
            };
            using var p = Process.Start(startInfo);
            p.WaitForExit();
            return p.ExitCode == 0;
        }

        public static string GetString(string cmd, string args)
        {
            try
            {
                var startInfo = new ProcessStartInfo(cmd)
                {
                    UseShellExecute = false,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using var p = Process.Start(startInfo);
                p.WaitForExit();
                return p.StandardOutput.ReadToEnd().Trim();
            }
            catch (Exception e)
            {
                FLLog.Warning("Shell", $"Error running {cmd} {args}");
                return "";
            }
        }
        
        public static void OpenCommand(string path)
        {
            if(Platform.RunningOS == OS.Windows)
            {
                Process.Start(new ProcessStartInfo(path) {UseShellExecute = true});
            } else if (Platform.RunningOS == OS.Mac) {
                Process.Start("open", string.Format("\"{0}\"", path));
            } else if (Platform.RunningOS == OS.Linux) {
                Process.Start("xdg-open", string.Format("\"{0}\"", path));
            }
        }
    }
}