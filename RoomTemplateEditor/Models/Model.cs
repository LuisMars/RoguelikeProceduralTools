using System.Text.Json.Serialization;
using System.Text.Json;
using MermaidDotNet.Models;
using MermaidDotNet;

namespace RoomTemplateEditor.Models;

public class MultidimensionalArrayConverter : JsonConverter<string[,]>
{
    public override string[,] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Deserialize the JSON into a List<List<string>>
        var list = JsonSerializer.Deserialize<List<List<string>>>(ref reader, options);

        if (list == null || list.Count == 0)
        {
            return new string[0, 0];
        }

        int rows = list.Count;
        int columns = list[0].Count;

        // Verify that all rows have the same number of columns
        foreach (var row in list)
        {
            if (row.Count != columns)
            {
                throw new JsonException("All rows must have the same number of columns.");
            }
        }

        var result = new string[rows, columns];

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                result[i, j] = list[i][j];
            }
        }

        return result;
    }

    public override void Write(Utf8JsonWriter writer, string[,] value, JsonSerializerOptions options)
    {
        int rows = value.GetLength(0);
        int columns = value.GetLength(1);

        writer.WriteStartArray(); // Start of the main array
        for (int i = 0; i < rows; i++)
        {
            writer.WriteStartArray(); // Start of the row array
            for (int j = 0; j < columns; j++)
            {
                JsonSerializer.Serialize(writer, value[i, j], options);
            }
            writer.WriteEndArray(); // End of the row array
        }
        writer.WriteEndArray(); // End of the main array
    }
}

public class RoomTemplate
{
    public string TemplateName { get; set; }
    public List<Rule> Rules { get; set; } = [];
    public List<TileType> TileTypes { get; set; } = [];
    public List<string> Tags { get; set; } = [];
}

public class TileType
{
    public string Name { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
}

public record Rule
{
    public string Name { get; set; }
    public int Rows { get; set; }
    public int Columns { get; set; }
    [JsonConverter(typeof(MultidimensionalArrayConverter))]
    public string[,] Pattern { get; set; }
    [JsonConverter(typeof(MultidimensionalArrayConverter))]
    public string[,] Replacement { get; set; }
    public bool AllowVerticalMirroring { get; set; } = false;
    public bool AllowHorizontalMirroring { get; set; } = false;
    public bool AllowRotation { get; set; } = false;
    public int Priority { get; set; }
    public List<string> Tags { get; set; } = [];
}

public class MatrixTemplate
{
    public string Name { get; set; }

    [JsonConverter(typeof(MultidimensionalArrayConverter))]
    public string[,] Matrix { get; set; }
}

public record Cycle
{
    public string Name { get; set; }
    public List<CycleNode> Nodes { get; set; } = [];
    public List<CycleArc> Arcs { get; set; } = [];

    public string ToMermaid(string direction = "TD")
    {
        var nodes = Nodes
            .Where(n => !string.IsNullOrWhiteSpace(n.Name))
            .Select(n => new Node(n.Name, n.Name, n.ShapeType))
            .ToList();
        var links = Arcs
            .Where(a => a.From is not null && a.To is not null)
            .Select(a => new Link(a.From, a.To, a.Label, isBidirectional: !a.IsOneWay))
            .ToList();
        Flowchart flowchart = new(direction, nodes, links);
        return "%%{init: {'theme':'dark'}}%%\n" + flowchart.CalculateFlowchart();
    }
}

public record CycleNode
{
    public string Name { get; set; }

    public bool IsStart { get; set; }
    public bool IsGoal { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<string> ContainedKeys { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<string> KeyRequirements { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<string> ContainedLevers { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<string> LeverRequirements { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<string> LeverKeyRequirements { get; set; }

    [JsonIgnore]
    public Node.ShapeType ShapeType
    {
        get
        {
            if ((KeyRequirements?.Any() ?? false) || (LeverRequirements?.Any() ?? false) || IsGoal)
            {
                return Node.ShapeType.Hexagon;
            }

            if (IsStart)
            {
                return Node.ShapeType.Rounded;
            }

            if ((ContainedKeys?.Any() ?? false) || (ContainedLevers?.Any() ?? false))
            {
                return Node.ShapeType.Stadium;
            }

            return Node.ShapeType.Rectangle;
        }
    }
}
public record CycleArc
{
    public string From { get; set; }
    public string To { get; set; }
    public bool IsOneWay { get; set; }
    public bool InsertionPoint { get; set; }
    public bool IsLongInsertionPoint { get; set; }
    public bool IsSightLineOnly { get; set; }
    public bool IsSecret { get; set; }
    public int? Theme { get; set; }
    public bool IsDangerous { get; set; }
    public bool AlwaysAllowed { get; set; }

    [JsonIgnore]
    public string Label
    {
        get
        {
            var label = "";
            if (IsLongInsertionPoint)
            {
                label += "Long ";
            }
            if (InsertionPoint)
            {
                label += "Insertion Point";
            }
            if (IsSightLineOnly)
            {
                label += "Sight line";
            }
            if (IsSecret)
            {
                label += "Secret";
            }
            if (IsDangerous)
            {
                label += "Danger";
            }
            if (Theme.HasValue)
            {
                label += $"\\nTheme {Theme}";
            }
            return label;
        }

    }
}