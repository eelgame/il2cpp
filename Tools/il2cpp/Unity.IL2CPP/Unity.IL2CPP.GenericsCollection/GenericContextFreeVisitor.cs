using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.Cecil.Awesome;
using Unity.Cecil.Awesome.Comparers;
using Unity.Cecil.Visitor;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.GenericSharing;

namespace Unity.IL2CPP.GenericsCollection
{
	public class GenericContextFreeVisitor : Visitor
	{
		private readonly InflatedCollectionCollector _generics;

		private readonly PrimaryCollectionContext _context;

		public GenericContextFreeVisitor(PrimaryCollectionContext context, InflatedCollectionCollector generics)
		{
			_context = context;
			_generics = generics;
		}

		protected override void Visit(TypeDefinition typeDefinition, Context context)
		{
			_context.Global.Services.ErrorInformation.CurrentType = typeDefinition;
			if (ArrayRegistration.ShouldForce2DArrayFor(_context, typeDefinition))
			{
				ProcessArray(typeDefinition, 2);
			}
			if (typeDefinition.HasGenericParameters && GenericSharingAnalysis.AreFullySharableGenericParameters(_context, typeDefinition.GenericParameters))
			{
				ProcessGenericType(GenericSharingAnalysis.GetFullySharedType(typeDefinition));
			}
			ProcessIReferenceIfNeeded(typeDefinition);
			base.Visit(typeDefinition, context);
		}

		protected override void Visit(MethodDefinition methodDefinition, Context context)
		{
			_context.Global.Services.ErrorInformation.CurrentMethod = methodDefinition;
			if (MethodHasFullySharableGenericParameters(methodDefinition) && (!methodDefinition.DeclaringType.HasGenericParameters || TypeHasFullySharableGenericParameters(methodDefinition.DeclaringType)))
			{
				GenericContextAwareVisitor.ProcessGenericMethod(_context, (GenericInstanceMethod)GenericSharingAnalysis.GetFullySharedMethod(methodDefinition), _generics);
			}
			if ((methodDefinition.HasGenericParameters || methodDefinition.DeclaringType.HasGenericParameters) && (!methodDefinition.HasGenericParameters || GenericSharingAnalysis.AreFullySharableGenericParameters(_context, methodDefinition.GenericParameters)) && !methodDefinition.IsStripped() && (!methodDefinition.DeclaringType.HasGenericParameters || GenericSharingAnalysis.AreFullySharableGenericParameters(_context, methodDefinition.DeclaringType.GenericParameters)))
			{
				_context.Global.Collectors.GenericMethods.Add(_context, GenericSharingAnalysis.GetFullySharedMethod(methodDefinition));
			}
			foreach (GenericParameter genericParameter in methodDefinition.GenericParameters)
			{
				foreach (GenericParameterConstraint constraint in genericParameter.Constraints)
				{
					if (constraint.ConstraintType is GenericInstanceType && !constraint.ConstraintType.ContainsGenericParameters())
					{
						ProcessGenericType((GenericInstanceType)constraint.ConstraintType);
					}
				}
			}
			base.Visit(methodDefinition, context);
		}

		private bool MethodHasFullySharableGenericParameters(MethodDefinition methodDefinition)
		{
			if (methodDefinition.HasGenericParameters)
			{
				return GenericSharingAnalysis.AreFullySharableGenericParameters(_context, methodDefinition.GenericParameters);
			}
			return false;
		}

		private bool TypeHasFullySharableGenericParameters(TypeDefinition typeDefinition)
		{
			if (typeDefinition.HasGenericParameters)
			{
				return GenericSharingAnalysis.AreFullySharableGenericParameters(_context, typeDefinition.GenericParameters);
			}
			return false;
		}

		protected override void Visit(CustomAttributeArgument customAttributeArgument, Context context)
		{
			ProcessCustomAttributeArgument(_context, customAttributeArgument);
			base.Visit(customAttributeArgument, context);
		}

		private void ProcessCustomAttributeTypeReferenceRecursive(TypeReference typeReference)
		{
			if (typeReference is ArrayType arrayType)
			{
				ProcessCustomAttributeTypeReferenceRecursive(arrayType.ElementType);
			}
			else if (typeReference.IsGenericInstance)
			{
				ProcessGenericType((GenericInstanceType)typeReference);
			}
		}

		private void ProcessCustomAttributeArgument(ReadOnlyContext context, CustomAttributeArgument customAttributeArgument)
		{
			if (TypeReferenceEqualityComparer.AreEqual(customAttributeArgument.Type, context.Global.Services.TypeProvider.Corlib.MainModule.GetType("System", "Type")))
			{
				if (customAttributeArgument.Value is TypeReference typeReference)
				{
					ProcessCustomAttributeTypeReferenceRecursive(typeReference);
				}
			}
			else if (customAttributeArgument.Value is CustomAttributeArgument[] array)
			{
				CustomAttributeArgument[] array2 = array;
				foreach (CustomAttributeArgument customAttributeArgument2 in array2)
				{
					ProcessCustomAttributeArgument(context, customAttributeArgument2);
				}
			}
			else if (customAttributeArgument.Value is CustomAttributeArgument)
			{
				ProcessCustomAttributeArgument(context, (CustomAttributeArgument)customAttributeArgument.Value);
			}
		}

