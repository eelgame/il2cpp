using System;
using System.Collections.Generic;
using System.Text;
using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.Cecil.Awesome.Comparers;
using Unity.Cecil.Awesome.Ordering;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Components.Base;
using Unity.IL2CPP.Contexts.Forking;
using Unity.IL2CPP.Contexts.Forking.Steps;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.Diagnostics;
using Unity.MiniProfiling;

namespace Unity.IL2CPP.Naming
{
	public class NamingComponent : MayViolateContextBoundariesComponentBase<ReadOnlyContext, INamingService, NamingComponent>, INamingService, IDumpableState
	{
		private readonly Dictionary<string, string> _cleanNamesCache;

		private readonly Dictionary<MethodReference, string> _methodNameOnlyCache;

		private readonly Dictionary<TypeReference, string> _typeNameOnlyCache;

		private readonly Dictionary<string, string> _stringLiteralCache;

		private readonly HashCodeCache<TypeReference> _typeHashCache;

		private readonly HashCodeCache<MethodReference> _methodHashCache;

		private readonly HashCodeCache<string> _stringLiteralHashCache;

		private readonly StringBuilder _cleanStringBuilder = new StringBuilder();

		public NamingComponent(LateContextAccess<ReadOnlyContext> readOnlyContextAccess)
			: base(readOnlyContextAccess)
		{
			_cleanNamesCache = new Dictionary<string, string>();
			_methodNameOnlyCache = new Dictionary<MethodReference, string>(new MethodReferenceComparer());
			_typeNameOnlyCache = new Dictionary<TypeReference, string>(new TypeReferenceEqualityComparer());
			_stringLiteralCache = new Dictionary<string, string>();
			_typeHashCache = new HashCodeCache<TypeReference>(SemiUniqueStableTokenGenerator.GenerateFor, new TypeReferenceEqualityComparer());
			_methodHashCache = new HashCodeCache<MethodReference>(SemiUniqueStableTokenGenerator.GenerateFor, new MethodReferenceComparer());
			_stringLiteralHashCache = new HashCodeCache<string>(SemiUniqueStableTokenGenerator.GenerateFor);
		}

		private NamingComponent(LateContextAccess<ReadOnlyContext> readOnlyContextAccess, Dictionary<MethodReference, string> existingMethods, Dictionary<TypeReference, string> existingTypes, Dictionary<string, string> existingStringLiterals, Dictionary<string, string> existingCleanNames)
			: base(readOnlyContextAccess)
		{
			_methodNameOnlyCache = new Dictionary<MethodReference, string>(existingMethods, new MethodReferenceComparer());
			_typeNameOnlyCache = new Dictionary<TypeReference, string>(existingTypes, new TypeReferenceEqualityComparer());
			_cleanNamesCache = new Dictionary<string, string>(existingCleanNames);
			_stringLiteralCache = new Dictionary<string, string>(existingStringLiterals);
			_typeHashCache = new HashCodeCache<TypeReference>(SemiUniqueStableTokenGenerator.GenerateFor, new TypeReferenceEqualityComparer());
			_methodHashCache = new HashCodeCache<MethodReference>(SemiUniqueStableTokenGenerator.GenerateFor, new MethodReferenceComparer());
			_stringLiteralHashCache = new HashCodeCache<string>(SemiUniqueStableTokenGenerator.GenerateFor);
		}

		public bool IsCached(TypeReference type)
		{
			return _typeNameOnlyCache.ContainsKey(type);
		}

		public string ForTypeNameOnly(TypeReference type)
		{
			if (_typeNameOnlyCache.TryGetValue(type, out var value))
			{
				return value;
			}
			value = ForTypeNameInternal(type);
			_typeNameOnlyCache[type] = value;
			return value;
		}

		public string ForRuntimeUniqueTypeNameOnly(TypeReference type)
		{
			string text = ForTypeNameOnly(type);
			string uniqueIdentifier = base.Context.Global.Services.ContextScope.UniqueIdentifier;
			if (uniqueIdentifier != null)
			{
				return uniqueIdentifier + "_" + text;
			}
			return text;
		}

