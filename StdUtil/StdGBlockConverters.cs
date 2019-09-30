using System;
using System.Collections.Generic;
using AGBLang;
using System.Text;

namespace AGBLang.StdUtil {
	class GBlockConvertUtility {
		public static void ApplyModAndMeta(
			MutableGrammarBlock newBlock,
			GrammarBlock sourceBlock,
			GBlockConvertListener listener,
			GBlockConverter metaConv = null,
			GBlockConverter modConv = null
		) {
			if (sourceBlock.metaInfo != null) {
				var result = listener.metaConverter.ConvertGBlock(sourceBlock.metaInfo, listener);
				if (result.result != null) {
					newBlock.AddMetaInfo(result.result);
				}
			}
			if (sourceBlock.modifier != null) {
				var result = listener.modConverter.ConvertGBlock(sourceBlock.modifier, listener);
				if (result.result != null) {
					newBlock.AddModifier(result.result);
				}
			}
			listener.AdditionalEdit(newBlock);
		}
	}
	public class ClusterGBlockConverter : GBlockConverter {
		public IEnumerable<GBlockConverter> converters;
		GBlockConvertResult GBlockConverter.ConvertGBlock(GrammarBlock sourceGBlock, GBlockConvertListener listener) {
			foreach (var converter in converters) {
				var converted = converter.ConvertGBlock(sourceGBlock, listener);
				if (converted.didConvert) {
					return converted;
				}
			}
			return default(GBlockConvertResult);
		}
	}
	public class GBlockConverter_Replace : GBlockConverter {
		public Dictionary<string, GrammarBlock> number;
		GBlockConvertResult GBlockConverter.ConvertGBlock(GrammarBlock sourceGBlock, GBlockConvertListener listener) {
			if (sourceGBlock.unit == null)
				return default(GBlockConvertResult);
			if (number.TryGetValue(sourceGBlock.unit.word, out var value)) {
				
				if(value == null) {
					return new GBlockConvertResult(true, null);
				}
				//doubt : using sub block converter?
				var newMGBlock = listener.subBlockConverter.ConvertGBlock(value, listener);
				GBlockConvertUtility.ApplyModAndMeta(newMGBlock.result, sourceGBlock, listener);
				return new GBlockConvertResult(true, newMGBlock.result);
			}
			return default(GBlockConvertResult);
		}
	}
	public class GBlockConverter_PronounSpecifier : GBlockConverter {
		public Dictionary<string, MutableGrammarBlock> dict = new Dictionary<string, MutableGrammarBlock>();
		GBlockConvertResult GBlockConverter.ConvertGBlock(GrammarBlock sourceGBlock, GBlockConvertListener listener) {
			if (!GrammarBlockUtils.HasMetaInfo(sourceGBlock, StdMetaInfos.pronoun.word))
				return default(GBlockConvertResult);
			if (dict.TryGetValue(sourceGBlock.unit.word, out var value)) {
				var clusterGBC = new ClusterGBlockConverter {
					converters = new List<GBlockConverter> {
						new GBlockConverter_GUnitFilter {filteringString = StdMetaInfos.pronoun.word},
						listener.metaConverter
					}
				};
				GBlockConvertUtility.ApplyModAndMeta(value, sourceGBlock, listener, clusterGBC);
				return new GBlockConvertResult { didConvert = true, result = value };
			}
			return default(GBlockConvertResult);
		}
	}
	public class GBlockConverter_Default : GBlockConverter {
		public static GBlockConverter_Default instance = new GBlockConverter_Default();
		GBlockConvertResult GBlockConverter.ConvertGBlock(GrammarBlock sourceGBlock, GBlockConvertListener listener) {
			if (sourceGBlock.cluster != null) {
				var newClusterGB = new StdMutableClusterGBlock();
				foreach (var subGB in sourceGBlock.cluster.blocks) {
					var convertedSub = listener.subBlockConverter.ConvertGBlock(subGB, listener).result;
					if (convertedSub != null)
						newClusterGB.subBlocks.Add(convertedSub);
				}
				GBlockConvertUtility.ApplyModAndMeta(newClusterGB, sourceGBlock, listener);
				return new GBlockConvertResult(true, newClusterGB);
			} else if (sourceGBlock.unit != null) {
				var newGUnit = new StdMutableGUnit { word = sourceGBlock.unit.word };
				GBlockConvertUtility.ApplyModAndMeta(newGUnit, sourceGBlock, listener);
				return new GBlockConvertResult(true, newGUnit);
			}
			return default(GBlockConvertResult);
		}
	}
	public class GBlockConverter_Activizer : GBlockConverter {
		public GrammarBlock defaultSubject;
		GBlockConvertResult GBlockConverter.ConvertGBlock(GrammarBlock sourceGBlock, GBlockConvertListener listener) {
			//only applly to SV or Condition SV
			if (!GrammarBlockUtils.HasMetaInfo(sourceGBlock, StdMetaInfos.sv.word) && !GrammarBlockUtils.HasMetaInfo(sourceGBlock, StdMetaInfos.conditionSV.word)) {
				return default(GBlockConvertResult);
			}
			//search passive be
			List<GrammarUnit> passiveVerbList = null;
			var originalSubject = sourceGBlock.cluster.blocks[0];
			var convertedSubject = listener.subBlockConverter.ConvertGBlock(originalSubject, listener).result;
			var originalVerbs = sourceGBlock.cluster.blocks[1];
			GrammarBlockUtils.DeepForEachBlockUnit(
				originalVerbs,
				(mainVerbUnit) => {
					if (GrammarBlockUtils.IsUnit(mainVerbUnit, "be")) {
						GrammarBlockUtils.DeepForEachBlockUnit(
							mainVerbUnit.modifier,
							(contentVerbUnit) => {
								if (passiveVerbList == null) {
									passiveVerbList = new List<GrammarUnit>();
								}
								passiveVerbList.Add(contentVerbUnit);
							},
							StdMetaInfos.verbalBlock.word
						);

					}
				},
				StdMetaInfos.verbalBlock.word
			);
			//no passive verb found
			if (passiveVerbList == null)
				return default(GBlockConvertResult);
			//search normal verbs
			List<GrammarBlock> normalVerbList = null;
#if false
			GrammarBlockUtils.ForEachUnits(
				originalVerbs,
				(gUnit) => {
					if (GrammarBlockUtils.ShallowSeekByMetaInfo(sourceGBlock.cluster.blocks[1], StdMetaInfos.verbalBlock.word) != null) {
						if (normalVerbList == null) {
							normalVerbList = new List<GrammarBlock>();
						}
						normalVerbList.Add(gUnit);
					}
				},
				StdMetaInfos.modifierCluster.word
			);
#endif
			MutableGrammarBlock converted = null;
			#region passive only
			
			if (passiveVerbList.Count > 0) {
				var newSVCluster = new StdMutableClusterGBlock { };
				StdMutableClusterGBlock newSV = null;
				foreach (var passiveVerb in passiveVerbList) {
					newSV = new StdMutableClusterGBlock { };
					(newSV as MutableClusterGrammarBlock).AddBlock(defaultSubject);
					var activizedVerb = listener.subBlockConverter.ConvertGBlock(passiveVerb, listener).result;
					activizedVerb.AddModifier(convertedSubject);
					(newSV as MutableClusterGrammarBlock).AddBlock(activizedVerb);
					(newSVCluster as MutableClusterGrammarBlock).AddBlock(newSV);
				}
				if (passiveVerbList.Count == 1) {
					converted = newSV;
				} else {
					converted = newSVCluster;
				}
			}
			#endregion
			#region no result
			if (converted == null) {
				return default(GBlockConvertResult);
			}
			GBlockConvertUtility.ApplyModAndMeta(converted, sourceGBlock, listener);
			return new GBlockConvertResult(true, converted);
			#endregion
		}
	}
	public class GBlockConverter_EachBlock : GBlockConverter {
		GBlockConvertResult GBlockConverter.ConvertGBlock(GrammarBlock sourceGBlock, GBlockConvertListener listener) {
			if (!GrammarBlockUtils.IsUnit(sourceGBlock, "each")) {
				return default(GBlockConvertResult);
			}
			if(!GrammarBlockUtils.IsUnit(sourceGBlock.modifier, "turn")) {
				return default(GBlockConvertResult);
			}
			var newSubject = listener.subBlockConverter.ConvertGBlock(sourceGBlock.modifier, listener).result;
			var newVerb = new StdMutableGUnit { word = "begin" };
			(newVerb as MutableGrammarBlock).AddMetaInfo(StdMetaInfos.verbalBlock);
			MutableClusterGrammarBlock newSV = new StdMutableClusterGBlock();
			newSV.AddBlock(newSubject);
			newSV.AddBlock(newVerb);
			newSV.AddMetaInfo(StdMetaInfos.conditionSV);
			return new GBlockConvertResult(true, newSV);
		}
	}
	public struct GBlockConvertResult {
		public GBlockConvertResult(bool _didConvert, MutableGrammarBlock _result) {
			didConvert = _didConvert;
			result = _result;
		}
		public bool didConvert;
		public MutableGrammarBlock result;
	}
	public class GBlockConverter_GUnitFilter : GBlockConverter {
		public string filteringString;
		GBlockConvertResult GBlockConverter.ConvertGBlock(GrammarBlock sourceGBlock, GBlockConvertListener listener) {
			if (sourceGBlock.unit == null)
				return default(GBlockConvertResult);
			if (sourceGBlock.unit.word.Equals(filteringString, System.StringComparison.CurrentCultureIgnoreCase))
				return new GBlockConvertResult(true, null);
			return default(GBlockConvertResult);
		}
	}
}
