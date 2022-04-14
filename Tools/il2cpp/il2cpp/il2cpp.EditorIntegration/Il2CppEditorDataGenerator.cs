using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using NiceIO;
using Unity.IL2CPP.Common;
using UnityEditorInternal;

namespace il2cpp.EditorIntegration
{
	public class Il2CppEditorDataGenerator : IDisposable
	{
		public const string DataFileName = "Il2CppToEditorData.json";

		private readonly string[] _args;

		private readonly NPath _outputDirectory;

		private readonly Lazy<Il2CppToEditorData> _data;

		public Il2CppEditorDataGenerator(string[] args, NPath outputDirectory)
		{
			_args = args;
			_outputDirectory = outputDirectory;
			_data = new Lazy<Il2CppToEditorData>(InitIl2CppToEditorData);
		}

		public void LogException(Exception ex)
		{
			if (ex is AggregateException ex2)
			{
				{
					foreach (Exception innerException in ex2.InnerExceptions)
					{
						LogException(innerException);
					}
					return;
				}
			}
			string text = ((ex is UserMessageException) ? ex.Message : ((ex is AdditionalErrorInformationException) ? (ex.Message + "\n" + ex.InnerException.ToString()) : ((!(ex is PathTooLongException)) ? ex.ToString() : $"The specified output path is too long to write generated files. Please choose a location with a shorter path.\n{ex}")));
			_data.Value.Messages.Add(new Message
			{
				Type = Il2CppMessageType.Error,
				Text = text
			});
		}

		public void LogFromMessages(IEnumerable<string> messages)
		{
			foreach (string message in messages)
			{
				_data.Value.Messages.Add(new Message
				{
					Type = Il2CppMessageType.Warning,
					Text = message
				});
			}
		}

		public void Write()
		{
			if (!_data.IsValueCreated)
			{
				return;
			}
			foreach (Message message in _data.Value.Messages)
			{
				string msg = $"{message.Type}: {message.Text}";
				if (message.Type == Il2CppMessageType.Error)
				{
					ConsoleOutput.Error.WriteLine(msg);
				}
				else
				{
					ConsoleOutput.Info.WriteLine(msg);
				}
			}
			_outputDirectory.Combine("Il2CppToEditorData.json").WriteAllText(JsonConvert.SerializeObject(_data.Value));
		}

		public void Dispose()
		{
			Write();
		}

		private Il2CppToEditorData InitIl2CppToEditorData()
		{
			return new Il2CppToEditorData
			{
				Messages = new List<Message>(),
				CommandLine = (_args ?? Array.Empty<string>()).SeparateWithSpaces()
			};
		}
	}
}
