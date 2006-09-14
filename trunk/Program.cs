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
            Suggestions BunchOfSuggestions = new Suggestions(args[0], args[1]);
        }
    }
    public class Suggestions
    {
        public Suggestions(string FileName,string Prefix)
        {
            SuggestionsSet Suggestions = new SuggestionsSet(FileName, Prefix);
            Suggestions.Execute();
        }       
    }

    public class SuggestionState
    {
        public  HttpWebRequest Request;
        public  string Keyword;
    }

    public class SuggestionsSet
    {
        private string _keywordsFilename;
        private Dictionary<string, int> _suggestions = new Dictionary<string, int>();
        private string _initialSuggestion;
        private int ThreadCount = 0;

        public SuggestionsSet(string FileName, string SuggestionString)
        {
            ThreadPool.SetMaxThreads(100,1000);
            ServicePointManager.MaxServicePoints = 128;
            _keywordsFilename = FileName;
            _initialSuggestion = SuggestionString;
        }
        public void Execute()
        {
            this.GetSuggestions(_initialSuggestion);
            while (ThreadCount != 0)
            {
                Thread.Sleep(TimeSpan.FromSeconds(10));
            }
            StreamWriter KeywordsFileWriter = new StreamWriter(_keywordsFilename);
            foreach (string Keyword in _suggestions.Keys)
            {
                KeywordsFileWriter.WriteLine(Keyword + "," + _suggestions[Keyword]);
            }
            KeywordsFileWriter.Close();
        }

        public void GetSuggestions(String SuggestionString)
        {
            HttpWebRequest SuggestionResultsRequest = (HttpWebRequest)WebRequest.Create("http://www.google.com/complete/search?output=toolbar&q=" + SuggestionString);
            SuggestionResultsRequest.KeepAlive = false;
            SuggestionState State = new SuggestionState();
            State.Keyword = SuggestionString;
            State.Request = SuggestionResultsRequest;
            Interlocked.Increment(ref ThreadCount);
            IAsyncResult result = SuggestionResultsRequest.BeginGetResponse(GotSuggestions,State);
        }
        public void GotSuggestions(IAsyncResult asynchronousResult)
        {
            try
            {
                SuggestionState State = (SuggestionState)asynchronousResult.AsyncState;
                HttpWebRequest Request = State.Request;
                string SuggestionString = State.Keyword;
                HttpWebResponse SuggestionResultsResponse = (HttpWebResponse)Request.EndGetResponse(asynchronousResult);
                StreamReader SuggestionResultsStream = new StreamReader(SuggestionResultsResponse.GetResponseStream());
                XPathDocument SuggestionResultsXPathDoc = new XPathDocument(SuggestionResultsStream);
                XPathNavigator SuggestionsNav = SuggestionResultsXPathDoc.CreateNavigator();
                XPathNodeIterator SuggestionsIterator = SuggestionsNav.Select("//CompleteSuggestion");
                Console.WriteLine("{0}: {1} results", SuggestionString, SuggestionsIterator.Count);
                if (SuggestionsIterator.Count == 10)
                {
                    foreach (string NextKeyword in new KeywordSet(SuggestionString))
                    {
                        GetSuggestions(NextKeyword);
                    }
                }
                while (SuggestionsIterator.MoveNext())
                {
                    string Keyword = SuggestionsIterator.Current.SelectSingleNode("suggestion").GetAttribute("data", "");
                    int NumberOfQueries = Convert.ToInt32(SuggestionsIterator.Current.SelectSingleNode("num_queries ").GetAttribute("int", ""));
                    if (!_suggestions.ContainsKey(Keyword))
                    {
                        _suggestions.Add(Keyword, NumberOfQueries);
                    }
                }
                SuggestionResultsResponse.Close();
            }
            catch (Exception error)
            {
                Console.WriteLine(error.Message);
            }
            finally
            {
                Interlocked.Decrement(ref ThreadCount);
            }
        }
    }
}