		public string ForMethodNameOnly(MethodReference method)
		{
			if (_methodNameOnlyCache.TryGetValue(method, out var value))
			{
				return value;
			}
			string text = ForMethodInternal(method);
			_methodNameOnlyCache[method] = text;
			return text;
		}

		public string ForRuntimeUniqueMethodNameOnly(MethodReference method)
		{
			string text = ForMethodNameOnly(method);
			string uniqueIdentifier = base.Context.Global.Services.ContextScope.UniqueIdentifier;
			if (uniqueIdentifier != null)
			{
				return uniqueIdentifier + "_" + text;
			}
			return text;
		}

		public string Clean(string name)
		{
			if (_cleanNamesCache.ContainsKey(name))
			{
				return _cleanNamesCache[name];
			}
			_cleanStringBuilder.Clear();
			return _cleanNamesCache[name] = CleanNoCaching(name, _cleanStringBuilder);
		}

		public static string CleanNoCaching(string name, StringBuilder sb)
		{
			char[] array = name.ToCharArray();
			for (int i = 0; i < array.Length; i++)
			{
				char c = array[i];
				if (IsSafeCharacter(c) || (IsAsciiDigit(c) && i != 0))
				{
					sb.Append(c);
					continue;
				}
				ushort num = Convert.ToUInt16(c);
				if (num < 255)
				{
					if (num == 46 || num == 47 || num == 96 || num == 95)
					{
						sb.Append("_");
					}
					else
					{
						sb.AppendFormat("U{0:X2}", num);
					}
				}
				else if (num < 4095)
				{
					sb.AppendFormat("U{0:X3}", num);
				}
				else
				{
					sb.AppendFormat("U{0:X4}", num);
				}
			}
			return sb.ToString();
		}

		public string ForStringLiteralIdentifier(string literal)
		{
			if (_stringLiteralCache.TryGetValue(literal, out var value))
			{
				return value;
			}
			string text = "_stringLiteral" + GenerateUniqueStringLiteralPostFix(literal);
			_stringLiteralCache[literal] = text;
			return text;
		}

		public string ForRuntimeUniqueStringLiteralIdentifier(string literal)
		{
			string text = ForStringLiteralIdentifier(literal);
			string uniqueIdentifier = base.Context.Global.Services.ContextScope.UniqueIdentifier;
			if (uniqueIdentifier != null)
			{
				return uniqueIdentifier + text;
			}
			return text;
		}

		public string ForComTypeInterfaceFieldGetter(TypeReference interfaceType)
		{
			return "get_" + ForInteropInterfaceVariable(interfaceType);
		}

		public string ForInteropInterfaceVariable(TypeReference interfaceType)
		{
			if (interfaceType.IsIActivationFactory(base.Context))
			{
				return "activationFactory";
			}
			string text = ForTypeNameOnly(interfaceType).TrimStart(new char[1] { '_' });
			return "____" + text.Substring(0, 2).ToLower() + text.Substring(2);
		}

		public string ForCurrentCodeGenModuleVar()
		{
			if (base.Context.Global.Services.ContextScope.UniqueIdentifier != null)
			{
				return "g_" + base.Context.Global.Services.ContextScope.UniqueIdentifier + "_CodeGenModule";
			}
			return null;
		}

		public string ForMetadataGlobalVar(string name)
		{
			string uniqueIdentifier = base.Context.Global.Services.ContextScope.UniqueIdentifier;
			if (uniqueIdentifier != null)
			{
				return name + "_" + uniqueIdentifier;
			}
			return name;
		}

		public string ForReloadMethodMetadataInitialized()
		{
			if (base.Context.Global.Services.ContextScope.UniqueIdentifier != null)
			{
				throw new NotSupportedException("Assembly reloading is not supported in per-assembly mode");
			}
			return ForMetadataGlobalVar("g_MethodMetadataInitialized");
		}

		internal static bool IsSafeCharacter(char c)
		{
			if ((c < 'a' || c > 'z') && (c < 'A' || c > 'Z'))
			{
				return c == '_';
			}
			return true;
		}

		internal static bool IsAsciiDigit(char c)
		{
			if (c >= '0')
			{
				return c <= '9';
			}
			return false;
		}

