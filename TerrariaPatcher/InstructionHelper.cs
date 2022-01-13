using dnlib.DotNet.Emit;

namespace TerrariaPatcher;

internal static class InstructionHelper {
	public static bool Is(this Instruction instruction, Code opCode) => instruction.OpCode.Code == opCode;
	public static bool IsConstant(this Instruction instruction, int value)
		=> instruction.IsLdcI4() && instruction.GetLdcI4Value() == value;
	public static bool IsConstant(this Instruction instruction, long value)
		=> instruction.Is(Code.Ldc_I8) && (long) instruction.Operand == value;
	public static bool IsConstant(this Instruction instruction, float value)
		=> instruction.Is(Code.Ldc_R4) && (float) instruction.Operand == value;
	public static bool IsConstant(this Instruction instruction, double value)
		=> instruction.Is(Code.Ldc_R8) && (double) instruction.Operand == value;
	public static bool IsConstant(this Instruction instruction, string value)
		=> instruction.Is(Code.Ldstr) && (string) instruction.Operand == value;
}
