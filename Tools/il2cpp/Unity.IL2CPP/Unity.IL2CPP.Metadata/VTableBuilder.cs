using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.Cecil.Awesome.Comparers;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Collectors;
using Unity.IL2CPP.Contexts.Components.Base;
using Unity.IL2CPP.Contexts.Forking.Steps;
using Unity.IL2CPP.GenericSharing;

namespace Unity.IL2CPP.Metadata
{
	public class VTableBuilder : StatefulComponentBase<IVTableBuilder, object, VTableBuilder>, IVTableBuilder
	{
		private class NotAvailable : IVTableBuilder
		{
			public int IndexFor(ReadOnlyContext context, MethodDefinition method)
			{
				throw new NotSupportedException();
			}

			public VTable VTableFor(ReadOnlyContext context, TypeReference typeReference)
			{
				throw new NotSupportedException();
			}

			public MethodReference GetVirtualMethodTargetMethodForConstrainedCallOnValueType(ReadOnlyContext context, TypeReference type, MethodReference method)
			{
				throw new NotSupportedException();
			}
		}

		private readonly Dictionary<MethodReference, int> _methodSlots;

		private readonly Dictionary<TypeReference, VTable> _vtables;

		public const int InvalidMethodSlot = 65535;

		public VTableBuilder()
		{
			_methodSlots = new Dictionary<MethodReference, int>(new MethodReferenceComparer());
			_vtables = new Dictionary<TypeReference, VTable>(new TypeReferenceEqualityComparer());
		}

		private VTableBuilder(Dictionary<MethodReference, int> methodSlots, Dictionary<TypeReference, VTable> vtables)
		{
			_methodSlots = new Dictionary<MethodReference, int>(methodSlots, new MethodReferenceComparer());
			_vtables = new Dictionary<TypeReference, VTable>(vtables, new TypeReferenceEqualityComparer());
		}

		public int IndexFor(ReadOnlyContext context, MethodDefinition method)
		{
			if (method.DeclaringType.IsInterface)
			{
				SetupMethodSlotsForInterface(method.DeclaringType);
				return GetSlot(method);
			}
			VTableFor(context, method.DeclaringType);
			if (!method.IsVirtual)
			{
				return 65535;
			}
			return _methodSlots[method];
		}

		private int GetSlot(MethodReference method)
		{
			return _methodSlots[method];
		}

		private void SetSlot(MethodReference method, int slot)
		{
			_methodSlots[method] = slot;
		}

		public VTable VTableFor(ReadOnlyContext context, TypeReference typeReference)
		{
			if (_vtables.TryGetValue(typeReference, out var value))
			{
				return value;
			}
			if (typeReference.IsArray)
			{
				throw new InvalidOperationException("Calculating vtable for arrays is not supported.");
			}
			TypeDefinition typeDefinition = typeReference.Resolve();
			if (typeDefinition.IsInterface && !typeDefinition.IsComOrWindowsRuntimeInterface(context) && context.Global.Services.WindowsRuntime.GetNativeToManagedAdapterClassFor(typeDefinition) == null)
			{
				throw new InvalidOperationException("Calculating vtable for non-COM interface is not supported.");
			}
			int currentSlot = ((typeDefinition.BaseType != null) ? VTableFor(context, typeReference.GetBaseType(context)).Slots.Count : 0);
			Dictionary<TypeReference, int> dictionary = SetupInterfaceOffsets(context, typeReference, ref currentSlot);
			if (!(typeReference is GenericInstanceType genericInstanceType))
			{
				return VTableForType(context, typeDefinition, dictionary, currentSlot);
			}
			return VTableForGenericInstance(context, genericInstanceType, dictionary);
		}

		private static int VirtualMethodCount(TypeReference type)
		{
			return type.Resolve().Methods.Count((MethodDefinition m) => m.IsVirtual && !m.IsStripped());
		}

