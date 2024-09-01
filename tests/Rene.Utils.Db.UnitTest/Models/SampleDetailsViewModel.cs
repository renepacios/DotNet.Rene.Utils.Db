namespace Rene.Utils.Db.UnitTest.Models;

using Magic.AutoMapper;

public class SampleDetailsViewModel : SampleViewModel
, IWithGenericHandler<Sample>
, IMapFrom<Sample>
{
    public string Description { get; set; }


    public static SampleDetailsViewModel Create(int id, string name, string description)
    {
        return new SampleDetailsViewModel
        {
            Id = id,
            Name = name,
            Description = description
        };
    }
    public static SampleDetailsViewModel Create(Sample sample)
    {
        return new SampleDetailsViewModel
        {
            Id = sample.Id,
            Name = sample.Name,
            Description = sample.Description
        };
    }
    public static SampleDetailsViewModel Create()
    {
        return new SampleDetailsViewModel
        {
            Id = 99,
            Name = "NoBodySample",
            Description = "This is a fake description"
        };
    }

    public static SampleDetailsViewModel DefaultSampleDetails => Create();
}