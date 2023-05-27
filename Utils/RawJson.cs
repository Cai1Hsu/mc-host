using System.Buffers;
using System.Text;
using System.Text.Json;

namespace mchost.Utils;

public class RawJson
{
    private Utf8JsonWriter writer;

    private MemoryStream ms;

    private bool IsFlushed = false;

    public RawJson()
    {
        ms = new();
        writer = new(ms);
    }

    public RawJson(string text)
    {
        ms = new();
        writer = new(ms);

        writer.WriteStartObject();
        WriteText(text);
        writer.WriteEndObject();
        IsFlushed = true;
    }

    public RawJson(string text, string color)
    {
        ms = new();
        writer = new(ms);

        writer.WriteStartObject();
        WriteText(text, color);
        writer.WriteEndObject();
        IsFlushed = true;
    }

    public RawJson WriteText(string text, string color)
    {
        if (IsFlushed) throw new InvalidOperationException("RawJson is already flushed");

        writer.WriteString("text", text);
        writer.WriteString("color", color);
        return this;
    }

    public RawJson WriteText(string text)
    {
        if (IsFlushed) throw new InvalidOperationException("RawJson is already flushed");

        writer.WriteString("text", text);
        return this;
    }

    public RawJson WriteColor(string color)
    {
        if (IsFlushed) throw new InvalidOperationException("RawJson is already flushed");

        writer.WriteString("color", color);
        return this;
    }

    public RawJson WriteProperty(string key, string val)
    {
        if (IsFlushed) throw new InvalidOperationException("RawJson is already flushed");

        writer.WriteString(key, val);
        return this;
    }

    public RawJson WriteProperty(string key, int val)
    {
        if (IsFlushed) throw new InvalidOperationException("RawJson is already flushed");

        writer.WriteNumber(key, val);
        return this;
    }
    
    public RawJson WritePropertyName(string name)
    {
        if (IsFlushed) throw new InvalidOperationException("RawJson is already flushed");

        writer.WritePropertyName(name);
        return this;
    }

    public RawJson WriteStartObject()
    {
        if (IsFlushed) throw new InvalidOperationException("RawJson is already flushed");

        writer.WriteStartObject();
        return this;
    }

    public RawJson WriteEndObject()
    {
        if (IsFlushed) throw new InvalidOperationException("RawJson is already flushed");

        writer.WriteEndObject();
        return this;
    }

    public RawJson WriteStartArray()
    {
        if (IsFlushed) throw new InvalidOperationException("RawJson is already flushed");

        writer.WriteStartArray();
        return this;
    }

    public RawJson WriteEndArray()
    {
        if (IsFlushed) throw new InvalidOperationException("RawJson is already flushed");

        writer.WriteEndArray();
        return this;
    }

    public override string ToString()
    {
        IsFlushed = true;
        writer.Flush();
        return Encoding.UTF8.GetString(ms.ToArray());
    }

}
