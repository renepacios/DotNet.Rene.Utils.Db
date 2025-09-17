namespace Rene.Utils.Db.UnitTest.Models;

public class Sample : IEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }

    public static Sample Create(int id, string name, string description)
    {
        return new Sample
        {
            Id = id,
            Name = name,
            Description = description
        };
    }

    public static Sample Create(SampleDetailsViewModel sampleDetails)
    {
        return new Sample
        {
            Id = sampleDetails.Id,
            Name = sampleDetails.Name,
            Description = sampleDetails.Description
        };
    }

    public static Sample Create()
    {
        return new Sample
        {
            Id = 99,
            Name = "NoBodySample",
            Description = "This is a fake description"
        };
    }

}