		private Dictionary<TypeReference, int> SetupInterfaceOffsets(ReadOnlyContext context, TypeReference type, ref int currentSlot)
		{
			Dictionary<TypeReference, int> dictionary = new Dictionary<TypeReference, int>(new TypeReferenceEqualityComparer());
			if (type.IsInterface())
			{
				TypeDefinition typeDefinition = type.Resolve();
				if (typeDefinition.Module == context.Global.Services.TypeProvider.Corlib.MainModule && typeDefinition.Namespace == "System.Collections.Generic" && typeDefinition.Name == "IEnumerable`1")
				{
					foreach (InterfaceImplementation @interface in typeDefinition.Interfaces)
					{
						TypeReference interfaceType = @interface.InterfaceType;
						if (interfaceType.Namespace == "System.Collections" && interfaceType.Name == "IEnumerable")
						{
							int num = VirtualMethodCount(type);
							dictionary.Add(interfaceType, num);
							currentSlot = num + 1;
							return dictionary;
						}
					}
					return dictionary;
				}
				return dictionary;
			}
			for (TypeReference baseType = type.GetBaseType(context); baseType != null; baseType = baseType.GetBaseType(context))
			{
				VTable vTable = VTableFor(context, baseType);
				foreach (TypeReference interface2 in baseType.GetInterfaces(context))
				{
					SetupMethodSlotsForInterface(interface2);
					int num3 = (dictionary[interface2] = vTable.InterfaceOffsets[interface2]);
				}
			}
			foreach (TypeReference interface3 in type.GetInterfaces(context))
			{
				if (!dictionary.ContainsKey(interface3))
				{
					SetupMethodSlotsForInterface(interface3);
					dictionary.Add(interface3, currentSlot);
					currentSlot += VirtualMethodCount(interface3);
				}
			}
			return dictionary;
		}

		private void SetupMethodSlotsForInterface(TypeReference typeReference)
		{
			if (!typeReference.Resolve().IsInterface)
			{
				throw new Exception();
			}
			int num = 0;
			foreach (MethodDefinition item in typeReference.Resolve().Methods.Where((MethodDefinition m) => m.IsVirtual && !m.IsStatic && !m.IsStripped()))
			{
				SetSlot(TypeResolver.For(typeReference).Resolve(item), num++);
			}
		}

		private VTable VTableForGenericInstance(ReadOnlyContext context, GenericInstanceType genericInstanceType, Dictionary<TypeReference, int> offsets)
		{
			TypeDefinition typeDefinition = genericInstanceType.Resolve();
			List<MethodReference> list = new List<MethodReference>(VTableFor(context, typeDefinition).Slots);
			TypeResolver typeResolver = new TypeResolver(genericInstanceType);
			for (int i = 0; i < list.Count; i++)
			{
				MethodReference methodReference = list[i];
				if (methodReference != null)
				{
					MethodReference method = (list[i] = typeResolver.Resolve(methodReference));
					SetSlot(method, GetSlot(methodReference));
				}
			}
			for (int j = 0; j < typeDefinition.Methods.Count; j++)
			{
				MethodDefinition methodDefinition = typeDefinition.Methods[j];
				if (methodDefinition.IsVirtual)
				{
					MethodReference methodReference3 = typeResolver.Resolve(methodDefinition);
					if (!_methodSlots.ContainsKey(methodReference3))
					{
						int slot = GetSlot(methodDefinition);
						SetSlot(methodReference3, slot);
					}
				}
			}
			VTable vTable = new VTable(list.AsReadOnly(), offsets);
			_vtables[genericInstanceType] = vTable;
			return vTable;
		}

		private VTable VTableForType(ReadOnlyContext context, TypeDefinition typeDefinition, Dictionary<TypeReference, int> interfaceOffsets, int currentSlot)
		{
			TypeReference baseType = typeDefinition.BaseType;
			List<MethodReference> list = ((baseType != null) ? new List<MethodReference>(VTableFor(context, baseType).Slots) : new List<MethodReference>());
			if (currentSlot > list.Count)
			{
				list.AddRange(new MethodReference[currentSlot - list.Count]);
			}
			Dictionary<MethodReference, MethodDefinition> overrides = CollectOverrides(typeDefinition);
			Dictionary<MethodReference, MethodReference> overrideMap = new Dictionary<MethodReference, MethodReference>(new MethodReferenceComparer());
			if (!typeDefinition.IsInterface)
			{
				OverrideInterfaceMethods(interfaceOffsets, list, overrides, overrideMap);
			}
			SetupInterfaceMethods(context, typeDefinition, interfaceOffsets, overrideMap, list);
			ValidateInterfaceMethodSlots(typeDefinition, interfaceOffsets, list);
			SetupClassMethods(context, list, typeDefinition, overrideMap);
			OverriddenonInterfaceMethods(overrides, list, overrideMap);
			ReplaceOverriddenMethods(overrideMap, list);
			ValidateAllMethodSlots(context, typeDefinition, list);
			VTable vTable = new VTable(list.AsReadOnly(), interfaceOffsets);
			_vtables[typeDefinition] = vTable;
			return vTable;
		}

