using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Unity.Cecil.Awesome;
using Unity.Cecil.Awesome.Comparers;
using Unity.Cecil.Visitor;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Collectors;
using Unity.IL2CPP.GenericSharing;
using Unity.IL2CPP.Metadata;

namespace Unity.IL2CPP.GenericsCollection
{
	public class GenericContextAwareVisitor : Visitor
	{
		private readonly PrimaryCollectionContext _context;

		private readonly InflatedCollectionCollector _generics;

		private readonly GenericContext _genericContext;

		public GenericContextAwareVisitor(PrimaryCollectionContext context, InflatedCollectionCollector generics, GenericContext genericContext)
		{
			_context = context;
			_generics = generics;
			_genericContext = genericContext;
		}

		protected override void Visit(TypeDefinition typeDefinition, Context context)
		{
			if (context.Role != Role.NestedType)
			{
				base.Visit(typeDefinition, context);
			}
		}

		protected override void Visit(MethodDefinition methodDefinition, Context context)
		{
			if ((methodDefinition.HasGenericParameters && (_genericContext.Method == null || _genericContext.Method.Resolve() != methodDefinition)) || GenericsUtilities.CheckForMaximumRecursion(_context, _genericContext.Type) || GenericsUtilities.CheckForMaximumRecursion(_context, _genericContext.Method))
			{
				return;
			}
			if (!methodDefinition.HasBody && _context.Global.Results.Setup.RuntimeImplementedMethodWriters.TryGetGenericSharingDataFor(methodDefinition, out var value))
			{
				foreach (RuntimeGenericData item in value)
				{
					if (item is RuntimeGenericTypeData runtimeGenericTypeData)
					{
						Visit(runtimeGenericTypeData.GenericType, context);
						continue;
					}
					if (item is RuntimeGenericMethodData runtimeGenericMethodData)
					{
						Visit(runtimeGenericMethodData.GenericMethod, context);
						continue;
					}
					throw new NotImplementedException();
				}
			}
			base.Visit(methodDefinition, context);
		}

		protected override void Visit(PropertyDefinition propertyDefinition, Context context)
		{
			if (!GenericsUtilities.CheckForMaximumRecursion(_context, _genericContext.Type))
			{
				base.Visit(propertyDefinition, context);
			}
		}

		protected override void Visit(FieldDefinition fieldDefinition, Context context)
		{
			if (!GenericsUtilities.CheckForMaximumRecursion(_context, _genericContext.Type))
			{
				base.Visit(fieldDefinition, context);
			}
		}

		protected override void Visit(ArrayType arrayType, Context context)
		{
			ProcessArray(_context, arrayType.ElementType, arrayType.Rank);
			base.Visit(arrayType, context);
		}

		protected override void Visit(GenericInstanceType genericInstanceType, Context context)
		{
			GenericInstanceType inflatedType = Inflater.InflateType(_genericContext, genericInstanceType);
			ProcessGenericType(inflatedType);
			base.Visit(genericInstanceType, context);
		}

		protected override void Visit(FieldReference fieldReference, Context context)
		{
			if (fieldReference.DeclaringType is GenericInstanceType genericInstanceType)
			{
				GenericInstanceType genericInstanceType2 = Inflater.InflateType(_genericContext, genericInstanceType);
				ProcessGenericType(genericInstanceType2);
				GenericContextAwareVisitor visitor = new GenericContextAwareVisitor(_context, _generics, new GenericContext(genericInstanceType2, _genericContext.Method));
				fieldReference.Resolve().Accept(visitor);
			}
			else
			{
				base.Visit(fieldReference, context);
			}
		}

