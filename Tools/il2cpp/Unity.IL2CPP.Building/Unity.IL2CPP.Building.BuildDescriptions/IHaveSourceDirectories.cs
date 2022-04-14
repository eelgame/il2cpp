using System.Collections.Generic;
using NiceIO;

namespace Unity.IL2CPP.Building.BuildDescriptions
{
	internal interface IHaveSourceDirectories
	{
		IEnumerable<NPath> SourceDirectories { get; }
	}
}
