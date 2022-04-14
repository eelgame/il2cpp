using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Unity.IL2CPP.Tiny
{
	internal static class TinyVirtualRemap
	{
		private static readonly ReadOnlyCollection<ITinyVirtualRemapHandler> _virtualRemapHandlers;

		static TinyVirtualRemap()
		{
			_virtualRemapHandlers = new List<ITinyVirtualRemapHandler>
			{
				new VirtualRemapEnumEquals()
			}.AsReadOnly();
		}

		public static bool ShouldRemap(TinyVirtualMethodData virtualMethodData)
		{
			return HandlerFor(virtualMethodData)?.ShouldRemapVirtualMethod(virtualMethodData) ?? false;
		}

		public static string RemappedMethodNameFor(TinyVirtualMethodData virtualMethodData, bool returnAsByRefParameter)
		{
			ITinyVirtualRemapHandler tinyVirtualRemapHandler = HandlerFor(virtualMethodData);
			if (tinyVirtualRemapHandler != null)
			{
				return tinyVirtualRemapHandler.RemappedMethodNameFor(virtualMethodData, returnAsByRefParameter);
			}
			throw new InvalidOperationException($"No virtual remap handler found for {virtualMethodData.VirtualMethod}. Call ShouldRemap first.");
		}

		private static ITinyVirtualRemapHandler HandlerFor(TinyVirtualMethodData virtualMethodData)
		{
			return _virtualRemapHandlers.SingleOrDefault((ITinyVirtualRemapHandler h) => h.ShouldRemapVirtualMethod(virtualMethodData));
		}
	}
}
