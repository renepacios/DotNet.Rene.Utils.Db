namespace Rene.Utils.Db.UnitTest.Builders;

using Models;

public class SampleBuilder
{
    public static List<Sample> CreateList(int i)
    {
        //implement this method
        var list = new List<Sample>();
        for (var j = 0; j < i; j++)
        {
            list.Add(Sample.Create(j, $"Name_{j}", $"Description_{j}"));
        }
        return list;
    }
}
