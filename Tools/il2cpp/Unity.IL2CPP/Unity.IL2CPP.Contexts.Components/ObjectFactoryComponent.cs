using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.Cecil.Awesome.Comparers;
using Unity.IL2CPP.AssemblyConversion;
using Unity.IL2CPP.Contexts.Components.Base;
using Unity.IL2CPP.Contexts.Forking;
using Unity.IL2CPP.Contexts.Forking.Steps;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.GenericSharing;
using Unity.IL2CPP.Marshaling;
using Unity.IL2CPP.Marshaling.MarshalInfoWriters;
using Unity.IL2CPP.Metadata;
using Unity.IL2CPP.Metadata.Dat;
using Unity.IL2CPP.Tiny;

namespace Unity.IL2CPP.Contexts.Components
{
	public class ObjectFactoryComponent : MayViolateContextBoundariesComponentBase<TinyWriteContext, IObjectFactory, ObjectFactoryComponent>, IObjectFactory
	{
		private class NopMetadataWriterStep : IMetadataWriterStep
		{
			public void Write()
			{
			}
		}

		private class NotAvailable : IObjectFactory
		{
			public IRuntimeMetadataAccess GetDefaultRuntimeMetadataAccess(SourceWritingContext context, MethodReference method, MethodMetadataUsage methodMetadataUsage, MethodUsage methodUsage)
			{
				throw new NotSupportedException();
			}

			public IMetadataWriterImplementation CreateMetadataWriter(GlobalWriteContext context, ReadOnlyCollection<AssemblyDefinition> assemblies)
			{
				throw new NotSupportedException();
			}

			public MetadataDatWriterBase CreateMetadataDatWriter(GlobalMetadataWriteContext context)
			{
				throw new NotSupportedException();
			}

			public IMetadataWriterStep CreateClassLibrarySpecificBigMetadataWriterStep(SourceWritingContext context)
			{
				throw new NotSupportedException();
			}

			public DefaultMarshalInfoWriter CreateMarshalInfoWriter(ReadOnlyContext context, TypeReference type, MarshalType marshalType, MarshalInfo marshalInfo, bool useUnicodeCharSet, bool forByReferenceType, bool forFieldMarshaling, bool forReturnValue, bool forNativeToManagedWrapper, HashSet<TypeReference> typesForRecursiveFields)
			{
				throw new NotSupportedException();
			}
		}

		public ObjectFactoryComponent(LateContextAccess<TinyWriteContext> tinyWriteContext)
			: base(tinyWriteContext)
		{
		}

		public IRuntimeMetadataAccess GetDefaultRuntimeMetadataAccess(SourceWritingContext context, MethodReference method, MethodMetadataUsage methodMetadataUsage, MethodUsage methodUsage)
		{
			if (context.Global.Parameters.UsingTinyBackend)
			{
				return new TinyRuntimeMetadataAccess(base.Context, method, methodMetadataUsage, methodUsage);
			}
			DefaultRuntimeMetadataAccess defaultRuntimeMetadataAccess = new DefaultRuntimeMetadataAccess(context, method, methodMetadataUsage, methodUsage);
			if (method != null && GenericSharingAnalysis.IsSharedMethod(context, method))
			{
				return new SharedRuntimeMetadataAccess(context, method, defaultRuntimeMetadataAccess);
			}
			return defaultRuntimeMetadataAccess;
		}

		public IMetadataWriterImplementation CreateMetadataWriter(GlobalWriteContext context, ReadOnlyCollection<AssemblyDefinition> assemblies)
		{
			if (context.Parameters.UsingTinyBackend)
			{
				return new TinyMetadataWriter(base.Context, assemblies);
			}
			return new BigMetadataWriter(context.CreateSourceWritingContext());
		}

		public MetadataDatWriterBase CreateMetadataDatWriter(GlobalMetadataWriteContext context)
		{
			if (context.Parameters.UsingTinyBackend)
			{
				return new TinyMetadataDatWriter(context.CreateWriteContext());
			}
			return new Libil2cppMetadataDatWriter(context.CreateWriteContext());
		}

		public IMetadataWriterStep CreateClassLibrarySpecificBigMetadataWriterStep(SourceWritingContext context)
		{
			return new NopMetadataWriterStep();
		}

