
namespace RailOps.Helpers
{
    public static class TurkeyTime
    {
        private static readonly TimeZoneInfo TimeZone =
            TimeZoneInfo.FindSystemTimeZoneById(
                OperatingSystem.IsWindows()
                    ? "Turkey Standard Time"
                    : "Europe/Istanbul");

        public static string Format(DateTime utcDateTime)
        {
            // Veritabanındaki zamanların UTC olduğunu biliyoruz.
            var utc = DateTime.SpecifyKind(
                utcDateTime,
                DateTimeKind.Utc);

            var localTime = TimeZoneInfo.ConvertTimeFromUtc(
                utc,
                TimeZone);

            return localTime.ToString("dd.MM.yyyy HH:mm");
        }
    }
}
