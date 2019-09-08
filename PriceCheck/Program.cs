using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;

namespace PriceCheck
{
    class Program
    {
        private const int STD_OUTPUT_HANDLE = -11;
        private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;
        private const uint DISABLE_NEWLINE_AUTO_RETURN = 0x0008;

        [DllImport("kernel32.dll")]
        private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll")]
        public static extern uint GetLastError();

        static void Main(string[] args)
        {
            var iStdOut = GetStdHandle(STD_OUTPUT_HANDLE);
            if (!GetConsoleMode(iStdOut, out uint outConsoleMode))
            {
                Console.WriteLine("failed to get output console mode");
                Console.ReadKey();
                return;
            }

            outConsoleMode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING | DISABLE_NEWLINE_AUTO_RETURN;
            if (!SetConsoleMode(iStdOut, outConsoleMode))
            {
                Console.WriteLine($"failed to set output console mode, error code: {GetLastError()}");
                Console.ReadKey();
                return;
            }

            Console.WriteLine("Price checks");

            var ci = CultureInfo.GetCultureInfo("en-US");

            var cardFile = File.ReadAllLines("./cards.txt");
            var cards = cardFile.ToDictionary(x => x.Split(',').First(), x => x.Split(',').Last());

            bool foundGoodPrice = false;

            foreach (var card in cards)
            {
                double targetPrice = double.Parse(card.Value);

                using (var httpClient = new HttpClient())
                {
                    var res = httpClient.GetStringAsync($"https://api.scryfall.com/cards/named?exact={card.Key}").Result;

                    dynamic json = JsonConvert.DeserializeObject(res);
                    if (double.TryParse((string)json.prices.tix, NumberStyles.AllowDecimalPoint, ci.NumberFormat, out double price))
                    {
                        if (price < targetPrice)
                        {
                            Console.Beep();
                            foundGoodPrice = true;
                            Console.WriteLine($"\u001b[32m{card.Key} price: {json.prices.tix}\u001b[0m");
                        }
                        else
                        {
                            Console.WriteLine($"\u001b[31m{card.Key} price: {json.prices.tix}\u001b[0m");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Error parsing price for {card.Key}");
                    }
                }
            }

            if (foundGoodPrice)
            {
                Console.ReadKey();
            }
            else
            {
                Thread.Sleep(1500);
            }
        }
    }
}
