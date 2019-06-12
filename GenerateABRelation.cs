using ETModel;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class GenerateABRelation : Editor
{
    static Dictionary<string, Dictionary<Type,string>> DicABRelation = new Dictionary<string, Dictionary<Type, string>>();
    public const string abConfigPath = "Assets/Bundles/ABConfig/ABConfig.txt";
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
                if (ext.Contains("jpg") || ext.Contains("png") || ext.Contains("tga")|| ext.Contains("JPG") || ext.Contains("PNG") || ext.Contains("TGA"))
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
        //生成配置文件
        GenerateABConfigeFile(Path.Combine(Application.dataPath, "Bundles/ABConfig"));
        UpdateABMappingCode();
    }

    private static void GenerateABConfigeFile(string dir)
    {
        ABConfig abConfig = new ABConfig();
        abConfig.ListABRelation.Clear();
        foreach (var item in DicABRelation)
        {
            foreach (var abAndType in item.Value)
            {
                var resInfo = new ResInfo() { abName= abAndType.Value,TypeRes=abAndType.Key.FullName,assetName=item.Key};
                abConfig.ListABRelation.Add(resInfo);
            }
        }
        using (FileStream fileStream = new FileStream($"{dir}/ABConfig.txt", FileMode.Create))
        {
            string json = JsonUtility.ToJson(abConfig);
            byte[] bytes = JsonUtility.ToJson(abConfig,true).ToByteArray();
            fileStream.Write(bytes, 0, bytes.Length);
        }
        //设置bundle
        ETEditor.BuildEditor.SetBundle(abConfigPath, "ABConfig", true);
        AssetDatabase.Refresh();
        AssetDatabase.SaveAssets();
    }

    private static void Generate(string abname, string assetName, Type TypeAsset)
    {
        if (TypeAsset.FullName=="UnityEditor.Animations.AnimatorController"|| TypeAsset.FullName == "UnityEngine.Timeline.TimelineAsset")
        {
            return;
        }
        if (TypeAsset.FullName == "UnityEditor.SceneAsset")
        {
            TypeAsset = typeof(UnityEngine.GameObject);
        }
        if (!TypeAsset.FullName.Contains("UnityEngine"))
        {
            Debug.Log($"<color=yellow>{TypeAsset.FullName}不是运行时类型！</color>");
            return;
        }
        assetName = assetName.ToLower();
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

namespace {0}
{
    class ABMapping
    {

        {1}
    }
}
";

        #region write info
        string variable = string.Empty;
        Dictionary<string, string> dicVariableNotRepeat = new Dictionary<string, string>();//key,variable
        System.Text.RegularExpressions.Regex regNum = new System.Text.RegularExpressions.Regex("^[0-9]");
        foreach (var item in DicABRelation)
        {
            foreach (var abAndType in item.Value)
            {
                var showKey = ProcessShowKey(dicVariableNotRepeat, regNum, item);
            }
        }
        foreach (var item in dicVariableNotRepeat)
        {
            variable += $"public const string {item.Key} = \"{item.Value}\";" + "\n        ";
        }
        #endregion

        string textHotfix = temp1.Replace("{1}", variable);
        string textModel = temp1.Replace("{1}", variable);
        string HotfixFolderAbMappingFile = Application.dataPath + "/../Hotfix/ABMapping/ABMapping.cs";
        string ModelFolderAbMappingFile = Application.dataPath + "/ScriptsG05/Tools/ABHelper/ABMapping.cs";
        using (FileStream fs = new FileStream(HotfixFolderAbMappingFile, FileMode.Create, FileAccess.Write))
        {
            using (StreamWriter sw = new StreamWriter(fs))
            {
                textHotfix=textHotfix.Replace("{0}", "Hotfix");
                sw.Write(textHotfix);
            }
        }
        using (FileStream fs = new FileStream(ModelFolderAbMappingFile, FileMode.Create, FileAccess.Write))
        {
            using (StreamWriter sw = new StreamWriter(fs))
            {
                textModel=textModel.Replace("{0}", "Model");
                sw.Write(textModel);
            }
        }
        AssetDatabase.Refresh();
        AssetDatabase.SaveAssets();
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
