using System.Collections.Generic;
using UnityEngine;

namespace UnknownCreator.Modules
{
    [System.Serializable]
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField] private List<SerializedDictionaryKVPProps<TKey, TValue>> dictionaryList = new();

        public IReadOnlyList<SerializedDictionaryKVPProps<TKey, TValue>> kv => dictionaryList;


        /*void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            foreach (var kVP in this)
            {
                if (dictionaryList.FirstOrDefault(value => this.Comparer.Equals(value.Key, kVP.Key))
                    is SerializedDictionaryKVPProps<TKey, TValue> serializedKVP)
                {
                    serializedKVP.Value = kVP.Value;
                }
                else
                {
                    dictionaryList.Add(kVP);
                }
            }

            dictionaryList.RemoveAll(value => ContainsKey(value.Key) == false);

            for (int i = 0; i < dictionaryList.Count; i++)
            {
                dictionaryList[i].index = i;
            }
        } */


        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            // 更新或添加键值对
            foreach (var kVP in this)
            {
                bool found = false;

                for (int i = 0; i < dictionaryList.Count; i++)
                {
                    if (this.Comparer.Equals(dictionaryList[i].Key, kVP.Key))
                    {
                        dictionaryList[i].Value = kVP.Value;
                        found = true;
                        break;
                    }
                }

                if (!found)
                    dictionaryList.Add(kVP);
            }

            // 移除不再存在的键
            dictionaryList.RemoveAll(value => !ContainsKey(value.Key));

            // 更新索引
            for (int i = 0; i < dictionaryList.Count; i++)
            {
                dictionaryList[i].index = i;
            }
        }
        

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            Clear();

            HashSet<TKey> seenKeys = new();
            SerializedDictionaryKVPProps<TKey, TValue> serializedKVP;
            for (int i = 0; i < dictionaryList.Count; i++)
            {
                serializedKVP = dictionaryList[i];

                // 跳过已经是null的键
                if (serializedKVP.Key == null || EqualityComparer<TKey>.Default.Equals(serializedKVP.Key, default))
                {
                    serializedKVP.isKeyDuplicated = true;
                    continue;
                }

                if (seenKeys.Contains(serializedKVP.Key))
                {
                    serializedKVP.isKeyDuplicated = true;
                    UCMDebug.LogWarning($"重复键 '{serializedKVP.Key}' 在索引 {i} 处发现。将键设置为null。");

                    // 将重复键设为null
                    serializedKVP.Key = default;
                }
                else
                {
                    serializedKVP.isKeyDuplicated = false;
                    Add(serializedKVP.Key, serializedKVP.Value);
                    seenKeys.Add(serializedKVP.Key);
                }
            }

            // 最后清理null键的条目
            dictionaryList.RemoveAll(r => r.Key == null || EqualityComparer<TKey>.Default.Equals(r.Key, default));
        }

        public new TValue this[TKey key]
        {
            get
            {
#if UNITY_EDITOR
                if (ContainsKey(key))
                {
                    // 手动查找重复键
                    bool hasDuplicate = false;
                    int duplicateCount = 0;
                    for (int i = 0; i < dictionaryList.Count; i++)
                    {
                        if (this.Comparer.Equals(dictionaryList[i].Key, key))
                        {
                            duplicateCount++;
                            if (duplicateCount > 1)
                            {
                                hasDuplicate = true;
                                break;
                            }
                        }
                    }

                    if (hasDuplicate)
                    {
                        Debug.LogError($"Key '{key}' is duplicated {duplicateCount} times in the dictionary.");
                    }

                    return base[key];
                }
                else
                {
                    Debug.LogError($"Key '{key}' not found in dictionary.");
                    return default(TValue);
                }
#else
        return base[key];
#endif
            }

            set
            {
#if UNITY_EDITOR
                if (ContainsKey(key))
                {
                    // 更新值
                    base[key] = value;

                    // 手动查找并更新序列化列表中的相应项
                    for (int i = 0; i < dictionaryList.Count; i++)
                    {
                        if (this.Comparer.Equals(dictionaryList[i].Key, key))
                        {
                            dictionaryList[i].Value = value;
                            break;
                        }
                    }
                }
                else
                {
                    // 添加新项
                    Add(key, value);
                    dictionaryList.Add(new SerializedDictionaryKVPProps<TKey, TValue>(key, value));
                }
#else
        base[key] = value;
#endif
            }
        }



        [System.Serializable]
        public class SerializedDictionaryKVPProps<TypeKey, TypeValue>
        {
            public TypeKey Key;
            public TypeValue Value;

            public int index;
            public bool isKeyDuplicated;

            public SerializedDictionaryKVPProps(TypeKey key, TypeValue value) { this.Key = key; this.Value = value; }

            public static implicit operator SerializedDictionaryKVPProps<TypeKey, TypeValue>(KeyValuePair<TypeKey, TypeValue> kvp)
                => new SerializedDictionaryKVPProps<TypeKey, TypeValue>(kvp.Key, kvp.Value);
            public static implicit operator KeyValuePair<TypeKey, TypeValue>(SerializedDictionaryKVPProps<TypeKey, TypeValue> kvp)
                => new KeyValuePair<TypeKey, TypeValue>(kvp.Key, kvp.Value);
        }
    }

}