using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;

namespace SSD_Assignment___Banking_Application
{
    public sealed class AuthenticateService
    {
        private static readonly Lazy<AuthenticateService> _instance =
           new Lazy<AuthenticateService>(() => new AuthenticateService());

        private const string Domain = "ITSLIGO.LAN";
        private const string TellerGroup = "Bank Teller";
        private const string AdminGroup = "Bank Teller Administrator";

        public static AuthenticateService Instance => _instance.Value;
        public string CurrentUser { get; private set; }
        public bool IsAuthenticated { get; private set; }
        public bool IsAdmin { get; private set; }

        private AuthenticateService() { }

        //Authenticates the user with the given username and password.
        public bool Authenticate(string username, string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    AuditLogger.LogAuth(username ?? "Unknown", false, "Empty credentials");
                    return false;
                }

                using (var context = new PrincipalContext(ContextType.Domain, Domain))
                {
                    if (!context.ValidateCredentials(username, password))
                    {
                        AuditLogger.LogAuth(username, false, "Invalid credentials");
                        return false;
                    }

                    using (var user = UserPrincipal.FindByIdentity(context, username))
                    {
                        if (user == null)
                        {
                            AuditLogger.LogAuth(username, false, "User not found");
                            return false;
                        }

                        bool isTeller = user.IsMemberOf(context, IdentityType.Name, TellerGroup);
                        if (!isTeller)
                        {
                            AuditLogger.LogAuth(username, false, "Not in Bank Teller group");
                            return false;
                        }

                        IsAdmin = user.IsMemberOf(context, IdentityType.Name, AdminGroup);
                        CurrentUser = username;
                        IsAuthenticated = true;

                        AuditLogger.LogAuth(username, true);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                AuditLogger.LogAuth(username, false, ex.Message);
                Console.WriteLine($"Authentication error: {ex.Message}");
                return false;
            }
        }

        //Requests admin approval for the given operation.
        //If the current user is an administrator, the operation is automatically approved.
        //Otherwise, the user is prompted to enter admin credentials.
        public bool RequestAdminApproval(string operation)
        {
            if (IsAdmin)
            {
                Console.WriteLine($"Administrator {CurrentUser} approved: {operation}");
                AuditLogger.LogAdminAction(CurrentUser, operation, true);
                return true;
            }

            Console.WriteLine("\n=== ADMINISTRATOR APPROVAL REQUIRED ===");
            Console.Write("Admin Username: ");
            string adminUser = Console.ReadLine();
            Console.Write("Admin Password: ");
            string adminPwd = ReadPassword();

            try
            {
                using (var context = new PrincipalContext(ContextType.Domain, Domain))
                {
                    if (!context.ValidateCredentials(adminUser, adminPwd))
                    {
                        Console.WriteLine("Invalid credentials.");
                        AuditLogger.LogAdminAction(adminUser, operation, false);
                        return false;
                    }

                    using (var user = UserPrincipal.FindByIdentity(context, adminUser))
                    {
                        if (user != null && user.IsMemberOf(context, IdentityType.Name, AdminGroup))
                        {
                            Console.WriteLine("Approval granted.");
                            AuditLogger.LogAdminAction(adminUser, operation, true);
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AuditLogger.LogError(adminUser, $"Admin approval error: {ex.Message}");
            }

            Console.WriteLine("Approval denied.");
            AuditLogger.LogAdminAction(adminUser, operation, false);
            return false;
        }

        //Reads a password from the console, replacing each character with an asterisk as it is entered.
        private string ReadPassword()
        {
            var pwd = new StringBuilder();
            while (true)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter) break;
                if (key.Key == ConsoleKey.Backspace && pwd.Length > 0)
                {
                    pwd.Remove(pwd.Length - 1, 1);
                    Console.Write("\b \b");
                }
                else if (!char.IsControl(key.KeyChar))
                {
                    pwd.Append(key.KeyChar);
                    Console.Write("*");
                }
            }
            Console.WriteLine();
            return pwd.ToString();
        }
    }
}
