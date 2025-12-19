using Banking_Application;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSD_Assignment___Banking_Application
{
    public sealed class DataAccessLayer
    {
        private readonly List<Bank_Account> _accounts = new List<Bank_Account>();
        private const string DbName = "Banking Database.db";
        private static readonly Lazy<DataAccessLayer> _instance =
            new Lazy<DataAccessLayer>(() => new DataAccessLayer());
        private readonly EncryptService _crypto = EncryptService.Instance;

        public static DataAccessLayer Instance => _instance.Value;

        //Returns a new SQLite connection to the banking database.
        private SqliteConnection GetConnection()
        {
            return new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = DbName,
                Mode = SqliteOpenMode.ReadWriteCreate
            }.ToString());
        }

        //Initialises the database by creating the Bank_Accounts table if it does not already exist.
        private void InitDb()
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"CREATE TABLE IF NOT EXISTS Bank_Accounts(
                        accountNo TEXT PRIMARY KEY,
                        name TEXT NOT NULL,
                        address_line_1 TEXT,
                        address_line_2 TEXT,
                        address_line_3 TEXT,
                        town TEXT NOT NULL,
                        balance REAL NOT NULL,
                        accountType INTEGER NOT NULL,
                        overdraftAmount REAL,
                        interestRate REAL
                    ) WITHOUT ROWID";
                    cmd.ExecuteNonQuery();
                }
            }
        }

        //Loads all accounts from the database into memory.
        //If the database does not exist, it is created first.
        public void LoadAccounts()
        {
            if (!File.Exists(DbName))
            {
                InitDb();
                return;
            }

            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM Bank_Accounts";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int type = reader.GetInt32(7);

                            if (type == 1) //Current
                            {
                                _accounts.Add(new Current_Account
                                {
                                    AccountNo = reader.GetString(0),
                                    Name = _crypto.Decrypt(reader.GetString(1)),
                                    AddressLine1 = _crypto.Decrypt(reader.GetString(2)),
                                    AddressLine2 = _crypto.Decrypt(reader.GetString(3)),
                                    AddressLine3 = _crypto.Decrypt(reader.GetString(4)),
                                    Town = _crypto.Decrypt(reader.GetString(5)),
                                    Balance = reader.GetDouble(6),
                                    OverdraftAmount = reader.GetDouble(8)
                                });
                            }
                            else //Savings
                            {
                                _accounts.Add(new Savings_Account
                                {
                                    AccountNo = reader.GetString(0),
                                    Name = _crypto.Decrypt(reader.GetString(1)),
                                    AddressLine1 = _crypto.Decrypt(reader.GetString(2)),
                                    AddressLine2 = _crypto.Decrypt(reader.GetString(3)),
                                    AddressLine3 = _crypto.Decrypt(reader.GetString(4)),
                                    Town = _crypto.Decrypt(reader.GetString(5)),
                                    Balance = reader.GetDouble(6),
                                    InterestRate = reader.GetDouble(9)
                                });
                            }
                        }
                    }
                }
            }
        }

        //Adds an account to the database and to the list of accounts in memory.
        public string AddAccount(Bank_Account acct)
        {
            _accounts.Add(acct);

            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO Bank_Accounts VALUES(
                        @no, @name, @a1, @a2, @a3, @town, @bal, @type, @od, @rate)";

                    cmd.Parameters.AddWithValue("@no", acct.AccountNo);
                    cmd.Parameters.AddWithValue("@name", _crypto.Encrypt(acct.Name));
                    cmd.Parameters.AddWithValue("@a1", _crypto.Encrypt(acct.AddressLine1));
                    cmd.Parameters.AddWithValue("@a2", _crypto.Encrypt(acct.AddressLine2 ?? ""));
                    cmd.Parameters.AddWithValue("@a3", _crypto.Encrypt(acct.AddressLine3 ?? ""));
                    cmd.Parameters.AddWithValue("@town", _crypto.Encrypt(acct.Town));
                    cmd.Parameters.AddWithValue("@bal", acct.Balance);

                    if (acct is Current_Account ca)
                    {
                        cmd.Parameters.AddWithValue("@type", 1);
                        cmd.Parameters.AddWithValue("@od", ca.OverdraftAmount);
                        cmd.Parameters.AddWithValue("@rate", DBNull.Value);
                    }
                    else if (acct is Savings_Account sa)
                    {
                        cmd.Parameters.AddWithValue("@type", 2);
                        cmd.Parameters.AddWithValue("@od", DBNull.Value);
                        cmd.Parameters.AddWithValue("@rate", sa.InterestRate);
                    }

                    cmd.ExecuteNonQuery();
                }
            }

            AuditLogger.LogTransaction(AuthenticateService.Instance.CurrentUser,
                acct.AccountNo, acct.Name, "Account Creation", acct.Balance);

            return acct.AccountNo;
        }

        //Finds a bank account by its account number and returns it if found.
        //If the account is found, a balance query transaction is logged.
        public Bank_Account FindAccount(string accountNo)
        {
            if (!SqlInputValidator.IsValidGuid(accountNo)) return null;

            var acct = _accounts.FirstOrDefault(a => a.AccountNo.Equals(accountNo,
                StringComparison.OrdinalIgnoreCase));

            if (acct != null)
            {
                AuditLogger.LogTransaction(AuthenticateService.Instance.CurrentUser,
                    acct.AccountNo, acct.Name, "Balance Query");
            }

            return acct;
        }

        //Closes a bank account by its account number.
        //Requests admin approval before deleting the account.
        //If the account is found and admin approval is granted,
        //the account is deleted from the database and the list of accounts in memory.
        //A transaction log is written for the account closure.
        public bool CloseAccount(string accountNo)
        {
            var acct = _accounts.FirstOrDefault(a => a.AccountNo.Equals(accountNo,
                StringComparison.OrdinalIgnoreCase));

            if (acct == null) return false;

            if (!AuthenticateService.Instance.RequestAdminApproval($"Close {accountNo}"))
                return false;

            _accounts.Remove(acct);

            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM Bank_Accounts WHERE accountNo = @no";
                    cmd.Parameters.AddWithValue("@no", accountNo);
                    cmd.ExecuteNonQuery();
                }
            }

            AuditLogger.LogTransaction(AuthenticateService.Instance.CurrentUser,
                acct.AccountNo, acct.Name, "Account Closure");

            return true;
        }


        //Lodges a given amount of money into a bank account.
        public bool Lodge(string accountNo, double amount, string reason = null)
        {
            var acct = _accounts.FirstOrDefault(a => a.AccountNo.Equals(accountNo,
                StringComparison.OrdinalIgnoreCase));

            if (acct == null) return false;

            acct.Lodge(amount);

            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "UPDATE Bank_Accounts SET balance = @bal WHERE accountNo = @no";
                    cmd.Parameters.AddWithValue("@bal", acct.Balance);
                    cmd.Parameters.AddWithValue("@no", accountNo);
                    cmd.ExecuteNonQuery();
                }
            }

            AuditLogger.LogTransaction(AuthenticateService.Instance.CurrentUser,
                acct.AccountNo, acct.Name, "Lodgement", amount, reason);

            return true;
        }

        //Withdraws a given amount of money from a bank account.
        public bool Withdraw(string accountNo, double amount, string reason = null)
        {
            var acct = _accounts.FirstOrDefault(a => a.AccountNo.Equals(accountNo,
                StringComparison.OrdinalIgnoreCase));

            if (acct == null || !acct.Withdraw(amount)) return false;

            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "UPDATE Bank_Accounts SET balance = @bal WHERE accountNo = @no";
                    cmd.Parameters.AddWithValue("@bal", acct.Balance);
                    cmd.Parameters.AddWithValue("@no", accountNo);
                    cmd.ExecuteNonQuery();
                }
            }

            AuditLogger.LogTransaction(AuthenticateService.Instance.CurrentUser,
                acct.AccountNo, acct.Name, "Withdrawal", amount, reason);

            return true;
        }
    }
}