		protected override void Visit(MethodBody methodBody, Context context)
		{
			if (!methodBody.Method.HasGenericParameters)
			{
				base.Visit(methodBody, context);
			}
		}

		protected override void Visit(MethodReference methodReference, Context context)
		{
			GenericInstanceType genericInstanceType = methodReference.DeclaringType as GenericInstanceType;
			GenericInstanceMethod genericInstanceMethod = methodReference as GenericInstanceMethod;
			if (IsFullyInflated(genericInstanceMethod))
			{
				GenericInstanceType genericInstanceType2 = null;
				if (IsFullyInflated(genericInstanceType))
				{
					genericInstanceType2 = Inflater.InflateType(null, genericInstanceType);
					ProcessGenericType(genericInstanceType2);
				}
				GenericContextAwareVisitor.ProcessGenericMethod(_context, Inflater.InflateMethod(new GenericContext(genericInstanceType2, null), genericInstanceMethod), _generics);
			}
			else if (IsFullyInflated(genericInstanceType))
			{
				ProcessGenericType(Inflater.InflateType(null, genericInstanceType));
			}
			if (!methodReference.HasGenericParameters && !methodReference.IsGenericInstance)
			{
				base.Visit(methodReference, context);
			}
		}

		protected override void Visit(ArrayType arrayType, Context context)
		{
			if (!arrayType.ContainsGenericParameters())
			{
				ProcessArray(arrayType.ElementType, arrayType.Rank);
			}
			base.Visit(arrayType, context);
		}

		protected override void Visit(Instruction instruction, Context context)
		{
			if (instruction.OpCode.Code == Code.Newarr)
			{
				TypeReference typeReference = (TypeReference)instruction.Operand;
				if (!typeReference.ContainsGenericParameters())
				{
					ProcessArray(typeReference, 1);
				}
			}
			base.Visit(instruction, context);
		}

		protected override void Visit(GenericInstanceType genericInstanceType, Context context)
		{
			if (!genericInstanceType.ContainsGenericParameters())
			{
				GenericInstanceType inflatedType = Inflater.InflateType(null, genericInstanceType);
				ProcessGenericType(inflatedType);
			}
			base.Visit(genericInstanceType, context);
		}

		private void ProcessGenericType(GenericInstanceType inflatedType)
		{
			GenericContextAwareVisitor.ProcessGenericType(_context, inflatedType, _generics, null);
		}

		private void ProcessArray(TypeReference elementType, int rank)
		{
			for (TypeReference typeReference = elementType; typeReference != null; typeReference = typeReference.GetBaseType(_context))
			{
				InflateAndProcessArray(typeReference, rank);
				foreach (TypeReference @interface in typeReference.GetInterfaces(_context))
				{
					InflateAndProcessArray(@interface, rank);
				}
			}
		}

		private void InflateAndProcessArray(TypeReference elementType, int rank)
		{
			ArrayType inflatedType = new ArrayType(Inflater.InflateType(null, elementType), rank);
			GenericContextAwareVisitor.ProcessArray(_context, inflatedType, _generics, new GenericContext(null, null));
		}

		private static bool IsFullyInflated(GenericInstanceMethod genericInstanceMethod)
		{
			if (genericInstanceMethod != null && !genericInstanceMethod.GenericArguments.Any((TypeReference t) => t.ContainsGenericParameters()))
			{
				return !genericInstanceMethod.DeclaringType.ContainsGenericParameters();
			}
			return false;
		}

		private static bool IsFullyInflated(GenericInstanceType genericInstanceType)
		{
			if (genericInstanceType != null)
			{
				return !genericInstanceType.ContainsGenericParameters();
			}
			return false;
		}

		private void ProcessIReferenceIfNeeded(TypeDefinition typeDefinition)
		{
			if (typeDefinition.CanBoxToWindowsRuntime(_context))
			{
				GenericInstanceType genericInstanceType = new GenericInstanceType(_context.Global.Services.TypeProvider.IReferenceType);
				genericInstanceType.GenericArguments.Add(typeDefinition);
				ProcessGenericType(genericInstanceType);
				genericInstanceType = new GenericInstanceType(_context.Global.Services.TypeProvider.IReferenceArrayType);
				genericInstanceType.GenericArguments.Add(typeDefinition);
				ProcessGenericType(genericInstanceType);
			}
		}
	}
}
