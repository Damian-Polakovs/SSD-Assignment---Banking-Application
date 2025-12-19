using SSD_Assignment___Banking_Application;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Banking_Application
{
    public class Current_Account : Bank_Account
    {
        public double OverdraftAmount { get; set; }

        public Current_Account() : base() { }

        public Current_Account(string name, string a1, string a2, string a3, string town, double bal, double od)
            : base(name, a1, a2, a3, town, bal)
        {
            OverdraftAmount = od;
        }

        public override bool Withdraw(double amt)
        {
            if (amt < 0 || GetAvailableFunds() < amt) return false;
            Balance -= amt;
            return true;
        }

        public override double GetAvailableFunds() => Balance + OverdraftAmount;

        public override string ToString() => base.ToString() +
            $"Type: Current Account\nOverdraft: €{OverdraftAmount:N2}\nAvailable Funds: €{GetAvailableFunds():N2}\n";
    }
}
