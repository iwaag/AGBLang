using System.Collections.Generic;
using AGDev;
namespace AGBLang {
	#region behavior
	public interface BehaviorRequestListener {
		void OnSucceed(BehaviorTrigger trigger);
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
		void AnalyzeBehavior(GrammarBlock expressionGBlock, AsyncCollector<BehaviorTriggerSet> listener);
		bool AskForFloatAnswer(GrammarBlock question, AnswerListener<float> listener);
	}
	public interface ConfigurableBehaviorAnalyzer : BehaviorAnalyzer {
		void AddBehaver(Behaver behaver);
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
		void ReadyBehavior(BehaviorExpression bExpr, BehaviorRequestListener reqListener);
	}
	public interface BehaviorChecker {
		void ReadyCheckBehavior(BehaviorExpression bExpr, BehaviorCheckRequestListener chkReqListener);
	}
	public interface BehaviorSetCheck : BehaviorSetter, BehaviorChecker { }
	public interface Behaver : BehaviorSetCheck, AttributeMatcher { }
	#endregion
	#region behavior check
	public interface BehaviorCheckRequestListener {
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
		Picker<NounCommonSenseUnit, string> nounCSPicker { get; }
	}
	#endregion
}