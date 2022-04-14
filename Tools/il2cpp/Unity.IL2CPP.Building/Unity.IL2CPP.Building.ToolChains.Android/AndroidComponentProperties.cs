using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Unity.IL2CPP.Building.ToolChains.Android
{
	internal class AndroidComponentProperties
	{
		private class Parser
		{
			private readonly string m_Text;

			private int m_Position;

			public static IDictionary<string, string> Parse(string text)
			{
				Parser parser = new Parser(text);
				Dictionary<string, string> dictionary = new Dictionary<string, string>();
				while (true)
				{
					string key = parser.GetKey();
					if (key == null)
					{
						break;
					}
					parser.SkipSeparator();
					string value = parser.GetValue();
					dictionary.Add(key, value);
				}
				return dictionary;
			}

			private Parser(string text)
			{
				m_Text = text;
			}

			private string GetKey()
			{
				if (!SkipToKey())
				{
					return null;
				}
				string text = string.Empty;
				char result;
				while (TryGetChar(key: true, out result))
				{
					text += result;
				}
				if (text.Length == 0)
				{
					throw new Exception("Invalid property key.");
				}
				return text;
			}

			private string GetValue()
			{
				SkipWhitespace();
				string text = string.Empty;
				char result;
				while (TryGetChar(key: false, out result))
				{
					text += result;
				}
				if (text.Length == 0)
				{
					throw new Exception("Invalid property value.");
				}
				return text;
			}

			private bool SkipToKey()
			{
				while (m_Position < m_Text.Length)
				{
					char c = m_Text[m_Position];
					if (!IsWhitespace(c))
					{
						switch (c)
						{
						case '!':
						case '#':
							m_Position++;
							while (m_Position < m_Text.Length)
							{
								c = m_Text[m_Position];
								if (c == '\r' || c == '\n')
								{
									break;
								}
								m_Position++;
							}
							break;
						default:
							return true;
						case '\n':
						case '\r':
							break;
						}
					}
					m_Position++;
				}
				return false;
			}

			private void SkipWhitespace()
			{
				while (m_Position < m_Text.Length && IsWhitespace(m_Text[m_Position]))
				{
					m_Position++;
				}
			}

			private void SkipSeparator()
			{
				SkipWhitespace();
				if (m_Position < m_Text.Length)
				{
					char c = m_Text[m_Position];
					if (c == '=' || c == ':')
					{
						m_Position++;
					}
				}
			}

			private bool TryGetChar(bool key, out char result)
			{
				result = '\0';
				while (true)
				{
					if (m_Position == m_Text.Length)
					{
						return false;
					}
					char c = m_Text[m_Position];
					if (key)
					{
						if (c == '=' || c == ':' || IsWhitespace(c))
						{
							return false;
						}
						if (c == '\r' || c == '\n')
						{
							throw new Exception("Property value expected.");
						}
						m_Position++;
					}
					else
					{
						if (c == '\r' || c == '\n')
						{
							return false;
						}
						m_Position++;
					}
					if (c != '\\')
					{
						result = c;
						return true;
					}
					if (m_Position == m_Text.Length)
					{
						break;
					}
					c = m_Text[m_Position++];
					switch (c)
					{
					case 't':
						result = '\t';
						return true;
					case 'f':
						result = '\f';
						return true;
					case 'n':
						result = '\n';
						return true;
					case 'r':
						result = '\r';
						return true;
					case '\\':
						result = '\\';
						return true;
					case ' ':
						result = ' ';
						return true;
					case '!':
						result = '!';
						return true;
					case '#':
						result = '#';
						return true;
					case '=':
						result = '=';
						return true;
					case ':':
						result = ':';
						return true;
					case 'u':
					{
						if (m_Position + 4 >= m_Text.Length)
						{
							throw new Exception("Invalid Unicode character.");
						}
						string s = m_Text.Substring(m_Position, 4);
						m_Position += 4;
						ushort num = (result = (char)ushort.Parse(s, NumberStyles.HexNumber, null));
						return true;
					}
					case '\n':
						SkipWhitespace();
						break;
					case '\r':
						if (m_Position + 1 != m_Text.Length && m_Text[m_Position] == '\n')
						{
							m_Position++;
						}
						SkipWhitespace();
						break;
					default:
						throw new Exception($"Invalid escaped character '\\{c}'.");
					}
				}
				return false;
			}

			private static bool IsWhitespace(char c)
			{
				if (c == '\t' || c == '\f' || c == ' ')
				{
					return true;
				}
				return false;
			}
		}

		private static readonly Version k_DefaultVersion = new Version(0, 0, 0);

		private const string k_SourcePropertiesFileName = "source.properties";

		private readonly IDictionary<string, string> m_Properties;

		public string this[string key] => m_Properties[key];

		public string PackageDescription
		{
			get
			{
				if (!m_Properties.TryGetValue("Pkg.Desc", out var value))
				{
					return null;
				}
				return value;
			}
		}

		public Version PackageRevision
		{
			get
			{
				if (m_Properties.TryGetValue("Pkg.Revision", out var value))
				{
					Match match = Regex.Match(value, "^\\s*((\\d+\\.)*\\d+)(\\-\\w+)?");
					if (!match.Success)
					{
						return k_DefaultVersion;
					}
					value = match.Groups[1].Value;
					if (!Enumerable.Contains(value, '.'))
					{
						value += ".0";
					}
					else if (value.Count((char c) => c == '.') > 3)
					{
						return k_DefaultVersion;
					}
					return new Version(value);
				}
				return k_DefaultVersion;
			}
		}

		public static AndroidComponentProperties Read(string directory)
		{
			string path = Path.Combine(directory, "source.properties");
			if (!File.Exists(path))
			{
				return null;
			}
			try
			{
				return Parse(File.ReadAllText(path, Encoding.UTF8));
			}
			catch
			{
				return null;
			}
		}

		public static AndroidComponentProperties Parse(string text)
		{
			try
			{
				return new AndroidComponentProperties(Parser.Parse(text));
			}
			catch
			{
				return null;
			}
		}

		public static Version GetPackageRevision(string directory)
		{
			AndroidComponentProperties androidComponentProperties = Read(directory);
			if (androidComponentProperties == null)
			{
				return k_DefaultVersion;
			}
			return androidComponentProperties.PackageRevision;
		}

		private AndroidComponentProperties(IDictionary<string, string> properties)
		{
			m_Properties = properties;
		}
	}
}
