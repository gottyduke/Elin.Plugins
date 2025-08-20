namespace Cwl.Helper.Extensions;

public static class GameDateExt
{
    extension(GameDate date)
    {
        public int HourLater(int hours)
        {
            return date.GetRaw() + hours * Date.HourToken;
        }

        public int DayLater(int days)
        {
            return date.GetRaw() + days * Date.DayToken;
        }

        public int MonthLater(int months)
        {
            return date.GetRaw() + months * Date.MonthToken;
        }

        public int YearLater(int years)
        {
            return date.GetRaw() + years * Date.YearToken;
        }
    }
}