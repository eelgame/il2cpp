using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using Unity.IL2CPP.Attributes;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Marshaling.BodyWriters.NativeToManaged;
using Unity.IL2CPP.Metadata.RuntimeTypes;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Metadata
{
	internal static class PerAssemblyCodeMetadataWriter
	{
		public static string Write(SourceWritingContext context, AssemblyDefinition assembly, GenericContextCollection genericContextCollection, string assemblyMetadataRegistrationVarName, string codeRegistrationVarName)
		{
			using (ICppCodeWriter cppCodeWriter = context.CreateProfiledSourceWriterInOutputDirectory(context.Global.Services.PathFactory.GetFileNameForAssembly(assembly, "CodeGen.c")))
			{
				MethodDefinition[] array = (from m in assembly.MainModule.GetAllTypes().SelectMany((TypeDefinition t) => t.Methods)
					orderby m.MetadataToken.RID
					select m).ToArray();
				TypeDefinition[] collection = (from m in assembly.MainModule.GetAllTypes()
					orderby m.MetadataToken.RID
					select m).ToArray();
				string methodPointers = WriteMethodPointers(context, cppCodeWriter, array);
				WriteAdjustorThunks(context, cppCodeWriter, array).Deconstruct(out var item2, out var item3);
				uint value = item2;
				string adjustorThunks = item3;
				string invokerIndices = WriteInvokerIndices(context, cppCodeWriter, array);
				int usedMethodsWithReversePInvokeWrappersCount;
				string reversePinvokeIndices = WriteReversePInvokeIndices(context, cppCodeWriter, assembly, out usedMethodsWithReversePInvokeWrappersCount);
				List<IGenericParameterProvider> list = new List<IGenericParameterProvider>(collection);
				list.AddRange(array);
				List<IGenericParameterProvider> res = list.Where((IGenericParameterProvider item) => genericContextCollection.GetRGCTXEntriesCount(item) > 0).ToList();
				string rgctxIndices = WriteRGCTXIndices(context, cppCodeWriter, genericContextCollection, res);
				string rgctxValues = WriteRGCTXValues(context, cppCodeWriter, genericContextCollection, res);
				string cleanAssemblyName = context.Global.Services.Naming.ForCleanAssemblyFileName(assembly);
				string debugMetadata = WriteDebugger(context, cppCodeWriter, assembly, cleanAssemblyName);
				string text = "NULL";
				if (context.Global.Results.PrimaryWrite.AttributeWriterOutput.TryGetValue(assembly, out var value2) && value2.AttributeTypes.Count > 0)
				{
					text = AttributesSupport.AttributeGeneratorName(context.Global.Services.Naming, assembly);
					cppCodeWriter.WriteLine("extern " + AttributesSupport.AttributeGeneratorType() + " " + text + "[];");
				}
				string moduleInitializer = "NULL, // module initializer";
				MethodReference methodReference = assembly.ModuleInitializerMethod();
				if (methodReference != null)
				{
					moduleInitializer = context.Global.Services.Naming.ForMethodNameOnly(methodReference) + ", // module initializer";
				}
				string text2 = "NULL";
				TypeDefinition[] array2 = assembly.MainModule.GetAllTypes().Where(CompilerServicesSupport.HasEagerStaticClassConstructionEnabled).ToArray();
				if (array2.Length != 0)
				{
					text2 = "s_staticConstructorsToRunAtStartup";
					cppCodeWriter.WriteArrayInitializer("static TypeDefinitionIndex", text2, array2.Select((TypeDefinition t) => context.Global.SecondaryCollectionResults.Metadata.GetTypeInfoIndex(t).ToString(CultureInfo.InvariantCulture)).Append("0"), externArray: false, nullTerminate: false);
				}
				string text3 = context.Global.Services.Naming.ForCodeGenModule(assembly);
				WriteCodeGenModuleInitializer(cppCodeWriter, text3, Path.GetFileName(assembly.MainModule.FileName ?? assembly.MainModule.Name), array, methodPointers, value, adjustorThunks, invokerIndices, usedMethodsWithReversePInvokeWrappersCount, reversePinvokeIndices, res, rgctxIndices, rgctxValues, debugMetadata, genericContextCollection, text, moduleInitializer, text2, assemblyMetadataRegistrationVarName, codeRegistrationVarName);
				return text3;
			}
		}

		public static void WriteGenericsPseudoCodeGenModule(SourceWritingContext context, string pseudoAssemblyName, string assemblyMetadataRegistrationVarName, string codeRegistrationVarName)
		{
			using (ICppCodeWriter cppCodeWriter = context.CreateProfiledSourceWriterInOutputDirectory(context.Global.Services.PathFactory.GetFileName("CodeGen.c")))
			{
				string codeGenModule = cppCodeWriter.Context.Global.Services.Naming.ForCurrentCodeGenModuleVar();
				WriteCodeGenModuleInitializer(cppCodeWriter, codeGenModule, pseudoAssemblyName, null, null, null, null, null, 0, null, null, null, null, null, null, null, null, null, assemblyMetadataRegistrationVarName, codeRegistrationVarName);
			}
		}

		private static void WriteCodeGenModuleInitializer(ICppCodeWriter writer, string codeGenModule, string assemblyName, MethodDefinition[] sortedMethods = null, string methodPointers = null, uint? adjustorThunksCount = null, string adjustorThunks = null, string invokerIndices = null, int usedMethodsWithReversePInvokeWrappersCount = 0, string reversePinvokeIndices = null, List<IGenericParameterProvider> res = null, string rgctxIndices = null, string rgctxValues = null, string debugMetadata = null, GenericContextCollection genericContextCollection = null, string attributeSupportName = null, string moduleInitializer = null, string staticConstructorsToRunAtStartupArray = null, string assemblyMetadataRegistrationVarName = null, string codeRegistrationVarName = null)
		{
			writer.AddCodeGenMetadataIncludes();
			if (assemblyMetadataRegistrationVarName != null)
			{
				writer.AddForwardDeclaration("IL2CPP_EXTERN_C_CONST Il2CppMetadataRegistration " + assemblyMetadataRegistrationVarName);
			}
			if (codeRegistrationVarName != null)
			{
				writer.AddForwardDeclaration("IL2CPP_EXTERN_C_CONST Il2CppCodeRegistration " + codeRegistrationVarName);
			}
			writer.WriteStructInitializer("const Il2CppCodeGenModule", codeGenModule, new string[18]
			{
				"\"" + assemblyName + "\"",
				sortedMethods?.Length.ToString(CultureInfo.InvariantCulture) ?? "0",
				methodPointers ?? "NULL",
				adjustorThunksCount.HasValue ? adjustorThunksCount.Value.ToString() : "0",
				adjustorThunks ?? "NULL",
				invokerIndices ?? "NULL",
				usedMethodsWithReversePInvokeWrappersCount.ToString(CultureInfo.InvariantCulture),
				reversePinvokeIndices ?? "NULL",
				res?.Count.ToString(CultureInfo.InvariantCulture) ?? "0",
				rgctxIndices ?? "NULL",
				genericContextCollection?.GetRGCTXEntries().Count.ToString(CultureInfo.InvariantCulture) ?? "0",
				rgctxValues ?? "NULL",
				debugMetadata ?? "NULL",
				attributeSupportName ?? "NULL",
				moduleInitializer ?? "NULL",
				staticConstructorsToRunAtStartupArray ?? "NULL",
				(assemblyMetadataRegistrationVarName != null) ? ("&" + assemblyMetadataRegistrationVarName) : "NULL",
				(codeRegistrationVarName != null) ? ("&" + codeRegistrationVarName) : "NULL"
			}, externStruct: true);
		}

		private static string WriteMethodPointers(SourceWritingContext context, ICppCodeWriter writer, MethodDefinition[] sortedMethods)
		{
			foreach (MethodDefinition methodDefinition in sortedMethods)
			{
				writer.WriteCommentedLine("0x" + methodDefinition.MetadataToken.RID.ToString("X8") + " " + methodDefinition.FullName);
				string text = MethodTables.MethodPointerNameFor(context, methodDefinition);
				if (text != "NULL" && context.Global.Results.PrimaryWrite.Methods.HasIndex(methodDefinition))
				{
					writer.WriteLine("extern void {0} (void);", text);
				}
			}
			string text2 = "NULL";
			if (sortedMethods.Length != 0)
			{
				text2 = "s_methodPointers";
				writer.WriteArrayInitializer("static Il2CppMethodPointer", text2, sortedMethods.Select((MethodDefinition m) => context.Global.Results.PrimaryWrite.Methods.HasIndex(m) ? MethodTables.MethodPointerNameFor(context, m) : "NULL"), externArray: false, nullTerminate: false);
			}
			return text2;
		}

		private static Tuple<uint, string> WriteAdjustorThunks(SourceWritingContext context, ICppCodeWriter writer, MethodDefinition[] sortedMethods)
		{
			List<MethodDefinition> list = new List<MethodDefinition>();
			foreach (MethodDefinition methodDefinition in sortedMethods)
			{
				if (MethodWriter.HasAdjustorThunk(methodDefinition) && context.Global.Results.PrimaryWrite.Methods.HasIndex(methodDefinition))
				{
					string text = MethodTables.AdjustorThunkNameFor(context, methodDefinition);
					if (text != "NULL")
					{
						writer.WriteLine("extern void {0} (void);", text);
						list.Add(methodDefinition);
					}
				}
			}
			string text2 = "NULL";
			if (list.Count > 0)
			{
				text2 = "s_adjustorThunks";
				writer.WriteArrayInitializer("static Il2CppTokenAdjustorThunkPair", text2, list.Select((MethodDefinition m) => $"{{ 0x{m.MetadataToken.ToUInt32():X8}, {MethodTables.AdjustorThunkNameFor(context, m)} }}"), externArray: false, nullTerminate: false);
			}
			return new Tuple<uint, string>((uint)list.Count, text2);
		}

		private static string WriteInvokerIndices(SourceWritingContext context, ICppCodeWriter writer, MethodDefinition[] sortedMethods)
		{
			string text = "NULL";
			if (sortedMethods.Length != 0)
			{
				text = "s_InvokerIndices";
				writer.WriteArrayInitializer("static const int32_t", text, sortedMethods.Select((MethodDefinition m) => context.Global.Results.SecondaryCollection.Invokers.GetIndex(context, m).ToString(CultureInfo.InvariantCulture)), externArray: false, nullTerminate: false);
			}
			return text;
		}

		private static string WriteReversePInvokeIndices(SourceWritingContext context, ICppCodeWriter writer, AssemblyDefinition assembly, out int usedMethodsWithReversePInvokeWrappersCount)
		{
			string text = "NULL";
			List<KeyValuePair<MethodReference, uint>> list = (from m in context.Global.Results.PrimaryWrite.ReversePInvokeWrappers.SortedItems
				where m.Key.Module == assembly.MainModule
				orderby m.Key.Resolve().MetadataToken.RID
				select m).ToList();
			if (list.Count == 0)
			{
				usedMethodsWithReversePInvokeWrappersCount = 0;
				return text;
			}
			ReadOnlyHashSet<MethodReference> allUsedInflatedMethods = context.Global.Results.PrimaryWrite.MetadataUsage.GetInflatedMethods();
			KeyValuePair<MethodReference, uint>[] array = list.Where((KeyValuePair<MethodReference, uint> m) => ReversePInvokeMethodBodyWriter.IsReversePInvokeMethodThatMustBeGenerated(m.Key) || allUsedInflatedMethods.Contains(m.Key)).ToArray();
			if (array.Length != 0)
			{
				text = "s_reversePInvokeIndices";
				KeyValuePair<MethodReference, uint>[] array2 = array;
				foreach (KeyValuePair<MethodReference, uint> keyValuePair in array2)
				{
					writer.AddForwardDeclaration("extern const RuntimeMethod* " + context.Global.Services.Naming.ForRuntimeMethodInfo(keyValuePair.Key));
				}
				writer.WriteArrayInitializer("static const Il2CppTokenIndexMethodTuple", text, array.Select(delegate(KeyValuePair<MethodReference, uint> m)
				{
					context.Global.Results.PrimaryWrite.GenericMethods.TryGetValue(m.Key, out var genericMethodIndex);
					return $"{{ 0x{m.Key.Resolve().MetadataToken.ToUInt32():X8}, {m.Value.ToString(CultureInfo.InvariantCulture)},  (void**)&{context.Global.Services.Naming.ForRuntimeMethodInfo(m.Key)}, {genericMethodIndex} }}";
				}), externArray: false, nullTerminate: false);
			}
			usedMethodsWithReversePInvokeWrappersCount = array.Length;
			return text;
		}

		private static string WriteRGCTXIndices(SourceWritingContext context, ICppCodeWriter writer, GenericContextCollection genericContextCollection, List<IGenericParameterProvider> res)
		{
			string text = "NULL";
			if (res.Count > 0)
			{
				text = "s_rgctxIndices";
				writer.WriteArrayInitializer("static const Il2CppTokenRangePair", text, res.Select((IGenericParameterProvider item) => $"{{ 0x{item.MetadataToken.ToUInt32():X8}, {{ {genericContextCollection.GetRGCTXEntriesStartIndex(item)}, {genericContextCollection.GetRGCTXEntriesCount(item)} }} }}"), externArray: false, nullTerminate: false);
			}
			return text;
		}

		private static string WriteRGCTXValues(SourceWritingContext context, ICppCodeWriter writer, GenericContextCollection genericContextCollection, List<IGenericParameterProvider> res)
		{
			string text = "NULL";
			if (res.Count > 0)
			{
				text = "s_rgctxValues";
				writer.WriteArrayInitializer("static const Il2CppRGCTXDefinition", text, genericContextCollection.GetRGCTXEntries().Select(delegate(RGCTXEntry rgctxEntry)
				{
					try
					{
						uint rgctxTokenOrIndex = GetRgctxTokenOrIndex(rgctxEntry, (IIl2CppRuntimeType typeData) => (uint)context.Global.Results.SecondaryCollection.Types.GetIndex(typeData), context.Global.Results.PrimaryWrite.GenericMethods.GetIndex);
						return $"{{ (Il2CppRGCTXDataType){(int)rgctxEntry.Type}, {rgctxTokenOrIndex} }}";
					}
					catch (KeyNotFoundException e)
					{
						HandleRgctxKeyNotFoundException(context, rgctxEntry, e);
						throw;
					}
				}), externArray: false, nullTerminate: false);
			}
			return text;
		}

		private static uint GetRgctxTokenOrIndex(RGCTXEntry rgctxEntry, Func<IIl2CppRuntimeType, uint> typeAction, Func<MethodReference, uint> methodAction)
		{
			switch (rgctxEntry.Type)
			{
			case RGCTXType.Type:
			case RGCTXType.Class:
			case RGCTXType.Array:
				return typeAction(rgctxEntry.RuntimeType);
			case RGCTXType.Method:
				return methodAction((MethodReference)rgctxEntry.MemberReference);
			default:
				throw new InvalidOperationException(string.Format("Attempt to get metadata token for invalid ${0} {1}", "RGCTXType", rgctxEntry.Type));
			}
		}

		private static void HandleRgctxKeyNotFoundException(ReadOnlyContext context, RGCTXEntry rgctxEntry, KeyNotFoundException e)
		{
			int num = 0;
			string arg = string.Empty;
			switch (rgctxEntry.Type)
			{
			case RGCTXType.Type:
			case RGCTXType.Class:
			case RGCTXType.Array:
				num = GenericsUtilities.RecursiveGenericDepthFor((GenericInstanceType)rgctxEntry.RuntimeType.Type);
				arg = rgctxEntry.RuntimeType.Type.FullName;
				break;
			case RGCTXType.Method:
			{
				GenericInstanceMethod genericInstanceMethod = (GenericInstanceMethod)rgctxEntry.MemberReference;
				num = Math.Max(GenericsUtilities.RecursiveGenericDepthFor(genericInstanceMethod), GenericsUtilities.RecursiveGenericDepthFor(genericInstanceMethod.DeclaringType as GenericInstanceType));
				arg = genericInstanceMethod.FullName;
				break;
			}
			}
			if (num >= context.Global.InputData.MaximumRecursiveGenericDepth)
			{
				throw new InvalidOperationException($"A generic type or method was used, but no code for it was generated. Consider increasing the --maximum-recursive-generic-depth command line argument to {num + 1}.\nEncountered on: {arg}", e);
			}
		}

		private static string WriteDebugger(SourceWritingContext context, ICppCodeWriter writer, AssemblyDefinition assembly, string cleanAssemblyName)
		{
			if (!DebugWriter.ShouldEmitDebugInformation(context.Global.InputData, assembly))
			{
				return null;
			}
			string result = "NULL";
			if (context.Global.Parameters.EnableDebugger)
			{
				writer.WriteLine("extern const Il2CppDebuggerMetadataRegistration g_DebuggerMetadataRegistration" + cleanAssemblyName + ";");
				result = "&g_DebuggerMetadataRegistration" + cleanAssemblyName;
			}
			return result;
		}
	}
}
