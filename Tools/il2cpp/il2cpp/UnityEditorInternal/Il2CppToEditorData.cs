using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditorInternal
{
	[Serializable]
	internal class Il2CppToEditorData
	{
		[SerializeField]
		public List<Message> Messages;

		[SerializeField]
		public string CommandLine;
	}
}
