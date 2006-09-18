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
    public class SuggestionState
    {
        public  HttpWebRequest Request;
        public  string Keyword;
    }

    public class SuggestionsSet
    {
		private int _threadCount = 0;
        private string _keywordsFilename;
        private Dictionary<string, int> _suggestions = new Dictionary<string, int>();
		private Queue<string> _workqueue = new Queue<string>();

        public SuggestionsSet(string FileName, string SuggestionString)
        {
            ThreadPool.SetMaxThreads(1000,1000);
            ServicePointManager.MaxServicePoints = 128;
            _keywordsFilename = FileName;
			_workqueue.Enqueue(SuggestionString);
			ProcessQueue();
			WriteToFile();
        }
		public int Count
		{
			get
			{
				return _suggestions.Count;
			}
		}

        private void ProcessQueue()
        {
			while (_workqueue.Count > 0 || _threadCount>0)
			{
				//Console.Write("{0}:{1}  ", _threadCount, _workqueue.Count);
				if (_workqueue.Count > 0 && _threadCount<100)
				{
					Interlocked.Increment(ref _threadCount);
					ThreadPool.QueueUserWorkItem(new WaitCallback(GetSuggestions), _workqueue.Dequeue());
				}
				else
				{
					Thread.Sleep(20);
				}
			}
        }
		private void GetSuggestions(Object SuggestionObject)
		{
			try
			{
				string SuggestionString = (string)SuggestionObject;
				SuggestToolResults SuggestToolAnswer = new SuggestToolResults(SuggestionString);
				SuggestToolAnswer.Completed.WaitOne();
				if (SuggestToolAnswer.Count == 10)
				{
					KeywordSet NextKeywords = new KeywordSet(SuggestionString);
					foreach (string NextKeyword in NextKeywords)
					{
						_workqueue.Enqueue(NextKeyword);
					}
				}
				else if (SuggestToolAnswer.Success == true)
				{
					foreach (string Keyword in SuggestToolAnswer.Keys)
					{
						if (!_suggestions.ContainsKey(Keyword))
						{
							_suggestions.Add(Keyword, SuggestToolAnswer[Keyword]);
							Console.WriteLine(Keyword);
						}
					}
				}
				else
				{
					_workqueue.Enqueue(SuggestionString);
				}
			}
			catch (Exception error)
			{
				Console.WriteLine(error.Message);
			}
			finally
			{
				Interlocked.Decrement(ref _threadCount);
			}
		}
		private void WriteToFile()
		{
			StreamWriter KeywordsFileWriter = new StreamWriter(_keywordsFilename);
			foreach (string Keyword in _suggestions.Keys)
			{
				KeywordsFileWriter.WriteLine(Keyword + "," + _suggestions[Keyword]);
			}
			KeywordsFileWriter.Close();
		}
   }
	[TestFixture]
	public class SuggestionsSetTests
	{
		[Test]
		public void PerformanceTests()
		{
			DateTime StartTime = DateTime.Now;
			SuggestionsSet BunchOfSuggestions = new SuggestionsSet("HotelsInLondon.csv", "hotels+in+london+");
			TimeSpan TimeTaken = DateTime.Now.Subtract(StartTime);
			Assert.Greater(100,TimeTaken.TotalSeconds);
			Assert.AreEqual(174,BunchOfSuggestions.Count);
		}
	}
}