		protected override void Visit(MethodReference methodReference, Context context)
		{
			MethodDefinition methodDefinition = methodReference.Resolve();
			GenericInstanceType genericInstanceType = methodReference.DeclaringType as GenericInstanceType;
			if (methodReference is GenericInstanceMethod genericInstanceMethod)
			{
				ProcessGenericMethod(_context, Inflater.InflateMethod(_genericContext, genericInstanceMethod), _generics);
			}
			else if (methodDefinition != null && genericInstanceType != null)
			{
				ProcessGenericType(Inflater.InflateType(_genericContext, genericInstanceType));
			}
			else
			{
				base.Visit(methodReference, context);
			}
		}

		protected override void Visit(Instruction instruction, Context context)
		{
			if (instruction.OpCode.Code == Code.Newarr)
			{
				ProcessArray(_context, (TypeReference)instruction.Operand, 1);
			}
			if (instruction.OpCode.Code == Code.Callvirt && instruction.Previous != null && instruction.Previous.OpCode.Code == Code.Constrained)
			{
				TypeReference typeReference = (TypeReference)instruction.Previous.Operand;
				TypeResolver typeResolver = TypeResolver.For(_genericContext.Type, _genericContext.Method);
				TypeReference typeReference2 = typeResolver.Resolve(typeReference);
				MethodReference methodReference = (MethodReference)instruction.Operand;
				if (typeReference2.IsValueType() && methodReference.DeclaringType.IsInterface() && methodReference.IsGenericInstance)
				{
					IVTableBuilder vTable = _context.Global.Collectors.VTable;
					int num = vTable.IndexFor(_context, methodReference.Resolve());
					VTable vTable2 = vTable.VTableFor(_context, typeReference2);
					MethodReference methodReference2 = typeResolver.Resolve(methodReference);
					if (vTable2.InterfaceOffsets.TryGetValue(methodReference2.DeclaringType, out var value))
					{
						num += value;
						MethodReference methodReference3 = vTable2.Slots[num];
						GenericInstanceMethod method = Inflater.InflateMethod(new GenericContext(typeReference2 as GenericInstanceType, methodReference2 as GenericInstanceMethod), methodReference3.Resolve());
						ProcessGenericMethod(_context, method, _generics);
					}
				}
			}
			base.Visit(instruction, context);
		}

		private void ProcessGenericType(GenericInstanceType inflatedType)
		{
			ProcessGenericType(_context, inflatedType, _generics, _genericContext.Method);
		}

		internal static void ProcessGenericMethod(PrimaryCollectionContext context, GenericInstanceMethod method, InflatedCollectionCollector generics)
		{
			if (method.DeclaringType.IsGenericInstance)
			{
				ProcessGenericType(context, (GenericInstanceType)method.DeclaringType, generics, method);
			}
			ProcessGenericArguments(context, method.GenericArguments, generics);
			MethodReference sharedMethod = GenericSharingAnalysis.GetSharedMethod(context, method);
			if (GenericSharingAnalysis.CanShareMethod(context, method) && !MethodReferenceComparer.AreEqual(sharedMethod, method))
			{
				ProcessGenericMethod(context, (GenericInstanceMethod)sharedMethod, generics);
			}
			else if (generics.Methods.Add(method))
			{
				GenericContext genericContext = new GenericContext(method.DeclaringType as GenericInstanceType, method);
				method.Resolve().Accept(new GenericContextAwareVisitor(context, generics, genericContext));
			}
		}

		internal static void ProcessGenericType(PrimaryCollectionContext context, GenericInstanceType type, InflatedCollectionCollector generics, GenericInstanceMethod contextMethod)
		{
			generics.TypeDeclarations.Add(type);
			ProcessGenericArguments(context, type.GenericArguments, generics);
			GenericInstanceType sharedType = GenericSharingAnalysis.GetSharedType(context, type);
			if (GenericSharingAnalysis.CanShareType(context, type) && !TypeReferenceEqualityComparer.AreEqual(sharedType, type))
			{
				ProcessHardcodedDependencies(context, type, generics, contextMethod);
				ProcessGenericType(context, sharedType, generics, contextMethod);
			}
			else if (generics.Types.Add(type))
			{
				ProcessHardcodedDependencies(context, type, generics, contextMethod);
				GenericContext genericContext = new GenericContext(type, contextMethod);
				type.ElementType.Resolve().Accept(new GenericContextAwareVisitor(context, generics, genericContext));
			}
		}

