using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnknownCreator.Modules
{
    public class CustomScriptableObject : ScriptableObject
    {
        [field: ReadOnly]
        [field: SerializeField]
        public string cfgName { get; internal set; }



        public virtual void OnEnable()
        {

        }

        public virtual void OnValidate()
        {
            cfgName = name;
        }

        /*
        public virtual void OnBeforeSerialize()
        {
            //OnBeforeSerializeCustom();
            //  data = SerializeAllFieldsToJson();
        }

        public virtual void OnAfterDeserialize()
        {
            //DeserializeJsonToFields();
            //OnAfterDeserializeCustom();
        }

        protected virtual void OnAfterDeserializeCustom()
        {
        }

        protected virtual void OnBeforeSerializeCustom()
        {
        }



        [JsonIgnore, SerializeField, HideInInspector]
        protected string data { private set; get; }

        [JsonIgnore]
        protected Dictionary<string, object> dataDict { private set; get; } = new();

        private string SerializeAllFieldsToJson()
        {
            dataDict.Clear();
            object obj;
            FieldInfo[] fields = GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (FieldInfo field in fields)
            {
                if (IsSkip(field))
                    continue;
                obj = field.GetValue(this);
                dataDict.Add(field.Name, obj);
            }
            data = JsonMapper.ToJson(dataDict);
            UCMDebug.Log("【序列化】" + data);
            return data;
        }

        private void DeserializeJsonToFields()
        {
            if (string.IsNullOrEmpty(data))
            {
                UCMDebug.Log("【无法反序列化】");
                return;
            }

            dataDict.Clear();
            var obj = JsonMapper.ToObject(data, typeof(Dictionary<string, object>));
            if (obj is Dictionary<string, object> dict)
            {
                dataDict = dict;
            }
            else
            {
                UCMDebug.Log("【无法反序列化】");
                return;
            }


            FieldInfo[] fields = GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (FieldInfo field in fields)
            {
                if (IsSkip(field))
                    continue;

                if (dataDict.TryGetValue(field.Name, out var result) &&
                    result != null &&
                    field.FieldType == result.GetType())
                    field.SetValue(this, result);
            }

            string kv = null;
            foreach (var item in dataDict)
            {
                kv += "键值：" + item.Key + "\t";
            }
            UCMDebug.Log("【反序列化】");
            UCMDebug.Log(kv);

        }

        protected virtual bool IsSkip(FieldInfo field)
        => Attribute.IsDefined(field, typeof(JsonIgnoreAttribute));
        */
    }

}