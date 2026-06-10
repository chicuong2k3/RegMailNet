namespace RegMailNet.Utilities;

public static class MonthsMapping
{
    private static readonly Dictionary<string, string> Map = new()
    {
        ["1"] = "January",
        ["2"] = "February",
        ["3"] = "March",
        ["4"] = "April",
        ["5"] = "May",
        ["6"] = "June",
        ["7"] = "July",
        ["8"] = "August",
        ["9"] = "September",
        ["10"] = "October",
        ["11"] = "November",
        ["12"] = "December"
    };

    public static string GetMonthName(string monthNumber)
    {
        var key = monthNumber.TrimStart('0');
        if (key.Length == 0) key = "0";
        return Map.TryGetValue(key, out var name) ? name : "Invalid month number";
    }
}