		public DefaultMarshalInfoWriter CreateMarshalInfoWriter(ReadOnlyContext context, TypeReference type, MarshalType marshalType, MarshalInfo marshalInfo, bool useUnicodeCharSet, bool forByReferenceType, bool forFieldMarshaling, bool forReturnValue, bool forNativeToManagedWrapper, HashSet<TypeReference> typesForRecursiveFields)
		{
			if (context.Global.Parameters.UsingTinyBackend && marshalType != 0)
			{
				return new UnmarshalableMarshalInfoWriter(context, type);
			}
			useUnicodeCharSet |= (type.Resolve().Attributes & TypeAttributes.UnicodeClass) != 0;
			bool flag = MarshalingUtils.IsStringBuilder(type);
			if (type.MetadataType == MetadataType.String || flag)
			{
				if (context.Global.Parameters.UsingTinyBackend)
				{
					NativeType nativeType = StringMarshalInfoWriter.DetermineNativeTypeFor(marshalType, marshalInfo, useUnicodeCharSet, flag);
					if (nativeType == NativeType.BStr || nativeType == (NativeType)47)
					{
						return new UnmarshalableMarshalInfoWriter(context, type);
					}
				}
				return new StringMarshalInfoWriter(context, type, marshalType, marshalInfo, useUnicodeCharSet, forByReferenceType, forFieldMarshaling);
			}
			if (type.Resolve().IsDelegate() && (!(type is TypeSpecification) || type is GenericInstanceType))
			{
				if (marshalType == MarshalType.WindowsRuntime)
				{
					return new WindowsRuntimeDelegateMarshalInfoWriter(context, type);
				}
				if (context.Global.Parameters.UsingTinyClassLibraries)
				{
					return new TinyDelegateMarshalInfoWriter(base.Context, context, type, forFieldMarshaling);
				}
				return new DelegateMarshalInfoWriter(context, type, forFieldMarshaling);
			}
			NativeType? nativeType2 = marshalInfo?.NativeType;
			if (type.MetadataType == MetadataType.ValueType && MarshalingUtils.IsBlittable(type, nativeType2, marshalType, useUnicodeCharSet))
			{
				if (!forByReferenceType && !forFieldMarshaling && marshalInfo != null && marshalInfo.NativeType == NativeType.LPStruct)
				{
					return new LPStructMarshalInfoWriter(context, type, marshalType);
				}
				return new BlittableStructMarshalInfoWriter(context, type.Resolve(), marshalType);
			}
			if (type.IsPrimitive || type.IsPointer || type.IsEnum() || type.MetadataType == MetadataType.Void)
			{
				return new PrimitiveMarshalInfoWriter(context, type, marshalInfo, marshalType, useUnicodeCharSet);
			}
			if (!context.Global.Parameters.UsingTinyBackend && TypeReferenceEqualityComparer.AreEqual(type, context.Global.Services.TypeProvider.Corlib.MainModule.GetType("System.Runtime.InteropServices.HandleRef")))
			{
				return new HandleRefMarshalInfoWriter(context, type, forByReferenceType);
			}
			if (type is ByReferenceType byReferenceType)
			{
				TypeReference elementType = byReferenceType.ElementType;
				if (MarshalingUtils.IsBlittable(elementType, nativeType2, marshalType, useUnicodeCharSet) && (elementType.IsValueType() || type.IsPointer))
				{
					return new BlittableByReferenceMarshalInfoWriter(context, byReferenceType, marshalType, marshalInfo);
				}
				return new ByReferenceMarshalInfoWriter(context, byReferenceType, marshalType, marshalInfo, forNativeToManagedWrapper);
			}
			if (type is ArrayType arrayType)
			{
				TypeReference elementType2 = arrayType.ElementType;
				NativeType? nativeType3 = ((marshalInfo is ArrayMarshalInfo arrayMarshalInfo) ? new NativeType?(arrayMarshalInfo.ElementType) : null);
				if (marshalType != MarshalType.WindowsRuntime)
				{
					if (!MarshalingUtils.IsStringBuilder(elementType2) && (elementType2.MetadataType == MetadataType.Object || elementType2.MetadataType == MetadataType.Array || (elementType2.MetadataType == MetadataType.Class && !MarshalingUtils.HasMarshalableLayout(elementType2)) || arrayType.Rank != 1))
					{
						return new UnmarshalableMarshalInfoWriter(context, type);
					}
					if (marshalInfo != null && marshalInfo.NativeType == NativeType.SafeArray && !context.Global.Parameters.UsingTinyBackend)
					{
						return new ComSafeArrayMarshalInfoWriter(context, arrayType, marshalInfo);
					}
					if (marshalInfo != null && marshalInfo.NativeType == NativeType.FixedArray)
					{
						return new FixedArrayMarshalInfoWriter(context, arrayType, marshalType, marshalInfo);
					}
				}
				if (!forByReferenceType && !forFieldMarshaling && MarshalingUtils.IsBlittable(elementType2, nativeType3, marshalType, useUnicodeCharSet))
				{
					return new PinnedArrayMarshalInfoWriter(context, arrayType, marshalType, marshalInfo, useUnicodeCharSet);
				}
				if (marshalInfo == null && forFieldMarshaling && ComSafeArrayMarshalInfoWriter.IsMarshalableAsSafeArray(context, elementType2.MetadataType))
				{
					return new ComSafeArrayMarshalInfoWriter(context, arrayType);
				}
				return new LPArrayMarshalInfoWriter(context, arrayType, marshalType, marshalInfo);
			}
			TypeDefinition type2 = context.Global.Services.TypeProvider.Corlib.MainModule.GetType("System.Runtime.InteropServices.SafeHandle");
			if (type.DerivesFrom(context, type2, checkInterfaces: false))
			{
				return new SafeHandleMarshalInfoWriter(context, type, type2);
			}
			if (type.IsComOrWindowsRuntimeInterface(context) && !context.Global.Parameters.UsingTinyBackend)
			{
				return new ComObjectMarshalInfoWriter(context, type, marshalType, marshalInfo, forNativeToManagedWrapper);
			}
			if (type.IsSystemObject())
			{
				if (marshalInfo != null && !context.Global.Parameters.UsingTinyBackend)
				{
					switch (marshalInfo.NativeType)
					{
					case NativeType.IUnknown:
					case NativeType.IntF:
					case (NativeType)46:
						return new ComObjectMarshalInfoWriter(context, type, marshalType, marshalInfo, forNativeToManagedWrapper);
					case NativeType.Struct:
						return new ComVariantMarshalInfoWriter(context, type);
					}
				}
				if (marshalType == MarshalType.WindowsRuntime)
				{
					return new ComObjectMarshalInfoWriter(context, type, marshalType, marshalInfo, forNativeToManagedWrapper);
				}
			}
			TypeDefinition typeDefinition = type.Resolve();
			if (typeDefinition.IsAbstract && typeDefinition.IsSealed)
			{
				return new UnmarshalableMarshalInfoWriter(context, type);
			}
			if (TypeReferenceEqualityComparer.AreEqual(typeDefinition, context.Global.Services.TypeProvider.SystemException))
			{
				if (marshalType == MarshalType.WindowsRuntime)
				{
					return new ExceptionMarshalInfoWriter(context, typeDefinition);
				}
			}
			else
			{
				if (typeDefinition.MetadataType == MetadataType.Class && !(type is TypeSpecification) && typeDefinition.IsExposedToWindowsRuntime() && !typeDefinition.IsAttribute())
				{
					return new ComObjectMarshalInfoWriter(context, typeDefinition, marshalType, marshalInfo, forNativeToManagedWrapper);
				}
				if (marshalType == MarshalType.WindowsRuntime)
				{
					GenericInstanceType genericInstanceType = type as GenericInstanceType;
					if (genericInstanceType != null && context.Global.Services.TypeProvider.IReferenceType != null && type.IsNullable() && genericInstanceType.GenericArguments[0].CanBoxToWindowsRuntime(context))
					{
						return new WindowsRuntimeNullableMarshalInfoWriter(context, type);
					}
					TypeReference typeReference = context.Global.Services.WindowsRuntime.ProjectToWindowsRuntime(type);
					if (typeReference != type)
					{
						if (typeReference.IsComOrWindowsRuntimeInterface(context))
						{
							if (typeDefinition.IsInterface)
							{
								return new ComObjectMarshalInfoWriter(context, type, marshalType, marshalInfo, forNativeToManagedWrapper);
							}
							if (typeDefinition.IsValueType && genericInstanceType != null && typeDefinition.Namespace == "System.Collections.Generic" && typeDefinition.Name == "KeyValuePair`2")
							{
								return new KeyValuePairMarshalInfoWriter(context, genericInstanceType);
							}
						}
						if (type.Namespace == "System" && type.Name == "Uri")
						{
							return new UriMarshalInfoWriter(context, typeDefinition);
						}
						if (type.Namespace == "System" && type.Name == "DateTimeOffset")
						{
							return new DateTimeOffsetMarshalInfoWriter(context, typeDefinition);
						}
					}
					if (typeDefinition.Module.Assembly == context.Global.Services.TypeProvider.Corlib && typeDefinition.Namespace == "System" && typeDefinition.Name == "Type")
					{
						return new WindowsRuntimeTypeMarshalInfoWriter(context, typeDefinition);
					}
				}
			}
			if (MarshalDataCollector.HasCustomMarshalingMethods(type, nativeType2, marshalType, useUnicodeCharSet, forFieldMarshaling))
			{
				if (typesForRecursiveFields == null)
				{
					typesForRecursiveFields = new HashSet<TypeReference>(new TypeReferenceEqualityComparer());
				}
				FieldDefinition fieldDefinition = typeDefinition.GetTypeHierarchy().SelectMany((TypeDefinition t) => MarshalingUtils.NonStaticFieldsOf(t)).FirstOrDefault(delegate(FieldDefinition field)
				{
					typesForRecursiveFields.Add(type);
					try
					{
						if (typesForRecursiveFields.Contains(field.FieldType))
						{
							return true;
						}
						if (TypeReferenceEqualityComparer.AreEqual(field.FieldType, context.Global.Services.TypeProvider.Corlib.MainModule.GetType("System.Runtime.InteropServices.HandleRef")))
						{
							return true;
						}
						if (field.FieldType.IsArray)
						{
							if (!MarshalingUtils.IsMarshalableArrayField(field))
							{
								return true;
							}
							TypeReference elementType3 = ((ArrayType)field.FieldType).ElementType;
							bool flag2 = MarshalingUtils.HasMarshalableLayout(elementType3) && MarshalingUtils.IsMarshalable(context, elementType3, marshalType, field.MarshalInfo, useUnicodeCharSet, forFieldMarshaling: true, typesForRecursiveFields);
							if (!elementType3.IsPrimitive && !elementType3.IsPointer && !elementType3.IsEnum() && !flag2)
							{
								return true;
							}
						}
						if (field.FieldType.IsDelegate())
						{
							TypeResolver typeResolver = TypeResolver.For(field.FieldType);
							TypeDefinition typeDefinition2 = field.FieldType.Resolve();
							MethodReference methodReference = typeResolver.Resolve(typeDefinition2.Methods.Single((MethodDefinition m) => m.Name == "Invoke"));
							if (typesForRecursiveFields.Contains(methodReference.ReturnType.GetElementType()))
							{
								return true;
							}
							foreach (ParameterDefinition parameter in methodReference.Parameters)
							{
								if (typesForRecursiveFields.Contains(parameter.ParameterType.GetElementType()))
								{
									return true;
								}
							}
						}
						if (MarshalDataCollector.FieldIsArrayOfType(field, type))
						{
							return true;
						}
						return !MarshalingUtils.IsMarshalable(context, field.FieldType, marshalType, field.MarshalInfo, MarshalingUtils.UseUnicodeAsDefaultMarshalingForFields(typeDefinition), forFieldMarshaling: true, typesForRecursiveFields);
					}
					finally
					{
						typesForRecursiveFields.Remove(type);
					}
				});
				if (fieldDefinition != null)
				{
					return new TypeDefinitionWithUnsupportedFieldMarshalInfoWriter(context, typeDefinition, marshalType, fieldDefinition);
				}
				if (type.IsGenericInstance)
				{
					return new TypeDefinitionWithMarshalSizeOfOnlyMarshalInfoWriter(context, typeDefinition, marshalType, forFieldMarshaling, forByReferenceType, forReturnValue, forNativeToManagedWrapper);
				}
				return new TypeDefinitionMarshalInfoWriter(context, typeDefinition, marshalType, forFieldMarshaling, forByReferenceType, forReturnValue, forNativeToManagedWrapper);
			}
			return new UnmarshalableMarshalInfoWriter(context, type);
		}

