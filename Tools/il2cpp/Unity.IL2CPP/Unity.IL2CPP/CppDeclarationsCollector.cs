using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Unity.Cecil.Awesome.Comparers;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Results;

namespace Unity.IL2CPP
{
	public static class CppDeclarationsCollector
	{
		public static void PopulateCache(SourceWritingContext context, IEnumerable<TypeReference> rootTypes, CppDeclarationsCache cache)
		{
			CppDeclarations cppDeclarations = new CppDeclarations();
			TypeReference[] array = rootTypes.ToArray();
			while (true)
			{
				CppDeclarations cppDeclarations2 = new CppDeclarations();
				TypeReference[] array2 = array;
				foreach (TypeReference type in array2)
				{
					if (cache.TryGetValue(type, out var data))
					{
						cppDeclarations2.Add(data.declarations);
					}
					else
					{
						cppDeclarations2.Add(GetDeclarations(context, type, cache));
					}
				}
				ReadOnlyHashSet<TypeReference> readOnlyHashSet = cppDeclarations2.TypeIncludes.ExceptWith(cppDeclarations.TypeIncludes);
				cppDeclarations.Add(cppDeclarations2);
				if (readOnlyHashSet.Count != 0)
				{
					array = readOnlyHashSet.ToArray();
					continue;
				}
				break;
			}
		}

		public static ReadOnlyHashSet<TypeReference> GetDependencies(TypeReference type, ICppDeclarationsCache cache)
		{
			return GetDependencies(new TypeReference[1] { type }, cache);
		}

		public static ReadOnlyHashSet<TypeReference> GetDependencies(IEnumerable<TypeReference> types, ICppDeclarationsCache cache)
		{
			HashSet<TypeReference> hashSet = new HashSet<TypeReference>(new TypeReferenceEqualityComparer());
			TypeReference[] array = types.ToArray();
			int num = -1;
			while (num != hashSet.Count)
			{
				num = hashSet.Count;
				TypeReference[] array2 = array;
				foreach (TypeReference type in array2)
				{
					hashSet.UnionWith(cache.GetDeclarations(type).TypeIncludes);
				}
				array = hashSet.ToArray();
			}
			return hashSet.AsReadOnly();
		}

		private static ICppDeclarations GetDeclarations(SourceWritingContext context, TypeReference type, CppDeclarationsCache cache)
		{
			using (InMemoryCodeWriter inMemoryCodeWriter = new InMemoryCodeWriter(context))
			{
				SourceWriter.WriteTypeDefinition(context, inMemoryCodeWriter, type);
				CppDeclarationsCache.CacheData cacheData = new CppDeclarationsCache.CacheData
				{
					definition = inMemoryCodeWriter.GetSourceCodeString(),
					declarations = inMemoryCodeWriter.Declarations
				};
				cache.Add(type, cacheData);
				return cacheData.declarations;
			}
		}
	}
}
