using MongoDB.Bson.Serialization.Attributes;

namespace CreateGrid;

public class CellModel
{
    [BsonId]
    public LocationId _id { get; set; }

    public Geometry geometry { get; set; }

    public Offset offset { get; set; }
}

public class LocationId
{
    public double lat { get; set; }

    public double lon { get; set; }

    public int cellSize { get; set; }
}

public class Geometry
{
    public string type { get; set; }

    public double[] coordinates { get; set; }
}

public class Offset
{
    public double lon { get; set; }

    public double lat { get; set; }
}