		protected override IObjectFactory GetNotAvailableRead()
		{
			return new NotAvailable();
		}

		protected override ObjectFactoryComponent CreateEmptyInstance()
		{
			return new ObjectFactoryComponent(null);
		}

		protected override ObjectFactoryComponent CreateEmptyInstance(LateAccessForkingContainer lateAccess)
		{
			if (lateAccess is PrimaryWriteAssembliesLateAccessForkingContainer primaryWriteAssembliesLateAccessForkingContainer)
			{
				return new ObjectFactoryComponent(primaryWriteAssembliesLateAccessForkingContainer.TinyWriteContext);
			}
			if (lateAccess is SecondaryWriteLateAccessForkingContainer secondaryWriteLateAccessForkingContainer)
			{
				return new ObjectFactoryComponent(secondaryWriteLateAccessForkingContainer.TinyWriteContext);
			}
			if (lateAccess is SecondaryCollectionLateAccessForkingContainer)
			{
				return new ObjectFactoryComponent(new NotAvailableLateContextAccess<TinyWriteContext>("The TinyWriteContext is not available during secondary collection"));
			}
			if (lateAccess is PerAssemblyLateAccessForkingContainer perAssemblyLateAccessForkingContainer)
			{
				return new ObjectFactoryComponent(perAssemblyLateAccessForkingContainer.TinyWriteContext);
			}
			throw new ArgumentException($"Unhandled late access container of type {lateAccess.GetType()}");
		}

		protected override void ForkForPrimaryCollection(PrimaryCollectionLateAccessForkingContainer lateAccess, out object writer, out IObjectFactory reader, out ObjectFactoryComponent full)
		{
			NotAvailableFork(out writer, out reader, out full);
		}
	}
}