		private static void ProcessGenericArguments(ReadOnlyContext context, IEnumerable<TypeReference> genericArguments, InflatedCollectionCollector generics)
		{
			foreach (GenericInstanceType item in genericArguments.OfType<GenericInstanceType>())
			{
				generics.TypeDeclarations.Add(item);
			}
		}

		private static void ProcessHardcodedDependencies(PrimaryCollectionContext context, GenericInstanceType type, InflatedCollectionCollector generics, GenericInstanceMethod contextMethod)
		{
			if (context.Global.Parameters.UsingTinyClassLibraries)
			{
				return;
			}
			ModuleDefinition mainModule = context.Global.Services.TypeProvider.Corlib.MainModule;
			AddArrayIfNeeded(context, type, generics, contextMethod, mainModule.GetType("System.Collections.Generic", "IEnumerable`1"), mainModule.GetType("System.Collections.Generic", "ICollection`1"), mainModule.GetType("System.Collections.Generic", "IList`1"), mainModule.GetType("System.Collections.Generic", "IReadOnlyList`1"), mainModule.GetType("System.Collections.Generic", "IReadOnlyCollection`1"));
			if (type.GenericArguments.Count <= 0)
			{
				return;
			}
			TypeDefinition typeDefinition = type.Resolve();
			TypeReference typeReference = type.GenericArguments[0];
			if (typeDefinition == mainModule.GetType("System.Collections.Generic", "EqualityComparer`1"))
			{
				if (!typeReference.IsNullable())
				{
					AddGenericComparerIfNeeded(context, typeReference, generics, contextMethod, mainModule.GetType("System", "IEquatable`1"), mainModule.GetType("System.Collections.Generic", "GenericEqualityComparer`1"));
					AddEnumEqualityComparerIfNeeded(context, typeReference, generics, contextMethod);
				}
				else
				{
					TypeReference genericArgument = ((GenericInstanceType)typeReference).GenericArguments[0];
					AddGenericComparerIfNeeded(context, genericArgument, generics, contextMethod, mainModule.GetType("System", "IEquatable`1"), mainModule.GetType("System.Collections.Generic", "NullableEqualityComparer`1"));
				}
			}
			else if (typeDefinition == mainModule.GetType("System.Collections.Generic", "Comparer`1"))
			{
				AddGenericComparerIfNeeded(context, typeReference, generics, contextMethod, mainModule.GetType("System", "IComparable`1"), mainModule.GetType("System.Collections.Generic", "GenericComparer`1"));
			}
			else if (typeDefinition == mainModule.GetType("System.Collections.Generic", "ObjectComparer`1") && typeReference.IsNullable())
			{
				TypeReference genericArgument2 = ((GenericInstanceType)typeReference).GenericArguments[0];
				AddGenericComparerIfNeeded(context, genericArgument2, generics, contextMethod, mainModule.GetType("System", "IComparable`1"), mainModule.GetType("System.Collections.Generic", "NullableComparer`1"));
			}
		}

