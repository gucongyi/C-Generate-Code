using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace ETModel
{
    public class ABManager
    {
        public class DicKey
        {
            public string assetName;
            public Type typeAsset;
        }

        public static Dictionary<DicKey, string> DicABRelation = new Dictionary<DicKey, string>();
        public static List<ResInfo> ListABRelation = new List<ResInfo>();
        public static Dictionary<string, UnityEngine.Object> resourceCache = new Dictionary<string, UnityEngine.Object>();
        public static void Init()
        {
            ListABRelation.Clear();
            DicABRelation.Clear();
            //从ab中读取
            Game.Scene.GetComponent<ResourcesComponent>().LoadBundle("abconfig.unity3d");
            var ABConfig=GetAssetRes<TextAsset>("ABConfig", "abconfig.unity3d");
            //string str = File.ReadAllText(Path.Combine(Application.dataPath, "Bundles/ABConfig/ABConfig.txt"));
            var abConfig = JsonUtility.FromJson<ABConfig>(ABConfig.text);
            ListABRelation = abConfig.ListABRelation;
            for (int i=0;i< ListABRelation.Count;i++)
            {
                AddAbRelation(ListABRelation[i].assetName, ListABRelation[i].abName, Type.GetType($"{ListABRelation[i].TypeRes},UnityEngine"));
            }
        }
        private static void AddAbRelation(string assetName, string abname, Type TypeAsset)
        {
            DicKey dicKey = new DicKey() { assetName=assetName,typeAsset= TypeAsset };
            DicABRelation.Add(dicKey, abname);
        }
        public static T GetAsset<T>(string assetName) where T : UnityEngine.Object
        {
            Type t = typeof(T);
            assetName = assetName.ToLower();
            string bundleName = GetBundleNameByAssetNameAndType(assetName, t);
            bundleName = bundleName.ToLower();

            var resource = GetAssetCache(bundleName, assetName);

            if (resource != null)
            {
                if (t.Equals(typeof(UnityEngine.Sprite)))
                {
                    Texture2D tex = (Texture2D)resource;
                    Sprite s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero);
                    UnityEngine.Object o = (UnityEngine.Object)s;
                    return (T)o;
                }
                else
                {
                    return (T)resource;
                }
                
            }
                

            //加载bundle
            Game.Scene.GetComponent<ResourcesComponent>().LoadBundle(bundleName);
            return GetAssetRes<T>(assetName, bundleName);
        }

        public static T GetAssetRes<T>(string assetName, string bundleName) where T : UnityEngine.Object
        {
            assetName = assetName.ToLower();
            var resource = GetAssetCache(bundleName, assetName);
            if (resource != null) return (T)resource;

#if UNITY_EDITOR
            var s = UnityEditor.AssetDatabase.GetAssetPathsFromAssetBundleAndAssetName(bundleName, assetName);
            if (s.Length == 0)
            {
                throw new Exception("bundleName " + bundleName + "  AssetName " + assetName);
            }
            resource = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(s[0]);
#else
            if (!bundles.ContainsKey(bundleName))
            {
                throw new Exception($"读取资源{prefab} 没有找到 bundle  {bundleName} ");
            }
            resource = bundles[bundleName].AssetBundle.LoadAsset<T>(prefab);
#endif
            if (resource == null)
            {
                throw new Exception($"not found asset: {bundleName}/{assetName}");
            }
            string path = $"{bundleName}/{assetName}".ToLower();
            ABManager.resourceCache[path] = resource;
            return (T)resource;
        }

        public async static Task<T> GetAssetAsync<T>(string assetName) where T : UnityEngine.Object
        {
            Type t = typeof(T);
            assetName = assetName.ToLower();
            string bundleName = GetBundleNameByAssetNameAndType(assetName,t);
            bundleName = bundleName.ToLower();

            var resource = GetAssetCache(bundleName, assetName);
            if (resource != null) return (T)resource;

            //加载bundle
            await Game.Scene.GetComponent<ResourcesComponent>().LoadBundleAsync(bundleName);
            return GetAssetRes<T>(assetName, bundleName);
        }

        private static string GetBundleNameByAssetNameAndType(string assetName,Type t)
        {
            string bundleName = string.Empty;
            foreach (var item in DicABRelation)
            {
                if (item.Key.typeAsset.Equals(t) && item.Key.assetName.Equals(assetName))
                {
                    bundleName = item.Value;
                    break;
                }
            }

            return bundleName;
        }

        private static UnityEngine.Object GetAssetCache(string bundleName, string assetName)
        {
            string path = $"{bundleName}/{assetName}".ToLower();
            if (resourceCache.ContainsKey(path)) return resourceCache[path];
            return null;
        }
    }
}
