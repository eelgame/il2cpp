using System;
using NiceIO;
using Unity.IL2CPP.Options;

namespace Unity.IL2CPP.AssemblyConversion
{
	public class AssemblyConversionInputDataForTopLevelAccess
	{
		public readonly ConversionMode ConversionMode;

		public readonly Func<string[]> GetMasterToSlavePassThroughArguments;

		public readonly string SlaveEnableAttachMessage;

		public readonly NPath SlaveAssembly;

		public readonly Func<string, object, string> FormatArgumentFromCodeGenOptions;

		public AssemblyConversionInputDataForTopLevelAccess(ConversionMode conversionMode, Func<string[]> getMasterToSlavePassThroughArguments, NPath slaveAssembly, string slaveEnableAttachMessage, Func<string, object, string> formatArgumentFromCodeGenOptions)
		{
			ConversionMode = conversionMode;
			SlaveAssembly = slaveAssembly;
			SlaveEnableAttachMessage = slaveEnableAttachMessage;
			GetMasterToSlavePassThroughArguments = getMasterToSlavePassThroughArguments;
			FormatArgumentFromCodeGenOptions = formatArgumentFromCodeGenOptions;
		}
	}
}
