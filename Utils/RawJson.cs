using System.Text.Json;

namespace mchost.Utils;

public class RawJson
{
    Utf8JsonWriter writer = null!;

    Stream textStream = null!;

    public void applyCommonConfiguration()
    {
        textStream = new MemoryStream();
        writer = new Utf8JsonWriter(textStream);
    }

    public RawJson()
    {
        applyCommonConfiguration();
    }

    public RawJson(string text)
    {
        applyCommonConfiguration();

        WriteText(text);
    }

    public RawJson(string text, string color)
    {
        applyCommonConfiguration();

        WriteText(text);
        WriteColor(color);
    }

    public RawJson WriteText(string text)
    {
        writer.WriteString("text", text);
        return this;
    }
    public RawJson WriteColor(string color)
    {
        writer.WriteString("color", color);
        return this;
    }

    public RawJson WriteProperty(string key, string val)
    {
        writer.WriteString(key, val);
        return this;
    }

    public RawJson WriteProperty(string key, int val)
    {
        writer.WriteNumber(key, val);
        return this;
    }

    public RawJson WriteStartObject()
    {
        writer.WriteStartObject();
        return this;
    }

    public RawJson WriteEndObject()
    {
        writer.WriteEndObject();
        return this;
    }

    public override string ToString()
    {
        writer.Flush();
        return textStream.ToString() ?? string.Empty;
    }
}