		private static void AddEnumEqualityComparerIfNeeded(PrimaryCollectionContext context, TypeReference keyType, InflatedCollectionCollector generics, GenericInstanceMethod contextMethod)
		{
			if (keyType.IsEnum())
			{
				TypeReference underlyingEnumType = keyType.GetUnderlyingEnumType();
				TypeDefinition typeDefinition = null;
				switch (underlyingEnumType.MetadataType)
				{
				case MetadataType.SByte:
					typeDefinition = context.Global.Services.TypeProvider.Corlib.MainModule.GetType("System.Collections.Generic.SByteEnumEqualityComparer`1");
					break;
				case MetadataType.Int16:
					typeDefinition = context.Global.Services.TypeProvider.Corlib.MainModule.GetType("System.Collections.Generic.ShortEnumEqualityComparer`1");
					break;
				case MetadataType.Int64:
				case MetadataType.UInt64:
					typeDefinition = context.Global.Services.TypeProvider.Corlib.MainModule.GetType("System.Collections.Generic.LongEnumEqualityComparer`1");
					break;
				default:
					typeDefinition = context.Global.Services.TypeProvider.Corlib.MainModule.GetType("System.Collections.Generic.EnumEqualityComparer`1");
					break;
				}
				GenericInstanceType type = typeDefinition.MakeGenericInstanceType(GenericSharingAnalysis.GetUnderlyingSharedType(context, keyType));
				ProcessGenericType(context, type, generics, contextMethod);
			}
		}

		private static void AddArrayIfNeeded(PrimaryCollectionContext context, GenericInstanceType type, InflatedCollectionCollector generics, GenericInstanceMethod contextMethod, TypeDefinition ienumerableDefinition, TypeDefinition icollectionDefinition, TypeDefinition ilistDefinition, TypeDefinition ireadOnlyListDefinition, TypeDefinition ireadOnlyCollectionDefinition)
		{
			TypeDefinition typeDefinition = type.Resolve();
			if (typeDefinition == ienumerableDefinition || typeDefinition == icollectionDefinition || typeDefinition == ilistDefinition || typeDefinition == ireadOnlyListDefinition || typeDefinition == ireadOnlyCollectionDefinition)
			{
				ProcessArray(context, new ArrayType(type.GenericArguments[0]), generics, new GenericContext(type, contextMethod));
			}
		}

		private static void AddGenericComparerIfNeeded(PrimaryCollectionContext context, TypeReference genericArgument, InflatedCollectionCollector generics, GenericInstanceMethod contextMethod, TypeDefinition genericElementComparisonInterfaceDefinition, TypeDefinition genericComparerDefinition)
		{
			GenericInstanceType genericElementComparisonInterface = genericElementComparisonInterfaceDefinition.MakeGenericInstanceType(genericArgument);
			if (genericArgument.GetInterfaces(context).Any((TypeReference i) => TypeReferenceEqualityComparer.AreEqual(i, genericElementComparisonInterface)))
			{
				GenericInstanceType type = genericComparerDefinition.MakeGenericInstanceType(genericArgument);
				ProcessGenericType(context, type, generics, contextMethod);
			}
		}

		internal static void ProcessArray(PrimaryCollectionContext context, ArrayType inflatedType, InflatedCollectionCollector generics, GenericContext currentContext)
		{
			if (!generics.VisitedArrays.Add(inflatedType))
			{
				return;
			}
			TypeDefinition type = context.Global.Services.TypeProvider.Corlib.MainModule.GetType("System", "Array");
			IEnumerable<MethodDefinition> enumerable = ((type != null) ? type.Methods.Where((MethodDefinition m) => m.Name == "InternalArray__IEnumerable_GetEnumerator") : Enumerable.Empty<MethodDefinition>());
			if (context.Global.Parameters.UsingTinyBackend && type != null)
			{
				enumerable = enumerable.Concat(type.Methods.Where((MethodDefinition m) => m.Name.StartsWith("InternalArray__")));
			}
			foreach (MethodDefinition item in enumerable)
			{
				GenericContextAwareVisitor visitor = new GenericContextAwareVisitor(context, generics, new GenericContext(method: Inflater.InflateMethod(currentContext, new GenericInstanceMethod(item)
				{
					GenericArguments = { inflatedType.ElementType }
				}), type: currentContext.Type));
				item.Accept(visitor);
			}
			foreach (GenericInstanceMethod item2 in ArrayTypeInfoWriter.InflateArrayMethods(context, inflatedType).OfType<GenericInstanceMethod>())
			{
				ProcessGenericMethod(context, item2, generics);
			}
			foreach (GenericInstanceType arrayExtraType in GetArrayExtraTypes(context, inflatedType))
			{
				ProcessGenericType(context, arrayExtraType, generics, currentContext.Method);
				foreach (MethodDefinition item3 in enumerable)
				{
					GenericInstanceMethod genericInstanceMethod = new GenericInstanceMethod(item3);
					genericInstanceMethod.GenericArguments.Add(arrayExtraType.GenericArguments[0]);
					GenericInstanceMethod method2 = Inflater.InflateMethod(currentContext, genericInstanceMethod);
					ProcessGenericMethod(context, method2, generics);
				}
			}
		}

