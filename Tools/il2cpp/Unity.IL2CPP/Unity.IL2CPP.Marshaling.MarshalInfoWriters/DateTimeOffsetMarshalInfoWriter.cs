using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.MarshalInfoWriters
{
	internal class DateTimeOffsetMarshalInfoWriter : MarshalableMarshalInfoWriter
	{
		private readonly MarshaledType[] _marshaledTypes;

		private readonly TypeDefinition _windowsFoundationDateTime;

		private readonly TypeDefinition _systemDateTime;

		private readonly FieldDefinition _universalTimeField;

		private readonly FieldDefinition _dateTimeOffsetDateTimeField;

		private readonly FieldDefinition _dateTimeOffsetMinutesField;

		private readonly FieldDefinition _dateTimeDateDataField;

		private const long kTicksBetweenDotNetAndWindowsRuntimeTime = 504911232000000000L;

		public override MarshaledType[] MarshaledTypes => _marshaledTypes;

		public DateTimeOffsetMarshalInfoWriter(ReadOnlyContext context, TypeDefinition dateTimeOffset)
			: base(context, dateTimeOffset)
		{
			_dateTimeOffsetDateTimeField = dateTimeOffset.Fields.Single((FieldDefinition f) => f.Name == "m_dateTime");
			_dateTimeOffsetMinutesField = dateTimeOffset.Fields.Single((FieldDefinition f) => f.Name == "m_offsetMinutes");
			_systemDateTime = _dateTimeOffsetDateTimeField.FieldType.Resolve();
			_dateTimeDateDataField = _systemDateTime.Fields.Single((FieldDefinition f) => f.Name == "dateData");
			_windowsFoundationDateTime = context.Global.Services.WindowsRuntime.ProjectToWindowsRuntime(dateTimeOffset);
			_universalTimeField = _windowsFoundationDateTime.Fields.Single((FieldDefinition f) => f.Name == "UniversalTime");
			string text = context.Global.Services.Naming.ForVariable(_windowsFoundationDateTime);
			_marshaledTypes = new MarshaledType[1]
			{
				new MarshaledType(text, text)
			};
		}

		public override void WriteIncludesForFieldDeclaration(IGeneratedCodeWriter writer)
		{
			writer.AddIncludeForTypeDefinition(_windowsFoundationDateTime);
		}

		public override void WriteIncludesForMarshaling(IGeneratedMethodCodeWriter writer)
		{
			writer.AddIncludeForTypeDefinition(_typeRef);
			writer.AddIncludeForTypeDefinition(_windowsFoundationDateTime);
			writer.AddIncludeForTypeDefinition(_systemDateTime);
		}

		public override void WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess)
		{
			string text = _context.Global.Services.Naming.ForFieldSetter(_universalTimeField);
			string text2 = _context.Global.Services.Naming.ForFieldGetter(_dateTimeOffsetDateTimeField);
			string text3 = _context.Global.Services.Naming.ForFieldGetter(_dateTimeDateDataField);
			writer.WriteLine($"({destinationVariable}).{text}(({sourceVariable.Load()}.{text2}().{text3}() & 0x3FFFFFFFFFFFFFFF) - {504911232000000000L});");
		}

		public sealed override void WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool callConstructor, IRuntimeMetadataAccess metadataAccess)
		{
			string text = _context.Global.Services.Naming.ForFieldGetter(_universalTimeField);
			string text2 = _context.Global.Services.Naming.ForFieldSetter(_dateTimeOffsetDateTimeField);
			string text3 = _context.Global.Services.Naming.ForFieldSetter(_dateTimeOffsetMinutesField);
			string text4 = _context.Global.Services.Naming.ForFieldSetter(_dateTimeDateDataField);
			MethodDefinition methodDefinition = _typeRef.Resolve().Methods.Single((MethodDefinition m) => m.Name == "ToLocalTime" && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.MetadataType == MetadataType.Boolean);
			TypeDefinition typeDefinition = _context.Global.Services.TypeProvider.OptionalResolveInCoreLibrary("System", "ArgumentOutOfRangeException");
			MethodDefinition method = typeDefinition.Methods.Single((MethodDefinition m) => m.HasThis && m.IsConstructor && m.Parameters.Count == 2 && m.Parameters[0].ParameterType.MetadataType == MetadataType.String && m.Parameters[1].ParameterType.MetadataType == MetadataType.String);
			string text5 = destinationVariable.GetNiceName() + "Staging";
			string text6 = destinationVariable.GetNiceName() + "DateTime";
			string text7 = "(" + variableName + ")." + text + "()";
			object[] obj = new object[4] { text7, null, null, null };
			DateTime minValue = DateTime.MinValue;
			obj[1] = minValue.Ticks - 504911232000000000L;
			obj[2] = text7;
			minValue = DateTime.MaxValue;
			obj[3] = minValue.Ticks - 504911232000000000L;
			writer.WriteLine(string.Format("if ({0} < {1} || {2} > {3})", obj));
			using (new BlockWriter(writer))
			{
				string text8 = metadataAccess.StringLiteral("ticks");
				string text9 = metadataAccess.StringLiteral("Ticks must be between DateTime.MinValue.Ticks and DateTime.MaxValue.Ticks.");
				writer.WriteLine(_context.Global.Services.Naming.ForVariable(typeDefinition) + " exception = " + Emit.NewObj(_context, typeDefinition, metadataAccess) + ";");
				writer.WriteMethodCallStatement(metadataAccess, "exception", null, method, MethodCallType.Normal, text8, text9);
				writer.WriteStatement(Emit.RaiseManagedException("exception"));
			}
			writer.WriteLine();
			writer.WriteLine(_context.Global.Services.Naming.ForVariable(_typeRef) + " " + text5 + ";");
			writer.WriteLine(_context.Global.Services.Naming.ForVariable(_systemDateTime) + " " + text6 + ";");
			writer.WriteLine($"{text6}.{text4}({text7} + {504911232000000000L});");
			writer.WriteLine(text5 + "." + text2 + "(" + text6 + ");");
			writer.WriteLine(text5 + "." + text3 + "(0);");
			writer.WriteLine(_context.Global.Services.Naming.ForVariable(methodDefinition.ReturnType) + " result;");
			writer.WriteMethodCallWithResultStatement(metadataAccess, Emit.AddressOf(text5), null, methodDefinition, MethodCallType.Normal, "result", "true");
			writer.WriteStatement(destinationVariable.Store("result"));
		}
	}
}
