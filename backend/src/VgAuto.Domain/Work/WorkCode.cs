using System;
using System.Globalization;
using System.Linq;

namespace VgAuto.Core.Domain
{
    /// <summary>
    /// Readable work number, e.g. RP_TF_2019_HC_2026_09_28_15:
    /// type (RP repair / OF offer only) _ client initials _ vehicle year _ manufacturer and model initials _ start date _ work number.
    /// Missing parts are written as X. It follows the current data (an accepted offer becomes RP).
    /// </summary>
    public static class WorkCode
    {
        public static string Format(bool isRepair, string clientName, int? vehicleYear, string manufacturer, string model, DateTime startedOn, string number)
        {
            var type = isRepair ? "RP" : "OF";
            var client = Initials(clientName);
            var year = vehicleYear?.ToString(CultureInfo.InvariantCulture) ?? "XXXX";
            var vehicle = Initials(manufacturer, 1) + Initials(model, 1);
            var date = ToLocal(startedOn).ToString("yyyy_MM_dd", CultureInfo.InvariantCulture);
            return string.Join("_", type, client, year, vehicle, date, string.IsNullOrWhiteSpace(number) ? "X" : number.Trim());
        }

        /// <summary>First letter of each word ("Terry Fox" → TF), at most <paramref name="max"/> letters; X when empty.</summary>
        public static string Initials(string text, int max = 3)
        {
            if (string.IsNullOrWhiteSpace(text)) return "X";
            var words = text.Split(new[] { ' ', '-', '.', ',', '&', '/', '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
            var all = words.Select(w => w.FirstOrDefault(char.IsLetterOrDigit)).Where(c => c != default).ToList();
            // "Wei Chen 陈伟" → WC: Latin initials when there are any, otherwise the characters themselves
            var latin = all.Where(c => c < 128).ToList();
            var letters = (latin.Count > 0 ? latin : all).Take(max).ToArray();
            return letters.Length == 0 ? "X" : new string(letters).ToUpperInvariant();
        }

        private static DateTime ToLocal(DateTime value) =>
            value.Kind == DateTimeKind.Local ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc).ToLocalTime();
    }
}
