using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using NiceIO;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Scheduling.Streams;
using Unity.IL2CPP.GenericSharing;
using Unity.IL2CPP.Marshaling.BodyWriters.ManagedToNative;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.CodeWriters
{
	public class InMemoryGeneratedMethodCodeWriter : InMemoryCodeWriter, IGeneratedMethodCodeWriter, IGeneratedCodeWriter, ICppCodeWriter, ICodeWriter, IDisposable, IStream
	{
		private readonly Dictionary<string, MethodMetadataUsage> _methodMetadataUsages = new Dictionary<string, MethodMetadataUsage>();

		public ReadOnlyDictionary<string, MethodMetadataUsage> MethodMetadataUsages => _methodMetadataUsages.AsReadOnly();

		public bool ErrorOccurred { get; set; }

		public virtual NPath FileName => null;

		public InMemoryGeneratedMethodCodeWriter(SourceWritingContext context)
			: base(context)
		{
		}

		public void AddMetadataUsage(string identifier, MethodMetadataUsage usage)
		{
			_methodMetadataUsages.Add(identifier, usage);
		}

		public void AddIncludeForMethodDeclaration(MethodReference method)
		{
			TypeReference declaringType = method.DeclaringType;
			if ((!declaringType.IsInterface() || declaringType.IsComOrWindowsRuntimeInterface(base.Context)) && !declaringType.IsArray && !declaringType.HasGenericParameters && _cppDeclarations._methods.Add(method))
			{
				MethodSignatureWriter.RecordIncludes(this, method);
				if (GenericSharingAnalysis.CanShareMethod(base.Context, method))
				{
					MethodReference sharedMethod = GenericSharingAnalysis.GetSharedMethod(base.Context, method);
					MethodSignatureWriter.RecordIncludes(this, sharedMethod);
					_cppDeclarations._sharedMethods.Add(sharedMethod);
				}
			}
		}

		public string VirtualCallInvokeMethod(MethodReference method, TypeResolver typeResolver, bool skipFirstParameter = false, bool? isInterfaceMethod = null, bool? isGenericInstance = null)
		{
			bool returnAsByRefParameter = base.Context.Global.Parameters.ReturnAsByRefParameter;
			bool flag = method.ReturnType.IsVoid();
			bool flag2 = !flag && returnAsByRefParameter;
			bool flag3 = !flag && !returnAsByRefParameter;
			List<TypeReference> list = new List<TypeReference>();
			if (flag3)
			{
				list.Add(typeResolver.ResolveReturnType(method));
			}
			IEnumerable<ParameterDefinition> enumerable;
			if (!skipFirstParameter)
			{
				IEnumerable<ParameterDefinition> parameters = method.Parameters;
				enumerable = parameters;
			}
			else
			{
				enumerable = method.Parameters.Skip(1);
			}
			IEnumerable<ParameterDefinition> source = enumerable;
			list.AddRange(source.Select((ParameterDefinition p) => typeResolver.Resolve(GenericParameterResolver.ResolveParameterTypeIfNeeded(method, p))));
			if (flag2)
			{
				list.Add(typeResolver.ResolveReturnType(method).MakePointerType());
			}
			string text = string.Empty;
			if (list.Count > 0)
			{
				text = "< " + ((IEnumerable<TypeReference>)list).Select((Func<TypeReference, string>)base.Context.Global.Services.Naming.ForVariable).AggregateWithComma() + " >";
			}
			int num = source.Count() + (flag2 ? 1 : 0);
			bool flag4 = isInterfaceMethod ?? method.Resolve().DeclaringType.IsInterface;
			bool flag5 = isGenericInstance ?? method.Resolve().HasGenericParameters;
			_cppDeclarations._virtualMethods.Add(new VirtualMethodDeclarationData(method, num, flag || returnAsByRefParameter, flag4, flag5));
			string text2 = (flag4 ? "Interface" : "Virt");
			return string.Format("{0}{1}{2}Invoker{3}{4}::Invoke", flag5 ? "Generic" : string.Empty, text2, flag3 ? "Func" : "Action", num, text);
		}

		public void WriteInternalCallResolutionStatement(MethodDefinition method, IRuntimeMetadataAccess metadataAccess)
		{
			string text = method.FullName.Substring(method.FullName.IndexOf(" ") + 1);
			WriteLine("typedef {0};", MethodSignatureWriter.GetICallMethodVariable(base.Context, method));
			WriteLine("static {0}_ftn _il2cpp_icall_func;", base.Context.Global.Services.Naming.ForMethodNameOnly(method));
			WriteLine("if (!_il2cpp_icall_func)", base.Context.Global.Services.Naming.ForMethodNameOnly(method));
			WriteLine("_il2cpp_icall_func = ({0}_ftn)il2cpp_codegen_resolve_icall (\"{1}\");", base.Context.Global.Services.Naming.ForMethodNameOnly(method), text);
		}

		public void AddInternalPInvokeMethodDeclaration(string methodName, string internalPInvokeDeclaration, string moduleName, bool forForcedInternalPInvoke, bool isExplicitlyInternal)
		{
			if (forForcedInternalPInvoke)
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.AppendFormat("#if {0} || {1}\n", "FORCE_PINVOKE_INTERNAL", PInvokeMethodBodyWriter.FORCE_PINVOKE_lib_INTERNAL(moduleName));
				stringBuilder.AppendLine(internalPInvokeDeclaration);
				stringBuilder.AppendFormat("#endif\n");
				if (!_cppDeclarations._internalPInvokeMethodDeclarationsForForcedInternalPInvoke.ContainsKey(methodName))
				{
					_cppDeclarations._internalPInvokeMethodDeclarationsForForcedInternalPInvoke.Add(methodName, stringBuilder.ToString());
				}
				return;
			}
			StringBuilder stringBuilder2 = new StringBuilder();
			if (!isExplicitlyInternal)
			{
				stringBuilder2.AppendFormat("#if !{0} && !{1}\n", "FORCE_PINVOKE_INTERNAL", PInvokeMethodBodyWriter.FORCE_PINVOKE_lib_INTERNAL(moduleName));
			}
			stringBuilder2.AppendLine(internalPInvokeDeclaration);
			if (!isExplicitlyInternal)
			{
				stringBuilder2.AppendFormat("#endif\n");
			}
			if (!_cppDeclarations._internalPInvokeMethodDeclarations.ContainsKey(methodName))
			{
				_cppDeclarations._internalPInvokeMethodDeclarations.Add(methodName, stringBuilder2.ToString());
			}
		}

		public void Write(IGeneratedMethodCodeWriter other)
		{
			foreach (KeyValuePair<string, MethodMetadataUsage> methodMetadataUsage in other.MethodMetadataUsages)
			{
				_methodMetadataUsages.Add(methodMetadataUsage.Key, methodMetadataUsage.Value);
			}
			_cppDeclarations.Add(other.Declarations);
			base.Writer.Flush();
			other.Writer.Flush();
			Stream baseStream = other.Writer.BaseStream;
			long position = baseStream.Position;
			baseStream.Seek(0L, SeekOrigin.Begin);
			baseStream.CopyTo(base.Writer.BaseStream);
			baseStream.Seek(position, SeekOrigin.Begin);
			base.Writer.Flush();
		}
	}
}
