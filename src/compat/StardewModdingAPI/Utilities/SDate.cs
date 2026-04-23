using System;

namespace StardewModdingAPI.Utilities {

    /// <summary>In-game date (season + day + year), matching desktop SMAPI's SDate.</summary>
    public class SDate : IEquatable<SDate>, IComparable<SDate> {

        public int    Day    { get; }
        public string Season { get; }
        public int    Year   { get; }

        // Total days from the start of the game (Day 1 Spring Year 1 = 1)
        public int DaysSinceStart => (Year - 1) * 112 + SeasonIndex * 28 + Day;

        private int SeasonIndex => Season switch {
            "spring" => 0, "summer" => 1, "fall" => 2, "winter" => 3, _ => 0
        };

        public SDate(int day, string season) : this(day, season, 1) { }

        public SDate(int day, string season, int year) {
            if (day < 1 || day > 28) throw new ArgumentOutOfRangeException(nameof(day), "Day must be 1–28.");
            string s = season?.ToLower() ?? "";
            if (s != "spring" && s != "summer" && s != "fall" && s != "winter")
                throw new ArgumentException($"Invalid season '{season}'.", nameof(season));
            if (year < 1) throw new ArgumentOutOfRangeException(nameof(year));
            Day    = day;
            Season = s;
            Year   = year;
        }

        /// <summary>Get the current in-game date.</summary>
        public static SDate Now() {
            try {
                var t   = Type.GetType("StardewValley.Game1, Stardew Valley");
                var day = (int)(t?.GetField("dayOfMonth",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
                    ?.GetValue(null) ?? 1);
                var seasonIdx = (int)(t?.GetField("seasonIndex",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
                    ?.GetValue(null) ?? 0);
                var year = (int)(t?.GetField("year",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
                    ?.GetValue(null) ?? 1);
                string[] seasons = { "spring", "summer", "fall", "winter" };
                return new SDate(Math.Max(1, day), seasons[seasonIdx % 4], Math.Max(1, year));
            } catch {
                return new SDate(1, "spring", 1);
            }
        }

        public SDate AddDays(int days) {
            int total = DaysSinceStart + days;
            if (total < 1) total = 1;
            int y = (total - 1) / 112 + 1;
            int r = (total - 1) % 112;
            int si = r / 28;
            int d  = r % 28 + 1;
            string[] seasons = { "spring", "summer", "fall", "winter" };
            return new SDate(d, seasons[si], y);
        }

        public int    CompareTo(SDate? other) => other == null ? 1 : DaysSinceStart.CompareTo(other.DaysSinceStart);
        public bool   Equals(SDate? other)    => other != null && DaysSinceStart == other.DaysSinceStart;
        public override bool Equals(object? obj) => obj is SDate s && Equals(s);
        public override int  GetHashCode()       => DaysSinceStart;
        public override string ToString()        => $"{Season} {Day} Y{Year}";

        public static bool operator ==(SDate? a, SDate? b) => a?.Equals(b) ?? b is null;
        public static bool operator !=(SDate? a, SDate? b) => !(a == b);
        public static bool operator < (SDate a, SDate b)   => a.CompareTo(b) <  0;
        public static bool operator > (SDate a, SDate b)   => a.CompareTo(b) >  0;
        public static bool operator <=(SDate a, SDate b)   => a.CompareTo(b) <= 0;
        public static bool operator >=(SDate a, SDate b)   => a.CompareTo(b) >= 0;
    }
}
