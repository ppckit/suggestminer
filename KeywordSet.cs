using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Xml;
using System.Xml.XPath;
using System.IO;
using System.Collections;
using System.Threading;
using NUnit.Framework;

namespace SuggestMiner
{
	public class KeywordSet : List<string>
	{
		private const string Alphabet = "abcdefghijklmnopqrstuvwxyz.1234567890";
		public KeywordSet(string Prefix):base()
		{
			if (Prefix.Length > 0)
			{
				char LastLetter = Prefix[Prefix.Length - 1];
				if (LastLetter != '+')
				{
					this.Add(Prefix + "+");
				}
			}
			foreach (char Letter in Alphabet.ToCharArray())
			{
				this.Add(Prefix + Letter);
			}

		}
	}

	[TestFixture]
	public class KeywordSetTests
	{
		[Test]
		public void KeywordEndingInSpaceTest()
		{
			KeywordSet EndingInSpaceTest = new KeywordSet("Hotels+In+");
			Assert.AreEqual(37, EndingInSpaceTest.Count);
		}
		[Test]
		public void KeywordEndingInLetterTest()
		{
			KeywordSet EndingInLetterTest = new KeywordSet("Hotels+In");
			Assert.AreEqual(38, EndingInLetterTest.Count);
		}
		[Test]
		public void EmptyKeywordTest()
		{
			KeywordSet EmptyKeywordTest = new KeywordSet("");
			Assert.AreEqual(37, EmptyKeywordTest.Count);
		}
	}
}
