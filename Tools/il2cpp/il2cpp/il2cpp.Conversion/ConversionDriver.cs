using Unity.IL2CPP.AssemblyConversion;

namespace il2cpp.Conversion
{
	internal static class ConversionDriver
	{
		public static ConversionResults Run()
		{
			return AssemblyConverter.ConvertAssemblies(ContextDataFactory.CreateConversionDataFromOptions(), ContextDataFactory.CreateConversionParametersFromOptions(), ContextDataFactory.CreateTopLevelDataFromOptions());
		}
	}
}
