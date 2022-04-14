using System;
using UnityEngine;

namespace UnityEditorInternal
{
	[Serializable]
	internal class Message
	{
		[SerializeField]
		public Il2CppMessageType Type;

		[SerializeField]
		public string Text;
	}
}
