using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSD_Assignment___Banking_Application
{
    public static class SqlInputValidator
    {
        public static string Sanitise(string input)
        {
            return string.IsNullOrWhiteSpace(input) ? "" : input.Trim();
        }

        public static bool IsValidGuid(string value)
        {
            return Guid.TryParse(value, out _);
        }

        public static bool IsValidAmount(string value, out double amount)
        {
            amount = 0;
            return double.TryParse(value, out amount) && amount >= 0 && amount <= 1000000;
        }

        public static bool IsValidName(string name)
        {
            return !string.IsNullOrWhiteSpace(name) && name.Length >= 2 && name.Length <= 100;
        }
    }
}
