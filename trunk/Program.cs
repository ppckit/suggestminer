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
    class Program
    {
        static void Main(string[] args)
        {
            SuggestionsSet BunchOfSuggestions = new SuggestionsSet(args[0], args[1]);
        }
    }

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
            //ThreadPool.SetMaxThreads(100,1000);
            ServicePointManager.MaxServicePoints = 128;
            _keywordsFilename = FileName;
			_workqueue.Enqueue(SuggestionString);
			ProcessQueue();
			WriteToFile();
        }
        public void ProcessQueue()
        {
			while (_workqueue.Count > 0 || _threadCount>0)
			{
				if (_workqueue.Count > 0)
				{
					ThreadPool.QueueUserWorkItem(new WaitCallback(GetSuggestions), _workqueue.Dequeue());
				}
			}
        }
		private void GetSuggestions(Object SuggestionObject)
		{
			Interlocked.Increment(ref _threadCount);
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
			else
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
			Interlocked.Decrement(ref _threadCount);
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
		public void TimeTakenTest()
		{
			DateTime StartTime = DateTime.Now;
			SuggestionsSet BunchOfSuggestions = new SuggestionsSet("Expedia.csv","Expedia+");
			TimeSpan TimeTaken = DateTime.Now.Subtract(StartTime);
			Console.WriteLine(TimeTaken.TotalSeconds);
		}
	}
}
