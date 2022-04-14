namespace Unity.IL2CPP
{
	public interface IFieldReferenceCollector
	{
		uint GetOrCreateIndex(Il2CppRuntimeFieldReference il2CppRuntimeField);
	}
}
