namespace TenantPortal.Shared.Helpers
{
    /// <summary>
    /// Date calculation utilities shared across services.
    /// </summary>
    public static class DateHelper
    {
        /// <summary>
        /// Returns a <see cref="DateTime"/> for the given day of month in the target year/month,
        /// clamped to the last valid day when the month is shorter than <paramref name="dueDayOfMonth"/>.
        /// </summary>
        /// <param name="dueDayOfMonth">The desired day (1–31) from the rent schedule.</param>
        /// <param name="targetYear">The year of the due date being calculated.</param>
        /// <param name="targetMonth">The month of the due date being calculated.</param>
        /// <returns>
        /// A <see cref="DateTime"/> at midnight UTC on the adjusted due date.
        /// For example, a due day of 31 in February returns the 28th (or 29th in a leap year).
        /// </returns>
        public static DateTime GetAdjustedDueDate(int dueDayOfMonth, int targetYear, int targetMonth)
        {
            // Clamp to the last day of the month to handle February and 30-day months
            int adjustedDay = Math.Min(dueDayOfMonth, DateTime.DaysInMonth(targetYear, targetMonth));
            return new DateTime(targetYear, targetMonth, adjustedDay);
        }
    }
}
