using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace SSD_Assignment___Banking_Application
{
    public static class AuditLogger
    {
        private const string SourceName = "SSD Banking Application";

        static AuditLogger()
        {
            try
            {
                if (!EventLog.SourceExists(SourceName))
                    EventLog.CreateEventSource(SourceName, "Application");
            }
            catch { }
        }

        //Logs a transaction to the Windows Event Log.
        public static void LogTransaction(string teller, string accountNo, string accountHolder,
            string transactionType, double? amount = null, string reason = null)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("=== BANKING TRANSACTION ===");
                sb.AppendLine($"WHO (Teller): {teller}");
                sb.AppendLine($"WHO (Account): {accountNo} - {accountHolder}");
                sb.AppendLine($"WHAT: {transactionType}");
                sb.AppendLine($"WHERE: {GetDeviceInfo()}");
                sb.AppendLine($"WHEN: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

                if (amount.HasValue)
                {
                    sb.AppendLine($"AMOUNT: €{amount.Value:N2}");
                    if (amount.Value > 10000)
                    {
                        sb.AppendLine($"WHY: {reason ?? "Not specified"}");
                    }
                }

                sb.AppendLine($"HOW: {GetAppMetadata()}");

                EventLog.WriteEntry(SourceName, sb.ToString(), EventLogEntryType.Information);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Logging error: {ex.Message}");
            }
        }

        //Logs an authentication attempt to the Windows Event Log.
        public static void LogAuth(string user, bool success, string reason = null)
        {
            try
            {
                var msg = $"Authentication {(success ? "SUCCESS" : "FAILED")}\nUser: {user}\n" +
                          $"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\nDevice: {GetDeviceInfo()}";
                if (!success && reason != null) msg += $"\nReason: {reason}";

                EventLog.WriteEntry(SourceName, msg, success ? EventLogEntryType.Information : EventLogEntryType.Warning);
            }
            catch { }
        }

        //Logs an admin action to the Windows Event Log.
        public static void LogAdminAction(string admin, string action, bool approved)
        {
            try
            {
                var msg = $"Admin Action {(approved ? "APPROVED" : "DENIED")}\n" +
                          $"Admin: {admin}\nAction: {action}\nTime: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                          $"Device: {GetDeviceInfo()}";

                EventLog.WriteEntry(SourceName, msg, approved ? EventLogEntryType.Information : EventLogEntryType.Warning);
            }
            catch { }
        }

        //Logs an application error to the Windows Event Log.
        public static void LogError(string user, string error)
        {
            try
            {
                var msg = $"Application Error\nUser: {user}\nError: {error}\n" +
                          $"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";

                EventLog.WriteEntry(SourceName, msg, EventLogEntryType.Error);
            }
            catch { }
        }

        //Retrieves a string containing information about the current device and user.
        private static string GetDeviceInfo()
        {
            try
            {
                var mac = NetworkInterface.GetAllNetworkInterfaces()
                    .FirstOrDefault(n => n.OperationalStatus == OperationalStatus.Up &&
                                       n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    ?.GetPhysicalAddress().ToString() ?? "Unknown";

                var sid = WindowsIdentity.GetCurrent().User?.Value ?? "Unknown";
                return $"MAC: {mac}, SID: {sid}";
            }
            catch { return "Unknown"; }
        }

        //Retrieves a string containing information about the current application, including its version and a hash of the executable file.
        private static string GetAppMetadata()
        {
            try
            {
                var asm = System.Reflection.Assembly.GetExecutingAssembly();
                var ver = asm.GetName().Version;

                string hash = "N/A";
                if (File.Exists(asm.Location))
                {
                    using (var sha = SHA256.Create())
                    using (var stream = File.OpenRead(asm.Location))
                        hash = Convert.ToBase64String(sha.ComputeHash(stream)).Substring(0, 16);
                }

                return $"Banking App v{ver}, Hash: {hash}...";
            }
            catch { return "Banking Application"; }
        }
    }
}
