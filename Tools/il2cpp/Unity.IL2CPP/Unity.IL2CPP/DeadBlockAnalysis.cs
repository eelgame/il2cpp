using System.Linq;
using Unity.Cecil.Awesome.CFG;

namespace Unity.IL2CPP
{
	internal static class DeadBlockAnalysis
	{
		public static void MarkBlocksDeadIfNeeded(InstructionBlock[] instructionBlocks)
		{
			if (instructionBlocks.Count() != 1)
			{
				for (int i = 0; i < instructionBlocks.Length; i++)
				{
					instructionBlocks[i].MarkIsDead();
				}
				instructionBlocks[0].MarkIsAliveRecursive();
			}
		}
	}
}
