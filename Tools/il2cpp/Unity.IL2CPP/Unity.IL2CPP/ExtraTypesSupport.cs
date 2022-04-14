using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using NiceIO;
using Unity.Cecil.Visitor;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.GenericsCollection;

namespace Unity.IL2CPP
{
	public struct ExtraTypesSupport
	{
		private readonly GenericContextFreeVisitor _visitor;

		private readonly IEnumerable<AssemblyDefinition> _usedAssemblies;

		public ExtraTypesSupport(PrimaryCollectionContext context, InflatedCollectionCollector collectionCollector, IEnumerable<AssemblyDefinition> usedAssemblies)
		{
			_usedAssemblies = usedAssemblies;
			_visitor = new GenericContextFreeVisitor(context, collectionCollector);
		}

		public bool AddType(TypeNameParseInfo typeNameInfo)
		{
			try
			{
				TypeReferenceFor(typeNameInfo);
				return true;
			}
			catch (TypeResolutionException)
			{
				return false;
			}
		}

		private TypeReference TypeReferenceFor(TypeNameParseInfo typeNameInfo)
		{
			TypeReference typeReference = GetTypeByName(CecilElementTypeNameFor(typeNameInfo), typeNameInfo.Assembly);
			if (typeReference == null)
			{
				throw new TypeResolutionException(typeNameInfo);
			}
			if (typeNameInfo.HasGenericArguments)
			{
				GenericInstanceType genericInstanceType = new GenericInstanceType(typeReference);
				foreach (TypeNameParseInfo typeArgument in typeNameInfo.TypeArguments)
				{
					genericInstanceType.GenericArguments.Add(TypeReferenceFor(typeArgument));
				}
				genericInstanceType.Accept(_visitor);
				typeReference = genericInstanceType;
			}
			if (typeNameInfo.IsPointer)
			{
				int num = typeNameInfo.Modifiers.Count((int m) => m == -1);
				PointerType pointerType = new PointerType(typeReference);
				for (int i = 1; i < num; i++)
				{
					pointerType = new PointerType(pointerType);
				}
				pointerType.Accept(_visitor);
				typeReference = pointerType;
			}
			if (typeNameInfo.IsArray)
			{
				ArrayType arrayType = new ArrayType(typeReference, typeNameInfo.Ranks[0]);
				for (int j = 1; j < typeNameInfo.Ranks.Length; j++)
				{
					arrayType = new ArrayType(arrayType, typeNameInfo.Ranks[j]);
				}
				arrayType.Accept(_visitor);
				typeReference = arrayType;
			}
			return typeReference;
		}

		private static string CecilElementTypeNameFor(TypeNameParseInfo typeNameInfo)
		{
			if (!typeNameInfo.IsNested)
			{
				return typeNameInfo.ElementTypeName;
			}
			string text = typeNameInfo.Name;
			if (!string.IsNullOrEmpty(typeNameInfo.Namespace))
			{
				text = typeNameInfo.Namespace + "." + text;
			}
			return typeNameInfo.Nested.Aggregate(text, (string c, string n) => c + "/" + n);
		}

		private TypeReference GetTypeByName(string name, AssemblyNameParseInfo assembly)
		{
			if (string.IsNullOrEmpty(assembly?.Name))
			{
				return _usedAssemblies.Select((AssemblyDefinition a) => a.MainModule.GetType(name)).FirstOrDefault((TypeDefinition t) => t != null);
			}
			return _usedAssemblies.FirstOrDefault((AssemblyDefinition a) => a.Name.Name == assembly.Name)?.MainModule.GetType(name);
		}

		public static IEnumerable<string> BuildExtraTypesList(NPath[] extraTypesFiles)
		{
			HashSet<string> hashSet = new HashSet<string>();
			foreach (NPath nPath in extraTypesFiles)
			{
				try
				{
					foreach (string item in from l in File.ReadAllLines(nPath)
						select l.Trim() into l
						where l.Length > 0
						select l)
					{
						if (!item.StartsWith(";") && !item.StartsWith("#") && !item.StartsWith("//"))
						{
							hashSet.Add(item);
						}
					}
				}
				catch (Exception)
				{
					ConsoleOutput.Info.WriteLine("WARNING: Cannot open extra file list {0}. Skipping.", nPath);
				}
			}
			return hashSet;
		}
	}
}
