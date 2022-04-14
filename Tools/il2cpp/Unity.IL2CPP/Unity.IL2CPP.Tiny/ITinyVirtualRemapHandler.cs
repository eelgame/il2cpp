namespace Unity.IL2CPP.Tiny
{
	internal interface ITinyVirtualRemapHandler
	{
		bool ShouldRemapVirtualMethod(TinyVirtualMethodData virtualMethodData);

		string RemappedMethodNameFor(TinyVirtualMethodData virtualMethodData, bool returnAsByRefParameter);
	}
}
