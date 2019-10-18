using System.Collections.Generic;
using AGDev;
namespace AGBLang {
	#region grammar block
	public interface GrammarBlock {
		GrammarUnit unit { get; }
		ClusterGrammarBlock cluster { get; }
		GrammarBlock modifier { get; }
		GrammarBlock metaInfo { get; }
	}
	public interface ClusterGrammarBlock : GrammarBlock {
		IList<GrammarBlock> blocks { get; }
	}
	public interface GrammarUnit : GrammarBlock {
		string word { get; }
	}
	public interface BehaviorExpression {
		GrammarBlock subject { get; }
		GrammarUnit verb { get; }
		GrammarBlock asGBlock { get; }
	}
	#endregion
	#region interpreter
	public interface NaturalLanguageProcessor {
		void PerformSyntacticProcess(string naturalLanguage, Taker<GrammarBlock> listener);
	}
	public interface ConfigurableLProcessor : NaturalLanguageProcessor {
		void SetFormat(string configuration);
		void AddDictoinary(string configuration);
	}
	#endregion
}