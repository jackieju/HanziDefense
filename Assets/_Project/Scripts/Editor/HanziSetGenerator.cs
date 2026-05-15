#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using HanziZombieDefense.ScriptableObjects;

namespace HanziZombieDefense.Editor
{
    /// <summary>
    /// One-click generator that materialises <see cref="HanziSetDefinition"/> assets
    /// for HSK levels 1-6, seeded with a small curated character pool per level so the
    /// game can run end-to-end before a full HSK list is sourced.
    /// </summary>
    public static class HanziSetGenerator
    {
        private const string OutputFolder = "Assets/_Project/ScriptableObjects/Instances/HanziSets";

        [MenuItem("Tools/Hanzi Zombie Defense/Generate HSK Sets")]
        public static void GenerateHskSets()
        {
            EnsureFolderExists(OutputFolder);

            int created = 0;
            int updated = 0;

            for (int level = 1; level <= 6; level++)
            {
                var characters = GetCharactersForLevel(level);
                int maxStrokes = MaxStrokesForLevel(level);

                string assetPath = $"{OutputFolder}/HSK{level}.asset";
                bool isNew = !File.Exists(assetPath);

                var asset = AssetDatabase.LoadAssetAtPath<HanziSetDefinition>(assetPath);
                if (asset == null)
                {
                    asset = ScriptableObject.CreateInstance<HanziSetDefinition>();
                    AssetDatabase.CreateAsset(asset, assetPath);
                }

                ApplyValuesViaSerializedObject(asset, $"HSK{level}", level, maxStrokes, characters);

                if (isNew) created++;
                else updated++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"[HanziSetGenerator] Done. Created {created}, updated {updated} HanziSetDefinition asset(s) in '{OutputFolder}'.");
        }

        // ─────────────────────────── Asset wiring ───────────────────────────

        /// <summary>
        /// HanziSetDefinition exposes its fields as private+SerializeField, so we use
        /// SerializedObject to write values rather than adding a public setter API
        /// that would only exist for the editor.
        /// </summary>
        private static void ApplyValuesViaSerializedObject(
            HanziSetDefinition asset,
            string setName,
            int hskLevel,
            int maxStrokeCount,
            IReadOnlyList<string> characters)
        {
            var so = new SerializedObject(asset);

            var nameProp = so.FindProperty("setName");
            var levelProp = so.FindProperty("hskLevel");
            var strokeProp = so.FindProperty("maxStrokeCount");
            var charsProp = so.FindProperty("characters");

            if (nameProp != null) nameProp.stringValue = setName;
            if (levelProp != null) levelProp.intValue = hskLevel;
            if (strokeProp != null) strokeProp.intValue = maxStrokeCount;

            if (charsProp != null && charsProp.isArray)
            {
                charsProp.arraySize = characters.Count;
                for (int i = 0; i < characters.Count; i++)
                {
                    charsProp.GetArrayElementAtIndex(i).stringValue = characters[i];
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void EnsureFolderExists(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;

            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            string leaf = Path.GetFileName(folder);

            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolderExists(parent);
            }

            AssetDatabase.CreateFolder(parent, leaf);
        }

        // ─────────────────────────── Curated HSK pools ───────────────────────────

        private static int MaxStrokesForLevel(int level)
        {
            return level switch
            {
                1 => 8,
                2 => 10,
                3 => 12,
                4 => 14,
                5 => 16,
                6 => 20,
                _ => 12,
            };
        }

        private static IReadOnlyList<string> GetCharactersForLevel(int level)
        {
            return level switch
            {
                1 => Hsk1,
                2 => Hsk2,
                3 => Hsk3,
                4 => Hsk4,
                5 => Hsk5,
                6 => Hsk6,
                _ => new List<string>(),
            };
        }

        // The pools below are curated subsets — common, high-frequency characters
        // representative of each HSK band. They are intentionally small (≥20 each)
        // so the game has working data before the full official lists are imported.

        private static readonly List<string> Hsk1 = new List<string>
        {
            "我", "你", "他", "她", "好", "是", "不", "人", "一", "二",
            "三", "四", "五", "六", "七", "八", "九", "十", "口", "日",
            "月", "上", "下", "大", "小", "中", "山", "水", "火", "木",
        };

        private static readonly List<string> Hsk2 = new List<string>
        {
            "学", "生", "老", "师", "朋", "友", "家", "爱", "去", "来",
            "看", "听", "说", "读", "写", "吃", "喝", "买", "卖", "走",
            "回", "想", "觉", "得", "做", "工", "作", "时", "间", "天",
        };

        private static readonly List<string> Hsk3 = new List<string>
        {
            "爸", "妈", "哥", "姐", "弟", "妹", "儿", "子", "女", "男",
            "请", "问", "谢", "对", "起", "再", "见", "早", "晚", "饭",
            "茶", "水", "果", "菜", "肉", "鱼", "钱", "块", "毛", "分",
        };

        private static readonly List<string> Hsk4 = new List<string>
        {
            "经", "济", "文", "化", "教", "育", "政", "府", "社", "会",
            "环", "境", "技", "术", "信", "息", "网", "络", "系", "统",
            "管", "理", "服", "务", "发", "展", "建", "设", "条", "件",
        };

        private static readonly List<string> Hsk5 = new List<string>
        {
            "传", "统", "现", "代", "历", "史", "民", "族", "国", "际",
            "贸", "易", "金", "融", "投", "资", "市", "场", "竞", "争",
            "效", "率", "质", "量", "标", "准", "规", "则", "原", "因",
        };

        private static readonly List<string> Hsk6 = new List<string>
        {
            "辩", "证", "矛", "盾", "趋", "势", "策", "略", "范", "畴",
            "概", "念", "逻", "辑", "本", "质", "现", "象", "理", "论",
            "实", "践", "辨", "析", "综", "合", "归", "纳", "演", "绎",
        };
    }
}
#endif
