#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using dnlib.DotNet;
using dnlib.DotNet.Emit;

using Terraria;

namespace TerrariaPatcher;

public abstract class Patch {
	protected const string CONSTRUCTOR = ".ctor";
	protected const string STATIC_CONSTRUCTOR = ".cctor";

	public PatchSet? PatchSet { get; internal set; }
	public abstract PatchTarget TargetMethod { get; }

	internal TargetModule? currentPatchTargetModule;
	internal TypeDef? currentPatchSetType;
	internal TypeDef? currentPatchType;

	public abstract void PatchMethodBody(MethodDef method);

	protected Instruction Call(Delegate method) {
		if (this.currentPatchTargetModule is null) throw new InvalidOperationException("This method may be called only while patching.");
		if (method.Method.Module.Name == this.currentPatchTargetModule.CurrentModuleDef.Name) {
			// Calling a method in this module.
			return OpCodes.Call.ToInstruction(this.currentPatchTargetModule.CurrentModuleDef.Import(method.Method).ResolveMethodDefThrow());
		} else if (method.Method.Module.Name == this.currentPatchTargetModule.ModuleDef.Name) {
			// Calling a method in the target module.
			return OpCodes.Call.ToInstruction(this.currentPatchTargetModule.ModuleDef.Import(method.Method).ResolveMethodDefThrow());
		} else if (method.Method.Module == typeof(object).Module) {
			// Calling a core library method.
			throw new NotSupportedException("Call(Delegate) is only supported for a method in this module or the target module.");
		} else
			throw new ArgumentException($"Method is not part of any module I can reference: {method}");
	}
	protected Instruction Call(MethodDef method) {
		if (this.currentPatchTargetModule is null) throw new InvalidOperationException("This method may be called only while patching.");
		return OpCodes.Call.ToInstruction(method);
	}
	protected Instruction Call(Type type, string method) {
		if (this.currentPatchTargetModule is null) throw new InvalidOperationException("This method may be called only while patching.");
		if (type.Module.Name == this.currentPatchTargetModule.CurrentModuleDef.Name) {
			// Calling a method in this module.
			var typeDef = this.currentPatchTargetModule.CurrentModuleDef.Import(type).ResolveTypeDefThrow();
			var methodDef = typeDef.FindMethod(method);
			return OpCodes.Call.ToInstruction(methodDef);
		} else if (type.Module.Name == this.currentPatchTargetModule.ModuleDef.Name) {
			// Calling a method in the target module.
			var typeDef = this.currentPatchTargetModule.ModuleDef.Import(type).ResolveTypeDefThrow();
			var methodDef = typeDef.FindMethod(method);
			return OpCodes.Call.ToInstruction(methodDef);
		} else if (type.Module == typeof(object).Module) {
			// Calling a core library method.
			throw new NotSupportedException("For a core library method, a signature must be provided.");
		} else
			throw new ArgumentException($"Type is not part of any module I can reference: {type}");
	}
	protected Instruction Call(Type type, string method, MethodSig signature) {
		if (this.currentPatchTargetModule is null) throw new InvalidOperationException("This method may be called only while patching.");
		if (type.Module.Name == this.currentPatchTargetModule.CurrentModuleDef.Name) {
			// Calling a method in this module.
			var typeDef = this.currentPatchTargetModule.CurrentModuleDef.Import(type).ResolveTypeDefThrow();
			var methodDef = typeDef.FindMethod(method, signature);
			return OpCodes.Call.ToInstruction(methodDef);
		} else if (type.Module.Name == this.currentPatchTargetModule.ModuleDef.Name) {
			// Calling a method in the target module.
			var typeDef = this.currentPatchTargetModule.ModuleDef.Import(type).ResolveTypeDefThrow();
			var methodDef = typeDef.FindMethod(method, signature);
			return OpCodes.Call.ToInstruction(methodDef);
		} else if (type.Module == typeof(object).Module) {
			// Calling a core library method.
			var typeRef = this.currentPatchTargetModule.ModuleDef.CorLibTypes.GetTypeRef(type.Namespace, type.Name);
			var methodRef = new MemberRefUser(this.currentPatchTargetModule.ModuleDef, method, signature, typeRef);
			return OpCodes.Call.ToInstruction(methodRef);
		} else
			throw new ArgumentException($"Type is not part of any module I can reference: {type}");
	}

