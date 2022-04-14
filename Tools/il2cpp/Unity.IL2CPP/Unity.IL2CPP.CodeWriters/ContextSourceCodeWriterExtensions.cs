using System;
using NiceIO;
using Unity.IL2CPP.Contexts;
using Unity.MiniProfiling;

namespace Unity.IL2CPP.CodeWriters
{
	public static class ContextSourceCodeWriterExtensions
	{
		public static IGeneratedMethodCodeWriter CreateManagedSourceWriter(this SourceWritingContext context, NPath filename)
		{
			return context.CreateManagedSourceWriter(filename, createProfilerSection: false);
		}

		public static IGeneratedMethodCodeWriter CreateProfiledManagedSourceWriter(this SourceWritingContext context, NPath filename)
		{
			return context.CreateManagedSourceWriter(filename, createProfilerSection: true);
		}

		public static IGeneratedMethodCodeWriter CreateProfiledManagedSourceWriterInOutputDirectory(this SourceWritingContext context, string filename)
		{
			return context.CreateManagedSourceWriter(context.Global.InputData.OutputDir.Combine(filename), createProfilerSection: true);
		}

		public static IGeneratedMethodCodeWriter CreateManagedSourceWriterInOutputDirectory(this SourceWritingContext context, string filename)
		{
			return context.CreateManagedSourceWriter(context.Global.InputData.OutputDir.Combine(filename), createProfilerSection: false);
		}

		public static ICppCodeWriter CreateProfiledSourceWriterInOutputDirectory(this SourceWritingContext context, string filename)
		{
			return context.AsMinimal().CreateSourceWriter(context.Global.InputData.OutputDir.Combine(filename), createProfilerSection: true);
		}

		public static ICppCodeWriter CreateSourceWriterInOutputDirectory(this SourceWritingContext context, string filename)
		{
			return context.AsMinimal().CreateSourceWriter(context.Global.InputData.OutputDir.Combine(filename), createProfilerSection: false);
		}

		public static IGeneratedCodeWriter CreateProfiledGeneratedCodeSourceWriterInOutputDirectory(this SourceWritingContext context, string filename)
		{
			return context.CreateGeneratedCodeSourceWriter(context.Global.InputData.OutputDir.Combine(filename), createProfilerSection: true);
		}

		public static IGeneratedCodeWriter CreateProfiledGeneratedCodeSourceWriter(this SourceWritingContext context, NPath filename)
		{
			return context.CreateGeneratedCodeSourceWriter(filename, createProfilerSection: true);
		}

		public static ICppCodeWriter CreateProfiledSourceWriterInOutputDirectory(this MinimalContext context, string filename)
		{
			return context.CreateSourceWriter(context.Global.InputData.OutputDir.Combine(filename), createProfilerSection: true);
		}

		public static ICppCodeWriter CreateSourceWriterInOutputDirectory(this MinimalContext context, string filename)
		{
			return context.CreateSourceWriter(context.Global.InputData.OutputDir.Combine(filename), createProfilerSection: false);
		}

		private static IGeneratedMethodCodeWriter CreateManagedSourceWriter(this SourceWritingContext context, NPath filename, bool createProfilerSection)
		{
			NPath filePath = context.Global.Services.PathFactory.GetFilePath(filename);
			IDisposable profilerSection = (createProfilerSection ? MiniProfiler.Section(filePath.FileName) : null);
			return new ManagedSourceCodeWriter(context, filePath, profilerSection);
		}

		private static ICppCodeWriter CreateSourceWriter(this MinimalContext context, NPath filename, bool createProfilerSection)
		{
			NPath filePath = context.Global.Services.PathFactory.GetFilePath(filename);
			IDisposable profilerSection = (createProfilerSection ? MiniProfiler.Section(filePath.FileName) : null);
			return new SourceCodeWriter(context, filePath, profilerSection);
		}

		private static IGeneratedCodeWriter CreateGeneratedCodeSourceWriter(this SourceWritingContext context, NPath filename, bool createProfilerSection)
		{
			NPath filePath = context.Global.Services.PathFactory.GetFilePath(filename);
			IDisposable profilerSection = (createProfilerSection ? MiniProfiler.Section(filePath.FileName) : null);
			return new GeneratedCodeSourceCodeWriter(context, filePath, profilerSection);
		}
	}
}
