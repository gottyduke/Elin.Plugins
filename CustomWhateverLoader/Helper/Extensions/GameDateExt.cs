namespace Cwl.Helper.Extensions;

public static class GameDateExt
{
    public static int HourLater(this GameDate date, int hours)
    {
        return date.GetRaw() + hours * Date.HourToken;
    }

    public static int DayLater(this GameDate date, int days)
    {
        return date.GetRaw() + days * Date.DayToken;
    }

    public static int MonthLater(this GameDate date, int months)
    {
        return date.GetRaw() + months * Date.MonthToken;
    }

    public static int YearLater(this GameDate date, int years)
    {
        return date.GetRaw() + years * Date.YearToken;
    }
}