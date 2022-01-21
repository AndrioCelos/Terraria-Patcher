using System;
using System.Collections.Generic;

using Newtonsoft.Json;

namespace TerrariaPatcher.Mods;

internal class KeystrokeJsonConverter : JsonConverter {
	public override bool CanConvert(Type objectType) => objectType == typeof(Keystroke) || objectType == typeof(Keystroke[]);

	public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		=> reader.TokenType == JsonToken.Null ? throw new JsonSerializationException($"Cannot deserialise null to {objectType}.")
			: reader.TokenType != JsonToken.String ? throw new JsonSerializationException($"Invalid token type for {objectType}: expected String, but got {reader.TokenType}.")
			: objectType == typeof(Keystroke) ? CommandManager.ParseKeystrokes(reader.Value.ToString())[0]
			: CommandManager.ParseKeystrokes(reader.Value.ToString());

	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
		switch (value) {
			case Keystroke keystroke:
				writer.WriteValue(keystroke.ToString());
				break;
			case IEnumerable<Keystroke> keystrokes:
				writer.WriteValue(string.Join(",", keystrokes));
				break;
			default:
				throw new JsonSerializationException($"Unexpected value: expected {nameof(Keystroke)} or {nameof(Keystroke)}[], but got {value.GetType()}.");
		}
	}
}
