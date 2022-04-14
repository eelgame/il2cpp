using System.Collections.Generic;
using System.IO;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts.Components.Base;
using Unity.IL2CPP.Contexts.Services;

namespace Unity.IL2CPP.Contexts.Components
{
	public class ICallMappingComponent : ServiceComponentBase<IICallMappingService, ICallMappingComponent>, IICallMappingService
	{
		private struct ICallMapValue
		{
			public string Function;

			public string Header;
		}

		private Dictionary<string, ICallMapValue> Map;

		private void ReadMap(string path)
		{
			string[] array = File.ReadAllLines(path);
			string header = "";
			string[] array2 = array;
			foreach (string text in array2)
			{
				if (text.StartsWith(">"))
				{
					header = text.Substring(1);
				}
				if (!text.StartsWith(";") && !text.StartsWith("#") && !text.StartsWith("//") && !text.StartsWith(">"))
				{
					string[] array3 = text.Split(new char[1] { ' ' });
					if (array3.Length == 2)
					{
						ICallMapValue callMapValue = default(ICallMapValue);
						callMapValue.Function = array3[1];
						callMapValue.Header = header;
						ICallMapValue value = callMapValue;
						Map[array3[0]] = value;
					}
				}
			}
		}

		public ICallMappingComponent()
		{
			Map = new Dictionary<string, ICallMapValue>();
		}

		public void Initialize(AssemblyConversionContext context)
		{
			if (context.Parameters.UsingTinyClassLibraries)
			{
				ReadMap(CommonPaths.Il2CppRoot.Combine("libil2cpptiny/libil2cpptiny.icalls").ToString());
			}
			else
			{
				ReadMap(CommonPaths.Il2CppRoot.Combine("libil2cpp/libil2cpp.icalls").ToString());
			}
		}

		public string ResolveICallFunction(string icall)
		{
			if (Map.ContainsKey(icall))
			{
				return Map[icall].Function;
			}
			return null;
		}

		public string ResolveICallHeader(string icall)
		{
			if (Map.ContainsKey(icall))
			{
				if (!(Map[icall].Header == "null"))
				{
					return Map[icall].Header;
				}
				return null;
			}
			return null;
		}

		protected override ICallMappingComponent ThisAsFull()
		{
			return this;
		}

		protected override IICallMappingService ThisAsRead()
		{
			return this;
		}
	}
}
