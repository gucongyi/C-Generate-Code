using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
namespace ETModel
{
    [System.Serializable]
    public class ResInfo
    {
        public string TypeRes;
        public string abName;
        public string assetName;
    }
	public class ABConfig
    {
        //一对一，一对多的json不能序列化
        public List<ResInfo> ListABRelation = new List<ResInfo>();
    }
}