		private static Dictionary<MethodReference, MethodDefinition> CollectOverrides(TypeDefinition typeDefinition)
		{
			Dictionary<MethodReference, MethodDefinition> dictionary = new Dictionary<MethodReference, MethodDefinition>(new MethodReferenceComparer());
			foreach (MethodDefinition item in typeDefinition.Methods.Where((MethodDefinition m) => m.HasOverrides))
			{
				foreach (MethodReference @override in item.Overrides)
				{
					dictionary.Add(@override, item);
				}
			}
			return dictionary;
		}

		private void OverrideInterfaceMethods(Dictionary<TypeReference, int> interfaceOffsets, List<MethodReference> slots, Dictionary<MethodReference, MethodDefinition> overrides, Dictionary<MethodReference, MethodReference> overrideMap)
		{
			foreach (KeyValuePair<MethodReference, MethodDefinition> @override in overrides)
			{
				MethodReference key = @override.Key;
				if (key.DeclaringType.Resolve().IsInterface)
				{
					int slot = GetSlot(key);
					slot += interfaceOffsets[@override.Key.DeclaringType];
					slots[slot] = @override.Value;
					SetSlot(@override.Value, slot);
					overrideMap.Add(@override.Key, @override.Value);
				}
			}
		}

		private void SetupInterfaceMethods(ReadOnlyContext context, TypeDefinition typeDefinition, Dictionary<TypeReference, int> interfaceOffsets, Dictionary<MethodReference, MethodReference> overrideMap, List<MethodReference> slots)
		{
			foreach (KeyValuePair<TypeReference, int> interfaceOffset in interfaceOffsets)
			{
				TypeReference itf = interfaceOffset.Key;
				int value = interfaceOffset.Value;
				TypeDefinition typeDefinition2 = itf.Resolve();
				SetupMethodSlotsForInterface(itf);
				bool interfaceIsExplicitlyImplementedByClass = InterfaceIsExplicitlyImplementedByClass(typeDefinition, itf);
				foreach (MethodReference item in from m in typeDefinition2.Methods
					where !m.IsStatic && !m.IsStripped()
					select TypeResolver.For(itf).Resolve(m))
				{
					int num = value + GetSlot(item);
					MethodReference value2;
					if (typeDefinition.IsInterface)
					{
						MethodDefinition itfMethodDef = item.Resolve();
						TypeDefinition nativeToManagedAdapterClassFor = context.Global.Services.WindowsRuntime.GetNativeToManagedAdapterClassFor(typeDefinition);
						slots[num] = nativeToManagedAdapterClassFor.Methods.First((MethodDefinition m) => m.Overrides.Any((MethodReference o) => o.Resolve() == itfMethodDef));
					}
					else if (!overrideMap.TryGetValue(item, out value2))
					{
						foreach (MethodReference virtualMethod in typeDefinition.GetVirtualMethods())
						{
							if (CheckInterfaceMethodOverride(item, virtualMethod, requireNewslot: true, interfaceIsExplicitlyImplementedByClass, slots[num] == null))
							{
								slots[num] = virtualMethod;
								SetSlot(virtualMethod, num);
							}
						}
						if (slots[num] != null || typeDefinition.BaseType == null)
						{
							continue;
						}
						VTable vTable = VTableFor(context, typeDefinition.BaseType);
						for (int num2 = vTable.Slots.Count - 1; num2 >= 0; num2--)
						{
							MethodReference methodReference = vTable.Slots[num2];
							if (methodReference != null && CheckInterfaceMethodOverride(item, methodReference, requireNewslot: false, interfaceIsExplicitlyImplementedByClass: false, slotIsEmpty: true))
							{
								slots[num] = methodReference;
								if (!_methodSlots.ContainsKey(methodReference))
								{
									SetSlot(methodReference, num);
								}
							}
						}
					}
					else if (slots[num] != value2)
					{
						throw new Exception();
					}
				}
			}
		}

