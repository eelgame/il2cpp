using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.WindowsRuntime
{
	public abstract class CCWWriterBase : ICCWWriter
	{
		private static readonly Guid IID_IMarshal = new Guid(3, 0, 0, 192, 0, 0, 0, 0, 0, 0, 70);

		protected readonly TypeReference _type;

		protected readonly SourceWritingContext _context;

		protected virtual bool ImplementsAnyIInspectableInterfaces => false;

		protected virtual bool HasBaseClass => false;

		protected virtual IList<TypeReference> InterfacesToForwardToBaseClass => new TypeReference[0];

		protected abstract IEnumerable<TypeReference> AllImplementedInterfaces { get; }

		private IEnumerable<TypeReference> AllInteropInterfaces => AllImplementedInterfaces.Concat(InterfacesToForwardToBaseClass);

		protected virtual IEnumerable<string> AllQueryableInterfaceNames => AllImplementedInterfaces.Select((TypeReference t) => _context.Global.Services.Naming.ForTypeNameOnly(t));

		protected virtual bool IsManagedObjectHolder => true;

		protected CCWWriterBase(SourceWritingContext context, TypeReference type)
		{
			_context = context;
			_type = type;
		}

		protected void AddIncludes(IGeneratedCodeWriter writer)
		{
			writer.AddIncludeForTypeDefinition(_type);
			foreach (TypeReference allImplementedInterface in AllImplementedInterfaces)
			{
				writer.AddIncludeForTypeDefinition(allImplementedInterface);
			}
		}

		public abstract void Write(IGeneratedMethodCodeWriter writer);

		public virtual void WriteCreateComCallableWrapperFunctionBody(IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess)
		{
			string text = _context.Global.Services.Naming.ForComCallableWrapperClass(_type);
			writer.WriteLine("void* memory = il2cpp::utils::Memory::Malloc(sizeof(" + text + "));");
			writer.WriteLine("if (memory == NULL)");
			using (new BlockWriter(writer))
			{
				writer.WriteLine("il2cpp_codegen_raise_out_of_memory_exception();");
			}
			writer.WriteLine();
			writer.AddInclude("utils/New.h");
			writer.WriteLine("return static_cast<Il2CppIManagedObjectHolder*>(new(memory) " + text + "(obj));");
		}

		protected void WriteCommonInterfaceMethods(IGeneratedMethodCodeWriter writer)
		{
			WriteQueryInterfaceDefinition(writer);
			WriteAddRefDefinition(writer);
			WriteReleaseDefinition(writer);
			WriteGetIidsDefinition(writer);
			if (ImplementsAnyIInspectableInterfaces)
			{
				WriteGetRuntimeClassNameDefinition(writer);
				WriteGetTrustLevelDefinition(writer);
			}
		}

		private void WriteQueryInterfaceDefinition(IGeneratedMethodCodeWriter writer)
		{
			writer.WriteLine();
			writer.WriteLine("virtual il2cpp_hresult_t STDCALL QueryInterface(const Il2CppGuid& iid, void** object) IL2CPP_OVERRIDE");
			using (new BlockWriter(writer))
			{
				bool flag = false;
				foreach (TypeReference allImplementedInterface in AllImplementedInterfaces)
				{
					if (!allImplementedInterface.IsIActivationFactory(_context) && allImplementedInterface.GetGuid(_context) == IID_IMarshal)
					{
						flag = true;
						break;
					}
				}
				writer.WriteLine("if (::memcmp(&iid, &Il2CppIUnknown::IID, sizeof(Il2CppGuid)) == 0");
				writer.Write(" || ::memcmp(&iid, &Il2CppIInspectable::IID, sizeof(Il2CppGuid)) == 0");
				if (!flag)
				{
					writer.WriteLine();
					writer.WriteLine(" || ::memcmp(&iid, &Il2CppIAgileObject::IID, sizeof(Il2CppGuid)) == 0)");
				}
				else
				{
					writer.WriteLine(")");
				}
				using (new BlockWriter(writer))
				{
					writer.WriteLine("*object = GetIdentity();");
					writer.WriteLine("AddRefImpl();");
					writer.WriteLine("return IL2CPP_S_OK;");
				}
				writer.WriteLine();
				if (IsManagedObjectHolder)
				{
					WriteQueryInterfaceForInterface(writer, "Il2CppIManagedObjectHolder");
				}
				foreach (string allQueryableInterfaceName in AllQueryableInterfaceNames)
				{
					WriteQueryInterfaceForInterface(writer, allQueryableInterfaceName);
				}
				if (!flag)
				{
					WriteQueryInterfaceForInterface(writer, "Il2CppIMarshal");
				}
				if (IsManagedObjectHolder)
				{
					WriteQueryInterfaceForInterface(writer, "Il2CppIWeakReferenceSource");
				}
				if (HasBaseClass)
				{
					string text = _context.Global.Services.Naming.ForVariable(_type);
					string text2 = _context.Global.Services.Naming.ForIl2CppComObjectIdentityField();
					writer.WriteLine("return ((" + text + ")GetManagedObjectInline())->" + text2 + "->QueryInterface(iid, object);");
				}
				else
				{
					writer.WriteLine("*object = NULL;");
					writer.WriteLine("return IL2CPP_E_NOINTERFACE;");
				}
			}
		}

		private static void WriteQueryInterfaceForInterface(IGeneratedMethodCodeWriter writer, string interfaceName)
		{
			writer.WriteLine("if (::memcmp(&iid, &" + interfaceName + "::IID, sizeof(Il2CppGuid)) == 0)");
			using (new BlockWriter(writer))
			{
				writer.WriteLine("*object = static_cast<" + interfaceName + "*>(this);");
				writer.WriteLine("AddRefImpl();");
				writer.WriteLine("return IL2CPP_S_OK;");
			}
			writer.WriteLine();
		}

		private void WriteAddRefDefinition(IGeneratedMethodCodeWriter writer)
		{
			writer.WriteLine();
			writer.WriteLine("virtual uint32_t STDCALL AddRef() IL2CPP_OVERRIDE");
			using (new BlockWriter(writer))
			{
				writer.WriteLine("return AddRefImpl();");
			}
		}

		private void WriteReleaseDefinition(IGeneratedMethodCodeWriter writer)
		{
			writer.WriteLine();
			writer.WriteLine("virtual uint32_t STDCALL Release() IL2CPP_OVERRIDE");
			using (new BlockWriter(writer))
			{
				writer.WriteLine("return ReleaseImpl();");
			}
		}

		private void WriteGetIidsDefinition(IGeneratedMethodCodeWriter writer)
		{
			int num = 0;
			foreach (TypeReference allInteropInterface in AllInteropInterfaces)
			{
				if (allInteropInterface.Resolve().IsExposedToWindowsRuntime())
				{
					num++;
				}
			}
			if (!ImplementsAnyIInspectableInterfaces && num == 0)
			{
				return;
			}
			writer.WriteLine();
			writer.WriteLine("virtual il2cpp_hresult_t STDCALL GetIids(uint32_t* iidCount, Il2CppGuid** iids) IL2CPP_OVERRIDE");
			using (new BlockWriter(writer))
			{
				if (num > 0)
				{
					writer.WriteLine($"Il2CppGuid* interfaceIds = il2cpp_codegen_marshal_allocate_array<Il2CppGuid>({num});");
					int num2 = 0;
					foreach (TypeReference allInteropInterface2 in AllInteropInterfaces)
					{
						if (allInteropInterface2.Resolve().IsExposedToWindowsRuntime())
						{
							string arg = _context.Global.Services.Naming.ForTypeNameOnly(allInteropInterface2);
							writer.AddIncludeForTypeDefinition(allInteropInterface2);
							writer.WriteLine($"interfaceIds[{num2}] = {arg}::IID;");
							num2++;
						}
					}
					writer.WriteLine();
					writer.WriteLine($"*iidCount = {num};");
					writer.WriteLine("*iids = interfaceIds;");
					writer.WriteLine("return IL2CPP_S_OK;");
				}
				else
				{
					writer.WriteLine("return ComObjectBase::GetIids(iidCount, iids);");
				}
			}
		}

		private void WriteGetRuntimeClassNameDefinition(IGeneratedMethodCodeWriter writer)
		{
			writer.WriteLine();
			writer.WriteLine("virtual il2cpp_hresult_t STDCALL GetRuntimeClassName(Il2CppHString* className) IL2CPP_OVERRIDE");
			using (new BlockWriter(writer))
			{
				writer.WriteLine("return GetRuntimeClassNameImpl(className);");
			}
		}

		private void WriteGetTrustLevelDefinition(IGeneratedMethodCodeWriter writer)
		{
			writer.WriteLine();
			writer.WriteLine("virtual il2cpp_hresult_t STDCALL GetTrustLevel(int32_t* trustLevel) IL2CPP_OVERRIDE");
			using (new BlockWriter(writer))
			{
				writer.WriteLine("return ComObjectBase::GetTrustLevel(trustLevel);");
			}
		}
	}
}
