using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Banking_Application
{
    public class Savings_Account: Bank_Account
    {

        public double InterestRate { get; set; }

        public Savings_Account() : base() { }

        public Savings_Account(string name, string a1, string a2, string a3, string town, double bal, double rate)
            : base(name, a1, a2, a3, town, bal)
        {
            InterestRate = rate;
        }


        //Withdraws a given amount of money from the savings account.
        public override bool Withdraw(double amt)
        {
            if (amt < 0 || Balance < amt) return false; 
            Balance -= amt;
            return true; //Savings account cannot go into overdraft.
        }

        public override double GetAvailableFunds() => Balance;

        public override string ToString() => base.ToString() +
            $"Type: Savings Account\nInterest Rate: {InterestRate:N2}%\n";
    }
}