		private void ValidateInterfaceMethodSlots(TypeDefinition typeDefinition, Dictionary<TypeReference, int> interfaceOffsets, List<MethodReference> slots)
		{
			if (typeDefinition.IsAbstract)
			{
				return;
			}
			foreach (KeyValuePair<TypeReference, int> interfaceOffset in interfaceOffsets)
			{
				TypeReference key = interfaceOffset.Key;
				int value = interfaceOffset.Value;
				TypeResolver @object = TypeResolver.For(key);
				foreach (MethodReference item in key.Resolve().Methods.Where((MethodDefinition m) => !m.IsStatic && !m.IsStripped()).Select(@object.Resolve))
				{
					int index = value + GetSlot(item);
					if (slots[index] == null)
					{
						throw new Exception($"Interface {key.FullName} method {item.FullName} not implemented on non-abstract class {typeDefinition.FullName}");
					}
				}
			}
		}

		private void SetupClassMethods(ReadOnlyContext context, List<MethodReference> slots, TypeDefinition typeDefinition, Dictionary<MethodReference, MethodReference> overrideMap)
		{
			int num = 0;
			foreach (MethodDefinition method in typeDefinition.Methods.Where((MethodDefinition m) => m.IsVirtual && !m.IsStripped()))
			{
				if (!method.IsNewSlot)
				{
					int num2 = -1;
					for (TypeReference baseType = typeDefinition.GetBaseType(context); baseType != null; baseType = baseType.GetBaseType(context))
					{
						foreach (MethodReference virtualMethod in baseType.GetVirtualMethods())
						{
							if (!(method.Name != virtualMethod.Name) && VirtualMethodResolution.MethodSignaturesMatch(method, virtualMethod))
							{
								num2 = GetSlot(virtualMethod);
								overrideMap.Add(virtualMethod, method);
								break;
							}
						}
						if (num2 >= 0)
						{
							break;
						}
					}
					if (num2 >= 0)
					{
						SetSlot(method, num2);
					}
				}
				if (method.IsNewSlot && !method.IsFinal && _methodSlots.ContainsKey(method))
				{
					_methodSlots.Remove(method);
				}
				if (!_methodSlots.ContainsKey(method))
				{
					if (typeDefinition.IsInterface)
					{
						if (slots.Count == num)
						{
							slots.Add(null);
						}
						SetSlot(method, num++);
					}
					else
					{
						int count = slots.Count;
						slots.Add(null);
						SetSlot(method, count);
					}
				}
				int slot = GetSlot(method);
				if (!method.IsAbstract || typeDefinition.IsComOrWindowsRuntimeInterface(context))
				{
					slots[slot] = method;
				}
				else if (typeDefinition.IsInterface)
				{
					TypeDefinition nativeToManagedAdapterClassFor = context.Global.Services.WindowsRuntime.GetNativeToManagedAdapterClassFor(typeDefinition);
					slots[slot] = nativeToManagedAdapterClassFor.Methods.First((MethodDefinition m) => m.Overrides.Any((MethodReference o) => o.Resolve() == method));
				}
				else
				{
					slots[slot] = null;
				}
			}
		}

		private void OverriddenonInterfaceMethods(Dictionary<MethodReference, MethodDefinition> overrides, List<MethodReference> slots, Dictionary<MethodReference, MethodReference> overrideMap)
		{
			foreach (KeyValuePair<MethodReference, MethodDefinition> @override in overrides)
			{
				MethodReference key = @override.Key;
				MethodDefinition value = @override.Value;
				TypeReference declaringType = key.DeclaringType;
				if (declaringType.Resolve().IsInterface)
				{
					continue;
				}
				int slot = GetSlot(key);
				slots[slot] = value;
				SetSlot(value, slot);
				overrideMap.TryGetValue(key, out var value2);
				if (value2 != null)
				{
					if (!MethodReferenceComparer.AreEqual(value2, value))
					{
						throw new InvalidOperationException($"Error while creating VTable for {declaringType}. The base method {key} is implemented both by {value2} and {value}.");
					}
				}
				else
				{
					overrideMap.Add(key, value);
				}
			}
		}

		private static void ReplaceOverriddenMethods(Dictionary<MethodReference, MethodReference> overrideMap, List<MethodReference> slots)
		{
			if (overrideMap.Count <= 0)
			{
				return;
			}
			for (int i = 0; i < slots.Count; i++)
			{
				if (slots[i] != null && overrideMap.TryGetValue(slots[i], out var value))
				{
					slots[i] = value;
				}
			}
		}

