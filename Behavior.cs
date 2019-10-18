using System.Collections.Generic;
using AGDev;
namespace AGBLang {
	#region behavior
	public interface BehaviorReadySupport {
		AssetMediator assetMediator { get; }
	}

	public interface TriggerInfo {
		bool IsReady();
	}

	public class NameAndBTrigger {
		public string name;
		public BehaviorTrigger bTrigger;
	}
	public class BehaviorTriggerSet {
		public BehaviorTrigger rootTrigger;
		public IEnumerable<NameAndBTrigger> namedTriggers;
	}
	public interface BehaviorAnalyzer {
		void AnalyzeBehavior(GrammarBlock expressionGBlock, Taker<BehaviorTriggerSet> listener);
		bool AskForFloatAnswer(GrammarBlock question, AnswerListener<float> listener);
	}
	public interface ConfigurableBehaviorAnalyzer : BehaviorAnalyzer {
		void AddBehaver(Behaver behaver);
	}
	public interface AssetMediator {
		AssetType GetImplementedAsset<AssetType>(GrammarBlock gBlock);
		AssetType GetImplementedModule<AssetType>();
		void SeekAsset<AssetType>(GrammarBlock gBlock, Taker<AssetType> taker);
		void SeekModule<AssetType>(Taker<AssetType> taker);
		IEnumerable<AssetType> GetImplementedAssets<AssetType>();
	}
	#endregion
	#region behaver
	public enum AttributeMatchResult {
		POSITIVE = 0,
		NEGATIVE = 1,
		NEUTRAL = 2
	}
	public interface AttributeMatcher {
		AttributeMatchResult MatchAttribue(GrammarBlock attribute);
	}
	public interface BehaviorSetter {
		BehaviorTrigger ReadyBehavior(BehaviorExpression bExpr, BehaviorReadySupport reqListener);
	}
	public interface BehaviorChecker {
		BehaviorCheckTrigger ReadyCheckBehavior(BehaviorExpression bExpr, BehaviorReadySupport chkReqListener);
	}
	public interface BehaviorSetCheck : BehaviorSetter, BehaviorChecker { }
	public interface Behaver : BehaviorSetCheck, AttributeMatcher { }
	#endregion
	#region behavior check
	public interface BehaviorCheckReadySupport {
		void OnSucceed(BehaviorCheckTrigger trigger);
	}
	public interface BehaviorCheckListener {
		void OnResultInPositive();
		void OnResultInNegative();
	}
	public interface BehaviorCheckTrigger {
		void BeginBehavior(BehaviorCheckListener behaviorListener);
	}
	#endregion
	#region common sense
	public interface AnswerListener<AnswerType> {
		void OnAnswerUpdate(AnswerType answer);
	}
	public interface NounCommonSenseUnit {
		bool isPhysical { get; }
		IEnumerable<string> give { get; }
		IEnumerable<string> givenBy { get; }
	}
	public interface CommonSenseGiver {
		Giver<NounCommonSenseUnit, string> nounCSGiver { get; }
	}
	#endregion
}