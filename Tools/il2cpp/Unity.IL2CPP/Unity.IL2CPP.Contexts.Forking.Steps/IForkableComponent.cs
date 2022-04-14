namespace Unity.IL2CPP.Contexts.Forking.Steps
{
	public interface IForkableComponent<TWrite, TRead, TFull>
	{
		void ForkForPrimaryWrite(PrimaryWriteAssembliesLateAccessForkingContainer lateAccess, out TWrite writer, out TRead reader, out TFull full);

		void MergeForPrimaryWrite(TFull forked);

		void ForkForPrimaryCollection(PrimaryCollectionLateAccessForkingContainer lateAccess, out TWrite writer, out TRead reader, out TFull full);

		void MergeForPrimaryCollection(TFull forked);

		void ForkForSecondaryWrite(SecondaryWriteLateAccessForkingContainer lateAccess, out TWrite writer, out TRead reader, out TFull full);

		void MergeForSecondaryWrite(TFull forked);

		void ForkForSecondaryCollection(SecondaryCollectionLateAccessForkingContainer lateAccess, out TWrite writer, out TRead reader, out TFull full);

		void MergeForSecondaryCollection(TFull forked);

		void ForkForPartialPerAssembly(PerAssemblyLateAccessForkingContainer lateAccess, out TWrite writer, out TRead reader, out TFull full);

		void MergeForPartialPerAssembly(TFull forked);

		void ForkForFullPerAssembly(PerAssemblyLateAccessForkingContainer lateAccess, out TWrite writer, out TRead reader, out TFull full);

		void MergeForFullPerAssembly(TFull forked);
	}
}
