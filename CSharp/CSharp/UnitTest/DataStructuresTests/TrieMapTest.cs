using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DataStructures.Trees;
using NUnit.Framework;
using Fact = NUnit.Framework.TestAttribute;

namespace UnitTest.DataStructuresTests
{
    public static class TrieMapTest
    {
        [Fact]
        public static void DoTest()
        {
            var trieMap = new TrieMap<int>();

            // Insert some how to words
            const string prefixHowTo = "How to make";
            var wordHowToSand = prefixHowTo + " a sandwitch";
            var wordHowToRobot = prefixHowTo + " a robot";
            var wordHowToOmelet = prefixHowTo + " an omelet";
            var wordHowToProp = prefixHowTo + " a proposal";
            var listOfHow = new List<string> { wordHowToSand, wordHowToRobot, wordHowToOmelet, wordHowToProp };
            trieMap.Add(wordHowToOmelet, 7);
            trieMap.Add(wordHowToSand, 11);
            trieMap.Add(wordHowToRobot, 15);
            trieMap.Add(wordHowToProp, 19);

            // Count of words = 4
            Debug.Assert(trieMap.Count == 4);

            // Insert some dictionary words
            var prefixAct = "act";
            var wordActs = prefixAct + "s";
            var wordActor = prefixAct + "or";
            var wordActing = prefixAct + "ing";
            var wordActress = prefixAct + "ress";
            var wordActive = prefixAct + "ive";
            var listOfActWords = new List<string> { wordActs, wordActor, wordActing, wordActress, wordActive };
            trieMap.Add(wordActress, 82);
            trieMap.Add(wordActive, 65);
            trieMap.Add(wordActing, 34);
            trieMap.Add(wordActs, 81);
            trieMap.Add(wordActor, 32);

            // Count of words = 9
          Assert.AreEqual(9, trieMap.Count);

            // ASSERT THE WORDS IN TRIE.

            // Search for a word that doesn't exist
            Assert.False(trieMap.ContainsWord(prefixHowTo));

            // Search for prefix
            Assert.True(trieMap.ContainsPrefix(prefixHowTo));

            // Search for a prefix using a word
            Assert.True(trieMap.ContainsPrefix(wordHowToSand));

            // Get all words that start with the how-to prefix
            var someHowToWords = trieMap.SearchByPrefix(prefixHowTo).ToList();
          Assert.AreEqual(someHowToWords.Count, listOfHow.Count);

            // Assert there are only two words under the prefix "acti" -> active, & acting
            var someActiWords = trieMap.SearchByPrefix("acti").Select(item => item.Key).ToList();
          Assert.AreEqual(2, someActiWords.Count);
            Assert.Contains(wordActing, someActiWords);
            Assert.Contains(wordActive, someActiWords);

            // Assert that "acto" is not a word
            Assert.False(trieMap.ContainsWord("acto"));

            //
            // TEST GETTING VALUES ASSOCIATED TO WORDS
            int actressRecord;
            trieMap.SearchByWord(wordActress, out actressRecord);
          Assert.AreEqual(82, actressRecord);
            int howToProposeRequests;
            trieMap.SearchByWord(wordHowToProp, out howToProposeRequests);
          Assert.AreEqual(19, howToProposeRequests);

            //
            // TEST DELETING SOMETHINGS

            // Removing a prefix should fail
            bool removingActoFails;
            try
            {
                // try removing a non-terminal word
                trieMap.Remove("acto");
                removingActoFails = false;
            }
            catch
            {
                // if exception occured then code works, word doesn't exist.
                removingActoFails = true;
            }

            Assert.True(removingActoFails);
          Assert.AreEqual(9, trieMap.Count);

            // Removing a word should work
            bool removingActingPasses;
            try
            {
                // try removing a non-terminal word
                trieMap.Remove(wordActing);
                removingActingPasses = true;
            }
            catch
            {
                // if exception occured then code DOESN'T work, word does exist.
                removingActingPasses = false;
            }

            Assert.True(removingActingPasses);
          Assert.AreEqual(8, trieMap.Count);
            someActiWords = trieMap.SearchByPrefix("acti").Select(item => item.Key).ToList();
            // Assert.Single(someActiWords);
            Assert.Contains(wordActive, someActiWords);

            //
            // TEST ENUMERATOR
            var enumerator = trieMap.GetEnumerator();
            var allWords = new List<string>();
            while (enumerator.MoveNext())
            {
                allWords.Add(enumerator.Current.Key);
            }

            // Assert size
          Assert.AreEqual(allWords.Count, trieMap.Count);

            // Assert each element
            foreach (var word in allWords)
            {
                Assert.True(listOfActWords.Contains(word) || listOfHow.Contains(word));
            }
        }
    }
}
