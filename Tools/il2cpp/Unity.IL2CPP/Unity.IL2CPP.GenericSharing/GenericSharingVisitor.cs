using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Unity.Cecil.Awesome.Comparers;
using Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Results;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.GenericSharing
{
	public class GenericSharingVisitor
	{
		private readonly PrimaryCollectionContext _context;

		private readonly Dictionary<TypeDefinition, GenericSharingData> _genericTypeData = new Dictionary<TypeDefinition, GenericSharingData>();

		private readonly Dictionary<MethodDefinition, GenericSharingData> _genericMethodData = new Dictionary<MethodDefinition, GenericSharingData>();

		private List<RuntimeGenericData> _typeList;

		private List<RuntimeGenericData> _methodList;

		public GenericSharingVisitor(PrimaryCollectionContext context)
		{
			_context = context;
		}

		public void Collect(AssemblyDefinition assembly)
		{
			foreach (TypeDefinition allType in assembly.MainModule.GetAllTypes())
			{
				ProcessType(allType);
			}
		}

		public void Add(GenericSharingVisitor other)
		{
			foreach (KeyValuePair<TypeDefinition, GenericSharingData> genericTypeDatum in other._genericTypeData)
			{
				_genericTypeData.Add(genericTypeDatum.Key, genericTypeDatum.Value);
			}
			foreach (KeyValuePair<MethodDefinition, GenericSharingData> genericMethodDatum in other._genericMethodData)
			{
				_genericMethodData.Add(genericMethodDatum.Key, genericMethodDatum.Value);
			}
		}

		public GenericSharingAnalysisResults Complete()
		{
			return new GenericSharingAnalysisResults(_genericTypeData.AsReadOnly(), _genericMethodData.AsReadOnly());
		}

		private void AddType(TypeDefinition typeDefinition, List<RuntimeGenericData> runtimeGenericDataList)
		{
			_genericTypeData.Add(typeDefinition, new GenericSharingData(runtimeGenericDataList.AsReadOnly()));
			CollectGenericMethodsFromRgctxs(runtimeGenericDataList);
		}

		private void AddMethod(MethodDefinition methodDefinition, List<RuntimeGenericData> runtimeGenericDataList)
		{
			_genericMethodData.Add(methodDefinition, new GenericSharingData(runtimeGenericDataList.AsReadOnly()));
			CollectGenericMethodsFromRgctxs(runtimeGenericDataList);
		}

		private void CollectGenericMethodsFromRgctxs(List<RuntimeGenericData> runtimeGenericDataList)
		{
			if (_context.Global.Parameters.UsingTinyBackend)
			{
				return;
			}
			foreach (RuntimeGenericData runtimeGenericData in runtimeGenericDataList)
			{
				if (runtimeGenericData.InfoType == RuntimeGenericContextInfo.Method)
				{
					_context.Global.Collectors.GenericMethods.Add(_context, ((RuntimeGenericMethodData)runtimeGenericData).GenericMethod);
				}
			}
		}

		public void ProcessType(TypeDefinition type)
		{
			_typeList = new List<RuntimeGenericData>();
			foreach (MethodDefinition method in type.Methods)
			{
				if (!method.HasBody)
				{
					if (!_context.Global.Results.Setup.RuntimeImplementedMethodWriters.TryGetGenericSharingDataFor(method, out var value))
					{
						continue;
					}
					foreach (RuntimeGenericData item in value)
					{
						if (item is RuntimeGenericTypeData runtimeGenericTypeData)
						{
							AddData(runtimeGenericTypeData.InfoType, runtimeGenericTypeData.GenericType);
							continue;
						}
						if (item is RuntimeGenericMethodData runtimeGenericMethodData)
						{
							AddMethodUsage(runtimeGenericMethodData.GenericMethod);
							continue;
						}
						throw new NotImplementedException();
					}
					continue;
				}
				_methodList = new List<RuntimeGenericData>();
				foreach (ExceptionHandler exceptionHandler in method.Body.ExceptionHandlers)
				{
					if (exceptionHandler.CatchType != null)
					{
						AddClassUsage(exceptionHandler.CatchType);
					}
				}
				foreach (Instruction instruction in method.Body.Instructions)
				{
					Process(instruction, method);
				}
				if (_methodList.Count > 0)
				{
					AddMethod(method, _methodList);
				}
			}
			if (_typeList.Count > 0)
			{
				AddType(type, _typeList);
			}
		}

		private void Process(Instruction instruction, MethodDefinition method)
		{
			switch (instruction.OpCode.Code)
			{
			case Code.Newobj:
			{
				MethodReference methodReference3 = (MethodReference)instruction.Operand;
				if (methodReference3.DeclaringType.IsArray)
				{
					AddClassUsage(methodReference3.DeclaringType);
					break;
				}
				AddClassUsage(methodReference3.DeclaringType);
				AddMethodUsage(methodReference3);
				break;
			}
			case Code.Newarr:
			{
				TypeReference genericType4 = (TypeReference)instruction.Operand;
				AddArrayUsage(genericType4);
				break;
			}
			case Code.Ldsfld:
			case Code.Ldsflda:
			case Code.Stsfld:
			{
				TypeReference declaringType = ((FieldReference)instruction.Operand).DeclaringType;
				AddStaticUsage(_context, declaringType);
				break;
			}
			case Code.Unbox:
			{
				TypeReference typeReference2 = (TypeReference)instruction.Operand;
				if (typeReference2.IsNullable())
				{
					AddClassUsage(((GenericInstanceType)typeReference2).GenericArguments[0]);
				}
				else
				{
					AddClassUsage(typeReference2);
				}
				break;
			}
			case Code.Castclass:
			case Code.Isinst:
			case Code.Box:
			case Code.Constrained:
			{
				TypeReference genericType3 = (TypeReference)instruction.Operand;
				AddClassUsage(genericType3);
				break;
			}
			case Code.Unbox_Any:
			{
				TypeReference typeReference = (TypeReference)instruction.Operand;
				AddClassUsage(typeReference);
				if (_context.Global.Parameters.CanShareEnumTypes)
				{
					AddClassUsageForFirstGenericArgumentOfGenericInstanceType(typeReference);
				}
				break;
			}
			case Code.Sizeof:
			{
				TypeReference genericType2 = (TypeReference)instruction.Operand;
				AddClassUsage(genericType2);
				break;
			}
			case Code.Ldtoken:
				if (instruction.Operand is TypeReference genericType)
				{
					AddTypeUsage(genericType);
				}
				if (instruction.Operand is MethodReference genericMethod)
				{
					AddMethodUsage(genericMethod);
				}
				if (instruction.Operand is FieldReference fieldReference)
				{
					AddClassUsage(fieldReference.DeclaringType);
				}
				break;
			case Code.Ldftn:
			{
				MethodReference genericMethod2 = (MethodReference)instruction.Operand;
				AddMethodUsage(genericMethod2);
				break;
			}
			case Code.Ldvirtftn:
			{
				MethodReference methodReference2 = (MethodReference)instruction.Operand;
				AddMethodUsage(methodReference2);
				if (methodReference2.DeclaringType.IsInterface())
				{
					AddClassUsage(methodReference2.DeclaringType);
				}
				break;
			}
			case Code.Call:
			case Code.Callvirt:
			{
				MethodReference methodReference = (MethodReference)instruction.Operand;
				if (!ArrayNaming.IsSpecialArrayMethod(methodReference) && (!methodReference.DeclaringType.IsSystemArray() || (!(methodReference.Name == "GetGenericValueImpl") && !(methodReference.Name == "SetGenericValueImpl"))))
				{
					if (instruction.OpCode.Code == Code.Callvirt && methodReference.DeclaringType.IsInterface() && !methodReference.IsGenericInstance)
					{
						AddClassUsage(methodReference.DeclaringType);
						if (instruction.Previous != null && instruction.Previous.OpCode.Code == Code.Constrained)
						{
							AddMethodUsage(methodReference);
						}
					}
					else
					{
						AddMethodUsage(methodReference);
					}
				}
				if (GenericSharingAnalysis.ShouldTryToCallStaticConstructorBeforeMethodCall(_context, methodReference, method))
				{
					AddStaticUsage(_context, methodReference.DeclaringType);
				}
				break;
			}
			default:
				if (instruction.Operand is MemberReference)
				{
					throw new NotImplementedException();
				}
				break;
			case Code.Cpobj:
			case Code.Ldobj:
			case Code.Ldfld:
			case Code.Ldflda:
			case Code.Stfld:
			case Code.Stobj:
			case Code.Ldelema:
			case Code.Ldelem_Any:
			case Code.Stelem_Any:
			case Code.Mkrefany:
			case Code.Initobj:
				break;
			}
		}

		private void AddClassUsageForFirstGenericArgumentOfGenericInstanceType(TypeReference type)
		{
			if (type is GenericInstanceType genericInstanceType && genericInstanceType.IsNullable())
			{
				AddClassUsage(genericInstanceType.GenericArguments[0]);
			}
		}

		public void AddStaticUsage(ReadOnlyContext context, TypeReference genericType)
		{
			AddStaticUsageRecursiveIfNeeded(context, genericType);
		}

		private void AddStaticUsageRecursiveIfNeeded(ReadOnlyContext context, TypeReference genericType)
		{
			if (genericType.IsGenericInstance)
			{
				AddData(RuntimeGenericContextInfo.Static, genericType);
			}
			TypeReference baseType = genericType.GetBaseType(context);
			if (baseType != null)
			{
				AddStaticUsageRecursiveIfNeeded(context, baseType);
			}
		}

		public void AddClassUsage(TypeReference genericType)
		{
			AddData(RuntimeGenericContextInfo.Class, genericType);
		}

		public void AddArrayUsage(TypeReference genericType)
		{
			AddData(RuntimeGenericContextInfo.Array, genericType);
		}

		public void AddTypeUsage(TypeReference genericType)
		{
			AddData(RuntimeGenericContextInfo.Type, genericType);
		}

		public void AddData(RuntimeGenericContextInfo infoType, TypeReference genericType)
		{
			RuntimeGenericTypeData data = new RuntimeGenericTypeData(infoType, genericType);
			List<RuntimeGenericData> list;
			switch (GenericUsageFor(data.GenericType))
			{
			case GenericContextUsage.Type:
				list = _typeList;
				break;
			case GenericContextUsage.Method:
				list = _methodList;
				break;
			case GenericContextUsage.Both:
				list = _methodList;
				break;
			case GenericContextUsage.None:
				return;
			default:
				throw new NotSupportedException("Invalid generic parameter usage");
			}
			if (list.FindIndex((RuntimeGenericData d) => d.InfoType == data.InfoType && TypeReferenceEqualityComparer.AreEqual(((RuntimeGenericTypeData)d).GenericType, data.GenericType)) == -1)
			{
				list.Add(data);
			}
		}

		public void AddMethodUsage(MethodReference genericMethod)
		{
			List<RuntimeGenericData> list;
			switch (GenericUsageFor(genericMethod))
			{
			case GenericContextUsage.Type:
				list = _typeList;
				break;
			case GenericContextUsage.Method:
				list = _methodList;
				break;
			case GenericContextUsage.Both:
				list = _methodList;
				break;
			case GenericContextUsage.None:
				return;
			default:
				throw new NotSupportedException("Invalid generic parameter usage");
			}
			RuntimeGenericMethodData data = new RuntimeGenericMethodData(RuntimeGenericContextInfo.Method, genericMethod);
			if (list.FindIndex((RuntimeGenericData d) => d.InfoType == data.InfoType && new MethodReferenceComparer().Equals(((RuntimeGenericMethodData)d).GenericMethod, data.GenericMethod)) == -1)
			{
				list.Add(data);
			}
		}

		internal static GenericContextUsage GenericUsageFor(MethodReference method)
		{
			GenericContextUsage genericContextUsage = GenericUsageFor(method.DeclaringType);
			if (method is GenericInstanceMethod genericInstanceMethod)
			{
				{
					foreach (TypeReference genericArgument in genericInstanceMethod.GenericArguments)
					{
						genericContextUsage |= GenericUsageFor(genericArgument);
					}
					return genericContextUsage;
				}
			}
			return genericContextUsage;
		}

		internal static GenericContextUsage GenericUsageFor(TypeReference type)
		{
			if (type is GenericParameter genericParameter)
			{
				if (genericParameter.Type != 0)
				{
					return GenericContextUsage.Method;
				}
				return GenericContextUsage.Type;
			}
			if (type is ArrayType arrayType)
			{
				return GenericUsageFor(arrayType.ElementType);
			}
			if (type is GenericInstanceType genericInstanceType)
			{
				GenericContextUsage genericContextUsage = GenericContextUsage.None;
				{
					foreach (TypeReference genericArgument in genericInstanceType.GenericArguments)
					{
						genericContextUsage |= GenericUsageFor(genericArgument);
					}
					return genericContextUsage;
				}
			}
			if (type is ByReferenceType byReferenceType)
			{
				return GenericUsageFor(byReferenceType.ElementType);
			}
			if (type is PointerType)
			{
				return GenericContextUsage.None;
			}
			if (type is PinnedType pinnedType)
			{
				return GenericUsageFor(pinnedType.ElementType);
			}
			if (type is TypeSpecification)
			{
				throw new NotSupportedException("TypeSpecification found which is not supported");
			}
			return GenericContextUsage.None;
		}
	}
}
