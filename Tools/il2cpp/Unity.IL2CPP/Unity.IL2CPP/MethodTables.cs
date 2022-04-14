using System;
using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.GenericSharing;
using Unity.IL2CPP.Metadata;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP
{
	public sealed class MethodTables
	{
		public static bool MethodNeedsTable(Il2CppMethodSpec method)
		{
			if (!method.GenericMethod.HasGenericParameters && !method.GenericMethod.DeclaringType.HasGenericParameters)
			{
				return !method.GenericMethod.ContainsGenericParameters();
			}
			return false;
		}

		internal static string MethodPointerNameFor(ReadOnlyContext context, MethodReference method)
		{
			return MethodPointerDataFor(context, method, () => "NULL", delegate(MethodReference originalOrSharedMethod, bool isSharedMethod)
			{
				string text = context.Global.Services.Naming.ForMethod(method);
				return (!isSharedMethod) ? text : (text + "_gshared");
			});
		}

		internal static TResult MethodPointerDataFor<TResult>(ReadOnlyContext context, MethodReference method, Func<TResult> createNullResult, Func<MethodReference, bool, TResult> createResult)
		{
			if (MethodWriter.IsGetOrSetGenericValueImplOnArray(method))
			{
				return createNullResult();
			}
			if (GenericsUtilities.IsGenericInstanceOfCompareExchange(method))
			{
				return createNullResult();
			}
			if (GenericsUtilities.IsGenericInstanceOfExchange(method))
			{
				return createNullResult();
			}
			if (!MethodWriter.MethodCanBeDirectlyCalled(context, method))
			{
				return createNullResult();
			}
			if (GenericSharingAnalysis.CanShareMethod(context, method))
			{
				return createResult(GenericSharingAnalysis.GetSharedMethod(context, method), arg2: true);
			}
			return createResult(method, arg2: false);
		}

		internal static string AdjustorThunkNameFor(ReadOnlyContext context, MethodReference method)
		{
			if (GenericSharingAnalysis.CanShareMethod(context, method))
			{
				method = GenericSharingAnalysis.GetSharedMethod(context, method);
				if (MethodWriter.HasAdjustorThunk(method))
				{
					return context.Global.Services.Naming.ForMethodAdjustorThunk(method);
				}
				return "NULL";
			}
			if (MethodWriter.HasAdjustorThunk(method))
			{
				return context.Global.Services.Naming.ForMethodAdjustorThunk(method);
			}
			return "NULL";
		}
	}
}
