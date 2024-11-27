using System.Text.RegularExpressions;

namespace GameMonitor.tests
{
    [TestFixture]
    public class RegexTests
    {   
        [TestCase("TS ELIMINATED @", "counterterrorists", true)]
        [TestCase("TERRORISTS WIN @", "terrorists", true)]
        [TestCase("BOMB DETONADED @", "terrorists", true)]
        [TestCase("CTS ELIMINATED @", "terrorists", true)]
        [TestCase("BOMB DEFUSED @", "counterterrorists", true)]
        [TestCase("COUNTER-TERRORISTS WIN @", "counterterrorists", true)]
        [TestCase("INVALID INPUT @", "", false)]
        [TestCase("random text", "asdasdsd", false)]
        [TestCase("", "", false)]
        public void Regex_ShouldMatchExpectedGroup(string input, string expectedGroup, bool isMatchExpected)
        {
            var match = Program.Regex.Match(input); 

            Assert.AreEqual(isMatchExpected, match.Success, $"Expected match success to be {isMatchExpected} for input '{input}'.");

            if (isMatchExpected)
            {
                Assert.IsTrue(match.Groups[expectedGroup].Success, $"Expected group '{expectedGroup}' to match.");
            }
        }
    }
}