#nullable enable

using System;
using System.Collections.Generic;
using System.IO;

using dnlib.DotNet;
using dnlib.DotNet.Emit;

using Newtonsoft.Json;

using Terraria;

namespace TerrariaPatcher.PatchSets;

internal class UuidSecurity : PatchSet {
	public override string Name => "UUID Security";
	public override Version Version => new(1, 0);
	public override string Description => "Send a unique UUID to each server you join. This enables the UUID to be used for authentication on a modded server without the risk of being exposed by joining a malicious server.";

	public static string? CurrentServerUuid;
	public static readonly Dictionary<string, Guid> ServerUuids = [];

	internal class InitializePatch : MainInitializePatch {
		public static void Prefix() {
			if (File.Exists(Path.Combine(Main.SavePath, "serverUuids.json"))) {
				using var reader = new JsonTextReader(new StreamReader(Path.Combine(Main.SavePath, "serverUuids.json")));
				new JsonSerializer().Populate(reader, ServerUuids);
			}
		}
	}

	internal class StartClientGameplayPatch : PrefixPatch {
		public override PatchTarget TargetMethod => PatchTarget.Create(Main.StartClientGameplay);

		public static void Prefix() {
			if (!ServerUuids.TryGetValue(Main.getIP, out var uuid)) {
				uuid = Guid.NewGuid();
				ServerUuids[Main.getIP] = uuid;
			}
			CurrentServerUuid = uuid.ToString();
			var file = Path.Combine(Main.SavePath, "serverUuids.json");
			using var writer = new JsonTextWriter(new StreamWriter(file));
			new JsonSerializer().Serialize(writer, ServerUuids);
		}
	}

	internal class SendDataPatch : Patch {
		public override PatchTarget TargetMethod => PatchTarget.Create(NetMessage.SendData);

		public override void PatchMethodBody(MethodDef method) {
			// Replace .Write(Main.clientUUID) with .Write(UuidSecurity.CurrentServerUuid ?? Main.clientUUID);
			var instructions = method.Body.Instructions;
			for (int i = 0; i < instructions.Count; i++) {
				if (instructions[i].Is(Code.Ldsfld) && ((IField) instructions[i].Operand).Name == nameof(Main.clientUUID)) {
					var normalField = (IField) instructions[i].Operand;
					var jumpTarget = instructions[i + 1];

					instructions[i] = this.LoadField(typeof(UuidSecurity), nameof(CurrentServerUuid));
					instructions.Insert(i + 1, OpCodes.Dup.ToInstruction());
					instructions.Insert(i + 2, OpCodes.Brtrue_S.ToInstruction(jumpTarget));
					instructions.Insert(i + 3, OpCodes.Pop.ToInstruction());
					instructions.Insert(i + 4, OpCodes.Ldsfld.ToInstruction(normalField));
					return;
				}
			}
			throw new ArgumentException("Couldn't find code to replace.");
		}
	}
}
