#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace HanziZombieDefense.Editor
{
    /// <summary>
    /// Editor tooling that converts the Make Me a Hanzi <c>graphics.txt</c> dump
    /// (newline-delimited JSON) into per-character files under
    /// <c>StreamingAssets/Hanzi/graphics/</c> and a top-level <c>hanzi_index.json</c>
    /// consumed by <see cref="HanziZombieDefense.Hanzi.Data.HanziDatabase"/>.
    /// </summary>
    public static class HanziDataProcessor
    {
        private const string GraphicsUrl =
            "https://raw.githubusercontent.com/skishore/makemeahanzi/master/graphics.txt";

        private const string StreamingAssetsRel = "Assets/_Project/StreamingAssets";
        private const string GraphicsOutFolderRel = "Assets/_Project/StreamingAssets/Hanzi/graphics";
        private const string IndexOutPathRel = "Assets/_Project/StreamingAssets/Hanzi/hanzi_index.json";

        // ─────────────────────────── Menu items ───────────────────────────

        [MenuItem("Tools/Hanzi Zombie Defense/Process Graphics Data")]
        public static void ProcessGraphicsDataMenu()
        {
            string sourcePath = EditorUtility.OpenFilePanel(
                "Select graphics.txt from Make Me a Hanzi",
                "",
                "txt");

            if (string.IsNullOrEmpty(sourcePath))
            {
                Debug.Log("[HanziDataProcessor] Cancelled.");
                return;
            }

            ProcessGraphicsFile(sourcePath);
        }

        [MenuItem("Tools/Hanzi Zombie Defense/Download Graphics Data")]
        public static void DownloadGraphicsDataMenu()
        {
            string tempPath = Path.Combine(
                Path.GetTempPath(),
                $"hanzi_graphics_{DateTime.UtcNow:yyyyMMddHHmmss}.txt");

            try
            {
                EditorUtility.DisplayProgressBar(
                    "Hanzi Zombie Defense",
                    $"Downloading graphics.txt from {GraphicsUrl}…",
                    0.1f);

                // WebClient is synchronous and ships with .NET — no third-party deps,
                // no editor-coroutine plumbing required for a one-shot tooling download.
                ServicePointManager.SecurityProtocol =
                    SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                using (var client = new WebClient())
                {
                    client.DownloadFile(GraphicsUrl, tempPath);
                }

                EditorUtility.ClearProgressBar();
                Debug.Log($"[HanziDataProcessor] Downloaded to {tempPath}");

                ProcessGraphicsFile(tempPath);
            }
            catch (Exception ex)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError($"[HanziDataProcessor] Download failed: {ex.Message}");
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    try { File.Delete(tempPath); } catch { /* best-effort cleanup */ }
                }
            }
        }

        // ─────────────────────────── Core processing ───────────────────────────

        public static void ProcessGraphicsFile(string sourcePath)
        {
            if (!File.Exists(sourcePath))
            {
                Debug.LogError($"[HanziDataProcessor] File not found: {sourcePath}");
                return;
            }

            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (string.IsNullOrEmpty(projectRoot))
            {
                Debug.LogError("[HanziDataProcessor] Could not resolve project root.");
                return;
            }

            string graphicsOutDir = Path.Combine(projectRoot, GraphicsOutFolderRel);
            string indexOutPath = Path.Combine(projectRoot, IndexOutPathRel);

            Directory.CreateDirectory(graphicsOutDir);
            Directory.CreateDirectory(Path.GetDirectoryName(indexOutPath));

            int totalLines = CountNonEmptyLines(sourcePath);
            int processed = 0;
            int written = 0;
            var indexEntries = new List<IndexEntry>(Math.Max(totalLines, 1024));

            try
            {
                using var reader = new StreamReader(sourcePath, Encoding.UTF8);
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    processed++;
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    if (processed % 50 == 0 || processed == totalLines)
                    {
                        float pct = totalLines > 0 ? (float)processed / totalLines : 0f;
                        if (EditorUtility.DisplayCancelableProgressBar(
                                "Processing Hanzi Graphics",
                                $"{processed} / {totalLines} characters",
                                pct))
                        {
                            Debug.LogWarning("[HanziDataProcessor] Cancelled by user.");
                            break;
                        }
                    }

                    if (!TryParseLine(line, out string character, out int strokeCount))
                        continue;

                    string codepoint = ToHexCodepoint(character);
                    string outPath = Path.Combine(graphicsOutDir, $"{codepoint}.json");
                    File.WriteAllText(outPath, line, new UTF8Encoding(false));
                    written++;

                    indexEntries.Add(new IndexEntry
                    {
                        character = character,
                        hsk = 0,
                        strokes = strokeCount
                    });
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            WriteIndexFile(indexOutPath, indexEntries);

            AssetDatabase.Refresh();

            Debug.Log(
                $"[HanziDataProcessor] Done. Wrote {written} character files to '{GraphicsOutFolderRel}', " +
                $"index with {indexEntries.Count} entries to '{IndexOutPathRel}'.");
        }

        // ─────────────────────────── Line parsing ───────────────────────────

        /// <summary>
        /// Extracts the <c>character</c> field and the length of the <c>medians</c> array
        /// from a single graphics.txt JSON line. Done by hand to avoid pulling JSON.NET.
        /// </summary>
        private static bool TryParseLine(string json, out string character, out int strokeCount)
        {
            character = null;
            strokeCount = 0;

            character = ExtractStringField(json, "character");
            if (string.IsNullOrEmpty(character)) return false;

            strokeCount = CountTopLevelArrayElements(json, "medians");
            return true;
        }

        private static string ExtractStringField(string json, string field)
        {
            string token = "\"" + field + "\"";
            int keyIdx = json.IndexOf(token, StringComparison.Ordinal);
            if (keyIdx < 0) return null;

            int colonIdx = json.IndexOf(':', keyIdx + token.Length);
            if (colonIdx < 0) return null;

            int firstQuote = json.IndexOf('"', colonIdx + 1);
            if (firstQuote < 0) return null;

            int secondQuote = json.IndexOf('"', firstQuote + 1);
            if (secondQuote < 0) return null;

            return json.Substring(firstQuote + 1, secondQuote - firstQuote - 1);
        }

        /// <summary>
        /// Counts the number of top-level elements in the named array field.
        /// For <c>medians</c> in graphics.txt this equals the stroke count.
        /// Walks bracket depth so nested arrays (each median is a list of points) are not double-counted.
        /// </summary>
        private static int CountTopLevelArrayElements(string json, string field)
        {
            string token = "\"" + field + "\"";
            int keyIdx = json.IndexOf(token, StringComparison.Ordinal);
            if (keyIdx < 0) return 0;

            int openBracket = json.IndexOf('[', keyIdx + token.Length);
            if (openBracket < 0) return 0;

            int depth = 0;
            int count = 0;
            bool sawAnyElement = false;

            for (int i = openBracket; i < json.Length; i++)
            {
                char c = json[i];
                if (c == '[')
                {
                    depth++;
                    if (depth == 2) // entering a top-level child array (one stroke's medians)
                    {
                        count++;
                        sawAnyElement = true;
                    }
                }
                else if (c == ']')
                {
                    depth--;
                    if (depth == 0) break;
                }
                else if (depth == 1 && !char.IsWhiteSpace(c) && c != ',')
                {
                    // Numeric/scalar element at top level — uncommon for medians but handle it.
                    if (!sawAnyElement)
                    {
                        // intentionally do nothing; nested arrays handle the common case
                    }
                }
            }
            return count;
        }

        // ─────────────────────────── Codepoint helpers ───────────────────────────

        private static string ToHexCodepoint(string character)
        {
            if (string.IsNullOrEmpty(character)) return "0";

            int codepoint = char.ConvertToUtf32(character, 0);
            return codepoint.ToString("x");
        }

        // ─────────────────────────── Index emission ───────────────────────────

        [Serializable]
        private class IndexEntry
        {
            public string character;
            public int hsk;
            public int strokes;
        }

        [Serializable]
        private class IndexFile
        {
            public List<IndexEntry> entries;
        }

        private static void WriteIndexFile(string indexOutPath, List<IndexEntry> entries)
        {
            var file = new IndexFile { entries = entries };
            string json = JsonUtility.ToJson(file, prettyPrint: false);
            File.WriteAllText(indexOutPath, json, new UTF8Encoding(false));
        }

        // ─────────────────────────── Misc ───────────────────────────

        private static int CountNonEmptyLines(string path)
        {
            int n = 0;
            using var reader = new StreamReader(path, Encoding.UTF8);
            while (reader.ReadLine() is { } line)
            {
                if (!string.IsNullOrWhiteSpace(line)) n++;
            }
            return n;
        }
    }
}
#endif
