using Bookstore3.Repository;
using Syncfusion.UI.Xaml.Grid;
using System.IO;

namespace Bookstore3.WPF;

internal static class SfDataGridOptionsPersistence
{
    public static SerializationOptions CreateSerializationOptions() => new()
    {
        SerializeColumns = true,
        SerializeSorting = true,
        SerializeFiltering = true,
        SerializeGrouping = true,
        SerializeGroupSummaries = true,
        SerializeCaptionSummary = true,
        SerializeTableSummaries = true,
        SerializeUnBoundRows = true,
        SerializeStackedHeaders = true,
        SerializeDetailsViewDefinition = true
    };

    public static DeserializationOptions CreateDeserializationOptions() => new()
    {
        DeserializeColumns = true,
        DeserializeSorting = true,
        DeserializeFiltering = true,
        DeserializeGrouping = true,
        DeserializeGroupSummaries = true,
        DeserializeCaptionSummary = true,
        DeserializeTableSummaries = true,
        DeserializeUnBoundRows = true,
        DeserializeStackedHeaders = true,
        DeserializeDetailsViewDefinition = true
    };

    public static bool Save(IAppOptionRepository repository, string optionKey, SfDataGrid dataGrid)
    {
        try
        {
            using var stream = new MemoryStream();
            dataGrid.Serialize(stream, CreateSerializationOptions());
            var serialized = Convert.ToBase64String(stream.ToArray());
            return repository.SetOptionAsString(optionKey, serialized);
        }
        catch
        {
            return false;
        }
    }

    public static bool TryLoad(IAppOptionRepository repository, string optionKey, SfDataGrid dataGrid)
    {
        var serialized = repository.GetOptionAsString(optionKey);
        if (string.IsNullOrEmpty(serialized))
            return false;

        try
        {
            var bytes = Convert.FromBase64String(serialized);
            using var stream = new MemoryStream(bytes);
            dataGrid.Deserialize(stream, CreateDeserializationOptions());
            return true;
        }
        catch
        {
            return false;
        }
    }
}