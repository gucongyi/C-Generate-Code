using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class GenerateABRelation : Editor
{
    static Dictionary<string, Dictionary<Type,string>> DicABRelation = new Dictionary<string, Dictionary<Type, string>>();
    [MenuItem("Tools/Generate AB Relation")]
    public static void GenerateRelation()
    {
        DicABRelation.Clear();
        var abNames = AssetDatabase.GetAllAssetBundleNames();
        foreach (var abname in abNames)
        {
            var paths = AssetDatabase.GetAssetPathsFromAssetBundle(abname);
            foreach (var eachPath in paths)
            {
                string assetName = Path.GetFileNameWithoutExtension(eachPath);
                var TypeAsset = AssetDatabase.GetMainAssetTypeAtPath(eachPath);
                Generate(abname, assetName, TypeAsset);
                //Debug.LogError($"eachPath:{eachPath} TypeAsset:{TypeAsset}");
                string ext = Path.GetExtension(eachPath);
                if (ext.Contains("jpg") || ext.Contains("png") || ext.Contains("tga"))
                {
                    Generate(abname, assetName, typeof(UnityEngine.Sprite));
                }
            }
        }
        #region print info
        foreach (var item in DicABRelation)
        {
            foreach (var abAndType in item.Value)
            {
                Debug.Log($"assetName:{item.Key} TypeAsset:{abAndType.Key} abName:{abAndType.Value}");
            }
        }
        #endregion
        UpdateABMappingCode();
    }

    private static void Generate(string abname, string assetName, Type TypeAsset)
    {
        if (TypeAsset.FullName=="UnityEditor.Animations.AnimatorController"|| TypeAsset.FullName == "UnityEngine.Timeline.TimelineAsset")
        {
            return;
        }
        if (TypeAsset.FullName == "UnityEditor.SceneAsset")
        {
            TypeAsset = typeof(GameObject);
        }
        if (!DicABRelation.ContainsKey(assetName))
        {
            Dictionary<Type, string> dicTypeAndAB = new Dictionary<Type, string>();
            dicTypeAndAB.Add(TypeAsset, abname);
            DicABRelation.Add(assetName, dicTypeAndAB);
        }
        else
        {
            bool isSame = false;
            foreach (var eachinfo in DicABRelation[assetName])
            {
                if (eachinfo.Key == TypeAsset && eachinfo.Value == abname)
                {
                    Debug.LogError($"abname:{abname} TypeAsset:{TypeAsset} 同一个包下assetName：{assetName}同名，请修改！");
                    isSame = true;
                }
                else if (eachinfo.Key == TypeAsset)
                {
                    Debug.LogError($"abname:{abname}与 abname:{eachinfo.Value} 不同包下 TypeAsset:{TypeAsset} assetName：{assetName}同名，请修改！");
                    isSame = true;
                }
            }
            if (!isSame)
            {
                //取出来
                Dictionary<Type, string> dicTypeAndAB = DicABRelation[assetName];
                dicTypeAndAB.Add(TypeAsset, abname);
            }

        }

    }

    public static void UpdateABMappingCode()
    {
        string temp1 = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hotfix
{
    class ABMapping
    {
        Dictionary<string, Dictionary<Type, string>> DicABRelation = new Dictionary<string, Dictionary<Type, string>>();

        {0}

        public void Init()
        {
            DicABRelation.Clear();
            {1}
        }

        private void AddAbRelation(string assetName, string abname, Type TypeAsset)
        {
            Dictionary<Type, string> abAndType = new Dictionary<Type, string>();
            abAndType.Add(TypeAsset, abname);
            DicABRelation.Add(assetName, abAndType);
        }
    }
}
";

        #region write info
        string variable = string.Empty;
        string info = string.Empty;
        Dictionary<string, string> dicVariableNotRepeat = new Dictionary<string, string>();//key,variable
        System.Text.RegularExpressions.Regex regNum = new System.Text.RegularExpressions.Regex("^[0-9]");
        
        foreach (var item in DicABRelation)
        {
            foreach (var abAndType in item.Value)
            {
                var showKey=ProcessShowKey(dicVariableNotRepeat, regNum, item);
                info += $"AddAbRelation({showKey}, \"{abAndType.Value}\", typeof({abAndType.Key.FullName}));" + "\n            ";
            }
        }
        foreach (var item in dicVariableNotRepeat)
        {
            variable += $"public const string {item.Key} = \"{item.Value}\";" + "\n        ";
        }
        #endregion

        string text = temp1.Replace("{0}", variable).Replace("{1}", info);
        string FolderAbMappingFile = Application.dataPath + "/../Hotfix/ABMapping/ABMapping.cs";
        using (FileStream fs = new FileStream(FolderAbMappingFile, FileMode.Create, FileAccess.Write))
        {
            using (StreamWriter sw = new StreamWriter(fs))
            {
                sw.Write(text);
            }
        }
    }

    private static string ProcessShowKey(Dictionary<string, string> dicVariableNotRepeat, System.Text.RegularExpressions.Regex regNum, KeyValuePair<string, Dictionary<Type, string>> item)
    {
        var varKey = item.Key.Replace("-", "_").Replace(".", "_").Replace(" ", "_").Replace("(", "_").Replace(")", "_");
        if (regNum.IsMatch(varKey))
        {
            varKey = $"Num{varKey}";
        }
        if (!dicVariableNotRepeat.ContainsKey(varKey))
        {
            dicVariableNotRepeat.Add(varKey, item.Key);
        }
        return varKey;
    }
}
