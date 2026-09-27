using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace VgAuto.Core.Domain
{
    /// <summary>
    /// Currencies a company can choose in its settings. Amounts are only labelled with the currency;
    /// there is no conversion. Each currency is formatted the way it is written in its home country.
    /// </summary>
    public static class Currencies
    {
        public const string Default = "CAD";

        public record Currency(string Code, string Name, string Culture);

        public static readonly IReadOnlyList<Currency> All = new[]
        {
            new Currency("CAD", "Canadian dollar", "en-CA"),
            new Currency("USD", "US dollar", "en-US"),
            new Currency("EUR", "Euro", "de-DE"),
            new Currency("GBP", "British pound", "en-GB"),
            new Currency("CNY", "Chinese yuan", "zh-CN"),
            new Currency("HKD", "Hong Kong dollar", "zh-HK"),
            new Currency("TWD", "New Taiwan dollar", "zh-TW"),
            new Currency("JPY", "Japanese yen", "ja-JP"),
            new Currency("KRW", "South Korean won", "ko-KR"),
            new Currency("AUD", "Australian dollar", "en-AU"),
            new Currency("NZD", "New Zealand dollar", "en-NZ"),
            new Currency("SGD", "Singapore dollar", "en-SG"),
            new Currency("CHF", "Swiss franc", "de-CH"),
            new Currency("MXN", "Mexican peso", "es-MX"),
        };

        public static bool IsSupported(string code) => All.Any(c => c.Code == code);

        /// <summary>Upper case code of a supported currency, otherwise the default.</summary>
        public static string Normalize(string code)
        {
            var value = code?.Trim().ToUpperInvariant();
            return IsSupported(value) ? value : Default;
        }

        /// <summary>Formats an amount like "$1,234.50" (CAD), "1.234,50 €" (EUR) or "¥1,235" (JPY).</summary>
        public static string Format(decimal amount, string code)
        {
            var currency = All.First(c => c.Code == Normalize(code));
            var culture = (CultureInfo)CultureInfo.GetCultureInfo(currency.Culture).Clone();
            culture.NumberFormat.CurrencyDecimalDigits = Decimals(currency.Code);
            return amount.ToString("C", culture);
        }

        /// <summary>Number of minor units shown (0 for yen and won, otherwise 2).</summary>
        public static int Decimals(string code) => Normalize(code) is "JPY" or "KRW" ? 0 : 2;
    }
}
