using SSD_Assignment___Banking_Application;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Banking_Application
{
    public class Program
    {
        public static void Main(string[] args)
        {

            Console.WriteLine("=== SECURE BANKING APPLICATION ===\n");

            var auth = AuthenticateService.Instance;
            int attempts = 0;

            // Authentication with 3-attempt limit
            while (!auth.IsAuthenticated && attempts < 3)
            {
                Console.Write("Username: ");
                string username = Console.ReadLine();
                Console.Write("Password: ");
                string password = ReadPassword();

                if (!auth.Authenticate(username, password))
                {
                    attempts++;
                    Console.WriteLine($"\nAuthentication failed. {3 - attempts} attempts remaining.\n");
                }
            }

            if (!auth.IsAuthenticated)
            {
                Console.WriteLine("Maximum attempts exceeded. Exiting.");
                AuditLogger.LogAuth("Unknown", false, "Maximum login attempts exceeded");
                return;
            }

            Console.WriteLine($"\nWelcome, {auth.CurrentUser}!");
            Console.WriteLine($"Role: {(auth.IsAdmin ? "Administrator" : "Teller")}\n");

            var dal = DataAccessLayer.Instance;
            dal.LoadAccounts();

            RunMainMenu(dal, auth);
        }

        static void RunMainMenu(DataAccessLayer dal, AuthenticateService auth)
        {
            bool running = true;
            while (running)
            {
                Console.WriteLine("\n***BANKING MENU***");
                Console.WriteLine("1. Add Bank Account");
                Console.WriteLine("2. Close Bank Account");
                Console.WriteLine("3. View Account");
                Console.WriteLine("4. Lodge");
                Console.WriteLine("5. Withdraw");
                Console.WriteLine("6. Exit");
                Console.Write("Choice: ");

                string choice = Console.ReadLine();

                try
                {
                    switch (choice)
                    {
                        case "1": CreateAccount(dal); break;
                        case "2": CloseAccount(dal); break;
                        case "3": ViewAccount(dal); break;
                        case "4": LodgeMoney(dal); break;
                        case "5": WithdrawMoney(dal); break;
                        case "6": running = false; break;
                        default: Console.WriteLine("Invalid option."); break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    AuditLogger.LogError(auth.CurrentUser, ex.Message);
                }
            }
        }

        static void CreateAccount(DataAccessLayer dal)
        {
            Console.WriteLine("\n=== CREATE ACCOUNT ===");
            Console.WriteLine("1. Current  2. Savings");
            Console.Write("Type: ");
            string type = Console.ReadLine();

            // Validate and sanitize all inputs
            Console.Write("Name: ");
            string name = SqlInputValidator.Sanitise(Console.ReadLine());
            if (!SqlInputValidator.IsValidName(name))
            {
                Console.WriteLine("Invalid name (2-100 characters required).");
                return;
            }

            Console.Write("Address Line 1: ");
            string addr1 = SqlInputValidator.Sanitise(Console.ReadLine());
            Console.Write("Address Line 2: ");
            string addr2 = SqlInputValidator.Sanitise(Console.ReadLine());
            Console.Write("Address Line 3: ");
            string addr3 = SqlInputValidator.Sanitise(Console.ReadLine());
            Console.Write("Town: ");
            string town = SqlInputValidator.Sanitise(Console.ReadLine());

            Console.Write("Opening Balance: ");
            if (!SqlInputValidator.IsValidAmount(Console.ReadLine(), out double balance))
            {
                Console.WriteLine("Invalid amount (0-1,000,000 required).");
                return;
            }

            Bank_Account acct = null;

            if (type == "1")
            {
                Console.Write("Overdraft Amount: ");
                if (!SqlInputValidator.IsValidAmount(Console.ReadLine(), out double od))
                {
                    Console.WriteLine("Invalid overdraft amount.");
                    return;
                }
                acct = new Current_Account(name, addr1, addr2, addr3, town, balance, od);
            }
            else if (type == "2")
            {
                Console.Write("Interest Rate (%): ");
                if (!SqlInputValidator.IsValidAmount(Console.ReadLine(), out double rate))
                {
                    Console.WriteLine("Invalid interest rate.");
                    return;
                }
                acct = new Savings_Account(name, addr1, addr2, addr3, town, balance, rate);
            }
            else
            {
                Console.WriteLine("Invalid account type.");
                return;
            }

            string accountNo = dal.AddAccount(acct);
            Console.WriteLine($"\nAccount created successfully: {accountNo}");
        }

        static void CloseAccount(DataAccessLayer dal)
        {
            Console.Write("\nAccount Number: ");
            string accountNo = Console.ReadLine();

            var acct = dal.FindAccount(accountNo);
            if (acct == null)
            {
                Console.WriteLine("Account not found.");
                return;
            }

            Console.WriteLine(acct.ToString());
            Console.Write("Confirm deletion (Y/N): ");

            if (Console.ReadLine()?.ToUpper() == "Y")
            {
                // Administrator approval required
                if (dal.CloseAccount(accountNo))
                    Console.WriteLine("Account closed successfully.");
                else
                    Console.WriteLine("Account closure failed.");
            }
        }

        static void ViewAccount(DataAccessLayer dal)
        {
            Console.Write("\nAccount Number: ");
            var acct = dal.FindAccount(Console.ReadLine());

            if (acct == null)
                Console.WriteLine("Account not found.");
            else
                Console.WriteLine(acct.ToString());
        }

        static void LodgeMoney(DataAccessLayer dal)
        {
            Console.Write("\nAccount Number: ");
            string accountNo = Console.ReadLine();

            var acct = dal.FindAccount(accountNo);
            if (acct == null)
            {
                Console.WriteLine("Account not found.");
                return;
            }

            Console.Write("Amount to Lodge: ");
            if (!SqlInputValidator.IsValidAmount(Console.ReadLine(), out double amt))
            {
                Console.WriteLine("Invalid amount.");
                return;
            }

            string reason = null;
            if (amt > 10000)
            {
                Console.Write("Reason (>€10,000): ");
                reason = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(reason))
                {
                    Console.WriteLine("Reason required for large transactions.");
                    return;
                }
            }

            if (dal.Lodge(accountNo, amt, reason))
                Console.WriteLine($"Lodgement successful. New balance: €{acct.Balance:N2}");
            else
                Console.WriteLine("Lodgement failed.");
        }

        static void WithdrawMoney(DataAccessLayer dal)
        {
            Console.Write("\nAccount Number: ");
            string accountNo = Console.ReadLine();

            var acct = dal.FindAccount(accountNo);
            if (acct == null)
            {
                Console.WriteLine("Account not found.");
                return;
            }

            Console.WriteLine($"Available funds: €{acct.GetAvailableFunds():N2}");
            Console.Write("Amount to Withdraw: ");

            if (!SqlInputValidator.IsValidAmount(Console.ReadLine(), out double amt))
            {
                Console.WriteLine("Invalid amount.");
                return;
            }

            if (amt > acct.GetAvailableFunds())
            {
                Console.WriteLine("Insufficient funds.");
                return;
            }

            string reason = null;
            if (amt > 10000)
            {
                Console.Write("Reason (>€10,000): ");
                reason = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(reason))
                {
                    Console.WriteLine("Reason required for large transactions.");
                    return;
                }
            }

            if (dal.Withdraw(accountNo, amt, reason))
                Console.WriteLine($"Withdrawal successful. New balance: €{acct.Balance:N2}");
            else
                Console.WriteLine("Withdrawal failed.");
        }

        static string ReadPassword()
        {
            var pwd = new System.Text.StringBuilder();
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