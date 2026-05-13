namespace TenantPortal.Shared.Helpers
{
    public static class DateHelper
    {
        public static DateTime GetAdjustedDueDate(int dueDayOfMonth, int targetYear, int targetMonth)
        {
            int adjustedDay = Math.Min(dueDayOfMonth, DateTime.DaysInMonth(targetYear, targetMonth));
            return new DateTime(targetYear, targetMonth, adjustedDay);
        }
    }
}