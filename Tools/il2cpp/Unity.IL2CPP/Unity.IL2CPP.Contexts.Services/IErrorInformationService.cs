using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Unity.IL2CPP.Contexts.Services
{
	public interface IErrorInformationService
	{
		TypeDefinition CurrentType { get; set; }

		MethodDefinition CurrentMethod { get; set; }

		FieldDefinition CurrentField { get; set; }

		PropertyDefinition CurrentProperty { get; set; }

		EventDefinition CurrentEvent { get; set; }

		Instruction CurrentInstruction { get; set; }
	}
}
