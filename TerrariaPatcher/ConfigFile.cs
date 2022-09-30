using System.Collections.Generic;

using Newtonsoft.Json;

namespace TerrariaPatcher;

public class ConfigFile {
	public class PatchOptionsEntry {
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
		public bool Enabled { get; set; }
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public object Config { get; set; }
	}

	public Dictionary<string, PatchOptionsEntry> PatchOptions { get; set; } = new();
}