		private static void ValidateAllMethodSlots(ReadOnlyContext context, TypeDefinition typeDefinition, IEnumerable<MethodReference> slots)
		{
			if (typeDefinition.IsAbstract)
			{
				return;
			}
			foreach (MethodReference slot in slots)
			{
				if (slot != null)
				{
					MethodDefinition methodDefinition = slot.Resolve();
					if (!methodDefinition.IsStatic && (!methodDefinition.IsAbstract || slot.DeclaringType.IsComOrWindowsRuntimeInterface(context)))
					{
						continue;
					}
				}
				throw new Exception(string.Format("Invalid method '{0}' found in vtable for '{1}'", (slot == null) ? "null" : slot.FullName, typeDefinition.FullName));
			}
		}

		private static bool InterfaceIsExplicitlyImplementedByClass(TypeDefinition typeDefinition, TypeReference itf)
		{
			if (typeDefinition.BaseType != null)
			{
				return typeDefinition.Interfaces.Any((InterfaceImplementation classItf) => TypeReferenceEqualityComparer.AreEqual(itf, classItf.InterfaceType));
			}
			return true;
		}

		private static bool CheckInterfaceMethodOverride(MethodReference itfMethod, MethodReference virtualMethod, bool requireNewslot, bool interfaceIsExplicitlyImplementedByClass, bool slotIsEmpty)
		{
			if (itfMethod.Name == virtualMethod.Name)
			{
				if (!virtualMethod.Resolve().IsPublic)
				{
					return false;
				}
				if (!slotIsEmpty && requireNewslot)
				{
					if (!interfaceIsExplicitlyImplementedByClass)
					{
						return false;
					}
					if (!virtualMethod.Resolve().IsNewSlot)
					{
						return false;
					}
				}
				return VirtualMethodResolution.MethodSignaturesMatch(itfMethod, virtualMethod);
			}
			return false;
		}

		internal static MethodReference CloneMethodReference(GenericInstanceType genericInstanceType, MethodReference method)
		{
			MethodReference methodReference = new MethodReference(method.Name, method.ReturnType, genericInstanceType)
			{
				HasThis = method.HasThis,
				ExplicitThis = method.ExplicitThis,
				CallingConvention = method.CallingConvention
			};
			foreach (ParameterDefinition parameter in method.Parameters)
			{
				methodReference.Parameters.Add(new ParameterDefinition(parameter.Name, parameter.Attributes, parameter.ParameterType));
			}
			foreach (GenericParameter genericParameter in method.GenericParameters)
			{
				methodReference.GenericParameters.Add(new GenericParameter(genericParameter.Name, methodReference));
			}
			return methodReference;
		}

		public MethodReference GetVirtualMethodTargetMethodForConstrainedCallOnValueType(ReadOnlyContext context, TypeReference type, MethodReference method)
		{
			MethodDefinition methodDefinition = method.Resolve();
			if (!methodDefinition.IsVirtual)
			{
				return method;
			}
			if (type.IsPointer)
			{
				return null;
			}
			int num = IndexFor(context, methodDefinition);
			VTable vTable = VTableFor(context, type);
			if (method.DeclaringType.IsInterface())
			{
				if (vTable.InterfaceOffsets.TryGetValue(method.DeclaringType, out var value))
				{
					return vTable.Slots[value + num];
				}
				int hashCodeFor = TypeReferenceEqualityComparer.GetHashCodeFor(method.DeclaringType);
				foreach (KeyValuePair<TypeReference, int> interfaceOffset in vTable.InterfaceOffsets)
				{
					if (interfaceOffset.Key.IsGenericInstance)
					{
						GenericInstanceType sharedType = GenericSharingAnalysis.GetSharedType(context, interfaceOffset.Key);
						if (hashCodeFor == TypeReferenceEqualityComparer.GetHashCodeFor(sharedType) && TypeReferenceEqualityComparer.AreEqual(method.DeclaringType, sharedType))
						{
							return vTable.Slots[interfaceOffset.Value + num];
						}
					}
				}
				return null;
			}
			if (num >= vTable.Slots.Count)
			{
				return null;
			}
			MethodReference methodReference = vTable.Slots[num];
			if (methodReference.Name != methodDefinition.Name)
			{
				return null;
			}
			return methodReference;
		}

