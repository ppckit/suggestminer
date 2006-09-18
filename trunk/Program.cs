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

}
