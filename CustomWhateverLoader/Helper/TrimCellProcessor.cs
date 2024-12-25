using MethodTimer;

namespace Cwl.Helper;

public class TrimCellProcessor
{
    [Time]
    public static string? TrimCell(string? cell)
    {
        return cell?.Trim();
    }
}