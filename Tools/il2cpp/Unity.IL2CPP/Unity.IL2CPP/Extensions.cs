using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Mono.Collections.Generic;
using NiceIO;
using Unity.Cecil.Awesome;
using Unity.Cecil.Awesome.Comparers;
using Unity.Cecil.Awesome.Ordering;
using Unity.IL2CPP.AssemblyConversion;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Results;
using Unity.IL2CPP.Metadata;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP
{
	public static class Extensions
	{
		private class MemberOrdingComparerBy<T, K> : IComparer<T> where K : MemberReference
		{
			private readonly Func<T, K> _selector;

			public MemberOrdingComparerBy(Func<T, K> selector)
			{
				_selector = selector;
			}

			public int Compare(T x, T y)
			{
				return string.Compare(_selector(x).FullName, _selector(y).FullName, StringComparison.Ordinal);
			}
		}

		private class OrderingComparerBy<T, K> : IComparer<T> where K : IComparable
		{
			private readonly Func<T, K> _selector;

			public OrderingComparerBy(Func<T, K> selector)
			{
				_selector = selector;
			}

			public int Compare(T x, T y)
			{
				return _selector(x).CompareTo(_selector(y));
			}
		}

		private class DictionaryValueOrderingComparer<TKey> : IComparer<KeyValuePair<TKey, int>>, IComparer<KeyValuePair<TKey, uint>>
		{
			public int Compare(KeyValuePair<TKey, int> x, KeyValuePair<TKey, int> y)
			{
				return x.Value.CompareTo(y.Value);
			}

			public int Compare(KeyValuePair<TKey, uint> x, KeyValuePair<TKey, uint> y)
			{
				return x.Value.CompareTo(y.Value);
			}
		}

		private class DictionaryValueOrderingComparer<TKey, TMetadataIndex> : IComparer<KeyValuePair<TKey, TMetadataIndex>> where TMetadataIndex : MetadataIndex
		{
			public int Compare(KeyValuePair<TKey, TMetadataIndex> x, KeyValuePair<TKey, TMetadataIndex> y)
			{
				int index = x.Value.Index;
				return index.CompareTo(y.Value.Index);
			}
		}

		private class DictionaryKeyOrderingComparer<TValue> : IComparer<KeyValuePair<string, TValue>>
		{
			public int Compare(KeyValuePair<string, TValue> x, KeyValuePair<string, TValue> y)
			{
				return string.Compare(x.Key, y.Key, StringComparison.Ordinal);
			}
		}

		private class MemberReferenceDictionaryKeyOrderingComparer<TKey, TValue> : IComparer<KeyValuePair<TKey, TValue>> where TKey : MemberReference
		{
			public int Compare(KeyValuePair<TKey, TValue> x, KeyValuePair<TKey, TValue> y)
			{
				return string.Compare(x.Key.FullName, y.Key.FullName, StringComparison.Ordinal);
			}
		}

		private class Il2CppTypeDataDictionaryKeyOrderingComparer<TValue> : IComparer<KeyValuePair<Il2CppTypeData, TValue>>
		{
			public int Compare(KeyValuePair<Il2CppTypeData, TValue> x, KeyValuePair<Il2CppTypeData, TValue> y)
			{
				int num = string.Compare(x.Key.Type.FullName, y.Key.Type.FullName, StringComparison.Ordinal);
				if (num == 0)
				{
					int attrs = x.Key.Attrs;
					return attrs.CompareTo(y.Key.Attrs);
				}
				return num;
			}
		}

		private class GenericMethodReferenceDictionaryKeyOrderingComparer<TValue> : IComparer<KeyValuePair<Il2CppMethodSpec, TValue>>
		{
			public int Compare(KeyValuePair<Il2CppMethodSpec, TValue> x, KeyValuePair<Il2CppMethodSpec, TValue> y)
			{
				return x.Key.GenericMethod.Compare(y.Key.GenericMethod);
			}
		}

		private class ToStringDictionaryKeyOrderingComparer<TKey, TValue> : IComparer<KeyValuePair<TKey, TValue>> where TKey : class
		{
			public int Compare(KeyValuePair<TKey, TValue> x, KeyValuePair<TKey, TValue> y)
			{
				return string.Compare(x.Key.ToString(), y.Key.ToString(), StringComparison.Ordinal);
			}
		}

		private class OrderingComparer : IComparer<string>, IComparer<FieldReference>, IComparer<NPath>, IComparer<Il2CppMethodSpec>, IComparer<Il2CppRuntimeFieldReference>
		{
			public int Compare(string x, string y)
			{
				return string.Compare(x, y, StringComparison.Ordinal);
			}

			public int Compare(FieldReference x, FieldReference y)
			{
				return x.Compare(y);
			}

			public int Compare(Il2CppRuntimeFieldReference x, Il2CppRuntimeFieldReference y)
			{
				return Compare(x.Field, y.Field);
			}

			public int Compare(Il2CppMethodSpec x, Il2CppMethodSpec y)
			{
				return x.GenericMethod.Compare(y.GenericMethod);
			}

			public int Compare(NPath x, NPath y)
			{
				return string.Compare(x.ToString(SlashMode.Forward), y.ToString(SlashMode.Forward), StringComparison.Ordinal);
			}
		}

		public static bool HasFinalizer(this TypeDefinition type)
		{
			if (type.IsInterface)
			{
				return false;
			}
			if (type.MetadataType == MetadataType.Object)
			{
				return false;
			}
			if (type.BaseType == null)
			{
				return false;
			}
			if (!type.BaseType.Resolve().HasFinalizer())
			{
				return type.Methods.SingleOrDefault(IsFinalizerMethod) != null;
			}
			return true;
		}

		public static bool ShouldNotInline(this MethodReference method)
		{
			MethodDefinition methodDefinition = method.Resolve();
			if (methodDefinition == null)
			{
				return false;
			}
			return methodDefinition.ImplAttributes.HasFlag(MethodImplAttributes.NoInlining);
		}

		public static bool ShouldAgressiveInline(this MethodReference method)
		{
			MethodDefinition methodDefinition = method.Resolve();
			if (methodDefinition == null)
			{
				return false;
			}
			return methodDefinition.ImplAttributes.HasFlag(MethodImplAttributes.AggressiveInlining);
		}

		private static bool IsCheapGetterSetter(MethodDefinition method)
		{
			Mono.Collections.Generic.Collection<Instruction> instructions = method.Body.Instructions;
			if (instructions.Count > 4)
			{
				return false;
			}
			Instruction instruction = null;
			if (instructions.Count == 2 && instructions[0].OpCode == OpCodes.Ldsfld && instructions[1].OpCode == OpCodes.Ret)
			{
				instruction = instructions[0];
			}
			else if (instructions.Count == 3 && instructions[0].OpCode == OpCodes.Ldarg_0 && instructions[1].OpCode == OpCodes.Stsfld && instructions[2].OpCode == OpCodes.Ret)
			{
				instruction = instructions[1];
			}
			else if (instructions.Count == 3 && instructions[0].OpCode == OpCodes.Ldarg_0 && instructions[1].OpCode == OpCodes.Ldfld && instructions[2].OpCode == OpCodes.Ret)
			{
				instruction = instructions[1];
			}
			else if (instructions.Count == 4 && instructions[0].OpCode == OpCodes.Ldarg_0 && instructions[1].OpCode == OpCodes.Ldarg_1 && instructions[2].OpCode == OpCodes.Stfld && instructions[3].OpCode == OpCodes.Ret)
			{
				instruction = instructions[2];
			}
			if (instruction == null)
			{
				return false;
			}
			if (((FieldReference)instruction.Operand).Resolve() == null)
			{
				return false;
			}
			return true;
		}

		private static bool IsSmallWithoutCalls(MethodDefinition method)
		{
			if (method.Body.Instructions.Count <= 20)
			{
				return !method.Body.Instructions.Any((Instruction ins) => ins.OpCode.Code == Code.Call || ins.OpCode.Code == Code.Calli || ins.OpCode.Code == Code.Callvirt);
			}
			return false;
		}

		public static bool IsCheapToInline(this MethodReference method)
		{
			MethodDefinition methodDefinition = method.Resolve();
			if (methodDefinition == null)
			{
				return false;
			}
			if (!methodDefinition.HasBody)
			{
				return false;
			}
			return IsCheapGetterSetter(methodDefinition);
		}

		public static bool ShouldInline(this MethodReference method, AssemblyConversionParameters parameters)
		{
			if (parameters.EnableInlining && !method.ShouldNotInline())
			{
				if (!method.ShouldAgressiveInline())
				{
					return method.IsCheapToInline();
				}
				return true;
			}
			return false;
		}

		public static bool ShouldNotOptimize(this MethodReference method)
		{
			MethodDefinition methodDefinition = method.Resolve();
			if (methodDefinition == null)
			{
				return false;
			}
			return methodDefinition.ImplAttributes.HasFlag(MethodImplAttributes.NoOptimization);
		}

		public static TypeReference GetBaseType(this TypeReference typeReference, ReadOnlyContext context)
		{
			if (typeReference is TypeSpecification)
			{
				if (typeReference.IsArray)
				{
					return context.Global.Services.TypeProvider.SystemArray ?? context.Global.Services.TypeProvider.SystemObject;
				}
				if (typeReference.IsGenericParameter || typeReference.IsByReference || typeReference.IsPointer)
				{
					return null;
				}
				if (typeReference is SentinelType sentinelType)
				{
					return sentinelType.ElementType.GetBaseType(context);
				}
				if (typeReference is PinnedType pinnedType)
				{
					return pinnedType.ElementType.GetBaseType(context);
				}
				if (typeReference is RequiredModifierType requiredModifierType)
				{
					return requiredModifierType.ElementType.GetBaseType(context);
				}
				if (typeReference is OptionalModifierType optionalModifierType)
				{
					return optionalModifierType.ElementType.GetBaseType(context);
				}
			}
			return TypeResolver.For(typeReference).Resolve(typeReference.Resolve().BaseType);
		}

		public static IEnumerable<TypeDefinition> GetTypeHierarchy(this TypeDefinition type)
		{
			while (type != null)
			{
				yield return type;
				type = ((type.BaseType != null) ? type.BaseType.Resolve() : null);
			}
		}

		public static System.Collections.ObjectModel.ReadOnlyCollection<TypeReference> GetInterfaces(this TypeReference type, ReadOnlyContext context)
		{
			HashSet<TypeReference> hashSet = new HashSet<TypeReference>(new TypeReferenceEqualityComparer());
			AddInterfacesRecursive(context, type.Resolve(), type, hashSet);
			return hashSet.ToList().AsReadOnly();
		}

		private static void AddInterfacesRecursive(ReadOnlyContext context, TypeDefinition concreteType, TypeReference type, HashSet<TypeReference> interfaces)
		{
			if (type.IsArray)
			{
				return;
			}
			TypeResolver typeResolver = TypeResolver.For(type);
			foreach (InterfaceImplementation @interface in type.Resolve().Interfaces)
			{
				TypeReference typeReference = typeResolver.Resolve(@interface.InterfaceType);
				if (!concreteType.IsWindowsRuntime)
				{
					typeReference = context.Global.Services.WindowsRuntime.ProjectToCLR(typeReference);
				}
				if (interfaces.Add(typeReference))
				{
					AddInterfacesRecursive(context, concreteType, typeReference, interfaces);
				}
			}
		}

		public static TypeReference GetNonPinnedAndNonByReferenceType(this TypeReference type)
		{
			type = type.WithoutModifiers();
			TypeReference result = type;
			if (type is ByReferenceType byReferenceType)
			{
				result = byReferenceType.ElementType;
			}
			if (type is PinnedType pinnedType)
			{
				result = pinnedType.ElementType;
			}
			return result;
		}

		public static bool IsSuitableForStaticFieldInTinyProfile(this TypeReference typeReference)
		{
			typeReference = typeReference.WithoutModifiers();
			switch (typeReference.MetadataType)
			{
			case MetadataType.Boolean:
			case MetadataType.Char:
			case MetadataType.SByte:
			case MetadataType.Byte:
			case MetadataType.Int16:
			case MetadataType.UInt16:
			case MetadataType.Int32:
			case MetadataType.UInt32:
			case MetadataType.Int64:
			case MetadataType.UInt64:
			case MetadataType.Single:
			case MetadataType.Double:
			case MetadataType.Pointer:
			case MetadataType.IntPtr:
			case MetadataType.UIntPtr:
				return true;
			default:
				return typeReference.IsSystemType();
			case MetadataType.ValueType:
			case MetadataType.GenericInstance:
			{
				if (!typeReference.IsValueType())
				{
					return false;
				}
				TypeResolver typeResolver = TypeResolver.For(typeReference);
				foreach (FieldDefinition field in typeReference.Resolve().Fields)
				{
					if (!field.IsStatic && !typeResolver.Resolve(field.FieldType).IsSuitableForStaticFieldInTinyProfile())
					{
						return false;
					}
				}
				return true;
			}
			}
		}

		public static TypeReference GetUnderlyingEnumType(this TypeReference type)
		{
			return (type.Resolve() ?? throw new Exception("Failed to resolve type reference")).GetEnumUnderlyingType();
		}

		public static bool IsInterface(this TypeReference type)
		{
			if (type.IsArray)
			{
				return false;
			}
			if (type.IsGenericParameter)
			{
				return false;
			}
			return type.Resolve()?.IsInterface ?? false;
		}

		public static bool IsAbstract(this TypeReference type)
		{
			if (type.IsArray)
			{
				return false;
			}
			if (type.IsGenericParameter)
			{
				return false;
			}
			return type.Resolve()?.IsAbstract ?? false;
		}

		public static bool IsComInterface(this TypeReference type)
		{
			if (type.IsArray)
			{
				return false;
			}
			if (type.IsGenericParameter)
			{
				return false;
			}
			TypeDefinition typeDefinition = type.Resolve();
			if (typeDefinition != null && typeDefinition.IsInterface && typeDefinition.IsImport)
			{
				return !typeDefinition.IsWindowsRuntimeProjection();
			}
			return false;
		}

		public static bool IsWindowsRuntimePrimitiveType(this TypeReference type)
		{
			switch (type.MetadataType)
			{
			case MetadataType.Boolean:
			case MetadataType.Char:
			case MetadataType.Byte:
			case MetadataType.Int16:
			case MetadataType.UInt16:
			case MetadataType.Int32:
			case MetadataType.UInt32:
			case MetadataType.Int64:
			case MetadataType.UInt64:
			case MetadataType.Single:
			case MetadataType.Double:
			case MetadataType.String:
			case MetadataType.Object:
				return true;
			case MetadataType.ValueType:
				return type.FullName == "System.Guid";
			default:
				return false;
			}
		}

		public static string GetWindowsRuntimePrimitiveName(this TypeReference type)
		{
			switch (type.MetadataType)
			{
			case MetadataType.Boolean:
				return "Boolean";
			case MetadataType.Char:
				return "Char16";
			case MetadataType.Byte:
				return "UInt8";
			case MetadataType.Int16:
				return "Int16";
			case MetadataType.UInt16:
				return "UInt16";
			case MetadataType.Int32:
				return "Int32";
			case MetadataType.UInt32:
				return "UInt32";
			case MetadataType.Int64:
				return "Int64";
			case MetadataType.UInt64:
				return "UInt64";
			case MetadataType.Single:
				return "Single";
			case MetadataType.Double:
				return "Double";
			case MetadataType.String:
				return "String";
			case MetadataType.Object:
				return "Object";
			case MetadataType.ValueType:
				if (type.FullName == "System.Guid")
				{
					return "Guid";
				}
				break;
			}
			return null;
		}

		public static string GetWindowsRuntimeTypeName(this TypeReference type, ReadOnlyContext context)
		{
			string windowsRuntimePrimitiveName = type.GetWindowsRuntimePrimitiveName();
			if (windowsRuntimePrimitiveName != null)
			{
				return windowsRuntimePrimitiveName;
			}
			TypeReference typeReference = context.Global.Services.WindowsRuntime.ProjectToWindowsRuntime(type);
			if (typeReference is GenericInstanceType genericInstanceType)
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.Append(genericInstanceType.Namespace);
				stringBuilder.Append('.');
				stringBuilder.Append(genericInstanceType.Name);
				stringBuilder.Append('<');
				bool flag = false;
				foreach (TypeReference genericArgument in genericInstanceType.GenericArguments)
				{
					if (flag)
					{
						stringBuilder.Append(',');
					}
					flag = true;
					stringBuilder.Append(genericArgument.GetWindowsRuntimeTypeName(context));
				}
				stringBuilder.Append('>');
				return stringBuilder.ToString();
			}
			return typeReference.FullName;
		}

		private static bool AreGenericArgumentsValidForWindowsRuntimeType(ReadOnlyContext context, GenericInstanceType genericInstance)
		{
			foreach (TypeReference genericArgument in genericInstance.GenericArguments)
			{
				if (!genericArgument.IsValidForWindowsRuntimeType(context))
				{
					return false;
				}
			}
			return true;
		}

		public static bool IsValidForWindowsRuntimeType(this TypeReference type, ReadOnlyContext context)
		{
			if (type.IsWindowsRuntimePrimitiveType())
			{
				return true;
			}
			if (type.IsAttribute())
			{
				return false;
			}
			if (type.IsGenericInstance)
			{
				GenericInstanceType genericInstanceType = (GenericInstanceType)context.Global.Services.WindowsRuntime.ProjectToWindowsRuntime(type);
				if (!IsComOrWindowsRuntimeType(context, genericInstanceType, (TypeDefinition typeDef) => typeDef.IsExposedToWindowsRuntime() && (typeDef.IsInterface || typeDef.IsDelegate())))
				{
					return false;
				}
				return AreGenericArgumentsValidForWindowsRuntimeType(context, genericInstanceType);
			}
			if (type.IsGenericParameter || type is TypeSpecification)
			{
				return false;
			}
			return context.Global.Services.WindowsRuntime.ProjectToWindowsRuntime(type.Resolve()).IsExposedToWindowsRuntime();
		}

		private static bool IsComOrWindowsRuntimeType(ReadOnlyContext context, TypeReference type, Func<TypeDefinition, bool> predicate)
		{
			if (type.IsArray)
			{
				return false;
			}
			if (type.IsGenericParameter)
			{
				return false;
			}
			TypeDefinition typeDefinition = type.Resolve();
			if (typeDefinition == null)
			{
				return false;
			}
			if (!predicate(typeDefinition))
			{
				return false;
			}
			if (type is GenericInstanceType genericInstance)
			{
				return AreGenericArgumentsValidForWindowsRuntimeType(context, genericInstance);
			}
			return true;
		}

		public static bool IsWindowsRuntimeDelegate(this TypeReference type, ReadOnlyContext context)
		{
			return IsComOrWindowsRuntimeType(context, type, (TypeDefinition typeDef) => typeDef.IsDelegate() && typeDef.IsExposedToWindowsRuntime());
		}

		public static bool IsComOrWindowsRuntimeInterface(this TypeReference type, ReadOnlyContext context)
		{
			return IsComOrWindowsRuntimeType(context, type, (TypeDefinition typeDef) => typeDef.IsInterface && typeDef.IsComOrWindowsRuntimeType(context));
		}

		public static bool IsNullable(this TypeReference type)
		{
			if (type.IsArray)
			{
				return false;
			}
			if (type.IsGenericParameter)
			{
				return false;
			}
			if (!(type is GenericInstanceType genericInstanceType))
			{
				return false;
			}
			return genericInstanceType.ElementType.FullName == "System.Nullable`1";
		}

		public static bool HasStaticConstructor(this TypeReference typeReference)
		{
			TypeDefinition typeDefinition = typeReference.Resolve();
			if (typeDefinition != null)
			{
				return typeDefinition.Methods.SingleOrDefault(IsStaticConstructor) != null;
			}
			return false;
		}

		public static bool IsGenericParameter(this TypeReference typeReference)
		{
			if (typeReference is ArrayType)
			{
				return false;
			}
			if (typeReference is PointerType)
			{
				return false;
			}
			if (typeReference is ByReferenceType)
			{
				return false;
			}
			return typeReference.GetElementType().IsGenericParameter;
		}

		public static bool IsSameType(this TypeReference a, TypeReference b)
		{
			return TypeReferenceEqualityComparer.AreEqual(a, b);
		}

		public static bool IsSystemArray(this TypeReference typeReference)
		{
			if (typeReference.Namespace == "System" && typeReference.Name == "Array" && typeReference.Resolve().Module.Name == "mscorlib.dll")
			{
				return true;
			}
			return false;
		}

		public static bool IsIl2CppComObject(this TypeReference typeReference, ReadOnlyContext context)
		{
			return TypeReferenceEqualityComparer.AreEqual(typeReference, context.Global.Services.TypeProvider.Il2CppComObjectTypeReference);
		}

		public static bool IsIl2CppComDelegate(this TypeReference typeReference, ReadOnlyContext context)
		{
			return TypeReferenceEqualityComparer.AreEqual(typeReference, context.Global.Services.TypeProvider.Il2CppComDelegateTypeReference);
		}

		public static bool IsIActivationFactory(this TypeReference typeReference, ReadOnlyContext context)
		{
			return TypeReferenceEqualityComparer.AreEqual(typeReference, context.Global.Services.TypeProvider.IActivationFactoryTypeReference);
		}

		public static bool IsIntegralPointerType(this TypeReference typeReference)
		{
			if (typeReference.MetadataType != MetadataType.IntPtr)
			{
				return typeReference.MetadataType == MetadataType.UIntPtr;
			}
			return true;
		}

		public static bool IsSystemObject(this TypeReference typeReference)
		{
			return typeReference.MetadataType == MetadataType.Object;
		}

		public static bool IsSystemType(this TypeReference typeReference)
		{
			if (typeReference.Namespace == "System" && typeReference.Name == "Type" && typeReference.Resolve().Module.Name == "mscorlib.dll")
			{
				return true;
			}
			return false;
		}

		public static bool IsSpecialSystemBaseType(this TypeReference typeReference)
		{
			if (typeReference.Namespace == "System")
			{
				if (!(typeReference.Name == "Object") && !(typeReference.Name == "ValueType"))
				{
					return typeReference.Name == "Enum";
				}
				return true;
			}
			return false;
		}

		public static bool IsFinalizerMethod(this MethodDefinition method)
		{
			if (method.Name == "Finalize" && method.ReturnType.MetadataType == MetadataType.Void && !method.HasParameters)
			{
				return (method.Attributes & MethodAttributes.Family) != 0;
			}
			return false;
		}

		public static bool ContainsGenericArgumentsProjectableToClr(this GenericInstanceType type, ReadOnlyContext context)
		{
			foreach (TypeReference genericArgument in type.GenericArguments)
			{
				if (context.Global.Services.WindowsRuntime.ProjectToCLR(genericArgument) != genericArgument)
				{
					return true;
				}
				if (genericArgument is GenericInstanceType type2 && type2.ContainsGenericArgumentsProjectableToClr(context))
				{
					return false;
				}
			}
			return false;
		}

		public static bool NeedsWindowsRuntimeFactory(this TypeDefinition type)
		{
			if (type.Module.MetadataKind != MetadataKind.ManagedWindowsMetadata)
			{
				return false;
			}
			if (!type.IsPublic)
			{
				return false;
			}
			if (type.HasGenericParameters)
			{
				return false;
			}
			if (type.IsInterface || type.IsValueType || type.IsAttribute() || type.IsDelegate())
			{
				return false;
			}
			foreach (CustomAttribute customAttribute in type.CustomAttributes)
			{
				TypeReference attributeType = customAttribute.AttributeType;
				if (!(attributeType.Namespace != "Windows.Foundation.Metadata") && (attributeType.Name == "StaticAttribute" || attributeType.Name == "ActivatableAttribute"))
				{
					return true;
				}
			}
			foreach (MethodDefinition method in type.Methods)
			{
				if (method.IsConstructor && method.IsPublic && method.Parameters.Count == 0)
				{
					return true;
				}
			}
			return false;
		}

		public static bool NeedsComCallableWrapper(this TypeReference type, ReadOnlyContext context)
		{
			if (type.IsArray)
			{
				return type.GetInterfacesImplementedByComCallableWrapper(context).Any();
			}
			TypeDefinition typeDefinition = type.Resolve();
			if (typeDefinition.CanBoxToWindowsRuntime(context))
			{
				return true;
			}
			if (typeDefinition.IsInterface || typeDefinition.IsComOrWindowsRuntimeType(context) || typeDefinition.IsAbstract || typeDefinition.IsImport)
			{
				return false;
			}
			if (type is GenericInstanceType type2 && type2.ContainsGenericArgumentsProjectableToClr(context))
			{
				return false;
			}
			if (!typeDefinition.IsValueType && context.Global.Services.WindowsRuntime.ProjectToWindowsRuntime(typeDefinition) != typeDefinition && !typeDefinition.IsAttribute() && !typeDefinition.IsDelegate())
			{
				return true;
			}
			if (type.GetInterfacesImplementedByComCallableWrapper(context).Any())
			{
				return true;
			}
			while (typeDefinition.BaseType != null)
			{
				typeDefinition = typeDefinition.BaseType.Resolve();
				if (typeDefinition.IsComOrWindowsRuntimeType(context))
				{
					return true;
				}
			}
			return false;
		}

		public static IEnumerable<TypeReference> ImplementedComOrWindowsRuntimeInterfaces(this TypeReference type, ReadOnlyContext context)
		{
			List<TypeReference> list = new List<TypeReference>();
			TypeResolver typeResolver = TypeResolver.For(type);
			foreach (InterfaceImplementation @interface in type.Resolve().Interfaces)
			{
				TypeReference typeReference = typeResolver.Resolve(@interface.InterfaceType);
				if (typeReference.IsComOrWindowsRuntimeInterface(context))
				{
					list.Add(typeReference);
				}
			}
			return list;
		}

		public static IEnumerable<TypeReference> GetInterfacesImplementedByComCallableWrapper(this TypeReference type, ReadOnlyContext context)
		{
			if (type.IsNullable())
			{
				return Enumerable.Empty<TypeReference>();
			}
			HashSet<TypeReference> hashSet = new HashSet<TypeReference>(new TypeReferenceEqualityComparer());
			foreach (TypeReference item in GetAllValidComOrWindowsRuntimeTypesAssignableFrom(context, type, 0))
			{
				TypeReference typeReference = context.Global.Services.WindowsRuntime.ProjectToWindowsRuntime(item);
				if (typeReference.IsComOrWindowsRuntimeInterface(context))
				{
					hashSet.Add(typeReference);
				}
			}
			return hashSet;
		}

		private static void CollectComOrWindowsRuntimeTypesCovariantlyAssignableFrom(ReadOnlyContext context, GenericInstanceType type, HashSet<TypeReference> collectedTypes, int genericDepth)
		{
			TypeDefinition typeDefinition = type.Resolve();
			if (!typeDefinition.IsExposedToWindowsRuntime() && context.Global.Services.WindowsRuntime.ProjectToWindowsRuntime(typeDefinition) == typeDefinition)
			{
				return;
			}
			if (genericDepth > 1)
			{
				collectedTypes.Add(type);
				return;
			}
			TypeReference[][] array = new TypeReference[type.GenericArguments.Count][];
			for (int i = 0; i < array.Length; i++)
			{
				TypeReference typeReference = type.GenericArguments[i];
				GenericParameter genericParameter = typeDefinition.GenericParameters[i];
				GenericParameterAttributes genericParameterAttributes = genericParameter.Attributes & GenericParameterAttributes.VarianceMask;
				switch (genericParameterAttributes)
				{
				case GenericParameterAttributes.NonVariant:
					array[i] = ((!typeReference.IsValidForWindowsRuntimeType(context)) ? new TypeReference[0] : new TypeReference[1] { typeReference });
					break;
				case GenericParameterAttributes.Covariant:
					array[i] = (from t in GetAllValidComOrWindowsRuntimeTypesAssignableFrom(context, typeReference, genericDepth + 1)
						where t.IsValidForWindowsRuntimeType(context)
						select t).ToArray();
					break;
				case GenericParameterAttributes.Contravariant:
					throw new NotSupportedException("'" + type.FullName + "' type contains unsupported contravariant generic parameter '" + genericParameter.Name + "'.");
				default:
					throw new Exception($"'{genericParameter.Name}' generic parameter in '{type.FullName}' type contains invalid variance value '{genericParameterAttributes}'.");
				}
				if (array[i].Length == 0)
				{
					return;
				}
			}
			foreach (TypeReference[] typeCombination in GetTypeCombinations(array))
			{
				GenericInstanceType genericInstanceType = new GenericInstanceType(typeDefinition);
				TypeReference[] array2 = typeCombination;
				foreach (TypeReference item in array2)
				{
					genericInstanceType.GenericArguments.Add(item);
				}
				collectedTypes.Add(genericInstanceType);
			}
		}

		private static IEnumerable<TypeReference> GetAllValidComOrWindowsRuntimeTypesAssignableFrom(ReadOnlyContext context, TypeReference type, int genericDepth)
		{
			HashSet<TypeReference> hashSet = new HashSet<TypeReference>(new TypeReferenceEqualityComparer());
			CollectAllValidComOrWindowsRuntimeTypesAssignableFrom(context, type, hashSet, genericDepth);
			return hashSet;
		}

		private static void CollectAllValidComOrWindowsRuntimeTypesAssignableFrom(ReadOnlyContext context, TypeReference type, HashSet<TypeReference> collectedTypes, int genericDepth)
		{
			if (!collectedTypes.Add(type) || type.IsSystemObject())
			{
				return;
			}
			if (type.IsArray)
			{
				ArrayType arrayType = (ArrayType)type;
				if (arrayType.IsVector)
				{
					TypeDefinition[] array = new TypeDefinition[5]
					{
						context.Global.Services.TypeProvider.Corlib.MainModule.GetType("System.Collections.Generic.IList`1"),
						context.Global.Services.TypeProvider.Corlib.MainModule.GetType("System.Collections.Generic.ICollection`1"),
						context.Global.Services.TypeProvider.Corlib.MainModule.GetType("System.Collections.Generic.IEnumerable`1"),
						context.Global.Services.TypeProvider.Corlib.MainModule.GetType("System.Collections.Generic.IReadOnlyList`1"),
						context.Global.Services.TypeProvider.Corlib.MainModule.GetType("System.Collections.Generic.IReadOnlyCollection`1")
					};
					foreach (TypeDefinition typeDefinition in array)
					{
						if (typeDefinition != null)
						{
							GenericInstanceType genericInstanceType = new GenericInstanceType(typeDefinition);
							genericInstanceType.GenericArguments.Add(arrayType.ElementType);
							CollectAllValidComOrWindowsRuntimeTypesAssignableFrom(context, genericInstanceType, collectedTypes, genericDepth);
						}
					}
				}
				TypeDefinition systemArray = context.Global.Services.TypeProvider.SystemArray;
				if (systemArray != null)
				{
					CollectAllValidComOrWindowsRuntimeTypesAssignableFrom(context, systemArray, collectedTypes, genericDepth);
				}
				return;
			}
			if (!type.IsValueType())
			{
				if (type.IsGenericInstance)
				{
					CollectComOrWindowsRuntimeTypesCovariantlyAssignableFrom(context, (GenericInstanceType)type, collectedTypes, genericDepth);
				}
				TypeReference baseType = type.GetBaseType(context);
				if (baseType != null)
				{
					CollectAllValidComOrWindowsRuntimeTypesAssignableFrom(context, baseType, collectedTypes, baseType.IsGenericInstance ? (genericDepth + 1) : genericDepth);
				}
			}
			foreach (TypeReference @interface in type.GetInterfaces(context))
			{
				CollectAllValidComOrWindowsRuntimeTypesAssignableFrom(context, @interface, collectedTypes, genericDepth);
			}
		}

		private static IEnumerable<TypeReference[]> GetTypeCombinations(TypeReference[][] types, int level = 0)
		{
			TypeReference[] levelTypes = types[level];
			if (level + 1 == types.Length)
			{
				TypeReference[] array = levelTypes;
				foreach (TypeReference typeReference in array)
				{
					TypeReference[] array2 = new TypeReference[types.Length];
					array2[types.Length - 1] = typeReference;
					yield return array2;
				}
				yield break;
			}
			IEnumerable<TypeReference[]> typeCombinations = GetTypeCombinations(types, level + 1);
			foreach (TypeReference[] item in typeCombinations)
			{
				TypeReference[] array3 = levelTypes;
				foreach (TypeReference typeReference2 in array3)
				{
					TypeReference[] array4 = (TypeReference[])item.Clone();
					array4[level] = typeReference2;
					yield return array4;
				}
			}
		}

		public static bool IsComOrWindowsRuntimeMethod(this MethodDefinition method, ReadOnlyContext context)
		{
			TypeDefinition declaringType = method.DeclaringType;
			if (declaringType.IsWindowsRuntime)
			{
				return true;
			}
			if (declaringType.IsIl2CppComObject(context) || declaringType.IsIl2CppComDelegate(context))
			{
				return true;
			}
			if (!declaringType.IsImport)
			{
				return false;
			}
			if (!method.IsInternalCall && !method.IsFinalizerMethod())
			{
				return declaringType.IsInterface;
			}
			return true;
		}

		public static bool IsComOrWindowsRuntimeType(this TypeDefinition type, ReadOnlyContext context)
		{
			if (type.IsValueType)
			{
				return false;
			}
			if (type.IsDelegate())
			{
				return false;
			}
			if (type.IsIl2CppComObject(context) || type.IsIl2CppComDelegate(context))
			{
				return true;
			}
			if (type.IsImport)
			{
				if (type.IsWindowsRuntimeProjection)
				{
					return type.IsExposedToWindowsRuntime();
				}
				return true;
			}
			return type.IsWindowsRuntime;
		}

		public static bool IsStripped(this MethodReference method)
		{
			return method.Name.StartsWith("$__Stripped");
		}

		public static bool IsWindowsRuntimeProjection(this TypeDefinition type)
		{
			return type.IsWindowsRuntimeProjection;
		}

		public static bool IsExposedToWindowsRuntime(this TypeDefinition type)
		{
			if (type.IsWindowsRuntimeProjection)
			{
				ModuleDefinition module = type.Module;
				if (module != null && module.MetadataKind == MetadataKind.ManagedWindowsMetadata)
				{
					return type.IsPublic;
				}
			}
			return type.IsWindowsRuntime;
		}

		public static bool HasActivationFactories(this TypeReference type)
		{
			TypeDefinition typeDefinition = type.Resolve();
			if (!typeDefinition.IsWindowsRuntime || typeDefinition.IsValueType)
			{
				return false;
			}
			return typeDefinition.CustomAttributes.Any((CustomAttribute ca) => ca.AttributeType.FullName == "Windows.Foundation.Metadata.ActivatableAttribute" || ca.AttributeType.FullName == "Windows.Foundation.Metadata.StaticAttribute" || ca.AttributeType.FullName == "Windows.Foundation.Metadata.ComposableAttribute");
		}

		private static IEnumerable<TypeReference> GetTypesFromSpecificAttribute(this TypeDefinition type, string attributeName, Func<CustomAttribute, TypeReference> customAttributeSelector)
		{
			return type.CustomAttributes.Where((CustomAttribute ca) => ca.AttributeType.FullName == attributeName).Select(customAttributeSelector);
		}

		public static IEnumerable<TypeReference> GetStaticFactoryTypes(this TypeReference type)
		{
			TypeDefinition typeDefinition = type.Resolve();
			if (!typeDefinition.IsWindowsRuntime || typeDefinition.IsValueType)
			{
				return Enumerable.Empty<TypeReference>();
			}
			return typeDefinition.GetTypesFromSpecificAttribute("Windows.Foundation.Metadata.StaticAttribute", (CustomAttribute attribute) => (TypeReference)attribute.ConstructorArguments[0].Value);
		}

		public static IEnumerable<TypeReference> GetActivationFactoryTypes(this TypeReference type, ReadOnlyContext context)
		{
			TypeDefinition typeDefinition = type.Resolve();
			if (!typeDefinition.IsWindowsRuntime || typeDefinition.IsValueType)
			{
				return Enumerable.Empty<TypeReference>();
			}
			return typeDefinition.GetTypesFromSpecificAttribute("Windows.Foundation.Metadata.ActivatableAttribute", delegate(CustomAttribute attribute)
			{
				CustomAttributeArgument customAttributeArgument = attribute.ConstructorArguments[0];
				return customAttributeArgument.Type.IsSystemType() ? ((TypeReference)customAttributeArgument.Value) : context.Global.Services.TypeProvider.IActivationFactoryTypeReference;
			});
		}

		public static IEnumerable<TypeReference> GetComposableFactoryTypes(this TypeReference type)
		{
			TypeDefinition typeDefinition = type.Resolve();
			if (!typeDefinition.IsWindowsRuntime || typeDefinition.IsValueType)
			{
				return Enumerable.Empty<TypeReference>();
			}
			return typeDefinition.GetTypesFromSpecificAttribute("Windows.Foundation.Metadata.ComposableAttribute", (CustomAttribute attribute) => (TypeReference)attribute.ConstructorArguments[0].Value);
		}

		public static IEnumerable<TypeReference> GetAllFactoryTypes(this TypeReference type, ReadOnlyContext context)
		{
			TypeDefinition typeDefinition = type.Resolve();
			if (!typeDefinition.IsWindowsRuntime || typeDefinition.IsValueType)
			{
				return Enumerable.Empty<TypeReference>();
			}
			return typeDefinition.GetActivationFactoryTypes(context).Concat(typeDefinition.GetComposableFactoryTypes()).Concat(typeDefinition.GetStaticFactoryTypes())
				.Distinct(new TypeReferenceEqualityComparer());
		}

		public static TypeReference ExtractDefaultInterface(this TypeDefinition type)
		{
			if (!type.IsExposedToWindowsRuntime())
			{
				throw new ArgumentException($"Extracting default interface is only valid for Windows Runtime types. {type.FullName} is not a Windows Runtime type.");
			}
			foreach (InterfaceImplementation @interface in type.Interfaces)
			{
				foreach (CustomAttribute customAttribute in @interface.CustomAttributes)
				{
					if (customAttribute.AttributeType.FullName == "Windows.Foundation.Metadata.DefaultAttribute")
					{
						return @interface.InterfaceType;
					}
				}
			}
			throw new InvalidProgramException($"Windows Runtime class {type} has no default interface!");
		}

		public static bool CanBoxToWindowsRuntime(this TypeReference type, ReadOnlyContext context)
		{
			if (context.Global.Services.TypeProvider.IReferenceType == null)
			{
				return false;
			}
			if (type.MetadataType == MetadataType.Object)
			{
				return false;
			}
			if (type.IsWindowsRuntimePrimitiveType())
			{
				return true;
			}
			if (type is ArrayType arrayType)
			{
				if (context.Global.Services.TypeProvider.IReferenceArrayType == null)
				{
					return false;
				}
				if (!arrayType.IsVector)
				{
					return false;
				}
				if (arrayType.ElementType.IsArray)
				{
					return false;
				}
				if (!arrayType.ElementType.CanBoxToWindowsRuntime(context))
				{
					return arrayType.ElementType.MetadataType == MetadataType.Object;
				}
				return true;
			}
			TypeReference typeReference = context.Global.Services.WindowsRuntime.ProjectToWindowsRuntime(type);
			if (!typeReference.IsValueType())
			{
				return false;
			}
			if (typeReference == type)
			{
				return type.Resolve().IsExposedToWindowsRuntime();
			}
			return true;
		}

		public static bool StoresNonFieldsInStaticFields(this TypeReference type)
		{
			return type.HasActivationFactories();
		}

		public static bool IsStaticConstructor(this MethodReference methodReference)
		{
			MethodDefinition methodDefinition = methodReference.Resolve();
			if (methodDefinition == null)
			{
				return false;
			}
			if (methodDefinition.IsConstructor && methodDefinition.IsStatic)
			{
				return methodDefinition.Parameters.Count == 0;
			}
			return false;
		}

		public static bool IsStatic(this MethodReference methodReference)
		{
			return methodReference.Resolve()?.IsStatic ?? false;
		}

		public static MethodReference GetFactoryMethodForConstructor(this MethodReference constructor, IEnumerable<TypeReference> activationFactoryTypes, bool isComposing)
		{
			int num = (isComposing ? 2 : 0);
			foreach (TypeReference activationFactoryType in activationFactoryTypes)
			{
				foreach (MethodDefinition method in activationFactoryType.Resolve().Methods)
				{
					if (method.Parameters.Count - num != constructor.Parameters.Count)
					{
						continue;
					}
					bool flag = true;
					for (int i = 0; i < constructor.Parameters.Count; i++)
					{
						if (!TypeReferenceEqualityComparer.AreEqual(method.Parameters[i].ParameterType, constructor.Parameters[i].ParameterType))
						{
							flag = false;
							break;
						}
					}
					if (flag)
					{
						return method;
					}
				}
			}
			return null;
		}

		public static MethodReference GetOverriddenInterfaceMethod(this MethodReference overridingMethod, IEnumerable<TypeReference> candidateInterfaces)
		{
			MethodDefinition methodDefinition = overridingMethod.Resolve();
			if (methodDefinition.Overrides.Count > 0)
			{
				if (methodDefinition.Overrides.Count != 1)
				{
					throw new InvalidOperationException($"Cannot choose overridden method for '{overridingMethod.FullName}'");
				}
				return TypeResolver.For(overridingMethod.DeclaringType, overridingMethod).Resolve(methodDefinition.Overrides[0]);
			}
			return candidateInterfaces.SelectMany((TypeReference iface) => iface.GetMethods()).FirstOrDefault((MethodReference interfaceMethod) => overridingMethod.Name == interfaceMethod.Name && VirtualMethodResolution.MethodSignaturesMatchIgnoreStaticness(interfaceMethod, overridingMethod));
		}

		public static bool IsNormalStatic(this FieldReference field)
		{
			FieldDefinition fieldDefinition = field.Resolve();
			if (fieldDefinition.IsLiteral)
			{
				return false;
			}
			if (!fieldDefinition.IsStatic)
			{
				return false;
			}
			if (!fieldDefinition.HasCustomAttributes)
			{
				return true;
			}
			return fieldDefinition.CustomAttributes.All((CustomAttribute ca) => ca.AttributeType.Name != "ThreadStaticAttribute");
		}

		public static bool IsThreadStatic(this FieldReference field)
		{
			FieldDefinition fieldDefinition = field.Resolve();
			if (fieldDefinition.IsStatic && fieldDefinition.HasCustomAttributes)
			{
				return fieldDefinition.CustomAttributes.Any((CustomAttribute ca) => ca.AttributeType.Name == "ThreadStaticAttribute");
			}
			return false;
		}

		public static bool IsDelegate(this TypeReference typeReference)
		{
			TypeDefinition typeDefinition = typeReference.Resolve();
			if (typeDefinition != null && typeDefinition.BaseType != null)
			{
				return typeDefinition.BaseType.FullName == "System.MulticastDelegate";
			}
			return false;
		}

		public static bool DerivesFromObject(this TypeReference typeReference, ReadOnlyContext context)
		{
			TypeReference baseType = typeReference.GetBaseType(context);
			if (baseType == null)
			{
				return false;
			}
			return baseType.MetadataType == MetadataType.Object;
		}

		public static bool DerivesFrom(this TypeReference type, ReadOnlyContext context, TypeReference potentialBaseType, bool checkInterfaces = true)
		{
			while (type != null)
			{
				if (TypeReferenceEqualityComparer.AreEqual(type, potentialBaseType))
				{
					return true;
				}
				if (checkInterfaces)
				{
					foreach (TypeReference @interface in type.GetInterfaces(context))
					{
						if (TypeReferenceEqualityComparer.AreEqual(@interface, potentialBaseType))
						{
							return true;
						}
					}
				}
				type = type.GetBaseType(context);
			}
			return false;
		}

		public static bool IsVolatile(this FieldReference fieldReference)
		{
			if (fieldReference != null && fieldReference.FieldType.IsRequiredModifier && ((RequiredModifierType)fieldReference.FieldType).ModifierType.Name.Contains("IsVolatile"))
			{
				return true;
			}
			return false;
		}

		public static bool IsIntegralType(this TypeReference type)
		{
			if (!type.IsSignedIntegralType())
			{
				return type.IsUnsignedIntegralType();
			}
			return true;
		}

		public static bool IsSignedIntegralType(this TypeReference type)
		{
			if (type.MetadataType != MetadataType.SByte && type.MetadataType != MetadataType.Int16 && type.MetadataType != MetadataType.Int32)
			{
				return type.MetadataType == MetadataType.Int64;
			}
			return true;
		}

		public static bool IsUnsignedIntegralType(this TypeReference type)
		{
			if (type.MetadataType != MetadataType.Byte && type.MetadataType != MetadataType.UInt16 && type.MetadataType != MetadataType.UInt32)
			{
				return type.MetadataType == MetadataType.UInt64;
			}
			return true;
		}

		public static bool HasIID(this TypeReference type, ReadOnlyContext context)
		{
			if (type.IsComOrWindowsRuntimeInterface(context))
			{
				return !type.HasGenericParameters;
			}
			return false;
		}

		public static bool HasCLSID(this TypeReference type)
		{
			if (type is TypeSpecification || type is GenericParameter)
			{
				return false;
			}
			return type.Resolve().HasCLSID();
		}

		public static bool HasCLSID(this TypeDefinition type)
		{
			if (!type.IsInterface && !type.HasGenericParameters)
			{
				return type.CustomAttributes.Any((CustomAttribute a) => a.AttributeType.FullName == "System.Runtime.InteropServices.GuidAttribute");
			}
			return false;
		}

		public static Guid GetGuid(this TypeReference type, ReadOnlyContext context)
		{
			return context.Global.Services.GuidProvider.GuidFor(context, type);
		}

		public static string ToInitializer(this Guid guid)
		{
			byte[] array = guid.ToByteArray();
			uint num = BitConverter.ToUInt32(array, 0);
			ushort num2 = BitConverter.ToUInt16(array, 4);
			ushort num3 = BitConverter.ToUInt16(array, 6);
			return "{" + $" 0x{num:x}, 0x{num2:x}, 0x{num3:x}, 0x{array[8]:x}, 0x{array[9]:x}, 0x{array[10]:x}, 0x{array[11]:x}, 0x{array[12]:x}, 0x{array[13]:x}, 0x{array[14]:x}, 0x{array[15]:x} " + "}";
		}

		public static IEnumerable<CustomAttribute> GetConstructibleCustomAttributes(this ICustomAttributeProvider customAttributeProvider)
		{
			return customAttributeProvider.CustomAttributes.Where(delegate(CustomAttribute ca)
			{
				TypeDefinition typeDefinition = ca.AttributeType.Resolve();
				return typeDefinition != null && !typeDefinition.IsWindowsRuntime;
			});
		}

		public static bool IsPrimitiveType(this MetadataType type)
		{
			if (type - 2 <= MetadataType.UInt64)
			{
				return true;
			}
			return false;
		}

		public static bool IsPrimitiveCppType(this string typeName)
		{
			switch (typeName)
			{
			case "bool":
			case "char":
			case "wchar_t":
			case "size_t":
			case "int8_t":
			case "int16_t":
			case "int32_t":
			case "int64_t":
			case "uint8_t":
			case "uint16_t":
			case "uint32_t":
			case "uint64_t":
			case "double":
			case "float":
				return true;
			default:
				return false;
			}
		}

		public static bool IsCallInstruction(this Instruction instruction)
		{
			Code code = instruction.OpCode.Code;
			if ((uint)(code - 39) <= 1u || code == Code.Callvirt || code == Code.Newobj)
			{
				return true;
			}
			return false;
		}

		public static System.Collections.ObjectModel.ReadOnlyCollection<NPath> ToSortedCollection(this IEnumerable<NPath> set)
		{
			return set.ToSortedCollection(new OrderingComparer());
		}

		public static System.Collections.ObjectModel.ReadOnlyCollection<string> ToSortedCollection(this IEnumerable<string> set)
		{
			return set.ToSortedCollection(new OrderingComparer());
		}

		public static System.Collections.ObjectModel.ReadOnlyCollection<FieldReference> ToSortedCollection(this IEnumerable<FieldReference> set)
		{
			return set.ToSortedCollection(new OrderingComparer());
		}

		public static System.Collections.ObjectModel.ReadOnlyCollection<ArrayType> ToSortedCollection(this IEnumerable<ArrayType> set)
		{
			return set.ToSortedCollection(new TypeOrderingComparer());
		}

		public static System.Collections.ObjectModel.ReadOnlyCollection<Il2CppMethodSpec> ToSortedCollection(this IEnumerable<Il2CppMethodSpec> set)
		{
			return set.ToSortedCollection(new OrderingComparer());
		}

		public static System.Collections.ObjectModel.ReadOnlyCollection<Il2CppRuntimeFieldReference> ToSortedCollection(this IEnumerable<Il2CppRuntimeFieldReference> set)
		{
			return set.ToSortedCollection(new OrderingComparer());
		}

		public static System.Collections.ObjectModel.ReadOnlyCollection<StringMetadataToken> ToSortedCollection(this IEnumerable<StringMetadataToken> set)
		{
			return set.ToSortedCollection(new StringMetadataTokenComparer());
		}

		public static System.Collections.ObjectModel.ReadOnlyCollection<IIl2CppRuntimeType> ToSortedCollection(this IEnumerable<IIl2CppRuntimeType> set)
		{
			return set.ToSortedCollection(new Il2CppRuntimeTypeComparer());
		}

		public static System.Collections.ObjectModel.ReadOnlyCollection<IIl2CppRuntimeType[]> ToSortedCollection(this IEnumerable<IIl2CppRuntimeType[]> set)
		{
			return set.ToSortedCollection(new Il2CppRuntimeTypeArrayComparer());
		}

		public static System.Collections.ObjectModel.ReadOnlyCollection<T> ToSortedCollectionBy<T>(this IEnumerable<T> set, Func<T, int> selector)
		{
			return set.ToSortedCollection(new OrderingComparerBy<T, int>(selector));
		}

		public static System.Collections.ObjectModel.ReadOnlyCollection<T> ToSortedCollectionBy<T>(this IEnumerable<T> set, Func<T, string> selector)
		{
			return set.ToSortedCollection(new OrderingComparerBy<T, string>(selector));
		}

		public static System.Collections.ObjectModel.ReadOnlyCollection<T> ToSortedCollectionBy<T, K>(this IEnumerable<T> set, Func<T, K> selector) where K : MemberReference
		{
			return set.ToSortedCollection(new MemberOrdingComparerBy<T, K>(selector));
		}

		public static System.Collections.ObjectModel.ReadOnlyCollection<T> ToSortedCollection<T>(this IEnumerable<T> set, IComparer<T> comparer)
		{
			List<T> list = new List<T>(set);
			list.Sort(comparer);
			return list.AsReadOnly();
		}

		public static System.Collections.ObjectModel.ReadOnlyCollection<KeyValuePair<TKey, int>> ItemsSortedByValue<TKey>(this IEnumerable<KeyValuePair<TKey, int>> dict)
		{
			return dict.ToSortedCollection(new DictionaryValueOrderingComparer<TKey>());
		}

		public static System.Collections.ObjectModel.ReadOnlyCollection<KeyValuePair<TKey, uint>> ItemsSortedByValue<TKey>(this IEnumerable<KeyValuePair<TKey, uint>> dict)
		{
			return dict.ToSortedCollection(new DictionaryValueOrderingComparer<TKey>());
		}

		public static System.Collections.ObjectModel.ReadOnlyCollection<KeyValuePair<TKey, TMetadataIndex>> ItemsSortedByValue<TKey, TMetadataIndex>(this IEnumerable<KeyValuePair<TKey, TMetadataIndex>> dict) where TMetadataIndex : MetadataIndex
		{
			return dict.ToSortedCollection(new DictionaryValueOrderingComparer<TKey, TMetadataIndex>());
		}

		public static System.Collections.ObjectModel.ReadOnlyCollection<KeyValuePair<string, TValue>> ItemsSortedByKey<TValue>(this IEnumerable<KeyValuePair<string, TValue>> dict)
		{
			return dict.ToSortedCollection(new DictionaryKeyOrderingComparer<TValue>());
		}

		public static System.Collections.ObjectModel.ReadOnlyCollection<KeyValuePair<TKey, TValue>> ItemsSortedByKey<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> dict) where TKey : MemberReference
		{
			return dict.ToSortedCollection(new MemberReferenceDictionaryKeyOrderingComparer<TKey, TValue>());
		}

		public static System.Collections.ObjectModel.ReadOnlyCollection<KeyValuePair<IIl2CppRuntimeType, TValue>> ItemsSortedByKey<TValue>(this IEnumerable<KeyValuePair<IIl2CppRuntimeType, TValue>> dict)
		{
			return dict.ToSortedCollection(new Il2CppRuntimeTypeKeyComparer<IIl2CppRuntimeType, TValue>());
		}

		public static System.Collections.ObjectModel.ReadOnlyCollection<KeyValuePair<Il2CppTypeData, TValue>> ItemsSortedByKey<TValue>(this IEnumerable<KeyValuePair<Il2CppTypeData, TValue>> dict)
		{
			return dict.ToSortedCollection(new Il2CppTypeDataDictionaryKeyOrderingComparer<TValue>());
		}

		public static System.Collections.ObjectModel.ReadOnlyCollection<KeyValuePair<Il2CppMethodSpec, TValue>> ItemsSortedByKey<TValue>(this IEnumerable<KeyValuePair<Il2CppMethodSpec, TValue>> dict)
		{
			return dict.ToSortedCollection(new GenericMethodReferenceDictionaryKeyOrderingComparer<TValue>());
		}

		public static System.Collections.ObjectModel.ReadOnlyCollection<KeyValuePair<TKey, TValue>> ItemsSortedByKeyToString<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> dict) where TKey : class
		{
			return dict.ToSortedCollection(new ToStringDictionaryKeyOrderingComparer<TKey, TValue>());
		}

		public static System.Collections.ObjectModel.ReadOnlyCollection<TKey> KeysSortedByValue<TKey>(this IEnumerable<KeyValuePair<TKey, int>> dict)
		{
			return dict.KeysSortedByValue((TKey key) => key);
		}

		public static System.Collections.ObjectModel.ReadOnlyCollection<TKey> KeysSortedByValue<TKey>(this IEnumerable<KeyValuePair<TKey, uint>> dict)
		{
			return dict.KeysSortedByValue((TKey key) => key);
		}

		public static System.Collections.ObjectModel.ReadOnlyCollection<TSelectedKeyValue> KeysSortedByValue<TKey, TSelectedKeyValue>(this IEnumerable<KeyValuePair<TKey, uint>> dict, Func<TKey, TSelectedKeyValue> selector)
		{
			return (from kvp in dict.ItemsSortedByValue()
				select selector(kvp.Key)).ToList().AsReadOnly();
		}

		public static System.Collections.ObjectModel.ReadOnlyCollection<TKey> KeysSortedByValue<TKey>(this IDictionary<TKey, int> dict)
		{
			return dict.KeysSortedByValue((TKey key) => key);
		}

		public static System.Collections.ObjectModel.ReadOnlyCollection<TSelectedKeyValue> KeysSortedByValue<TKey, TSelectedKeyValue>(this IEnumerable<KeyValuePair<TKey, int>> dict, Func<TKey, TSelectedKeyValue> selector)
		{
			return (from kvp in dict.ItemsSortedByValue()
				select selector(kvp.Key)).ToList().AsReadOnly();
		}

		public static MethodReference ModuleInitializerMethod(this AssemblyDefinition assembly)
		{
			return assembly.MainModule.GetType("<Module>")?.Methods.FirstOrDefault((MethodDefinition m) => m.Name == ".cctor");
		}

		public static void Deconstruct<T1, T2>(this KeyValuePair<T1, T2> tuple, out T1 key, out T2 value)
		{
			key = tuple.Key;
			value = tuple.Value;
		}
	}
}
