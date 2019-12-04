using System.Collections.Generic;
using AGDev;

namespace AGBLang.StdUtil {
	public interface GBlockConvertListener {
		void AdditionalEdit(MutableGrammarBlock mgBlock);
		GBlockConverter subBlockConverter { get; }
		GBlockConverter metaConverter { get; }
		GBlockConverter modConverter { get; }
	};
	public interface GBlockConverter {
		GBlockConvertResult ConvertGBlock(GrammarBlock sourceGBlock, GBlockConvertListener listener);
	};
	public class PassThroughGBlockConverter : GBlockConverter {
		static public GBlockConverter instance => _instance;
		static public GBlockConverter _instance = new PassThroughGBlockConverter();
		GBlockConvertResult GBlockConverter.ConvertGBlock(GrammarBlock sourceGBlock, GBlockConvertListener listener) {
			return new GBlockConvertResult();
		}
	}
	public class PassThroughGBlockConvertListener : GBlockConvertListener {
		static public GBlockConvertListener instance => _instance;
		static public GBlockConvertListener _instance = new PassThroughGBlockConvertListener();
		GBlockConverter GBlockConvertListener.subBlockConverter => PassThroughGBlockConverter.instance;
		GBlockConverter GBlockConvertListener.metaConverter => PassThroughGBlockConverter.instance;
		GBlockConverter GBlockConvertListener.modConverter => PassThroughGBlockConverter.instance;
		void GBlockConvertListener.AdditionalEdit(MutableGrammarBlock mgBlock) {}
	}
	public class MixedGBlockConvertListener : GBlockConvertListener {
		GBlockConverter GBlockConvertListener.subBlockConverter => _subBlockConverter != null ? _subBlockConverter : _baseLisetner?.subBlockConverter;
		GBlockConverter GBlockConvertListener.metaConverter => _metaConverter != null ? _metaConverter : _baseLisetner?.metaConverter;
		GBlockConverter GBlockConvertListener.modConverter => _modConverter != null ? _modConverter : _baseLisetner?.modConverter;
		public GBlockConverter _subBlockConverter;
		public GBlockConverter _metaConverter;
		public GBlockConverter _modConverter;
		public GBlockConvertListener _baseLisetner;
		void GBlockConvertListener.AdditionalEdit(MutableGrammarBlock mgBlock) {
			_baseLisetner?.AdditionalEdit(mgBlock);
		}
	}
	public class RootGBlockConverter : GBlockConverter {
		public GBlockConverter_Replace replacer = new GBlockConverter_Replace();
		public GBlockConverter_PronounSpecifier pronouc = new GBlockConverter_PronounSpecifier();
		public GBlockConverter_Default defaultConv = new GBlockConverter_Default();
		public GBlockConverter_Activizer activizer = new GBlockConverter_Activizer();
		public GBlockConverter_EachBlock eachConv = new GBlockConverter_EachBlock();
		public ClusterGBlockConverter mainConverter = new ClusterGBlockConverter();
		public RootGBlockConverter() {
			var convList = new List<GBlockConverter>();
			mainConverter.converters = convList;
			replacer.number = new Dictionary<string, GrammarBlock>(); 
			replacer.number.Add("one", new MinimumGBUnit { word = "1"});
			replacer.number.Add("two", new MinimumGBUnit { word = "2" });
			replacer.number.Add("three", new MinimumGBUnit { word = "3" });
			pronouc.dict = new Dictionary<string, MutableGrammarBlock>();
			convList.Add(replacer);
			convList.Add(pronouc);
			convList.Add(activizer);
			convList.Add(eachConv);
			convList.Add(defaultConv);
            MutableGrammarUnit subj = new StdMutableGUnit { word = "player" };
            subj.AddMetaInfo(StdMetaInfos.nominalBlock);
            activizer.defaultSubject = subj;
		}
		public void SetPronounSolution(GrammarBlock sourceGBlock, GBlockConvertListener listener) {
			GrammarBlockUtils.DeepSeek(sourceGBlock, StdMetaInfos.nominalBlock.word, (gBlock) => {
				//acutual noun found
				if (!GrammarBlockUtils.HasMetaInfo(gBlock, StdMetaInfos.pronoun.word)) {
					pronouc.dict["they"] = listener.subBlockConverter.ConvertGBlock(gBlock, listener).result;
				}
			}, true);
		}
		GBlockConvertResult GBlockConverter.ConvertGBlock(GrammarBlock sourceGBlock, GBlockConvertListener listener) {
			return (mainConverter as GBlockConverter).ConvertGBlock(sourceGBlock, listener);
		}
	}
	public class StdGBlockFilter : ImmediateGiver<GrammarBlock, GrammarBlock> {
		public System.Action<GrammarBlock> onBeginSentenceLine;
		GrammarBlock ImmediateGiver<GrammarBlock, GrammarBlock>.PickBestElement(GrammarBlock key) {
			var listener = new MixedGBlockConvertListener();
			var rootConv = new RootGBlockConverter();
			listener._metaConverter = GBlockConverter_Default.instance;
			listener._modConverter = rootConv;
			listener._subBlockConverter = rootConv;

			if (GrammarBlockUtils.HasMetaInfo(key, StdMetaInfos.sentenceCluster.word) && key.cluster != null) {
				var newCluster = new StdMutableClusterGBlock();
				foreach (var sentence in key.cluster.blocks) {
					rootConv.SetPronounSolution(sentence, listener);
					var converterSetnence = (listener as GBlockConvertListener).subBlockConverter.ConvertGBlock(sentence, listener);
					(newCluster as MutableClusterGrammarBlock).AddBlock(converterSetnence.result);
				}
				GBlockConvertUtility.ApplyModAndMeta(newCluster as MutableGrammarBlock, key, listener);
				return newCluster;
			}
			rootConv.SetPronounSolution(key, listener);
			return (rootConv as GBlockConverter).ConvertGBlock(key, listener).result;
		}
	}
	/*public class LoggingGBConvListener : GBlockConvertListener {
		public GBlockConvertListener clientListener;
		GBlockConverter GBlockConvertListener.subBlockConverter => clientListener.subBlockConverter;

		GBlockConverter GBlockConvertListener.metaConverter => clientListener.metaConverter;

		GBlockConverter GBlockConvertListener.modConverter => clientListener.modConverter;

		void GBlockConvertListener.AcceptAdditional(Taker<GrammarBlock> metaTaker, Taker<GrammarBlock> modTaker) {
			clientListener.AcceptAdditional(metaTaker, modTaker);
		}
	}*/

} 