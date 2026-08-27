using Microsoft.Win32;

namespace Pulsebar
{
    /// <summary>
    /// Detects whether the PawnIO kernel driver (required by LibreHardwareMonitorLib for
    /// MSR-dependent sensors such as CPU clock and temperature) is installed on this machine.
    /// </summary>
    public static class PawnIoDetector
    {
        private const string ServiceKeyPath = @"SYSTEM\CurrentControlSet\Services\PawnIO";

        public static bool IsInstalled
        {
            get
            {
                try
                {
                    using (RegistryKey _key = Registry.LocalMachine.OpenSubKey(ServiceKeyPath))
                    {
                        return _key != null;
                    }
                }
                catch
                {
                    return false;
                }
            }
        }
    }
}