		protected override void DumpState(StringBuilder builder)
		{
			builder.AppendLine("-------MethodSlots-------");
			foreach (KeyValuePair<MethodReference, int> item in _methodSlots.ToSortedCollectionBy((KeyValuePair<MethodReference, int> i) => i.Key))
			{
				builder.AppendLine(item.Key.FullName);
				builder.AppendLine($"  Slot = {item.Value}");
			}
			builder.AppendLine("-------VTables-------");
			foreach (KeyValuePair<TypeReference, VTable> item2 in _vtables.ToSortedCollectionBy((KeyValuePair<TypeReference, VTable> i) => i.Key))
			{
				builder.AppendLine(item2.Key.FullName);
				if (item2.Value.Slots == null)
				{
					builder.AppendLine("  Slots: null");
				}
				else
				{
					builder.AppendLine("  Slots:\n" + item2.Value.Slots.Select((MethodReference m) => (m != null) ? ("    " + m.FullName) : "    null").AggregateWithNewLine());
				}
				if (item2.Value.InterfaceOffsets == null)
				{
					builder.AppendLine("  InterfaceOffsets: null");
					continue;
				}
				if (item2.Value.InterfaceOffsets.Count == 0)
				{
					builder.AppendLine("  InterfaceOffsets: Empty");
					continue;
				}
				builder.AppendLine("  InterfaceOffsets:");
				foreach (KeyValuePair<TypeReference, int> item3 in item2.Value.InterfaceOffsets.ToSortedCollectionBy((KeyValuePair<TypeReference, int> i) => i.Key))
				{
					builder.AppendLine("    " + item3.Key.FullName);
					builder.AppendLine($"      Offset = {item3.Value}");
				}
			}
		}

		protected override void HandleMergeForAdd(VTableBuilder forked)
		{
			foreach (KeyValuePair<MethodReference, int> methodSlot in forked._methodSlots)
			{
				SetSlot(methodSlot.Key, methodSlot.Value);
			}
			foreach (KeyValuePair<TypeReference, VTable> vtable in forked._vtables)
			{
				_vtables[vtable.Key] = vtable.Value;
			}
		}

		protected override void HandleMergeForMergeValues(VTableBuilder forked)
		{
			throw new NotSupportedException();
		}

		protected override VTableBuilder CreateEmptyInstance()
		{
			return new VTableBuilder();
		}

		protected override VTableBuilder CreateCopyInstance()
		{
			return new VTableBuilder(_methodSlots, _vtables);
		}

		protected override VTableBuilder ThisAsFull()
		{
			return this;
		}

		protected override object ThisAsRead()
		{
			throw new NotSupportedException();
		}

		protected override IVTableBuilder GetNotAvailableWrite()
		{
			return new NotAvailable();
		}

		protected override object GetNotAvailableRead()
		{
			throw new NotSupportedException();
		}

		protected override void ForkForSecondaryCollection(SecondaryCollectionLateAccessForkingContainer lateAccess, out IVTableBuilder writer, out object reader, out VTableBuilder full)
		{
			((ComponentBase<IVTableBuilder, object, VTableBuilder>)this).WriteOnlyFork(out writer, out reader, out full, ForkMode.Copy, MergeMode.None);
		}

		protected override void ForkForPrimaryWrite(PrimaryWriteAssembliesLateAccessForkingContainer lateAccess, out IVTableBuilder writer, out object reader, out VTableBuilder full)
		{
			((ComponentBase<IVTableBuilder, object, VTableBuilder>)this).WriteOnlyFork(out writer, out reader, out full, ForkMode.Copy, MergeMode.Add);
		}

		protected override void ForkForPrimaryCollection(PrimaryCollectionLateAccessForkingContainer lateAccess, out IVTableBuilder writer, out object reader, out VTableBuilder full)
		{
			((ComponentBase<IVTableBuilder, object, VTableBuilder>)this).WriteOnlyFork(out writer, out reader, out full, ForkMode.Copy, MergeMode.Add);
		}

		protected override void ForkForSecondaryWrite(SecondaryWriteLateAccessForkingContainer lateAccess, out IVTableBuilder writer, out object reader, out VTableBuilder full)
		{
			NotAvailableFork(out writer, out reader, out full);
		}
	}
}
