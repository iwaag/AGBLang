using System.Collections.Generic;
using System.IO;
using AGDev;
namespace AGBLang.StdUtil {
	public class ExampleNLangProcessor : NaturalLanguageProcessor {
		NaturalLanguageProcessor nlProcessor;
		public ExampleNLangProcessor(string dictionaryFilePath) {
			#region MAnlys
			var mAnlys = new StdMAnalyzer { };
			{
				var ignoreReader = new IgnoreBlockReader { markers = new List<string> { " ", "\r", "\n", "\t" } };
				var markerReader = new MarkerBlockReader {
					markers = new List<MarkerAndFormatID> {
				new MarkerAndFormatID{ marker = ".",   formatID = 2 },
				new MarkerAndFormatID{ marker = ",",   formatID = 3 },
				new MarkerAndFormatID{ marker = "###", formatID = 4 },
				new MarkerAndFormatID{ marker = ":",   formatID = 5 },
			}
				};
				var quoteBlockMarker = new QuoteBlockReader { formatID = 1, leftMarker = "\"", rightMarker = "\"" };
				mAnlys.blockReaderes.Add(ignoreReader);
				mAnlys.blockReaderes.Add(markerReader);
				mAnlys.blockReaderes.Add(quoteBlockMarker);
				mAnlys.generalReaders.Add(new NumberReader { });
				mAnlys.generalReaders.Add(new WordReader { });
			}
			#endregion
			#region MAnlys for dictionary creation
			var mAnlysForDict = new StdMAnalyzer { };
			{
				var ignoreReader = new IgnoreBlockReader { markers = new List<string> { " " } };
				var markerReader = new MarkerBlockReader {
					markers = new List<MarkerAndFormatID> {
				new MarkerAndFormatID{ marker = "(IGNORE)",   formatID = 2 },
				new MarkerAndFormatID{ marker = "(UNREADABLE)",   formatID = 3 }
			}
				};
				var quoteBlockMarker = new QuoteBlockReader { formatID = 1, leftMarker = "#", rightMarker = "#" };
				mAnlysForDict.blockReaderes.Add(ignoreReader);
				mAnlysForDict.blockReaderes.Add(markerReader);
				mAnlysForDict.blockReaderes.Add(quoteBlockMarker);
				mAnlysForDict.generalReaders.Add(new WordReader { });
			}
			#endregion
			#region dictionary creation
			var gDict = new IncrGAnlysDictionary();
			var formatReader = new FormatReader();
			formatReader.fReader = new FormToGAnlys { analyzer = mAnlysForDict, dict = gDict };
			formatReader.gAnlysDict = gDict;
			formatReader.metaInfos = new Dictionary<string, GrammarBlock>();
			formatReader.metaInfos[StdMetaInfos.sentenceCluster.word] = StdMetaInfos.sentenceCluster;
			formatReader.metaInfos[StdMetaInfos.nominalBlock.word] = StdMetaInfos.nominalBlock;
			formatReader.metaInfos[StdMetaInfos.verbalBlock.word] = StdMetaInfos.verbalBlock;
			formatReader.metaInfos[StdMetaInfos.quoteBlock.word] = StdMetaInfos.quoteBlock;
			formatReader.metaInfos[StdMetaInfos.sv.word] = StdMetaInfos.sv;
			formatReader.metaInfos[StdMetaInfos.conditionSV.word] = StdMetaInfos.conditionSV;
			formatReader.metaInfos[StdMetaInfos.negated.word] = StdMetaInfos.negated;
			formatReader.metaInfos[StdMetaInfos.title.word] = StdMetaInfos.title;
			formatReader.metaInfos[StdMetaInfos.clusterExtractable.word] = StdMetaInfos.clusterExtractable;
			formatReader.metaInfos[StdMetaInfos.metaCluster.word] = StdMetaInfos.metaCluster;
			formatReader.metaInfos[StdMetaInfos.anonymousCommand.word] = StdMetaInfos.anonymousCommand;
			formatReader.metaInfos[StdMetaInfos.modifierCluster.word] = StdMetaInfos.modifierCluster;
			formatReader.metaInfos[StdMetaInfos.quoteSV.word] = StdMetaInfos.quoteSV;
			formatReader.metaInfos[StdMetaInfos.pronoun.word] = StdMetaInfos.pronoun;
			formatReader.metaInfos[StdMetaInfos.plural.word] = StdMetaInfos.plural;
			formatReader.metaInfos[StdMetaInfos.unreadable.word] = StdMetaInfos.unreadable;
			{
				var conjCand = new IGAnlys_Candidates { };
				var and = new IGAnlys_Word { };
				and.AddMorphemeText("and");
				var or = new IGAnlys_Word { };
				or.AddMorphemeText("or");
				var quote = new IGAnlys_Quote { morphemeID = 3 };
				conjCand.analyzers.Add(and);
				conjCand.analyzers.Add(or);
				conjCand.analyzers.Add(quote);
				var conjGAnlys = new IGAnlys_RepeatableBlock { baseAnalyzer = conjCand };
				formatReader.cojunctionIGAnlys = conjGAnlys;
			}
			#endregion
			var dictionaryJsonText = File.ReadAllText(dictionaryFilePath);
			var reader = new RootReader { };
			reader.subReader.Push(new GrammarDictRoot { gAnlysDict = gDict });
			reader.Read(dictionaryJsonText);
			var rootGAnlys = gDict.dict["RootUnit"];
			nlProcessor = new MGSyntacticProcessor { gAnalyzer = new StdGrammarAnalyzer { incrGAnalyzer = rootGAnlys }, mAnalyzer = mAnlys };
		}

		void NaturalLanguageProcessor.PerformSyntacticProcess(string naturalLanguage, AsyncCollector<GrammarBlock> listener) {
			nlProcessor.PerformSyntacticProcess(naturalLanguage, listener);
		}
	}
}
