using AGDev;
using System.Collections;
using System.Collections.Generic;

namespace AGBLang.StdUtil {
	public class StdMAnalyzer : MorphemeAnalyzer {
		public List<BlockReader> blockReaderes = new List<BlockReader>();
		public List<GeneralReader> generalReaders = new List<GeneralReader>();
		public bool TryReadGeneral(string text, int startIndex, int lastIndex, out Morpheme product) {
			product = null;
			if (startIndex <= lastIndex) {
				product = null;
				foreach (var generalReader in generalReaders) {
					if (generalReader.ReadBlock(text, startIndex, lastIndex, out product)) {
						return true;
					}
				}
			}
			return false;
		}
		public DivisibleEnumerable<Morpheme> AnalyzeImmediate(string str) {
			var unitCluster = new MorphemeCluster { units = new List<Morpheme>() };
			int generalStartIndex = 0;
			int i = 0;
			for (; i < str.Length;) {
				bool didBlockReaderHit = false;
				foreach (var reader in blockReaderes) {
					int nextIndex = i;
					Morpheme blockProduct = null;
					if (didBlockReaderHit = reader.ReadBlock(str, i, out nextIndex, out blockProduct)) {
						if (generalStartIndex != i) {
							if (TryReadGeneral(str, generalStartIndex, i - 1, out Morpheme generalProduct)) {
								if (generalProduct != null) {
									unitCluster.units.Add(generalProduct);
								}
							}
						}
						i = generalStartIndex = nextIndex;
						if (blockProduct != null)
							unitCluster.units.Add(blockProduct);
						break;
					}
				}
				if (!didBlockReaderHit) {
					i++;
				}
			}
			if (TryReadGeneral(str, generalStartIndex, i - 1, out Morpheme lastGeneralProduct)) {
				if (lastGeneralProduct != null) {
					unitCluster.units.Add(lastGeneralProduct);
				}
			}
			if (unitCluster.units.Count > 0)
				return unitCluster;
			return null;
		}
		void MorphemeAnalyzer.AnalyzeFormat(string naturalLanguage, Taker<DivisibleEnumerable<Morpheme>> listener) {
			var result = AnalyzeImmediate(naturalLanguage);
			if(result != null)
				listener.Take(result);
			else
				listener.None();
		}
		public class MorphemeCluster : DivisibleEnumerable<Morpheme> {
			public List<Morpheme> units;
			public int startIndex = -1;
			IEnumerator<Morpheme> IEnumerable<Morpheme>.GetEnumerator() {
				return new MorphemeDivEnumerator { units = units, index = startIndex, startIndex = startIndex };
			}

			IEnumerator IEnumerable.GetEnumerator() {
				return new MorphemeDivEnumerator { units = units, index = startIndex, startIndex = startIndex };
			}

			DivisibleEnumerable<Morpheme> DivisibleEnumerable<Morpheme>.GetFollowing(int advanceCount) {
				return new MorphemeCluster { units = units, startIndex = startIndex + advanceCount };
			}
			public class MorphemeDivEnumerator : IEnumerator<Morpheme> {
				public List<Morpheme> units;
				public int startIndex;
				public int index;
				public Morpheme Current => units[index];

				object IEnumerator.Current => units[index];

				public void Dispose() { }

				public bool MoveNext() {
					return ++index < units.Count;
				}

				public void Reset() {
					index = startIndex;
				}
			}
		}
	}
	public interface BlockReader {
		bool ReadBlock(string text, int startIndex, out int nextIndex, out Morpheme product);
	}
	public interface GeneralReader {
		bool ReadBlock(string text, int startIndex, int lastIndex, out Morpheme product);
	}
	public class NumberReader : GeneralReader {
		bool GeneralReader.ReadBlock(string text, int startIndex, int lastIndex, out Morpheme product) {
			product = null;
			var part = text.Substring(startIndex, lastIndex - startIndex + 1);
			if (float.TryParse(part, out float number)) {
				product = new Morpheme { id = 6, word = part };
				return true;
			}
			return false;
		}
	}
	public class WordReader : GeneralReader {
		bool GeneralReader.ReadBlock(string text, int startIndex, int lastIndex, out Morpheme product) {
			var part = text.Substring(startIndex, lastIndex - startIndex + 1);
			product = new Morpheme { id = 0, word = part };
			return true;
		}
	}
	public struct MarkerAndFormatID {
		public string marker;
		public int formatID;
	}
	public class QuoteBlockReader : BlockReader {
		public string leftMarker = "\"";
		public string rightMarker = "\"";
		public int formatID;
		bool BlockReader.ReadBlock(string text, int startIndex, out int nextIndex, out Morpheme product) {
			nextIndex = startIndex;
			product = null;
			int i = startIndex;
			if (MarkerBlockReader.CheckMarker(text, leftMarker, i, out i)) {
				int firstContentIndex = i;
				int lastContentIndex = i;
				while (i < text.Length) {
					if (MarkerBlockReader.CheckMarker(text, rightMarker, i, out i)) {
						nextIndex = i;
						product = new Morpheme { id = formatID, word = text.Substring(firstContentIndex, lastContentIndex - firstContentIndex) };
						return true;
					}
					lastContentIndex = ++i;
				}
			}
			return false;
		}
	}
	public class MarkerBlockReader : BlockReader {
		public List<MarkerAndFormatID> markers;
		bool BlockReader.ReadBlock(string text, int startIndex, out int nextIndex, out Morpheme product) {
			nextIndex = startIndex;
			product = null;
			foreach (var marker in markers) {
				if (CheckMarker(text, marker.marker, startIndex, out nextIndex)) {
					product = new Morpheme { id = marker.formatID, word = marker.marker };
					return true;
				}
			}
			return false;

		}
		public static bool CheckMarker(string text, string givenMarker, int startIndex, out int nextIndex) {
			nextIndex = startIndex;
			int offset = 0;
			while (offset < givenMarker.Length) {
				//text end
				if (offset + startIndex >= text.Length) {
					return false;
				}
				if (text[startIndex + offset] != givenMarker[offset]) {
					return false;
				}
				offset++;
				
			}
			nextIndex = startIndex + offset;
			return true;
		}
	}
	public class IgnoreBlockReader : BlockReader {
		public List<string> markers = new List<string>();
		bool BlockReader.ReadBlock(string text, int startIndex, out int nextIndex, out Morpheme product) {
			nextIndex = startIndex;
			product = null;
			foreach (var marker in markers) {
				if (MarkerBlockReader.CheckMarker(text, marker, startIndex, out nextIndex))
					return true;
			}
			return false;
		}
	}
}
