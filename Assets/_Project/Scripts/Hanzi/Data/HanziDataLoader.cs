using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace HanziZombieDefense.Hanzi.Data
{
    /// <summary>
    /// Parses HanziWriter / Make Me a Hanzi character JSON files into <see cref="HanziCharacter"/>.
    ///
    /// Source format (per character file):
    /// <code>
    /// { "character":"你",
    ///   "strokes":["M 123 456 ..."],
    ///   "medians":[ [[x,y],[x,y],...], [[x,y],...] ] }
    /// </code>
    /// Only the <c>medians</c> array is consumed. Coordinates are 1024×1024 Y-down
    /// in the source and are converted to Y-up by mapping <c>y → 1024 - y</c>.
    /// </summary>
    public static class HanziDataLoader
    {
        /// <summary>HanziWriter source coordinate space side length.</summary>
        public const float HanziWriterCanvasSize = 1024f;

        /// <summary>
        /// Parse a HanziWriter character JSON document.
        /// </summary>
        /// <param name="json">Raw JSON document text.</param>
        /// <returns>Hydrated character; throws on malformed input.</returns>
        public static HanziCharacter LoadCharacterFromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                throw new ArgumentException("JSON is null or empty.", nameof(json));

            var character = ExtractCharacterField(json);
            var medians = ExtractMedians(json);

            if (medians.Count == 0)
                throw new FormatException($"No medians found in JSON for character '{character}'.");

            var strokes = new List<HanziStroke>(medians.Count);
            foreach (var stroke in medians)
            {
                if (stroke.Count < 2) continue;
                strokes.Add(new HanziStroke(stroke));
            }

            if (strokes.Count == 0)
                throw new FormatException($"All medians for '{character}' had < 2 points.");

            return new HanziCharacter(character, strokes);
        }

        /// <summary>
        /// Asynchronously read a character file from disk and parse it.
        /// On WebGL, <see cref="File.ReadAllText(string)"/> may not be valid for
        /// StreamingAssets — callers should use <see cref="UnityEngine.Networking.UnityWebRequest"/>
        /// and feed the resulting text into <see cref="LoadCharacterFromJson"/> instead.
        /// </summary>
        public static async Task<HanziCharacter> LoadCharacterFromFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path is null or empty.", nameof(filePath));
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Hanzi character JSON not found.", filePath);

            string json;
            using (var reader = new StreamReader(filePath, Encoding.UTF8))
            {
                json = await reader.ReadToEndAsync();
            }

            return LoadCharacterFromJson(json);
        }

        // ─────────────────────────── Manual JSON helpers ───────────────────────────
        // We avoid Newtonsoft (not bundled by default) and JsonUtility (cannot represent
        // nested numeric arrays without per-stroke wrapper objects). A focused tokenizer
        // for the two fields we need is far smaller than pulling a dependency.

        private static string ExtractCharacterField(string json)
        {
            const string key = "\"character\"";
            int idx = json.IndexOf(key, StringComparison.Ordinal);
            if (idx < 0)
                throw new FormatException("Missing 'character' field in JSON.");

            int colon = json.IndexOf(':', idx + key.Length);
            if (colon < 0)
                throw new FormatException("Malformed 'character' field.");

            int quoteStart = json.IndexOf('"', colon + 1);
            if (quoteStart < 0)
                throw new FormatException("Malformed 'character' field (no opening quote).");

            int quoteEnd = json.IndexOf('"', quoteStart + 1);
            if (quoteEnd < 0)
                throw new FormatException("Malformed 'character' field (no closing quote).");

            return json.Substring(quoteStart + 1, quoteEnd - quoteStart - 1);
        }

        /// <summary>
        /// Extract the <c>medians</c> array as a list-of-lists of Y-up Vector2 points.
        /// The parser expects <c>[[ [x,y], [x,y] ], [...]]</c>; whitespace anywhere is fine.
        /// </summary>
        private static List<List<Vector2>> ExtractMedians(string json)
        {
            const string key = "\"medians\"";
            int idx = json.IndexOf(key, StringComparison.Ordinal);
            if (idx < 0)
                throw new FormatException("Missing 'medians' field in JSON.");

            int arrayStart = json.IndexOf('[', idx + key.Length);
            if (arrayStart < 0)
                throw new FormatException("Malformed 'medians' (no opening bracket).");

            int arrayEnd = FindMatchingBracket(json, arrayStart);
            if (arrayEnd < 0)
                throw new FormatException("Malformed 'medians' (unterminated).");

            var result = new List<List<Vector2>>();
            int cursor = arrayStart + 1;

            while (cursor < arrayEnd)
            {
                int strokeOpen = json.IndexOf('[', cursor);
                if (strokeOpen < 0 || strokeOpen >= arrayEnd) break;

                int strokeClose = FindMatchingBracket(json, strokeOpen);
                if (strokeClose < 0 || strokeClose > arrayEnd)
                    throw new FormatException("Malformed stroke array (unterminated).");

                result.Add(ParseStrokePoints(json, strokeOpen, strokeClose));
                cursor = strokeClose + 1;
            }

            return result;
        }

        /// <summary>
        /// Parse a single stroke's <c>[[x,y],[x,y],…]</c> body.
        /// Coordinates are flipped to Y-up by <c>y' = (canvas - y)</c>.
        /// </summary>
        private static List<Vector2> ParseStrokePoints(string json, int openIdx, int closeIdx)
        {
            var points = new List<Vector2>(32);
            int cursor = openIdx + 1;

            while (cursor < closeIdx)
            {
                int pairOpen = json.IndexOf('[', cursor);
                if (pairOpen < 0 || pairOpen >= closeIdx) break;

                int pairClose = json.IndexOf(']', pairOpen + 1);
                if (pairClose < 0 || pairClose > closeIdx)
                    throw new FormatException("Malformed point pair (unterminated).");

                ParseXYPair(json, pairOpen + 1, pairClose, out float x, out float y);
                points.Add(new Vector2(x, y));

                cursor = pairClose + 1;
            }

            return points;
        }

        /// <summary>
        /// Parse two comma-separated numbers from a half-open span [start, end).
        /// </summary>
        private static void ParseXYPair(string json, int start, int end, out float x, out float y)
        {
            int comma = json.IndexOf(',', start, end - start);
            if (comma < 0)
                throw new FormatException("Point pair missing comma.");

            string xs = json.Substring(start, comma - start).Trim();
            string ys = json.Substring(comma + 1, end - comma - 1).Trim();

            if (!float.TryParse(xs, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out x))
                throw new FormatException($"Bad X coordinate '{xs}'.");
            if (!float.TryParse(ys, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out y))
                throw new FormatException($"Bad Y coordinate '{ys}'.");
        }

        /// <summary>
        /// Find the bracket index matching the '[' at <paramref name="openIdx"/>,
        /// honoring quoted strings (so brackets inside string literals are skipped).
        /// </summary>
        private static int FindMatchingBracket(string json, int openIdx)
        {
            int depth = 0;
            bool inString = false;
            for (int i = openIdx; i < json.Length; i++)
            {
                char c = json[i];

                if (inString)
                {
                    if (c == '\\' && i + 1 < json.Length) { i++; continue; }
                    if (c == '"') inString = false;
                    continue;
                }

                if (c == '"') { inString = true; continue; }
                if (c == '[') depth++;
                else if (c == ']')
                {
                    depth--;
                    if (depth == 0) return i;
                }
            }
            return -1;
        }
    }
}