		internal static string EscapeKeywords(string fieldName)
		{
			return "___" + fieldName;
		}

		private string GetWellKnownNameFor(ReadOnlyContext context, TypeReference typeReference)
		{
			switch (typeReference.MetadataType)
			{
			case MetadataType.Object:
				return "RuntimeObject";
			case MetadataType.String:
				return "String_t";
			case MetadataType.IntPtr:
				return "IntPtr_t";
			case MetadataType.UIntPtr:
				return "UIntPtr_t";
			default:
			{
				TypeDefinition typeDefinition = typeReference.Resolve();
				if (typeDefinition != null && typeDefinition.Module != null && typeDefinition.Module.Name == "mscorlib.dll")
				{
					switch (typeReference.FullName)
					{
					case "System.Array":
						return "RuntimeArray";
					case "System.String":
						return "String_t";
					case "System.Type":
						return "Type_t";
					case "System.Delegate":
						return "Delegate_t";
					case "System.MulticastDelegate":
						return "MulticastDelegate_t";
					case "System.Exception":
						return "Exception_t";
					case "System.Reflection.Assembly":
						return "Assembly_t";
					case "System.Reflection.MemberInfo":
						return "MemberInfo_t";
					case "System.Reflection.MethodBase":
						return "MethodBase_t";
					case "System.Reflection.MethodInfo":
						return "MethodInfo_t";
					case "System.Reflection.FieldInfo":
						return "FieldInfo_t";
					case "System.Reflection.PropertyInfo":
						return "PropertyInfo_t";
					case "System.Reflection.EventInfo":
						return "EventInfo_t";
					case "System.MonoType":
						return "MonoType_t";
					case "System.Reflection.MonoMethod":
						return "MonoMethod_t";
					case "System.Reflection.MonoGenericMethod":
						return "MonoGenericMethod_t";
					case "System.Reflection.MonoField":
						return "MonoField_t";
					case "System.Reflection.MonoProperty":
						return "MonoProperty_t";
					case "System.Reflection.MonoEvent":
						return "MonoEvent_t";
					case "System.Text.StringBuilder":
						return "StringBuilder_t";
					case "System.Guid":
						return "Guid_t";
					}
				}
				if (typeReference.IsIActivationFactory(context))
				{
					return "Il2CppIActivationFactory";
				}
				if (typeReference.IsIl2CppComObject(context))
				{
					return "Il2CppComObject";
				}
				return null;
			}
			}
		}

		private string ForMethodInternal(MethodReference method)
		{
			GenericInstanceMethod genericInstanceMethod = method as GenericInstanceMethod;
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(Clean(method.DeclaringType.Name));
			stringBuilder.Append("_");
			stringBuilder.Append(Clean(method.Name));
			if (genericInstanceMethod != null)
			{
				foreach (TypeReference genericArgument in genericInstanceMethod.GenericArguments)
				{
					stringBuilder.Append("_Tis" + ForTypeNameOnly(genericArgument));
				}
			}
			stringBuilder.Append("_m");
			stringBuilder.Append(GenerateUniqueMethodPostFix(method));
			return stringBuilder.ToString();
		}

		private string ForTypeNameInternal(TypeReference typeReference)
		{
			string wellKnownNameFor = GetWellKnownNameFor(base.Context, typeReference);
			if (wellKnownNameFor != null)
			{
				return wellKnownNameFor;
			}
			string name = typeReference.Name;
			if (typeReference.IsArray)
			{
				name = ((ArrayType)typeReference).RankOnlyName();
			}
			return Clean(name) + "_t" + GenerateUniqueTypePostFix(typeReference);
		}

		private string GenerateUniqueTypePostFix(TypeReference typeReference)
		{
			return _typeHashCache.GetUniqueHash(typeReference);
		}

		private string GenerateUniqueMethodPostFix(MethodReference methodReference)
		{
			return _methodHashCache.GetUniqueHash(methodReference);
		}

		private string GenerateUniqueStringLiteralPostFix(string literal)
		{
			return _stringLiteralHashCache.GetUniqueHash(literal);
		}

