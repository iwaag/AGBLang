using System.Collections.Generic;
using AGBLang;
namespace AGDev.StdUtil {
	class TimeBehaver : Behaver {
		public class CountDownCheckTrigger : BehaviorCheckTrigger {
			public float triggerTime = 0;
			public List<PrvtSession> sessions = new List<PrvtSession>();
			void BehaviorCheckTrigger.BeginBehavior(BehaviorCheckListener behaviorListener) {
				sessions.Add(new PrvtSession { parent = this, behaviorListener = behaviorListener });
			}
			public class PrvtSession {
				public CountDownCheckTrigger parent;
				public float timeElapsed = 0;
				public BehaviorCheckListener behaviorListener;
				public bool didAlreadyHit = false;
				public void Elapse(float deltaTime) {
                    if (didAlreadyHit)
                        return;
					timeElapsed += deltaTime;
					if (parent.triggerTime <= timeElapsed) {
						behaviorListener.OnResultInPositive();
                        didAlreadyHit = true;

                    }
				}
			}
		}
		public Dictionary<string, int> timeNameToMSec = new Dictionary<string, int>();
		public List<CountDownCheckTrigger> triggers = new List<CountDownCheckTrigger>();
		public void ReadyBehavior(BehaviorExpression bExp, BehaviorRequestListener bReqLis) {
			//stub : explicit time passing
		}
		class TimeCountVisitor : GrammarBlockVisitor {
			public float result = 0;
			void GrammarBlockVisitor.IfGrammarUnit(GrammarUnit gUnit) {
				if (gUnit.modifier != null) {
					GBlockVisitor_GetFloat numberFetcher = new GBlockVisitor_GetFloat();
					if (string.Compare(gUnit.word, "hour", true) == 0) {
						GrammarBlockUtils.VisitGrammarBlock(gUnit.modifier, numberFetcher);
						result += numberFetcher.numberFloat * 60 * 60;
					} else if (string.Compare(gUnit.word, "minute", true) == 0) {
						GrammarBlockUtils.VisitGrammarBlock(gUnit.modifier, numberFetcher);
						result += numberFetcher.numberFloat * 60;
					} else if (string.Compare(gUnit.word, "second", true) == 0) {
						GrammarBlockUtils.VisitGrammarBlock(gUnit.modifier, numberFetcher);
						result += numberFetcher.numberFloat;
					}
				}
			}
			void GrammarBlockVisitor.IfHasModifier(GrammarBlock  modifier) {
			}
			void GrammarBlockVisitor.IfHasMetaInfo(GrammarBlock  metaInfo) {
			}

			void GrammarBlockVisitor.IfClusterGrammarBlock(ClusterGrammarBlock cluster) {
				foreach (var block in cluster.blocks) {
					GrammarBlockUtils.VisitGrammarBlock(block, this);
				}
			}
		}
		float TimeExpresssionToFloatSec(GrammarBlock timeExpression) {
			var vis = new TimeCountVisitor();
			GrammarBlockUtils.VisitGrammarBlock(timeExpression, vis);
			return vis.result;
		}
		void BehaviorChecker.ReadyCheckBehavior(BehaviorExpression bExpr, BehaviorCheckRequestListener chkReqListener) {
			if (string.Compare(bExpr.verb.word, "pass", true) == 0) {
				var newTrigger = new CountDownCheckTrigger { triggerTime = TimeExpresssionToFloatSec(bExpr.subject) };
				triggers.Add(newTrigger);
				chkReqListener.OnSucceed(newTrigger);
			}
		}
        public void Update() {
            foreach (var trigger in triggers) {
                foreach (var session in trigger.sessions) {
                    session.Elapse(UnityEngine.Time.deltaTime);
                }
            }
            foreach (var trigger in triggers) {
                trigger.sessions.RemoveAll((session) => session.didAlreadyHit);
            }
        }
		SortedSet<string> _candidates ;
		SortedSet<string> candidates {
			get {
				if (_candidates == null) {
					_candidates = new SortedSet<string>();
					_candidates.Add("time");
					_candidates.Add("hour");
					_candidates.Add("minute");
					_candidates.Add("second");
				}
				return _candidates;
			}
		}
		AttributeMatchResult AttributeMatcher.MatchAttribue(GrammarBlock attribute) {
			var checker = new MultiMatchingGBlockVisitor{ candidates = candidates};
			GrammarBlockUtils.VisitGrammarBlock(attribute, checker);
			if (checker.hasOne && !checker.hasUnmatch) {
				return AttributeMatchResult.POSITIVE;
			}
			return AttributeMatchResult.NEGATIVE;
		}
	}
	class GBlockVisitor_GetFloat : GrammarBlockVisitor {
		public float numberFloat = 0;
		void GrammarBlockVisitor.IfClusterGrammarBlock(ClusterGrammarBlock cluster) {}
		void GrammarBlockVisitor.IfGrammarUnit(GrammarUnit unit) {
			float.TryParse(unit.word, out numberFloat);
		}
		void GrammarBlockVisitor.IfHasMetaInfo(GrammarBlock meta) {}
		void GrammarBlockVisitor.IfHasModifier(GrammarBlock mod) {}
	}
	class MultiMatchingGBlockVisitor : GrammarBlockVisitor {
		public SortedSet<string> candidates;
		public bool hasUnmatch = false;
		public bool hasOne = false;
		void GrammarBlockVisitor.IfClusterGrammarBlock(ClusterGrammarBlock cluster) {
			foreach (var subBlock in cluster.blocks) {
				GrammarBlockUtils.VisitGrammarBlock(subBlock, this);
			}
		}

		void GrammarBlockVisitor.IfGrammarUnit(GrammarUnit gUnit) {
			if (candidates.Contains(gUnit.word)) {
				hasOne = true;
			}
			else {
				hasUnmatch = true;
			}
		}

		void GrammarBlockVisitor.IfHasMetaInfo(GrammarBlock meta) { }

		void GrammarBlockVisitor.IfHasModifier(GrammarBlock mod) { }
	};
}