	protected Instruction LoadField(Type type, string fieldName, bool byReference = false) {
		if (this.currentPatchTargetModule is null) throw new InvalidOperationException("This method may be called only while patching.");
		if (type.Module.Name == this.currentPatchTargetModule.CurrentModuleDef.Name) {
			// Loading a field in this module.
			var typeDef = this.currentPatchTargetModule.CurrentModuleDef.Import(type).ResolveTypeDefThrow();
			var field = typeDef.FindField(fieldName);
			if (field.IsStatic) return (byReference ? OpCodes.Ldsflda : OpCodes.Ldsfld).ToInstruction(field);
			else return (byReference ? OpCodes.Ldflda : OpCodes.Ldfld).ToInstruction(field);
		} else if (type.Module.Name == this.currentPatchTargetModule.ModuleDef.Name) {
			// Loading a field in the target module.
			var typeDef = this.currentPatchTargetModule.ModuleDef.Import(type).ResolveTypeDefThrow();
			var field = typeDef.FindField(fieldName);
			if (field.IsStatic) return (byReference ? OpCodes.Ldsflda : OpCodes.Ldsfld).ToInstruction(field);
			else return (byReference ? OpCodes.Ldflda : OpCodes.Ldfld).ToInstruction(field);
		} else
			throw new ArgumentException($"Type is not part of any module I can reference: {type}");
	}
	protected Instruction StoreField(Type type, string fieldName) {
		if (this.currentPatchTargetModule is null) throw new InvalidOperationException("This method may be called only while patching.");
		if (type.Module.Name == this.currentPatchTargetModule.CurrentModuleDef.Name) {
			// Loading a field in this module.
			var typeDef = this.currentPatchTargetModule.CurrentModuleDef.Import(type).ResolveTypeDefThrow();
			var field = typeDef.FindField(fieldName);
			if (field.IsStatic) return OpCodes.Stsfld.ToInstruction(field);
			else return OpCodes.Stfld.ToInstruction(field);
		} else if (type.Module.Name == this.currentPatchTargetModule.ModuleDef.Name) {
			// Loading a field in the target module.
			var typeDef = this.currentPatchTargetModule.ModuleDef.Import(type).ResolveTypeDefThrow();
			var field = typeDef.FindField(fieldName);
			if (field.IsStatic) return OpCodes.Stsfld.ToInstruction(field);
			else return OpCodes.Stfld.ToInstruction(field);
		} else
			throw new ArgumentException($"Type is not part of any module I can reference: {type}");
	}
}

public abstract class PrefixPatch : Patch {
	protected virtual bool PostfixIsOptional => false;