		void IDumpableState.DumpState(StringBuilder builder)
		{
			CollectorStateDumper.AppendCollection(builder, "_cleanNamesCache", _cleanNamesCache.Keys.ToSortedCollection());
			CollectorStateDumper.AppendCollection(builder, "_methodNameOnlyCache", _methodNameOnlyCache.Keys.ToSortedCollection());
			CollectorStateDumper.AppendCollection(builder, "_typeNameOnlyCache", _typeNameOnlyCache.Keys.ToSortedCollection());
			CollectorStateDumper.AppendValue(builder, "_stringLiteralCache.Count", _stringLiteralCache.Count);
			CollectorStateDumper.AppendValue(builder, "_typeHashCache.Count", _typeHashCache.Count);
			CollectorStateDumper.AppendValue(builder, "_methodHashCache.Count", _methodHashCache.Count);
			CollectorStateDumper.AppendValue(builder, "_stringLiteralHashCache.Count", _stringLiteralHashCache.Count);
		}

		protected override NamingComponent CreateCopyInstance(LateAccessForkingContainer lateAccess)
		{
			return new NamingComponent(lateAccess.ReadOnlyContext, _methodNameOnlyCache, _typeNameOnlyCache, _stringLiteralCache, _cleanNamesCache);
		}

		protected override NamingComponent CreateEmptyInstance(LateAccessForkingContainer lateAccess)
		{
			return new NamingComponent(lateAccess.ReadOnlyContext);
		}

		protected override void HandleMergeForAdd(NamingComponent forked)
		{
			using (MiniProfiler.Section("Merging Cache", "_typeNameOnlyCache"))
			{
				foreach (KeyValuePair<TypeReference, string> item in forked._typeNameOnlyCache)
				{
					_typeNameOnlyCache[item.Key] = item.Value;
				}
			}
			using (MiniProfiler.Section("Merging Cache", "_methodNameOnlyCache"))
			{
				foreach (KeyValuePair<MethodReference, string> item2 in forked._methodNameOnlyCache)
				{
					_methodNameOnlyCache[item2.Key] = item2.Value;
				}
			}
			using (MiniProfiler.Section("Merging Cache", "_stringLiteralCache"))
			{
				foreach (KeyValuePair<string, string> item3 in forked._stringLiteralCache)
				{
					_stringLiteralCache[item3.Key] = item3.Value;
				}
			}
			using (MiniProfiler.Section("Merging Cache", "_cleanNamesCache"))
			{
				foreach (KeyValuePair<string, string> item4 in forked._cleanNamesCache)
				{
					_cleanNamesCache[item4.Key] = item4.Value;
				}
			}
		}

		protected override void ForkForSecondaryCollection(SecondaryCollectionLateAccessForkingContainer lateAccess, out object writer, out INamingService reader, out NamingComponent full)
		{
			((ComponentBase<object, INamingService, NamingComponent>)this).ReadOnlyFork((LateAccessForkingContainer)lateAccess, out writer, out reader, out full, ForkMode.Copy, MergeMode.None);
		}

		protected override void ForkForPrimaryWrite(PrimaryWriteAssembliesLateAccessForkingContainer lateAccess, out object writer, out INamingService reader, out NamingComponent full)
		{
			((ComponentBase<object, INamingService, NamingComponent>)this).ReadOnlyFork((LateAccessForkingContainer)lateAccess, out writer, out reader, out full, ForkMode.Copy, MergeMode.Add);
		}

		protected override void ForkForPrimaryCollection(PrimaryCollectionLateAccessForkingContainer lateAccess, out object writer, out INamingService reader, out NamingComponent full)
		{
			((ComponentBase<object, INamingService, NamingComponent>)this).ReadOnlyFork((LateAccessForkingContainer)lateAccess, out writer, out reader, out full, ForkMode.Empty, MergeMode.Add);
		}

		protected override void ForkForSecondaryWrite(SecondaryWriteLateAccessForkingContainer lateAccess, out object writer, out INamingService reader, out NamingComponent full)
		{
			((ComponentBase<object, INamingService, NamingComponent>)this).ReadOnlyFork((LateAccessForkingContainer)lateAccess, out writer, out reader, out full, ForkMode.Copy, MergeMode.None);
		}
	}
}
