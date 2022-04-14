namespace Unity.IL2CPP.Contexts.Services
{
	public interface IICallMappingService
	{
		string ResolveICallFunction(string icall);

		string ResolveICallHeader(string icall);
	}
}
