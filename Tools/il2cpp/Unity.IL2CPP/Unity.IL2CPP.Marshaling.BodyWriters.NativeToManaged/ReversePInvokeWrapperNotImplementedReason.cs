namespace Unity.IL2CPP.Marshaling.BodyWriters.NativeToManaged
{
	internal enum ReversePInvokeWrapperNotImplementedReason
	{
		None,
		IsInstanceMethod,
		HasGenericParameters,
		AtLeastOneParameterIsGenericInstance,
		IsIntrinsicRemap,
		MissingPInvokeCallbackAttribute
	}
}
