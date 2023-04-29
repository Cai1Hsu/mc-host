using System.IO;
using System.Text.Json;

namespace mchost.Utils
{
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

        public void WriteText(string text) => writer.WriteString("text", text);

        public void WriteColor(string color) => writer.WriteString("color", color);

        public void WriteProperty(string key, string val) => writer.WriteString(key, val);
        
        public void WriteProperty(string key, int val) => writer.WriteNumber(key, val);

        public void WriteStartObject() => writer.WriteStartObject();
        
        public void WriteEndObject() => writer.WriteEndObject();

        public override string ToString()
        {
            writer.Flush();
            return textStream.ToString() ?? string.Empty;
        }
    }
}