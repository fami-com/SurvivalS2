namespace SurvivalS2API;

public static class Utils
{
    public static List<int> NLargest(IEnumerable<int> items, int n)
    {
        var temp = items.ToHashSet();
        return temp.Where(num => num != 0).OrderDescending().Take(n).ToList();
    }
    
    public static List<int> NSmallest(IEnumerable<int> items, int n)
    {
        var temp = items.ToHashSet();
        return temp.Where(num => num != 0).Order().Take(n).ToList();
    }
    
    public static List<List<int>> GetContinuousRuns(List<int> numbers)
    {
        var result = new List<List<int>>();
        var currentRun = new List<int>();

        numbers.Sort();
        
        foreach (var t in numbers)
        {
            if (currentRun.Count == 0 || t == currentRun[^1] + 1)
            {
                currentRun.Add(t);
            }
            else
            {
                result.Add(currentRun);
                currentRun = new List<int> { t };
            }
        }

        if (currentRun.Count > 0)
        {
            result.Add(currentRun);
        }

        return result;
    }

}