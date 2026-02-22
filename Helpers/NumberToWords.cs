using System;

namespace Grevity.Helpers
{
    public static class NumberToWords
    {
        public static string Convert(decimal number)
        {
            if (number == 0)
                return "Zero";

            if (number < 0)
                return "Minus " + Convert(Math.Abs(number));

            string words = "";

            long intPart = (long)Math.Truncate(number);
            int decPart = (int)((number - intPart) * 100);

            if (intPart > 0)
                words += ConvertInt(intPart);

            if (decPart > 0)
            {
                if (words != "")
                    words += "and ";
                words += ConvertInt(decPart) + "Paise ";
            }

            return words.Trim();
        }

        private static string ConvertInt(long number)
        {
            if (number == 0)
                return "";

            if (number < 20)
                return Units[(int)number] + " ";

            if (number < 100)
                return Tens[(int)(number / 10)] + " " + ConvertInt(number % 10);

            if (number < 1000)
                return Units[(int)(number / 100)] + " Hundred " + ConvertInt(number % 100);

            if (number < 100000)
                return ConvertInt(number / 1000) + " Thousand " + ConvertInt(number % 1000);

            if (number < 10000000)
                return ConvertInt(number / 100000) + " Lakh " + ConvertInt(number % 100000);

            return ConvertInt(number / 10000000) + " Crore " + ConvertInt(number % 10000000);
        }

        private static readonly string[] Units = {
            "", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Eleven",
            "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen"
        };

        private static readonly string[] Tens = {
            "", "Ten", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety"
        };
    }
}
