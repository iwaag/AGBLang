﻿using System;
using System.Collections.Generic;
using System.Text;
using AGBLang;
namespace AGDev.StdUtil {
	public class MGSyntacticProcessor : NaturalLanguageProcessor {
		public MorphemeAnalyzer mAnalyzer;
		public GrammarAnalyzer gAnalyzer;
		void NaturalLanguageProcessor.PerformSyntacticProcess(string naturalLanguage, AsyncCollector<GrammarBlock> listener) {
			mAnalyzer.AnalyzeForrmat(naturalLanguage, new FLis { parent = this, rootListener = listener});
		}
		public class FLis : AsyncCollector<DivisibleEnumerable<Morpheme>> {
			public MGSyntacticProcessor parent;
			public AsyncCollector<GrammarBlock> rootListener;
			public bool didCollect = false;
			void Collector<DivisibleEnumerable<Morpheme>>.Collect(DivisibleEnumerable<Morpheme> item) {
				didCollect = true;
				parent.gAnalyzer.AnalyzeGrammar(new GInput { morphemes = item }, rootListener);
			}

			void AsyncCollector<DivisibleEnumerable<Morpheme>>.OnFinish() {
				if (!didCollect) {
					rootListener.OnFinish();
				}

			}
			public class GInput : GAnlysInput {
				public DivisibleEnumerable<Morpheme> morphemes;
				IEnumerable<Morpheme> GAnlysInput.followings => morphemes;
				GAnlysInput GAnlysInput.GetAdvanced(int advanceCount) {
					return new GInput { morphemes = morphemes.GetFollowing(advanceCount) };
				}
			}
		}
	}
	#region mutable grammar block
	public interface MutableGrammarBlock : GrammarBlock {
		MutableGrammarUnit mUnit { get; }
		MutableClusterGrammarBlock mCluster { get; }
		void AddMetaInfo(GrammarBlock block);
		void AddModifier(GrammarBlock block);
	}
	public interface MutableClusterGrammarBlock : MutableGrammarBlock, ClusterGrammarBlock {
		void AddBlock(GrammarBlock grammarBlock);
	}
	public interface MutableGrammarUnit : MutableGrammarBlock, GrammarUnit {
		void SetWord(string word);
	}
	#endregion
	#region morpheme analysis
	public class Morpheme {
		public string word;
		public int id;
	}
	public interface DivisibleEnumerable<Type> : IEnumerable<Type> {
		DivisibleEnumerable<Type> GetFollowing(int advanceCount);
	}
	public interface MorphemeAnalyzer {
		void AnalyzeForrmat(string naturalLanguage, AsyncCollector<DivisibleEnumerable<Morpheme>> listener);
	}
	#endregion
	#region grammar analysis
	public interface GrammarAnalyzer {
		void AnalyzeGrammar(GAnlysInput input, AsyncCollector<GrammarBlock> listener);
	}
	public interface GAnlysInput {
		GAnlysInput GetAdvanced(int advanceCount);
		IEnumerable<Morpheme> followings { get; }
	}
	public interface AfterMatchListener {
		void OnResultRequested(System.Action<MutableGrammarBlock> blockCollector);
	}
	public interface IncrGAnalysisListener {
		void OnMatch(GAnlysInput _nextInput, AfterMatchListener listener, AlternativeIncrGAnalyzer alternative = null);

	}
	public interface IncrementalGAnalyzer {
		void Analyze(GAnlysInput input, IncrGAnalysisListener listener);
	}
	public interface AlternativeIncrGAnalyzer {
		void AnalyzeAgain(IncrGAnalysisListener listener);
	}
	#endregion
}
