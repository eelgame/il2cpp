using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using Unity.Cecil.Awesome.Comparers;
using Unity.IL2CPP.AssemblyConversion;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Results;
using Unity.IL2CPP.Debugger;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP
{
	internal static class DebugWriter
	{
		internal static void WriteDebugMetadata(SourceWritingContext sourceWritingContext, AssemblyDefinition assembly, SequencePointCollector sequencePointCollector, ICatchPointProvider catchPointProvider)
		{
			MethodDefinition[] methods = (from m in assembly.MainModule.GetAllTypes().SelectMany((TypeDefinition t) => t.Methods)
				orderby m.MetadataToken.RID
				select m).ToArray();
			using (ICppCodeWriter writer = sourceWritingContext.CreateProfiledSourceWriterInOutputDirectory(sourceWritingContext.Global.Services.PathFactory.GetFileNameForAssembly(assembly, "Debugger.c")))
			{
				if (ShouldEmitDebugInformation(sourceWritingContext.Global.InputData, assembly))
				{
					writer.AddCodeGenMetadataIncludes();
					WriteVariables(sequencePointCollector, writer, sourceWritingContext.Global.Results.SecondaryCollection.Types);
					WriteStringTable(sequencePointCollector, writer);
					WriteVariableRanges(sequencePointCollector, writer, methods);
					string sequencePointName = GetSequencePointName(sourceWritingContext, assembly);
					WriteSequencePoints(sourceWritingContext, sequencePointName, sequencePointCollector, writer);
					WriteCatchPoints(sourceWritingContext, catchPointProvider, writer);
					WriteSourceFileTable(sequencePointCollector, writer);
					int num = WriteTypeSourceMap(sourceWritingContext, sequencePointCollector, writer);
					WriteScopes(sequencePointCollector, writer);
					WriteScopeMap(sequencePointCollector, writer, methods);
					writer.WriteStructInitializer("const Il2CppDebuggerMetadataRegistration", "g_DebuggerMetadataRegistration" + sourceWritingContext.Global.Services.Naming.ForCleanAssemblyFileName(assembly), new string[12]
					{
						"(Il2CppMethodExecutionContextInfo*)g_methodExecutionContextInfos",
						"(Il2CppMethodExecutionContextInfoIndex*)g_methodExecutionContextInfoIndexes",
						"(Il2CppMethodScope*)g_methodScopes",
						"(Il2CppMethodHeaderInfo*)g_methodHeaderInfos",
						"(Il2CppSequencePointSourceFile*)g_sequencePointSourceFiles",
						$"{((ISequencePointCollector)sequencePointCollector).NumSeqPoints}",
						"(Il2CppSequencePoint*)" + sequencePointName,
						$"{catchPointProvider.NumCatchPoints}",
						"(Il2CppCatchPoint*)g_catchPoints",
						$"{num}",
						"(Il2CppTypeSourceFilePair*)g_typeSourceFiles",
						"(const char**)g_methodExecutionContextInfoStrings"
					}, externStruct: true);
				}
			}
		}

		public static bool ShouldEmitDebugInformation(AssemblyConversionInputData inputData, AssemblyDefinition assembly)
		{
			string assemblyFileName = Path.GetFileNameWithoutExtension(assembly.MainModule.FileName ?? assembly.MainModule.Name);
			if (!inputData.DebugAssemblyName.Any())
			{
				return true;
			}
			return inputData.DebugAssemblyName.Any((string debugAssemblyName) => debugAssemblyName.Contains(assemblyFileName));
		}

		internal static string GetSequencePointName(ReadOnlyContext context, AssemblyDefinition assembly)
		{
			return "g_sequencePoints" + context.Global.Services.Naming.ForCleanAssemblyFileName(assembly);
		}

		private static void WriteScopeMap(SequencePointCollector sequencePointCollector, ICppCodeWriter writer, MethodDefinition[] methods)
		{
			writer.WriteLine("#if IL2CPP_MONO_DEBUGGER");
			if (methods.Length != 0)
			{
				writer.WriteArrayInitializer("static const Il2CppMethodHeaderInfo", "g_methodHeaderInfos", methods.Select((MethodDefinition method) => GetScopeRange(sequencePointCollector, method)), externArray: false, nullTerminate: false);
			}
			else
			{
				writer.WriteLine("static const Il2CppMethodHeaderInfo g_methodHeaderInfos[1] = { { 0, 0, 0 } };");
			}
			writer.WriteLine("#else");
			writer.WriteLine("static const Il2CppMethodHeaderInfo g_methodHeaderInfos[1] = { { 0, 0, 0 } };");
			writer.WriteLine("#endif");
		}

		private static string GetScopeRange(ISequencePointCollector sequencePointCollector, MethodDefinition method)
		{
			if (sequencePointCollector.TryGetScopeRange(method, out var range))
			{
				return $"{{ {method.Body.CodeSize}, {range.Start}, {range.Length} }} /* {method.FullName} */";
			}
			return "{ 0, 0, 0 } /* " + method.FullName + " */";
		}

		private static void WriteScopes(SequencePointCollector sequencePointCollector, ICppCodeWriter writer)
		{
			writer.WriteLine("#if IL2CPP_MONO_DEBUGGER");
			ReadOnlyCollection<Unity.IL2CPP.Debugger.Range> scopes = sequencePointCollector.GetScopes();
			if (scopes.Count > 0)
			{
				writer.WriteArrayInitializer("static const Il2CppMethodScope", "g_methodScopes", scopes.Select((Unity.IL2CPP.Debugger.Range scope) => $"{{ {scope.Start}, {scope.Length} }}"), externArray: false, nullTerminate: false);
			}
			else
			{
				writer.WriteLine("static const Il2CppMethodScope g_methodScopes[1] = { { 0, 0 } };");
			}
			writer.WriteLine("#else");
			writer.WriteLine("static const Il2CppMethodScope g_methodScopes[1] = { { 0, 0 } };");
			writer.WriteLine("#endif");
		}

		private static int WriteTypeSourceMap(SourceWritingContext context, SequencePointCollector sequencePointCollector, ICppCodeWriter writer)
		{
			ReadOnlyCollection<SequencePointInfo> allSequencePoints = ((ISequencePointCollector)sequencePointCollector).GetAllSequencePoints();
			Dictionary<TypeDefinition, List<string>> dictionary = new Dictionary<TypeDefinition, List<string>>(new TypeReferenceEqualityComparer());
			int num = 0;
			foreach (SequencePointInfo item in allSequencePoints)
			{
				if (string.IsNullOrEmpty(item.SourceFile))
				{
					continue;
				}
				if (!dictionary.TryGetValue(item.Method.DeclaringType, out var value))
				{
					value = new List<string>();
					dictionary.Add(item.Method.DeclaringType, value);
				}
				bool flag = false;
				foreach (string item2 in value)
				{
					if (item2 == item.SourceFile)
					{
						flag = true;
					}
				}
				if (!flag)
				{
					value.Add(item.SourceFile);
					num++;
				}
			}
			writer.WriteLine("#if IL2CPP_MONO_DEBUGGER");
			if (num > 0)
			{
				writer.WriteArrayInitializer("static const Il2CppTypeSourceFilePair", "g_typeSourceFiles", dictionary.SelectMany((KeyValuePair<TypeDefinition, List<string>> kvp) => kvp.Value.Select((string file) => $"{{ {context.Global.Results.SecondaryCollection.Metadata.GetTypeInfoIndex(kvp.Key)}, {((ISequencePointCollector)sequencePointCollector).GetSourceFileIndex(file)} }}")), externArray: false, nullTerminate: false);
			}
			else
			{
				writer.WriteLine("static const Il2CppTypeSourceFilePair g_typeSourceFiles[1] = { { 0, 0 } };");
			}
			writer.WriteLine("#else");
			writer.WriteLine("static const Il2CppTypeSourceFilePair g_typeSourceFiles[1] = { { 0, 0 } };");
			writer.WriteLine("#endif");
			return num;
		}

		private static void WriteSourceFileTable(SequencePointCollector sequencePointCollector, ICppCodeWriter writer)
		{
			ReadOnlyCollection<ISequencePointSourceFileData> allSourceFiles = ((ISequencePointCollector)sequencePointCollector).GetAllSourceFiles();
			writer.WriteLine("#if IL2CPP_MONO_DEBUGGER");
			if (allSourceFiles.Count == 0)
			{
				writer.WriteLine("static const Il2CppSequencePointSourceFile g_sequencePointSourceFiles[1] = { NULL, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };");
			}
			else
			{
				writer.WriteLine("static const Il2CppSequencePointSourceFile g_sequencePointSourceFiles[] = {");
				int num = 0;
				foreach (ISequencePointSourceFileData item in allSourceFiles)
				{
					writer.Write("{ " + Formatter.AsUTF8CppStringLiteral(item.File) + ", { ");
					if (item.Hash != null && item.Hash.Length == 16)
					{
						for (int i = 0; i < 16; i++)
						{
							writer.Write(item.Hash[i].ToString());
							if (i != 15)
							{
								writer.Write(", ");
							}
						}
					}
					else
					{
						writer.Write("0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0");
					}
					writer.WriteLine($"}} }}, //{num} ");
					num++;
				}
				writer.WriteLine("};");
			}
			writer.WriteLine("#else");
			writer.WriteLine("static const Il2CppSequencePointSourceFile g_sequencePointSourceFiles[1] = { NULL, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };");
			writer.WriteLine("#endif");
		}

		private static void WriteSequencePoints(SourceWritingContext context, string variableName, SequencePointCollector sequencePointCollector, ICppCodeWriter writer)
		{
			ReadOnlyCollection<SequencePointInfo> allSequencePoints = ((ISequencePointCollector)sequencePointCollector).GetAllSequencePoints();
			writer.WriteLine("#if IL2CPP_MONO_DEBUGGER");
			if (allSequencePoints.Count > 0)
			{
				writer.WriteArrayInitializer("Il2CppSequencePoint", variableName, allSequencePoints.Select((SequencePointInfo seqPoint, int index) => GetSequencePoint(sequencePointCollector, context.Global.Results.SecondaryCollection.Metadata, seqPoint, index)), externArray: true, nullTerminate: false);
			}
			else
			{
				writer.WriteLine("extern Il2CppSequencePoint " + variableName + "[];");
				writer.WriteLine("Il2CppSequencePoint " + variableName + "[1] = { { 0, 0, 0, 0, 0, 0, 0, kSequencePointKind_Normal, 0, 0, } };");
			}
			writer.WriteLine("#else");
			writer.WriteLine("extern Il2CppSequencePoint " + variableName + "[];");
			writer.WriteLine("Il2CppSequencePoint " + variableName + "[1] = { { 0, 0, 0, 0, 0, 0, 0, kSequencePointKind_Normal, 0, 0, } };");
			writer.WriteLine("#endif");
		}

		private static void WriteCatchPoints(SourceWritingContext context, ICatchPointProvider catchPointProvider, ICppCodeWriter writer)
		{
			IEnumerable<CatchPointInfo> allCatchPoints = catchPointProvider.AllCatchPoints;
			writer.WriteLine("#if IL2CPP_MONO_DEBUGGER");
			if (catchPointProvider.NumCatchPoints > 0)
			{
				writer.WriteLine("static const Il2CppCatchPoint g_catchPoints[] = {");
				foreach (CatchPointInfo item in allCatchPoints)
				{
					writer.WriteLine(GetCatchPoint(context, item) + ",");
				}
				writer.WriteLine("};");
			}
			else
			{
				writer.WriteLine("static const Il2CppCatchPoint g_catchPoints[1] = { { 0, 0, 0, 0, } };");
			}
			writer.WriteLine("#else");
			writer.WriteLine("static const Il2CppCatchPoint g_catchPoints[1] = { { 0, 0, 0, 0, } };");
			writer.WriteLine("#endif");
		}

		private static string GetSequencePoint(ISequencePointCollector sequencePointCollector, IMetadataCollectionResults metadataCollector, SequencePointInfo seqPoint, int index)
		{
			string text;
			switch (seqPoint.Kind)
			{
			case SequencePointKind.Normal:
				text = "kSequencePointKind_Normal";
				break;
			case SequencePointKind.StepOut:
				text = "kSequencePointKind_StepOut";
				break;
			default:
				throw new NotSupportedException();
			}
			return $"{{ {metadataCollector.GetMethodIndex(seqPoint.Method)}, " + $"{sequencePointCollector.GetSourceFileIndex(seqPoint.SourceFile)}, " + $"{seqPoint.StartLine}, {seqPoint.EndLine}, " + $"{seqPoint.StartColumn}, {seqPoint.EndColumn}, " + $"{seqPoint.IlOffset}, {text}, 0, {sequencePointCollector.GetSeqPointIndex(seqPoint)} }} /* seqPointIndex: {index} */";
		}

		private static string GetCatchPoint(SourceWritingContext context, CatchPointInfo catchPoint)
		{
			return string.Format(arg1: (catchPoint.CatchHandler == null || catchPoint.CatchHandler.CatchType == null) ? (-1) : context.Global.Results.SecondaryCollection.Types.GetIndex(catchPoint.RuntimeType), format: "{{ {0}, {1}, ", arg0: context.Global.Results.SecondaryCollection.Metadata.GetMethodIndex(catchPoint.Method)) + $"{catchPoint.IlOffset}, {catchPoint.TryId}, {catchPoint.ParentTryId} }}";
		}

		private static void WriteVariableRanges(SequencePointCollector sequencePointCollector, ICppCodeWriter writer, MethodDefinition[] methods)
		{
			writer.WriteLine("#if IL2CPP_MONO_DEBUGGER");
			if (methods.Length != 0)
			{
				writer.WriteArrayInitializer("static const Il2CppMethodExecutionContextInfoIndex", "g_methodExecutionContextInfoIndexes", methods.Select((MethodDefinition method) => GetVariableRange(sequencePointCollector, method)), externArray: false, nullTerminate: false);
			}
			else
			{
				writer.WriteLine("static const Il2CppMethodExecutionContextInfoIndex g_methodExecutionContextInfoIndexes[1] = { { 0, 0} };");
			}
			writer.WriteLine("#else");
			writer.WriteLine("static const Il2CppMethodExecutionContextInfoIndex g_methodExecutionContextInfoIndexes[1] = { { 0, 0} };");
			writer.WriteLine("#endif");
		}

		private static string GetVariableRange(ISequencePointCollector sequencePointCollector, MethodDefinition method)
		{
			sequencePointCollector.TryGetVariableRange(method, out var range);
			return $"{{ {range.Start}, {range.Length} }} /* 0x{method.MetadataToken.ToUInt32():X8} {method.FullName} */";
		}

		private static void WriteStringTable(SequencePointCollector sequencePointCollector, ICppCodeWriter writer)
		{
			writer.WriteLine("#if IL2CPP_MONO_DEBUGGER");
			ReadOnlyCollection<string> allContextInfoStrings = ((ISequencePointCollector)sequencePointCollector).GetAllContextInfoStrings();
			if (allContextInfoStrings.Count > 0)
			{
				writer.WriteArrayInitializer("static const char*", "g_methodExecutionContextInfoStrings", allContextInfoStrings.Select((string str) => Formatter.AsUTF8CppStringLiteral(str) ?? ""), externArray: false, nullTerminate: false);
			}
			else
			{
				writer.WriteLine("static const char* g_methodExecutionContextInfoStrings[1] = { NULL };");
			}
			writer.WriteLine("#else");
			writer.WriteLine("static const char* g_methodExecutionContextInfoStrings[1] = { NULL };");
			writer.WriteLine("#endif");
		}

		private static void WriteVariables(SequencePointCollector sequencePointCollector, ICppCodeWriter writer, ITypeCollectorResults types)
		{
			writer.WriteLine("#if IL2CPP_MONO_DEBUGGER");
			ReadOnlyCollection<VariableData> variables = ((ISequencePointCollector)sequencePointCollector).GetVariables();
			if (variables.Count > 0)
			{
				writer.WriteArrayInitializer("static const Il2CppMethodExecutionContextInfo", "g_methodExecutionContextInfos", variables.Select((VariableData variable, int index) => $"{{ {types.GetIndex(variable.type)}, {variable.NameIndex},  {variable.ScopeIndex} }} /*tableIndex: {index} */"), externArray: false, nullTerminate: false);
			}
			else
			{
				writer.WriteLine("static const Il2CppMethodExecutionContextInfo g_methodExecutionContextInfos[1] = { { 0, 0, 0 } };");
			}
			writer.WriteLine("#else");
			writer.WriteLine("static const Il2CppMethodExecutionContextInfo g_methodExecutionContextInfos[1] = { { 0, 0, 0 } };");
			writer.WriteLine("#endif");
		}
	}
}
