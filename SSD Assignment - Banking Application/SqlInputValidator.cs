using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSD_Assignment___Banking_Application
{
    public static class SqlInputValidator
    {
        //Sanitises the input string by returning an empty string if it is null or whitespace,
        //otherwise trims the input string.
        public static string Sanitise(string input)
        {
            return string.IsNullOrWhiteSpace(input) ? "" : input.Trim();
        }

        //Validates if a given string is a valid GUID.
        public static bool IsValidGuid(string value)
        {
            return Guid.TryParse(value, out _);
        }

        //Validates if a given string is a valid amount in the range of 0 to 1,000,000.
        public static bool IsValidAmount(string value, out double amount)
        {
            amount = 0;
            return double.TryParse(value, out amount) && amount >= 0 && amount <= 1000000;
        }

        //Validates if a given string is a valid name.
        //A name is valid if it is not null, whitespace, or empty, and its length is between 2 and 100 characters.
        public static bool IsValidName(string name)
        {
            return !string.IsNullOrWhiteSpace(name) && name.Length >= 2 && name.Length <= 100;
        }
    }
}
