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
	public class SuggestToolResultsState
	{
		public HttpWebRequest Request;
		public string Keyword;
	}
	class SuggestToolResults:Dictionary<String,int>
	{
		public AutoResetEvent Completed = new AutoResetEvent(false);
		public bool Success = false;
		public SuggestToolResults(string StarterString):base()
		{
			HttpWebRequest SuggestionResultsRequest = (HttpWebRequest)WebRequest.Create("http://www.google.com/complete/search?output=toolbar&q=" + StarterString);
			SuggestionResultsRequest.KeepAlive = false;
			SuggestToolResultsState State = new SuggestToolResultsState();
			State.Keyword = StarterString;
			State.Request = SuggestionResultsRequest;
			IAsyncResult result = SuggestionResultsRequest.BeginGetResponse(SuggestToolCompleted, State);
		}

		public void SuggestToolCompleted(IAsyncResult asynchronousResult)
		{
			SuggestToolResultsState State = (SuggestToolResultsState)asynchronousResult.AsyncState;
			HttpWebRequest Request = State.Request;
			string SuggestionString = State.Keyword;
			try
			{
				HttpWebResponse SuggestionResultsResponse = (HttpWebResponse)Request.EndGetResponse(asynchronousResult);
				StreamReader SuggestionResultsStream = new StreamReader(SuggestionResultsResponse.GetResponseStream());
				XPathDocument SuggestionResultsXPathDoc = new XPathDocument(SuggestionResultsStream);
				XPathNavigator SuggestionsNav = SuggestionResultsXPathDoc.CreateNavigator();
				XPathNodeIterator SuggestionsIterator = SuggestionsNav.Select("//CompleteSuggestion");
				while (SuggestionsIterator.MoveNext())
				{
					string Keyword = SuggestionsIterator.Current.SelectSingleNode("suggestion").GetAttribute("data", "");
					int NumberOfQueries = Convert.ToInt32(SuggestionsIterator.Current.SelectSingleNode("num_queries ").GetAttribute("int", ""));
					if (!this.ContainsKey(Keyword))
					{
						this.Add(Keyword, NumberOfQueries);
					}
				}
				SuggestionResultsResponse.Close();
				Success = true;
			}
			catch (WebException error)
			{
				Console.WriteLine(error.Message);
			}
			finally
			{
				Completed.Set();
			}
		}
	}

	[TestFixture]
	public class SuggestToolResultsTests
	{
		[Test]
		public void PopularSearch()
		{
			SuggestToolResults PopularSearchSuggestToolResultsTest = new SuggestToolResults("Hotels+In+");
			PopularSearchSuggestToolResultsTest.Completed.WaitOne();
			Assert.AreEqual(10, PopularSearchSuggestToolResultsTest.Count);
		}
		[Test]
		public void UnpopularSearch()
		{
			SuggestToolResults PopularSearchSuggestToolResultsTest = new SuggestToolResults("dffhfhhdf");
			PopularSearchSuggestToolResultsTest.Completed.WaitOne();
			Assert.AreEqual(0, PopularSearchSuggestToolResultsTest.Count);
		}
	}
}
