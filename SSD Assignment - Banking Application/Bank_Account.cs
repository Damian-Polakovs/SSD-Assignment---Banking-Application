using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Banking_Application
{
    public abstract class Bank_Account
    {
        public string AccountNo { get; set; }
        public string Name { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string AddressLine3 { get; set; }
        public string Town { get; set; }
        public double Balance { get; set; }

        protected Bank_Account() { }

        protected Bank_Account(string name, string addr1, string addr2, string addr3, string town, double balance)
        {
            AccountNo = Guid.NewGuid().ToString();
            Name = name;
            AddressLine1 = addr1;
            AddressLine2 = addr2;
            AddressLine3 = addr3;
            Town = town;
            Balance = balance;
        }

        //Lodges a given amount of money into the account.
        public void Lodge(double amount)
        {
            if (amount < 0) throw new ArgumentException("Negative amount not allowed");
            Balance += amount;
        }

        public abstract bool Withdraw(double amount);
        public abstract double GetAvailableFunds();

        //Returns a string for the account.
        public override string ToString()
        {
            return $"\nAccount: {AccountNo}\nName: {Name}\nAddress: {AddressLine1}\n" +
                   $"{AddressLine2}\n{AddressLine3}\nTown: {Town}\nBalance: €{Balance:N2}\n";
        }
    }
}
