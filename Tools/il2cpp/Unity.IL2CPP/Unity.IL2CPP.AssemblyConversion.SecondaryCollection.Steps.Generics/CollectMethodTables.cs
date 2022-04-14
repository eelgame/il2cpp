using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Unity.Cecil.Awesome.Comparers;
using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Metadata;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.AssemblyConversion.SecondaryCollection.Steps.Generics
{
	public class CollectMethodTables : SimpleScheduledStepFunc<GlobalSecondaryCollectionContext, ReadOnlyMethodTables>
	{
		public class GenericMethodPointerTableEntry
		{
			public readonly int Index;

			public readonly bool IsNull;

			private readonly MethodReference _method;

			private readonly bool _isSharedMethod;

			public GenericMethodPointerTableEntry(int index, MethodReference method, bool isNull, bool isSharedMethod)
			{
				Index = index;
				IsNull = isNull;
				_method = method;
				_isSharedMethod = isSharedMethod;
			}

			public string Name(ReadOnlyContext context)
			{
				if (IsNull)
				{
					return "NULL";
				}
				if (_isSharedMethod)
				{
					return context.Global.Services.Naming.ForMethod(_method) + "_gshared";
				}
				return context.Global.Services.Naming.ForMethod(_method);
			}
		}

		public class GenericMethodTableEntry
		{
			public readonly int PointerTableIndex;

			public readonly int AdjustorThunkTableIndex;

			public readonly uint TableIndex;

			public readonly Il2CppMethodSpec Method;

			public GenericMethodTableEntry(Il2CppMethodSpec method, int pointerTableIndex, int adjustorThunkTableIndex, uint tableIndex)
			{
				if (method == null)
				{
					throw new ArgumentNullException("method");
				}
				PointerTableIndex = pointerTableIndex;
				AdjustorThunkTableIndex = adjustorThunkTableIndex;
				TableIndex = tableIndex;
				Method = method;
			}
		}

		public class GenericMethodAdjustorThunkTableEntry
		{
			public readonly int Index;

			private readonly MethodReference _method;

			public GenericMethodAdjustorThunkTableEntry(int index, MethodReference method)
			{
				Index = index;
				_method = method;
			}

			public string Name(ReadOnlyContext context)
			{
				return context.Global.Services.Naming.ForMethodAdjustorThunk(_method);
			}
		}

		public class TableData
		{
			private readonly Il2CppMethodSpec _genericMethodTableMethod;

			private readonly uint _genericMethodTableMethodIndex;

			private readonly bool _isNull;

			private readonly MethodReference _genericMethodPointerTableMethod;

			private readonly bool _hasAdjustorThunk;

			private readonly bool _isSharedMethod;

			private readonly int _hashCode;

			public bool NeedsAdjustorThunkTableEntry => _hasAdjustorThunk;

			private TableData(Il2CppMethodSpec genericMethodTableMethod, uint genericMethodTableMethodIndex)
			{
				_genericMethodTableMethod = genericMethodTableMethod;
				_genericMethodTableMethodIndex = genericMethodTableMethodIndex;
				_isNull = true;
				_hashCode = 0;
			}

			public TableData(Il2CppMethodSpec genericMethodTableMethod, MethodReference genericMethodPointerTableMethod, bool isSharedMethod, bool hasAdjustorThunk, uint genericMethodTableMethodIndex)
			{
				_genericMethodTableMethod = genericMethodTableMethod;
				_genericMethodTableMethodIndex = genericMethodTableMethodIndex;
				_genericMethodPointerTableMethod = genericMethodPointerTableMethod;
				_hasAdjustorThunk = hasAdjustorThunk;
				_isSharedMethod = isSharedMethod;
				_hashCode = MethodReferenceComparer.GetHashCodeFor(_genericMethodPointerTableMethod);
			}

			public static TableData CreateNull(Il2CppMethodSpec method, uint entryMethodIndex)
			{
				return new TableData(method, entryMethodIndex);
			}

			public GenericMethodPointerTableEntry CreateNullPointerTableEntry()
			{
				return new GenericMethodPointerTableEntry(0, null, isNull: true, isSharedMethod: false);
			}

			public GenericMethodPointerTableEntry CreatePointerTableEntry(int pointerTableIndex)
			{
				return new GenericMethodPointerTableEntry(pointerTableIndex, _genericMethodPointerTableMethod, _isNull, _isSharedMethod);
			}

			public GenericMethodAdjustorThunkTableEntry CreateAdjustorThunkTableEntry(int tableIndex)
			{
				return new GenericMethodAdjustorThunkTableEntry(tableIndex, _genericMethodPointerTableMethod);
			}

			public GenericMethodTableEntry CreateMethodTableEntry(int pointerTableIndex, int adjustorThunkTableIndex)
			{
				return new GenericMethodTableEntry(_genericMethodTableMethod, pointerTableIndex, adjustorThunkTableIndex, _genericMethodTableMethodIndex);
			}

			public override bool Equals(object obj)
			{
				if (!(obj is TableData tableData))
				{
					return false;
				}
				if (_isNull != tableData._isNull)
				{
					return false;
				}
				if (_isNull && tableData._isNull)
				{
					return true;
				}
				if (_hasAdjustorThunk != tableData._hasAdjustorThunk)
				{
					return false;
				}
				if (_isSharedMethod != tableData._isSharedMethod)
				{
					return false;
				}
				return MethodReferenceComparer.AreEqual(_genericMethodPointerTableMethod, tableData._genericMethodPointerTableMethod);
			}

			public override int GetHashCode()
			{
				return _hashCode;
			}
		}

		protected override string Name => "Collect Method Tables";

		protected override bool Skip(GlobalSecondaryCollectionContext context)
		{
			return context.Parameters.UsingTinyBackend;
		}

		protected override ReadOnlyMethodTables CreateEmptyResult()
		{
			return null;
		}

		protected override ReadOnlyMethodTables Worker(GlobalSecondaryCollectionContext context)
		{
			Dictionary<TableData, GenericMethodPointerTableEntry> dictionary = new Dictionary<TableData, GenericMethodPointerTableEntry>();
			List<GenericMethodPointerTableEntry> list = new List<GenericMethodPointerTableEntry>();
			List<GenericMethodAdjustorThunkTableEntry> list2 = new List<GenericMethodAdjustorThunkTableEntry>();
			List<GenericMethodTableEntry> list3 = new List<GenericMethodTableEntry>();
			TableData tableData = TableData.CreateNull(null, 0u);
			GenericMethodPointerTableEntry genericMethodPointerTableEntry = tableData.CreateNullPointerTableEntry();
			dictionary.Add(tableData, genericMethodPointerTableEntry);
			list.Add(genericMethodPointerTableEntry);
			ReadOnlyContext readOnlyContext = context.GetReadOnlyContext();
			foreach (Il2CppMethodSpec item in context.Results.PrimaryWrite.GenericMethods.SortedKeys.Where(MethodTables.MethodNeedsTable))
			{
				TableData tableData2 = MethodPointerKeyFor(readOnlyContext, item);
				GenericMethodPointerTableEntry genericMethodPointerTableEntry2 = ProcessForMethodPointerTable(dictionary, list, tableData2);
				GenericMethodAdjustorThunkTableEntry genericMethodAdjustorThunkTableEntry = ProcessForAdjustorThunk(list2, tableData2);
				list3.Add(tableData2.CreateMethodTableEntry(genericMethodPointerTableEntry2.Index, genericMethodAdjustorThunkTableEntry?.Index ?? (-1)));
			}
			return new ReadOnlyMethodTables(list, list2, list3);
		}

		private static GenericMethodPointerTableEntry ProcessForMethodPointerTable(Dictionary<TableData, GenericMethodPointerTableEntry> table, List<GenericMethodPointerTableEntry> orderedValues, TableData item)
		{
			if (table.TryGetValue(item, out var value))
			{
				return value;
			}
			GenericMethodPointerTableEntry genericMethodPointerTableEntry = item.CreatePointerTableEntry(table.Count);
			table.Add(item, genericMethodPointerTableEntry);
			orderedValues.Add(genericMethodPointerTableEntry);
			return genericMethodPointerTableEntry;
		}

		private static GenericMethodAdjustorThunkTableEntry ProcessForAdjustorThunk(List<GenericMethodAdjustorThunkTableEntry> orderedValues, TableData item)
		{
			if (!item.NeedsAdjustorThunkTableEntry)
			{
				return null;
			}
			GenericMethodAdjustorThunkTableEntry genericMethodAdjustorThunkTableEntry = item.CreateAdjustorThunkTableEntry(orderedValues.Count);
			orderedValues.Add(genericMethodAdjustorThunkTableEntry);
			return genericMethodAdjustorThunkTableEntry;
		}

		private static TableData MethodPointerKeyFor(ReadOnlyContext context, Il2CppMethodSpec method)
		{
			uint genericMethodIndex = context.Global.Results.PrimaryWrite.GenericMethods.GetIndex(method);
			return MethodTables.MethodPointerDataFor(context, method.GenericMethod, () => TableData.CreateNull(method, genericMethodIndex), (MethodReference originalOrSharedMethod, bool isSharedMethod) => new TableData(method, originalOrSharedMethod, isSharedMethod, MethodWriter.HasAdjustorThunk(originalOrSharedMethod), genericMethodIndex));
		}
	}
}
