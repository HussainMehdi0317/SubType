using System.IO;

namespace SubTyping;

public sealed class LocalTrackService
{
    public LocalTrackMetadata ReadMetadata(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("The selected audio file was not found.", filePath);
        }

        using var file = TagLib.File.Create(filePath);
        var artist = file.Tag.Performers.FirstOrDefault()?.Trim() ?? string.Empty;
        var title = file.Tag.Title?.Trim() ?? Path.GetFileNameWithoutExtension(filePath);
        return new LocalTrackMetadata(filePath, artist, title, file.Properties.Duration);
    }
}
