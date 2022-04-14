using System.Collections.Generic;
using System.Linq;
using NiceIO;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.Building.BuildDescriptions.Mono
{
	public class MonoSourceFileList
	{
		public static bool Available => MonoSourceDir != null;

		protected static NPath MonoSourceDir
		{
			get
			{
				if (!(CommonPaths.Il2CppRoot != null))
				{
					return null;
				}
				return CommonPaths.Il2CppRoot.Combine("external/mono");
			}
		}

		protected static NPath LibMonoDir
		{
			get
			{
				if (!(CommonPaths.Il2CppRoot != null))
				{
					return null;
				}
				return CommonPaths.Il2CppRoot.Combine("libmono");
			}
		}

		protected static NPath LibIL2CPPDir
		{
			get
			{
				if (!(CommonPaths.Il2CppRoot != null))
				{
					return null;
				}
				return CommonPaths.Il2CppRoot.Combine("libil2cpp");
			}
		}

		public static NPath MBedTLSDir => MonoSourceDir.Combine("external").Combine("mbedtls");

		public IEnumerable<NPath> MonoEglibIncludeDirs => new NPath[4]
		{
			LibMonoDir.Combine("config"),
			MonoSourceDir.Combine("mono/eglib"),
			LibIL2CPPDir.Combine("os/c-api"),
			MonoSourceDir
		};

		public IEnumerable<NPath> MonoIncludeDirs
		{
			get
			{
				yield return MonoSourceDir;
				yield return MonoSourceDir.Combine("mono/eglib");
				yield return MonoSourceDir.Combine("mono");
				yield return MonoSourceDir.Combine("mono/sgen");
				yield return MonoSourceDir.Combine("mono/utils");
				yield return MonoSourceDir.Combine("mono/metadata");
				yield return MonoSourceDir.Combine("mono/metadata/private");
				yield return LibIL2CPPDir.Combine("os/c-api");
				yield return LibMonoDir.Combine("config");
			}
		}

		public virtual IEnumerable<NPath> SGENGCSourceFiles()
		{
			return from f in MonoSourceDir.Combine("mono/sgen").Files("*.c*", recurse: true)
				where f.HasExtension("c", "cpp")
				select f;
		}

		public bool IsMonoEglibFile(NPath sourceFile)
		{
			if (Available)
			{
				return sourceFile.IsChildOf(MonoSourceDir.Combine("mono/eglib"));
			}
			return false;
		}

		public bool IsMonoMiniFile(NPath sourceFile)
		{
			if (Available)
			{
				return sourceFile.IsChildOf(MonoSourceDir.Combine("mono/mini"));
			}
			return false;
		}

		public bool IsMonoFile(NPath sourceFile)
		{
			if (Available)
			{
				return sourceFile.IsChildOf(MonoSourceDir);
			}
			return false;
		}

		public bool IsMonoDebuggerFile(NPath sourceFile)
		{
			if (Available)
			{
				return sourceFile.FileName == "debugger-agent.c";
			}
			return false;
		}

		public virtual IEnumerable<NPath> GetEGLibSourceFiles(Architecture architecture)
		{
			return new NPath[26]
			{
				MonoSourceDir.Combine("mono/eglib/garray.c"),
				MonoSourceDir.Combine("mono/eglib/gbytearray.c"),
				MonoSourceDir.Combine("mono/eglib/gdate-unity.c"),
				MonoSourceDir.Combine("mono/eglib/gdir-unity.c"),
				MonoSourceDir.Combine("mono/eglib/gerror.c"),
				MonoSourceDir.Combine("mono/eglib/gfile-unity.c"),
				MonoSourceDir.Combine("mono/eglib/gfile.c"),
				MonoSourceDir.Combine("mono/eglib/ghashtable.c"),
				MonoSourceDir.Combine("mono/eglib/giconv.c"),
				MonoSourceDir.Combine("mono/eglib/glist.c"),
				MonoSourceDir.Combine("mono/eglib/gmarkup.c"),
				MonoSourceDir.Combine("mono/eglib/gmem.c"),
				MonoSourceDir.Combine("mono/eglib/gmisc-unity.c"),
				MonoSourceDir.Combine("mono/eglib/goutput.c"),
				MonoSourceDir.Combine("mono/eglib/gpath.c"),
				MonoSourceDir.Combine("mono/eglib/gpattern.c"),
				MonoSourceDir.Combine("mono/eglib/gptrarray.c"),
				MonoSourceDir.Combine("mono/eglib/gqsort.c"),
				MonoSourceDir.Combine("mono/eglib/gqueue.c"),
				MonoSourceDir.Combine("mono/eglib/gshell.c"),
				MonoSourceDir.Combine("mono/eglib/gslist.c"),
				MonoSourceDir.Combine("mono/eglib/gspawn.c"),
				MonoSourceDir.Combine("mono/eglib/gstr.c"),
				MonoSourceDir.Combine("mono/eglib/gstring.c"),
				MonoSourceDir.Combine("mono/eglib/gunicode.c"),
				MonoSourceDir.Combine("mono/eglib/gutf8.c")
			};
		}

		public virtual IEnumerable<NPath> GetMetadataSourceFiles(Architecture architecture)
		{
			return new NPath[89]
			{
				MonoSourceDir.Combine("mono/metadata/appdomain.c"),
				MonoSourceDir.Combine("mono/metadata/assembly.c"),
				MonoSourceDir.Combine("mono/metadata/attach.c"),
				MonoSourceDir.Combine("mono/metadata/boehm-gc.c"),
				MonoSourceDir.Combine("mono/metadata/class-accessors.c"),
				MonoSourceDir.Combine("mono/metadata/class.c"),
				MonoSourceDir.Combine("mono/metadata/cominterop.c"),
				MonoSourceDir.Combine("mono/metadata/coree.c"),
				MonoSourceDir.Combine("mono/metadata/custom-attrs.c"),
				MonoSourceDir.Combine("mono/metadata/debug-helpers.c"),
				MonoSourceDir.Combine("mono/metadata/debug-mono-ppdb.c"),
				MonoSourceDir.Combine("mono/metadata/debug-mono-symfile.c"),
				MonoSourceDir.Combine("mono/metadata/decimal-ms.c"),
				MonoSourceDir.Combine("mono/metadata/domain.c"),
				MonoSourceDir.Combine("mono/metadata/dynamic-image.c"),
				MonoSourceDir.Combine("mono/metadata/dynamic-stream.c"),
				MonoSourceDir.Combine("mono/metadata/environment.c"),
				MonoSourceDir.Combine("mono/metadata/exception.c"),
				MonoSourceDir.Combine("mono/metadata/fdhandle.c"),
				MonoSourceDir.Combine("mono/metadata/file-mmap-posix.c"),
				MonoSourceDir.Combine("mono/metadata/file-mmap-windows.c"),
				MonoSourceDir.Combine("mono/metadata/filewatcher.c"),
				MonoSourceDir.Combine("mono/metadata/gc-stats.c"),
				MonoSourceDir.Combine("mono/metadata/gc.c"),
				MonoSourceDir.Combine("mono/metadata/handle.c"),
				MonoSourceDir.Combine("mono/metadata/icall-windows.c"),
				MonoSourceDir.Combine("mono/metadata/icall.c"),
				MonoSourceDir.Combine("mono/metadata/image.c"),
				MonoSourceDir.Combine("mono/metadata/jit-info.c"),
				MonoSourceDir.Combine("mono/metadata/loader.c"),
				MonoSourceDir.Combine("mono/metadata/locales.c"),
				MonoSourceDir.Combine("mono/metadata/lock-tracer.c"),
				MonoSourceDir.Combine("mono/metadata/marshal-windows.c"),
				MonoSourceDir.Combine("mono/metadata/marshal.c"),
				MonoSourceDir.Combine("mono/metadata/mempool.c"),
				MonoSourceDir.Combine("mono/metadata/metadata-cross-helpers.c"),
				MonoSourceDir.Combine("mono/metadata/metadata-verify.c"),
				MonoSourceDir.Combine("mono/metadata/metadata.c"),
				MonoSourceDir.Combine("mono/metadata/method-builder.c"),
				MonoSourceDir.Combine("mono/metadata/monitor.c"),
				MonoSourceDir.Combine("mono/metadata/mono-basic-block.c"),
				MonoSourceDir.Combine("mono/metadata/mono-conc-hash.c"),
				MonoSourceDir.Combine("mono/metadata/mono-config-dirs.c"),
				MonoSourceDir.Combine("mono/metadata/mono-config.c"),
				MonoSourceDir.Combine("mono/metadata/mono-debug.c"),
				MonoSourceDir.Combine("mono/metadata/mono-endian.c"),
				MonoSourceDir.Combine("mono/metadata/mono-hash.c"),
				MonoSourceDir.Combine("mono/metadata/mono-mlist.c"),
				MonoSourceDir.Combine("mono/metadata/mono-perfcounters.c"),
				MonoSourceDir.Combine("mono/metadata/mono-security-windows.c"),
				MonoSourceDir.Combine("mono/metadata/mono-security.c"),
				MonoSourceDir.Combine("mono/metadata/null-gc.c"),
				MonoSourceDir.Combine("mono/metadata/number-ms.c"),
				MonoSourceDir.Combine("mono/metadata/object.c"),
				MonoSourceDir.Combine("mono/metadata/opcodes.c"),
				MonoSourceDir.Combine("mono/metadata/profiler.c"),
				MonoSourceDir.Combine("mono/metadata/property-bag.c"),
				MonoSourceDir.Combine("mono/metadata/rand.c"),
				MonoSourceDir.Combine("mono/metadata/reflection.c"),
				MonoSourceDir.Combine("mono/metadata/remoting.c"),
				MonoSourceDir.Combine("mono/metadata/runtime.c"),
				MonoSourceDir.Combine("mono/metadata/security-core-clr.c"),
				MonoSourceDir.Combine("mono/metadata/security-manager.c"),
				MonoSourceDir.Combine("mono/metadata/seq-points-data.c"),
				MonoSourceDir.Combine("mono/metadata/sgen-bridge.c"),
				MonoSourceDir.Combine("mono/metadata/sgen-mono.c"),
				MonoSourceDir.Combine("mono/metadata/sgen-new-bridge.c"),
				MonoSourceDir.Combine("mono/metadata/sgen-old-bridge.c"),
				MonoSourceDir.Combine("mono/metadata/sgen-stw.c"),
				MonoSourceDir.Combine("mono/metadata/sgen-tarjan-bridge.c"),
				MonoSourceDir.Combine("mono/metadata/sgen-toggleref.c"),
				MonoSourceDir.Combine("mono/metadata/sre-encode.c"),
				MonoSourceDir.Combine("mono/metadata/sre-save.c"),
				MonoSourceDir.Combine("mono/metadata/sre.c"),
				MonoSourceDir.Combine("mono/metadata/string-icalls.c"),
				MonoSourceDir.Combine("mono/metadata/sysmath.c"),
				MonoSourceDir.Combine("mono/metadata/threadpool-io.c"),
				MonoSourceDir.Combine("mono/metadata/threadpool-worker-default.c"),
				MonoSourceDir.Combine("mono/metadata/threadpool.c"),
				MonoSourceDir.Combine("mono/metadata/threads.c"),
				MonoSourceDir.Combine("mono/metadata/unity-icall.c"),
				MonoSourceDir.Combine("mono/metadata/unity-liveness.c"),
				MonoSourceDir.Combine("mono/metadata/unity-utils.c"),
				MonoSourceDir.Combine("mono/metadata/verify.c"),
				MonoSourceDir.Combine("mono/metadata/w32file.c"),
				MonoSourceDir.Combine("mono/metadata/w32handle-namespace.c"),
				MonoSourceDir.Combine("mono/metadata/w32handle.c"),
				MonoSourceDir.Combine("mono/metadata/w32process.c"),
				MonoSourceDir.Combine("mono/metadata/w32socket.c")
			};
		}

		public virtual IEnumerable<NPath> GetMetadataDebuggerSourceFiles(Architecture architecture)
		{
			return new NPath[2]
			{
				MonoSourceDir.Combine("mono/metadata/mono-hash.c"),
				MonoSourceDir.Combine("mono/metadata/profiler.c")
			};
		}

		public virtual IEnumerable<NPath> GetMiniSourceFiles(Architecture architecture)
		{
			return new NPath[1] { MonoSourceDir.Combine("mono/mini/debugger-agent.c") };
		}

		public virtual IEnumerable<NPath> GetUtilsSourceFiles(Architecture architecture)
		{
			List<NPath> list = new List<NPath>();
			list.Add(MonoSourceDir.Combine("mono/utils/atomic.c"));
			list.Add(MonoSourceDir.Combine("mono/utils/bsearch.c"));
			list.Add(MonoSourceDir.Combine("mono/utils/dlmalloc.c"));
			list.Add(MonoSourceDir.Combine("mono/utils/hazard-pointer.c"));
			list.Add(MonoSourceDir.Combine("mono/utils/json.c"));
			list.Add(MonoSourceDir.Combine("mono/utils/lock-free-alloc.c"));
			list.Add(MonoSourceDir.Combine("mono/utils/lock-free-array-queue.c"));
			list.Add(MonoSourceDir.Combine("mono/utils/lock-free-queue.c"));
			list.Add(MonoSourceDir.Combine("mono/utils/memfuncs.c"));
			list.Add(MonoSourceDir.Combine("mono/utils/mono-codeman.c"));
			list.Add(MonoSourceDir.Combine("mono/utils/mono-conc-hashtable.c"));
			list.Add(MonoSourceDir.Combine("mono/utils/mono-context.c"));
			list.Add(MonoSourceDir.Combine("mono/utils/mono-counters.c"));
			list.Add(MonoSourceDir.Combine("mono/utils/mono-dl.c"));
			list.Add(MonoSourceDir.Combine("mono/utils/mono-error.c"));
			list.Add(MonoSourceDir.Combine("mono/utils/mono-filemap.c"));
			list.Add(MonoSourceDir.Combine("mono/utils/mono-hwcap.c"));
			list.Add(MonoSourceDir.Combine("mono/utils/mono-internal-hash.c"));
			list.Add(MonoSourceDir.Combine("mono/utils/mono-io-portability.c"));
			list.Add(MonoSourceDir.Combine("mono/utils/mono-linked-list-set.c"));
			list.Add(MonoSourceDir.Combine("mono/utils/mono-log-common.c"));
			list.Add(MonoSourceDir.Combine("mono/utils/mono-logger.c"));
			list.Add(MonoSourceDir.Combine("mono/utils/mono-math.c"));
			list.Add(MonoSourceDir.Combine("mono/utils/mono-md5.c"));
			list.Add(MonoSourceDir.Combine("mono/utils/mono-mmap-windows.c"));
			list.Add(MonoSourceDir.Combine("mono/utils/mono-mmap.c"));
			list.Add(MonoSourceDir.Combine("mono/utils/mono-networkinterfaces.c"));
			list.Add(MonoSourceDir.Combine("mono/utils/mono-os-mutex.c"));
			list.Add(MonoSourceDir.Combine("mono/utils/mono-path.c"));
			list.Add(MonoSourceDir.Combine("mono/utils/mono-poll.c"));
			list.Add(MonoSourceDir.Combine("mono/utils/mono-proclib-windows.c"));
			list.Add(MonoSourceDir.Combine("mono/utils/mono-proclib.c"));
			list.Add(MonoSourceDir.Combine("mono/utils/mono-property-hash.c"));
			list.Add(MonoSourceDir.Combine("mono/utils/mono-publib.c"));
			list.Add(MonoSourceDir.Combine("mono/utils/mono-sha1.c"));
			list.Add(MonoSourceDir.Combine("mono/utils/mono-stdlib.c"));
			list.Add(MonoSourceDir.Combine("mono/utils/mono-threads-coop.c"));
			list.Add(MonoSourceDir.Combine("mono/utils/mono-threads-state-machine.c"));
			list.Add(MonoSourceDir.Combine("mono/utils/mono-threads.c"));
			list.Add(MonoSourceDir.Combine("mono/utils/mono-tls.c"));
			list.Add(MonoSourceDir.Combine("mono/utils/mono-uri.c"));
			list.Add(MonoSourceDir.Combine("mono/utils/mono-value-hash.c"));
			list.Add(MonoSourceDir.Combine("mono/utils/monobitset.c"));
			list.Add(MonoSourceDir.Combine("mono/utils/networking-missing.c"));
			list.Add(MonoSourceDir.Combine("mono/utils/networking.c"));
			list.Add(MonoSourceDir.Combine("mono/utils/parse.c"));
			list.Add(MonoSourceDir.Combine("mono/utils/strenc.c"));
			list.Add(MonoSourceDir.Combine("mono/utils/unity-rand.c"));
			list.Add(MonoSourceDir.Combine("mono/utils/unity-time.c"));
			List<NPath> list2 = list;
			if (architecture is EmscriptenJavaScriptArchitecture)
			{
				list2.Add(MonoSourceDir.Combine("mono/utils/mono-hwcap-web.c"));
			}
			else if (architecture is x86Architecture || architecture is x64Architecture)
			{
				list2.Add(MonoSourceDir.Combine("mono/utils/mono-hwcap-x86.c"));
			}
			if (architecture is ARMv7Architecture)
			{
				list2.Add(MonoSourceDir.Combine("mono/utils/mono-hwcap-arm.c"));
			}
			if (architecture is ARM64Architecture)
			{
				list2.Add(MonoSourceDir.Combine("mono/utils/mono-hwcap-arm64.c"));
			}
			return list2;
		}
	}
}
