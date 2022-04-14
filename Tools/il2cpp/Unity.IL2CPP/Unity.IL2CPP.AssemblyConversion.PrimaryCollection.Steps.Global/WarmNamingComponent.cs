using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Services;

namespace Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Steps.Global
{
	public class WarmNamingComponent : GlobalScheduledStepAction<GlobalPrimaryCollectionContext, object>
	{
		protected override string Name => "Warm up Naming";

		protected override bool Skip(GlobalPrimaryCollectionContext context)
		{
			return context.Parameters.EnableSerialConversion;
		}

		protected override void ProcessItem(GlobalPrimaryCollectionContext context, AssemblyDefinition item, object globalState)
		{
			foreach (TypeDefinition item2 in item.AllDefinedTypes())
			{
				INamingService naming = context.Services.Naming;
				naming.ForTypeNameOnly(item2);
				foreach (MethodDefinition method2 in item2.Methods)
				{
					naming.ForMethodNameOnly(method2);
					naming.ForTypeNameOnly(method2.ReturnType);
					foreach (ParameterDefinition parameter in method2.Parameters)
					{
						naming.ForTypeNameOnly(parameter.ParameterType);
					}
					if (!method2.HasBody)
					{
						continue;
					}
					foreach (Instruction instruction in method2.Body.Instructions)
					{
						if (instruction.Operand is TypeReference type)
						{
							naming.ForTypeNameOnly(type);
						}
						else if (instruction.Operand is MethodReference method)
						{
							naming.ForMethodNameOnly(method);
						}
					}
				}
				foreach (EventDefinition @event in item2.Events)
				{
					naming.ForTypeNameOnly(@event.EventType);
				}
				foreach (FieldDefinition field in item2.Fields)
				{
					naming.ForTypeNameOnly(field.FieldType);
				}
			}
		}

		protected override object CreateGlobalState(GlobalPrimaryCollectionContext context)
		{
			return null;
		}
	}
}
