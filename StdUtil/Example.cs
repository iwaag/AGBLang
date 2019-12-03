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
			FormatReader.fReader = new FormToGAnlys { analyzer = mAnlysForDict, dict = gDict };
			FormatReader.gAnlysDict = gDict;
			FormatReader.metaInfos = new Dictionary<string, GrammarBlock>();
			FormatReader.metaInfos[StdMetaInfos.sentenceCluster.word] = StdMetaInfos.sentenceCluster;
			FormatReader.metaInfos[StdMetaInfos.nominalBlock.word] = StdMetaInfos.nominalBlock;
			FormatReader.metaInfos[StdMetaInfos.verbalBlock.word] = StdMetaInfos.verbalBlock;
			FormatReader.metaInfos[StdMetaInfos.quoteBlock.word] = StdMetaInfos.quoteBlock;
			FormatReader.metaInfos[StdMetaInfos.sv.word] = StdMetaInfos.sv;
			FormatReader.metaInfos[StdMetaInfos.conditionSV.word] = StdMetaInfos.conditionSV;
			FormatReader.metaInfos[StdMetaInfos.negated.word] = StdMetaInfos.negated;
			FormatReader.metaInfos[StdMetaInfos.title.word] = StdMetaInfos.title;
			FormatReader.metaInfos[StdMetaInfos.clusterExtractable.word] = StdMetaInfos.clusterExtractable;
			FormatReader.metaInfos[StdMetaInfos.metaCluster.word] = StdMetaInfos.metaCluster;
			FormatReader.metaInfos[StdMetaInfos.anonymousCommand.word] = StdMetaInfos.anonymousCommand;
			FormatReader.metaInfos[StdMetaInfos.modifierCluster.word] = StdMetaInfos.modifierCluster;
			FormatReader.metaInfos[StdMetaInfos.quoteSV.word] = StdMetaInfos.quoteSV;
			FormatReader.metaInfos[StdMetaInfos.pronoun.word] = StdMetaInfos.pronoun;
			FormatReader.metaInfos[StdMetaInfos.plural.word] = StdMetaInfos.plural;
			FormatReader.metaInfos[StdMetaInfos.unreadable.word] = StdMetaInfos.unreadable;
			{
				var conjCand = new IGAnlys_Candidates { };
				var and = new IGAnlys_Word { };
				and.AddMorphemeText("and");
				var or = new IGAnlys_Word { };
				or.AddMorphemeText("or");
				var quote = new IGAnlys_Quote { morphemeID = 3 };
				conjCand.candidates.Add(and);
				conjCand.candidates.Add(or);
				conjCand.candidates.Add(quote);
				var conjGAnlys = new IGAnlys_RepeatableBlock { baseAnalyzer = conjCand };
				FormatReader.cojunctionIGAnlys = conjGAnlys;
				FormatReader.preparer = new AnalyzePreparer();
			}
			#endregion
			var dictionaryJsonText = File.ReadAllText(dictionaryFilePath);
			var reader = new RootReader { };
			reader.subReader.Push(new GrammarDictRoot { gAnlysDict = gDict });
			reader.Read(dictionaryJsonText);
			var rootGAnlys = gDict.dict["RootUnit"];
			nlProcessor = new MGSyntacticProcessor {
				gAnalyzer = new StdGrammarAnalyzer { incrGAnalyzer = rootGAnlys, analyzePreparer = FormatReader.preparer },
				mAnalyzer = mAnlys
			};
		}

		void NaturalLanguageProcessor.PerformSyntacticProcess(string naturalLanguage, Taker<GrammarBlock> listener) {
			nlProcessor.PerformSyntacticProcess(naturalLanguage, new PrvtLis { listener = listener } );

		}
        class PrvtLis : Taker<GrammarBlock>  {
            public Taker<GrammarBlock> listener;
            void Taker<GrammarBlock>.Take(GrammarBlock item) {
                ImmediateGiver<GrammarBlock, GrammarBlock> filter = new StdGBlockFilter();
                var filteredGBlock = filter.PickBestElement(item);
                listener.Take(filteredGBlock);
            }
			void Taker<GrammarBlock>.None() {
				listener.None();
			}
		}
    }
}