	public override void PatchMethodBody(MethodDef method) {
		Local? stateLocal = null; Local? resultLocal = null;

		var patchTypeDef = currentPatchTargetModule.CurrentModuleDef.Import(this.GetType()).ResolveTypeDefThrow();
		var prefixMethodDef = patchTypeDef.FindMethod("Prefix");
		var postfixMethodDef = patchTypeDef.FindMethod("Postfix");

		if (prefixMethodDef is null && postfixMethodDef is null)
			throw new InvalidOperationException("No prefix or postfix found");

		if (prefixMethodDef is not null) {
			if (prefixMethodDef.Parameters.Any(p => p.Name == "__result"))
				resultLocal = GetResultLocal(method);

			if (prefixMethodDef.Parameters.FirstOrDefault(p => p.Name == "__state") is Parameter param) {
				stateLocal = new(param.Type is ByRefSig byRefSig ? byRefSig.Next : throw new InvalidOperationException("State parameter in prefix should be a ref or out parameter."), "__state");
				method.Body.Variables.Add(stateLocal);
			}

			var instructionsToAdd = new List<Instruction>();
			instructionsToAdd.AddRange(this.SetupArguments(method, prefixMethodDef, stateLocal, resultLocal));
			instructionsToAdd.Add(Call(prefixMethodDef));
			if (prefixMethodDef.ReturnType.ElementType == ElementType.Boolean) {
				if (resultLocal is not null) {
					instructionsToAdd.Add(OpCodes.Brtrue.ToInstruction(method.Body.Instructions[0]));
					instructionsToAdd.Add(OpCodes.Ldloc_S.ToInstruction(resultLocal));
					instructionsToAdd.Add(OpCodes.Ret.ToInstruction());
				} else {
					if (method.HasReturnType) throw new ArgumentException("Prefix that returns bool patching a non-void method must have an out __result parameter.");
					instructionsToAdd.Add(OpCodes.Brfalse.ToInstruction(method.Body.Instructions[method.Body.Instructions.Count - 1]));
				}
			} else if (prefixMethodDef.HasReturnType) {
				throw new InvalidOperationException($"Invalid return type for prefix method: {prefixMethodDef.ReturnType}");
			}

			for (int i = 0; i < instructionsToAdd.Count; i++)
				method.Body.Instructions.Insert(i, instructionsToAdd[i]);
		}
		var postfixMethod = this.GetType().GetMethod("Postfix", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
		if (postfixMethod is not null) {
			if (resultLocal is null && method.HasReturnType) {
				resultLocal = new(method.ReturnType, "__result");
				method.Body.Variables.Add(resultLocal);
			}

			var instructionsToAdd = new List<Instruction>();
			if (resultLocal is not null) {
				// If the method has a return type, the return value will be on the stack at this point.
				// We need to put it into the __result local.
				instructionsToAdd.Add(OpCodes.Stloc_S.ToInstruction(resultLocal));
			}
			instructionsToAdd.AddRange(this.SetupArguments(method, postfixMethodDef, stateLocal, resultLocal));
			instructionsToAdd.Add(Call(postfixMethodDef));
			if (method.HasReturnType) {
				if (postfixMethodDef.HasReturnType) {
					// Pass-through postfix: return the method's return value.
					var assignability = Program.TypeIsAssignableTo(postfixMethodDef.ReturnType, method.ReturnType);
					if (assignability == Program.AssignabilityResult.NotAssignable)
						throw new ArgumentException($"Postfix return type {postfixMethodDef.ReturnType} is not compatible with original return value of type {method.ReturnType}.");
					if (assignability == Program.AssignabilityResult.AssignableWithBox)
						instructionsToAdd.Add(OpCodes.Box.ToInstruction(new TypeSpecUser(postfixMethodDef.ReturnType)));
				} else {
					// Normal postfix: return __result
					instructionsToAdd.Add(OpCodes.Ldloc_S.ToInstruction(resultLocal));
				}
			} else if (postfixMethodDef.HasReturnType)
				throw new ArgumentException($"Postfix return type {postfixMethodDef.ReturnType} is not compatible with original return value of type {method.ReturnType}.");
			instructionsToAdd.Add(OpCodes.Ret.ToInstruction());

			for (int i = 0; i < method.Body.Instructions.Count; i++) {
				if (method.Body.Instructions[i].OpCode == OpCodes.Ret) {
					if (i == method.Body.Instructions.Count - 1) {
						method.Body.Instructions[i].OpCode = OpCodes.Nop;
						method.Body.Instructions[i].Operand = null;
					} else if (!this.PostfixIsOptional) {
						method.Body.Instructions[i].OpCode = OpCodes.Br;
						method.Body.Instructions[i].Operand = instructionsToAdd[0];
					}
				}
			}

			for (int i = 0; i < instructionsToAdd.Count; i++)
				method.Body.Instructions.Add(instructionsToAdd[i]);
		}
	}

	private static Local GetResultLocal(MethodDef method) {
		Local? resultLocal;
		if (!method.HasReturnType) throw new ArgumentException("Result injection is not valid on void method");
		resultLocal = method.Body.Variables.FirstOrDefault(p => p.Name == "__result");
		if (resultLocal is null) {
			resultLocal = new(method.ReturnType, "__result");
			method.Body.Variables.Add(resultLocal);
		}

		return resultLocal;
	}

	/// <remarks>
	///		The following names are allowed for injection parameters.
	///		<list type="table">
	///			<listheader>
	///				<term>Parameter name</term>
	///				<description>Value injected</description>
	///			</listheader>
	///			<item>
	///				<term><c>__instance</c></term>
	///				<description>The instance on which the original method was run. Only valid for instance methods.</description>
	///			</item>
	///			<item>
	///				<term><c>__state</c></term>
	///				<description>
	///					An added local variable allowing you to share state between a prefix and postfix.
	///					In the prefix, this must be an out parameter. In the postfix, this may be a ref parameter or not.
	///				</description>
	///			</item>
	///			<item>
	///				<term><c>__result</c></term>
	///				<description>
	///					In the prefix, the value to return when the original method is skipped.
	///					In the postfix, the original method's return value.
	///				</description>
	///			</item>
	///			<item>
	///				<term><c>__n</c></term>
	///				<description>The original method's parameter at index <c>n</c>. For instance methods, parameter 0 will be the instance.</description>
	///			</item>
	///			<item>
	///				<term>Original parameter name</term>
	///				<description>The original parameter value</description>
	///			</item>
	///		</list>
	///		<para>
	///			For a ref parameter <c>__instance</c> with a value type or,
	///			the injection parameter may be by reference to copy the reference or by value to dereference and copy the value.
	///			If by value, the injection parameter may be any type assignable from the original value.
	///		</para>
	///		<para>
	///			For by-value parameters,
	///			the injection parameter may be by reference to give read-write access to the original, or by value to copy it.
	///			If by value, the injection parameter may be any type assignable from the original value.
	///		</para>
	/// </remarks>
	private IEnumerable<Instruction> SetupArguments(MethodDef targetMethod, MethodDef prefixMethod, Local? stateLocal, Local? resultLocal) {
		foreach (var parameter in prefixMethod.Parameters) {
			if (parameter.Name == "__instance") {
				if (targetMethod.IsStatic)
					throw new InvalidOperationException("Instance parameter is not valid for a static method.");
				var thisParameter = targetMethod.Parameters[0];
				if (thisParameter.Type.IsByRef) {
					// The this parameter is a reference for instance methods of value types.
					// In this case, the injection parameter may also have a ref type.
					if (parameter.Type.IsByRef) {
						yield return Program.TypeIsAssignableTo(thisParameter.Type, parameter.Type) == Program.AssignabilityResult.Assignable
							? OpCodes.Ldarg_0.ToInstruction()
							: throw new ArgumentException($"Instance parameter of type {parameter.Type} is not compatible with target parameter of type {thisParameter.Type}.");
					} else {
						var assignability = Program.TypeIsAssignableTo(thisParameter.Type.Next, parameter.Type);
						if (assignability == Program.AssignabilityResult.NotAssignable)
							throw new ArgumentException($"Instance parameter of type {parameter.Type} is not compatible with target parameter of type {thisParameter.Type}.");
						yield return OpCodes.Ldarg_0.ToInstruction();
						yield return OpCodes.Ldobj.ToInstruction(new TypeSpecUser(thisParameter.Type.Next));
						if (assignability == Program.AssignabilityResult.AssignableWithBox)
							yield return OpCodes.Box.ToInstruction(new TypeSpecUser(thisParameter.Type.Next));
					}
				} else {
					if (parameter.Type.IsByRef) {
						yield return Program.TypeIsAssignableTo(new ByRefSig(thisParameter.Type), parameter.Type) == Program.AssignabilityResult.Assignable
						? OpCodes.Ldarga_S.ToInstruction(thisParameter)
						: throw new ArgumentException($"Instance parameter of type {parameter.Type} is not compatible with target parameter of type {thisParameter.Type}.");
					} else {
						var assignability = Program.TypeIsAssignableTo(thisParameter.Type, parameter.Type);
						yield return assignability != Program.AssignabilityResult.NotAssignable
							? OpCodes.Ldarg_0.ToInstruction()
							: throw new ArgumentException($"Instance parameter of type {parameter.Type} is not compatible with target parameter of type {thisParameter.Type}.");
						if (assignability == Program.AssignabilityResult.AssignableWithBox)
							yield return OpCodes.Box.ToInstruction(new TypeSpecUser(thisParameter.Type));
					}
				}
			} else if (parameter.Name == "__state") {
				if (stateLocal is null)
					throw new InvalidOperationException("Missing state local");
				else if (prefixMethod.Name != "Prefix") {
					var assignability = Program.TypeIsAssignableTo(stateLocal.Type, parameter.Type);
					if (assignability == Program.AssignabilityResult.NotAssignable)
						throw new InvalidCastException($"State parameter type {parameter.Type} is not compatible with {stateLocal.Type}");
					yield return parameter.Type.IsByRef
						? OpCodes.Ldloca_S.ToInstruction(stateLocal)
						: OpCodes.Ldloc_S.ToInstruction(stateLocal);
					if (assignability == Program.AssignabilityResult.AssignableWithBox)
						yield return OpCodes.Box.ToInstruction(new TypeSpecUser(stateLocal.Type));
				} else {
					yield return parameter.Type.IsByRef
						? OpCodes.Ldloca_S.ToInstruction(stateLocal)
						: OpCodes.Ldloc_S.ToInstruction(stateLocal);
				}
			} else if (parameter.Name == "__result") {
				if (resultLocal is null)
					throw new InvalidOperationException("Missing result local");
				else {
					if (parameter.Type.IsByRef) {
						yield return Program.TypeIsAssignableTo(new ByRefSig(resultLocal.Type), parameter.Type) == Program.AssignabilityResult.Assignable
						? OpCodes.Ldloca_S.ToInstruction(resultLocal)
						: throw new ArgumentException($"Instance parameter of type {parameter.Type} is not compatible with return value of type {resultLocal.Type}.");
					} else {
						var assignability = Program.TypeIsAssignableTo(resultLocal.Type, parameter.Type);
						yield return assignability != Program.AssignabilityResult.NotAssignable
							? OpCodes.Ldloc_S.ToInstruction(resultLocal)
							: throw new ArgumentException($"Instance parameter of type {parameter.Type} is not compatible with return value of type {resultLocal.Type}.");
						if (assignability == Program.AssignabilityResult.AssignableWithBox)
							yield return OpCodes.Box.ToInstruction(new TypeSpecUser(resultLocal.Type));
					}
				}
			} else if (parameter.Name.StartsWith("___")) {
				// Field/property parameter
				var memberName = parameter.Name.Substring(3);
				if (targetMethod.DeclaringType.GetField(memberName) is FieldDef field) {
					if (field.IsStatic) {
						yield return parameter.Type.IsByRef
							? OpCodes.Ldsflda.ToInstruction(field)
							: OpCodes.Ldsfld.ToInstruction(field);
					} else if (!targetMethod.IsStatic) {
						yield return OpCodes.Ldarg_0.ToInstruction();
						yield return parameter.Type.IsByRef
							? OpCodes.Ldflda.ToInstruction(field)
							: OpCodes.Ldfld.ToInstruction(field);
					} else
						throw new InvalidOperationException($"Can't read instance field {memberName} in a static method.");
				} else
					throw new InvalidOperationException($"Can't find member {memberName}");
			} else {
				if (parameter.Name.StartsWith("__") && int.TryParse(parameter.Name.Substring(2), out var paramIndex)) {
					if (paramIndex < 0 || paramIndex >= targetMethod.Parameters.Count)
						throw new ArgumentException($"Parameter index {paramIndex} is not valid in {prefixMethod.FullName}");
				} else {
					var found = false;
					for (paramIndex = 0; paramIndex < targetMethod.Parameters.Count; paramIndex++) {
						if (parameter.Name == targetMethod.Parameters[paramIndex].Name) {
							found = true;
							break;
						}
					}
					if (!found)
						throw new ArgumentException($"No parameter matching '{parameter.Name}' in {targetMethod.FullName}");
				}
				var targetParameter = targetMethod.Parameters[paramIndex];
				if (targetParameter.Type.IsByRef) {
					if (parameter.Type.IsByRef) {
						// If the target parameter is a ref parameter, the reference must be passed by value.
						yield return Program.TypeIsAssignableTo(targetParameter.Type, parameter.Type) == Program.AssignabilityResult.Assignable
							? paramIndex switch {
								0 => OpCodes.Ldarg_0.ToInstruction(),
								1 => OpCodes.Ldarg_1.ToInstruction(),
								2 => OpCodes.Ldarg_2.ToInstruction(),
								3 => OpCodes.Ldarg_3.ToInstruction(),
								_ => OpCodes.Ldarg_S.ToInstruction(targetParameter)
							}
							: throw new ArgumentException($"Parameter '{parameter.Name}' of type {parameter.Type} is not compatible with target parameter of type {targetParameter.Type}.");
					} else {
						var assignability = Program.TypeIsAssignableTo(targetParameter.Type.Next, parameter.Type);
						yield return assignability != Program.AssignabilityResult.NotAssignable
							? paramIndex switch {
								0 => OpCodes.Ldarg_0.ToInstruction(),
								1 => OpCodes.Ldarg_1.ToInstruction(),
								2 => OpCodes.Ldarg_2.ToInstruction(),
								3 => OpCodes.Ldarg_3.ToInstruction(),
								_ => OpCodes.Ldarg_S.ToInstruction(targetParameter)
							}
							: throw new ArgumentException($"Parameter '{parameter.Name}' of type {parameter.Type} is not compatible with target parameter of type {targetParameter.Type}.");
						yield return OpCodes.Ldobj.ToInstruction(new TypeSpecUser(targetParameter.Type.Next));
						if (assignability == Program.AssignabilityResult.AssignableWithBox)
							yield return OpCodes.Box.ToInstruction(new TypeSpecUser(targetParameter.Type.Next));
					}
				} else if (parameter.Type.IsByRef) {
					// If the patch parameter is a ref parameter and the target parameter is by value, it gives read-write access to the target argument.
					yield return Program.TypeIsAssignableTo(new ByRefSig(targetParameter.Type), parameter.Type) == Program.AssignabilityResult.Assignable
						? OpCodes.Ldarga_S.ToInstruction(targetParameter)
                        : throw new ArgumentException($"Parameter '{parameter.Name}' of type {parameter.Type} is not compatible with target parameter of type {targetParameter.Type}.");
				} else {
					var assignability = Program.TypeIsAssignableTo(targetParameter.Type, parameter.Type);
					yield return assignability != Program.AssignabilityResult.NotAssignable
                        ? paramIndex switch {
							0 => OpCodes.Ldarg_0.ToInstruction(),
							1 => OpCodes.Ldarg_1.ToInstruction(),
							2 => OpCodes.Ldarg_2.ToInstruction(),
							3 => OpCodes.Ldarg_3.ToInstruction(),
							_ => OpCodes.Ldarg_S.ToInstruction(targetParameter)
						}
						: throw new ArgumentException($"Parameter '{parameter.Name}' of type {parameter.Type} is not compatible with target parameter of type {targetParameter.Type}.");
					if (assignability == Program.AssignabilityResult.AssignableWithBox)
						yield return OpCodes.Box.ToInstruction(new TypeSpecUser(targetParameter.Type));
				}
			}
		}
	}
}

/// <summary>A <see cref="PrefixPatch"/> that runs when the game is initialising.</summary>
/// <remarks>
///		Implementers should use the <c>Prefix</c> method for initialising their own state,
///		and <c>Postfix</c> for initialisation that depends on other mods.
///	</remarks>
public abstract class MainInitializePatch : PrefixPatch {
	protected override bool PostfixIsOptional => true;
	public override PatchTarget TargetMethod => PatchTarget.Create(typeof(Main), "ClientInitialize");
}
