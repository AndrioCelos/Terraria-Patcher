using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace TerrariaPatcher;

public class ConfigFile {
	public class PatchOptionsEntry {
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate), DefaultValue(true)]
		public bool Enabled { get; set; }
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public object Config { get; set; }
	}

	public Dictionary<string, PatchOptionsEntry> PatchOptions { get; set; } = new();
}
