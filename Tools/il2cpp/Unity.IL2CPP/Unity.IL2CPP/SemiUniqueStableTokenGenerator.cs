using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Mono.Cecil;
using Unity.Cecil.Awesome;

namespace Unity.IL2CPP
{
	internal static class SemiUniqueStableTokenGenerator
	{
		private unsafe static string GenerateForString(string str)
		{
			using (SHA1 sHA = SHA1.Create())
			{
				byte[] array = new byte[str.Length * 2];
				try
				{
					fixed (char* ptr = str)
					{
						Marshal.Copy((IntPtr)ptr, array, 0, array.Length);
					}
				}
				finally
				{
				}
				byte[] array2 = sHA.ComputeHash(array);
				StringBuilder stringBuilder = new StringBuilder(array2.Length * 2);
				byte[] array3 = array2;
				foreach (byte b in array3)
				{
					stringBuilder.Append(b.ToString("X2"));
				}
				return stringBuilder.ToString();
			}
		}

		internal static string GenerateFor(TypeReference type)
		{
			return GenerateForString(type.AssemblyQualifiedName());
		}

		internal static string GenerateFor(MethodReference method)
		{
			return GenerateForString(method.AssemblyQualifiedName());
		}

		internal static string GenerateFor(string literal)
		{
			return GenerateForString(literal);
		}
	}
}