		internal static IEnumerable<GenericInstanceType> GetArrayExtraTypes(ReadOnlyContext context, ArrayType type)
		{
			if (type.Rank != 1)
			{
				return new GenericInstanceType[0];
			}
			List<TypeReference> list = new List<TypeReference>();
			if (!type.ElementType.IsValueType)
			{
				list.AddRange(ArrayTypeInfoWriter.TypeAndAllBaseAndInterfaceTypesFor(context, type.ElementType));
				if (type.ElementType.IsArray)
				{
					list.AddRange(GetArrayExtraTypes(context, (ArrayType)type.ElementType));
				}
			}
			else
			{
				list.Add(type.ElementType);
			}
			return GetArrayExtraTypes(context, list);
		}

		private static IEnumerable<GenericInstanceType> GetArrayExtraTypes(ReadOnlyContext context, IEnumerable<TypeReference> types)
		{
			TypeDefinition iListType = context.Global.Services.TypeProvider.Corlib.MainModule.GetType("System.Collections.Generic.IList`1");
			TypeDefinition iCollectionType = context.Global.Services.TypeProvider.Corlib.MainModule.GetType("System.Collections.Generic.ICollection`1");
			TypeDefinition iEnumerableType = context.Global.Services.TypeProvider.Corlib.MainModule.GetType("System.Collections.Generic.IEnumerable`1");
			TypeDefinition iReadOnlyListType = context.Global.Services.TypeProvider.Corlib.MainModule.GetType("System.Collections.Generic.IReadOnlyList`1");
			TypeDefinition iReadOnlyCollectionType = context.Global.Services.TypeProvider.Corlib.MainModule.GetType("System.Collections.Generic.IReadOnlyCollection`1");
			foreach (TypeReference type in types)
			{
				if (iListType != null)
				{
					GenericInstanceType genericInstanceType = new GenericInstanceType(iListType);
					genericInstanceType.GenericArguments.Add(type);
					yield return genericInstanceType;
				}
				if (iCollectionType != null)
				{
					GenericInstanceType genericInstanceType2 = new GenericInstanceType(iCollectionType);
					genericInstanceType2.GenericArguments.Add(type);
					yield return genericInstanceType2;
				}
				if (iEnumerableType != null)
				{
					GenericInstanceType genericInstanceType3 = new GenericInstanceType(iEnumerableType);
					genericInstanceType3.GenericArguments.Add(type);
					yield return genericInstanceType3;
				}
				if (iReadOnlyListType != null)
				{
					GenericInstanceType genericInstanceType4 = new GenericInstanceType(iReadOnlyListType);
					genericInstanceType4.GenericArguments.Add(type);
					yield return genericInstanceType4;
				}
				if (iReadOnlyCollectionType != null)
				{
					GenericInstanceType genericInstanceType5 = new GenericInstanceType(iReadOnlyCollectionType);
					genericInstanceType5.GenericArguments.Add(type);
					yield return genericInstanceType5;
				}
			}
		}

		private void ProcessArray(PrimaryCollectionContext context, TypeReference elementType, int rank)
		{
			ArrayType inflatedType = new ArrayType(Inflater.InflateType(_genericContext, elementType), rank);
			ProcessArray(context, inflatedType, _generics, _genericContext);
		}
	}
}
