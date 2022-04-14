using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NiceIO;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Forking.Providers;
using Unity.IL2CPP.Contexts.Services;

namespace Unity.IL2CPP.Diagnostics
{
	internal class CollectorStateDumper
	{
		private readonly Dictionary<Type, int> _dumpCounters;

		private readonly Dictionary<Type, string> _lastValue;

		public CollectorStateDumper()
		{
			_dumpCounters = new Dictionary<Type, int>();
			_lastValue = new Dictionary<Type, string>();
		}

		public CollectorStateDumper(CollectorStateDumper other)
		{
			_dumpCounters = new Dictionary<Type, int>(other._dumpCounters);
			_lastValue = new Dictionary<Type, string>(other._lastValue);
		}

		public void FastForwardDumpCounters(CollectorStateDumper other)
		{
			foreach (KeyValuePair<Type, int> dumpCounter in other._dumpCounters)
			{
				_dumpCounters[dumpCounter.Key] = dumpCounter.Value;
			}
		}

		public void DumpAll(ReadOnlyContext context, IUnrestrictedContextCollectorProvider collectors, IUnrestrictedContextStatefulServicesProvider statefulServices, string phaseName, NPath outputDirectory)
		{
			foreach (IDumpableState item in GetDumpableObjectsFromContext(collectors, statefulServices))
			{
				DumpCollector(context.Global.Services.PathFactory, item, phaseName, outputDirectory);
			}
		}

		private IEnumerable<IDumpableState> GetDumpableObjectsFromContext(IUnrestrictedContextCollectorProvider collectors, IUnrestrictedContextStatefulServicesProvider statefulServices)
		{
			return GetDumpableObjectsFromType(typeof(IUnrestrictedContextCollectorProvider), collectors).Concat(GetDumpableObjectsFromType(typeof(IUnrestrictedContextStatefulServicesProvider), statefulServices));
		}

		private IEnumerable<IDumpableState> GetDumpableObjectsFromType(Type type, object instance)
		{
			PropertyInfo[] properties = type.GetProperties();
			for (int i = 0; i < properties.Length; i++)
			{
				object obj = properties[i].GetMethod.Invoke(instance, new object[0]);
				if (obj != null && obj is IDumpableState dumpableState)
				{
					yield return dumpableState;
				}
			}
		}

		private void DumpCollector(IPathFactoryService pathFactoryService, IDumpableState obj, string phaseName, NPath outputDirectory)
		{
			Type type = obj.GetType();
			if (!_dumpCounters.TryGetValue(type, out var value))
			{
				value = (_dumpCounters[type] = 0);
			}
			NPath nPath = outputDirectory.Combine(pathFactoryService.GetFileName(obj.GetType().Name + "_" + value.ToString("D2") + "_" + phaseName + ".log"));
			string text = null;
			using (StreamWriter streamWriter = new StreamWriter(nPath.ToString()))
			{
				StringBuilder stringBuilder = new StringBuilder();
				obj.DumpState(stringBuilder);
				text = stringBuilder.ToString();
				bool flag = value != 0 && _lastValue[type] != text;
				streamWriter.WriteLine("---------Meta-----------");
				streamWriter.WriteLine($"Changed From Previous = {flag}");
				streamWriter.WriteLine("------------------------");
				streamWriter.WriteLine(text);
			}
			_dumpCounters[type] += 1;
			_lastValue[type] = text;
		}

		internal static void AppendTable<TKey, TValue>(StringBuilder builder, string tableName, IEnumerable<KeyValuePair<TKey, TValue>> table, Func<TKey, string> keyToString = null, Func<TValue, string> valueToString = null)
		{
			builder.AppendLine("--------------------");
			builder.AppendLine("Table : " + tableName);
			builder.AppendLine("--------------------");
			foreach (KeyValuePair<TKey, TValue> item in table)
			{
				builder.AppendLine((keyToString == null) ? item.Key.ToString() : keyToString(item.Key));
				builder.Append("  Value: ");
				builder.AppendLine((valueToString == null) ? item.Value.ToString() : valueToString(item.Value));
			}
			builder.AppendLine("--------------------");
		}

		internal static void AppendCollection<T>(StringBuilder builder, string tableName, IEnumerable<T> collection, Func<T, string> toString = null)
		{
			builder.AppendLine("--------------------");
			builder.AppendLine("Collection : " + tableName);
			builder.AppendLine("--------------------");
			foreach (T item in collection)
			{
				builder.AppendLine((toString == null) ? item.ToString() : toString(item));
			}
			builder.AppendLine("--------------------");
		}

		internal static void AppendValue(StringBuilder builder, string name, object value)
		{
			string text = ((value == null) ? "null" : value.ToString());
			builder.AppendLine(name + " = " + text);
		}
	}
}
