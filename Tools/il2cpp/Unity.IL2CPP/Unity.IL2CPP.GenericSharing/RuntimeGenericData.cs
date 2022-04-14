namespace Unity.IL2CPP.GenericSharing
{
	public abstract class RuntimeGenericData
	{
		public readonly RuntimeGenericContextInfo InfoType;

		public RuntimeGenericData(RuntimeGenericContextInfo infoType)
		{
			InfoType = infoType;
		}
	}
}
