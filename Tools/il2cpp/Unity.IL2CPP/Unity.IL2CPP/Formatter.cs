using System.Globalization;
using System.Text;

namespace Unity.IL2CPP
{
	public class Formatter
	{
		internal static string StringRepresentationFor(double value)
		{
			string stringValue = value.ToString("G17", CultureInfo.InvariantCulture);
			if (double.IsPositiveInfinity(value))
			{
				return "std::numeric_limits<double>::infinity()";
			}
			if (double.IsNegativeInfinity(value))
			{
				return "-std::numeric_limits<double>::infinity()";
			}
			if (double.IsNaN(value))
			{
				return "std::numeric_limits<double>::quiet_NaN()";
			}
			return CheckFloatingPointFormatting(stringValue);
		}

		internal static string StringRepresentationFor(float value)
		{
			string stringValue = value.ToString("G9", CultureInfo.InvariantCulture);
			if (float.IsPositiveInfinity(value))
			{
				return "std::numeric_limits<float>::infinity()";
			}
			if (float.IsNegativeInfinity(value))
			{
				return "-std::numeric_limits<float>::infinity()";
			}
			if (float.IsNaN(value))
			{
				return "std::numeric_limits<float>::quiet_NaN()";
			}
			if (value == float.MaxValue)
			{
				return "(std::numeric_limits<float>::max)()";
			}
			if (value == float.MinValue)
			{
				return "-(std::numeric_limits<float>::max)()";
			}
			return CheckFloatingPointFormatting(stringValue, "f");
		}

		private static string CheckFloatingPointFormatting(string stringValue, string suffix = "")
		{
			if (!stringValue.Contains("."))
			{
				stringValue = ((!stringValue.Contains("E")) ? (stringValue + $".0{suffix}") : (stringValue.Replace("E", $".0E") + suffix));
			}
			else if (suffix == "f")
			{
				stringValue += suffix;
			}
			return stringValue;
		}

		public static string AsUTF8CppStringLiteral(string str)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(str);
			StringBuilder stringBuilder = new StringBuilder(bytes.Length + 2);
			bool flag = false;
			stringBuilder.Append('"');
			byte[] array = bytes;
			for (int i = 0; i < array.Length; i++)
			{
				byte b = array[i];
				switch (b)
				{
				case 92:
					stringBuilder.Append("\\\\");
					flag = false;
					break;
				default:
					stringBuilder.Append("\\x" + b.ToString("X"));
					flag = true;
					break;
				case 32:
				case 33:
				case 34:
				case 35:
				case 36:
				case 37:
				case 38:
				case 39:
				case 40:
				case 41:
				case 42:
				case 43:
				case 44:
				case 45:
				case 46:
				case 47:
				case 48:
				case 49:
				case 50:
				case 51:
				case 52:
				case 53:
				case 54:
				case 55:
				case 56:
				case 57:
				case 58:
				case 59:
				case 60:
				case 61:
				case 62:
				case 63:
				case 64:
				case 65:
				case 66:
				case 67:
				case 68:
				case 69:
				case 70:
				case 71:
				case 72:
				case 73:
				case 74:
				case 75:
				case 76:
				case 77:
				case 78:
				case 79:
				case 80:
				case 81:
				case 82:
				case 83:
				case 84:
				case 85:
				case 86:
				case 87:
				case 88:
				case 89:
				case 90:
				case 91:
				case 93:
				case 94:
				case 95:
				case 96:
				case 97:
				case 98:
				case 99:
				case 100:
				case 101:
				case 102:
				case 103:
				case 104:
				case 105:
				case 106:
				case 107:
				case 108:
				case 109:
				case 110:
				case 111:
				case 112:
				case 113:
				case 114:
				case 115:
				case 116:
				case 117:
				case 118:
				case 119:
				case 120:
				case 121:
				case 122:
				case 123:
				case 124:
				case 125:
				case 126:
					if (flag)
					{
						flag = false;
						stringBuilder.Append("\" \"");
					}
					stringBuilder.Append((char)b);
					break;
				}
			}
			stringBuilder.Append('"');
			return stringBuilder.ToString();
		}

		public static string FormatChar(char c)
		{
			int num = c;
			return "(Il2CppChar)" + num;
		}

		internal static string Quote(object val)
		{
			string text = val.ToString();
			if (string.IsNullOrEmpty(text))
			{
				return "NULL";
			}
			return "\"" + text + "\"";
		}
	}
}
