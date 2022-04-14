using System;
using System.IO;
using System.Linq;
using System.Text;
using Mono.Cecil;
using NiceIO;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.Building.ToolChains
{
	public static class WinRTManifest
	{
		public static void Write(NPath outputDirectory, string executableName, Architecture architecture)
		{
			string newValue = ArchitectureToNameInManifest(architecture);
			NPath nPath = CommonPaths.Il2CppRoot.Combine("Unity.IL2CPP.WinRT\\AppxManifest.xml");
			NPath nPath2 = outputDirectory.Combine(nPath.FileName);
			string text = nPath.ReadAllText();
			nPath2.WriteAllText(text.Replace("__ARCHITECTURE__", newValue).Replace("__EXECUTABLE_NAME__", executableName));
		}

		public static void AddActivatableClasses(NPath manifestPath, string managedHostBinaryName)
		{
			string[] source = (from winmd in manifestPath.Parent.Files("*.winmd")
				select MakeActivatableExtensionElementForWinmd(winmd, managedHostBinaryName)).ToArray();
			if (source.Any())
			{
				string activatableClassesElement = source.Aggregate((string x, string y) => x + Environment.NewLine + y);
				AddActivatableClassesFromElement(manifestPath, activatableClassesElement);
			}
		}

		public static void AddActivatableClassesFromElement(NPath manifestPath, string activatableClassesElement)
		{
			string text = File.ReadAllText(manifestPath.ToString());
			int num = text.IndexOf("</Applications>");
			int num2 = text.IndexOf("<Extensions>", (num != -1) ? num : 0);
			if (num2 != -1)
			{
				int num3 = text.IndexOf('\n', num2) + 1;
				text = text.Substring(0, num3) + activatableClassesElement + text.Substring(num3);
			}
			else
			{
				int num4 = text.IndexOf("</Package>");
				if (num4 == -1)
				{
					throw new InvalidOperationException("Manifest is invalid: could not find end of Package element.");
				}
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.Append(text.Substring(0, num4));
				stringBuilder.AppendLine("  <Extensions>");
				stringBuilder.Append(activatableClassesElement);
				stringBuilder.AppendLine("  </Extensions>");
				stringBuilder.Append(text.Substring(num4));
				text = stringBuilder.ToString();
			}
			File.WriteAllText(manifestPath.ToString(), text);
		}

		public static string MakeActivatableExtensionElementForWinmd(NPath winmd, string managedHostBinaryName)
		{
			ReaderParameters parameters = new ReaderParameters
			{
				ApplyWindowsRuntimeProjections = true
			};
			using (ModuleDefinition moduleDefinition = ModuleDefinition.ReadModule(winmd.ToString(), parameters))
			{
				StringBuilder stringBuilder = new StringBuilder();
				string text = ((moduleDefinition.MetadataKind == MetadataKind.ManagedWindowsMetadata) ? managedHostBinaryName : winmd.ChangeExtension(".dll").FileName);
				stringBuilder.AppendLine("    <Extension Category=\"windows.activatableClass.inProcessServer\">");
				stringBuilder.AppendLine("      <InProcessServer>");
				stringBuilder.AppendLine("        <Path>" + text + "</Path>");
				foreach (TypeDefinition type in moduleDefinition.Types)
				{
					if (type.IsPublic && !type.IsValueType && !type.IsInterface)
					{
						stringBuilder.AppendLine($"        <ActivatableClass ActivatableClassId=\"{type.FullName}\" ThreadingModel=\"both\" />");
					}
				}
				stringBuilder.AppendLine("      </InProcessServer>");
				stringBuilder.AppendLine("    </Extension>");
				return stringBuilder.ToString();
			}
		}

		private static string ArchitectureToNameInManifest(Architecture architecture)
		{
			if (architecture is x86Architecture)
			{
				return "x86";
			}
			if (architecture is x64Architecture)
			{
				return "x64";
			}
			if (architecture is ARMv7Architecture)
			{
				return "arm";
			}
			if (architecture is ARM64Architecture)
			{
				return "arm64";
			}
			throw new NotSupportedException($"Architecture {architecture} is not supported by WinRTManifest!");
		}
	}
}
