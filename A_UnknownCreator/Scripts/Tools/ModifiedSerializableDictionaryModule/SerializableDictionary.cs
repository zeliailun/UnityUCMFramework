using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnknownCreator.Modules
{


    public interface ISerializedKVP<TKey, TValue>
    {
        TKey Key { get; set; }
        TValue Value { get; set; }
        int Index { get; set; }
        bool IsKeyDuplicated { get; set; }
    }

    // 普通值类型 KVP
    [Serializable]
    public class SerializedDictionaryKVPProps<TKey, TValue> : ISerializedKVP<TKey, TValue>
    {
        public TKey Key;
        public TValue Value;
        public int Index;
        public bool IsKeyDuplicated;

        public SerializedDictionaryKVPProps() { }
        public SerializedDictionaryKVPProps(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }

        public static implicit operator SerializedDictionaryKVPProps<TKey, TValue>(KeyValuePair<TKey, TValue> kvp)
            => new SerializedDictionaryKVPProps<TKey, TValue>(kvp.Key, kvp.Value);
        public static implicit operator KeyValuePair<TKey, TValue>(SerializedDictionaryKVPProps<TKey, TValue> kvp)
            => new KeyValuePair<TKey, TValue>(kvp.Key, kvp.Value);

        TKey ISerializedKVP<TKey, TValue>.Key { get => Key; set => Key = value; }
        TValue ISerializedKVP<TKey, TValue>.Value { get => Value; set => Value = value; }
        int ISerializedKVP<TKey, TValue>.Index { get => Index; set => Index = value; }
        bool ISerializedKVP<TKey, TValue>.IsKeyDuplicated { get => IsKeyDuplicated; set => IsKeyDuplicated = value; }
    }

    // 引用类型 KVP（支持多态 SerializeReference）
    [Serializable]
    public class SerializedDictionaryKVPPropsRef<TKey, TValue> : ISerializedKVP<TKey, TValue>
    {
        [SerializeReference, ShowSerializeReference] public TKey Key;
        [SerializeReference, ShowSerializeReference] public TValue Value;
        public int Index;
        public bool IsKeyDuplicated;

        public SerializedDictionaryKVPPropsRef() { }
        public SerializedDictionaryKVPPropsRef(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }

        public static implicit operator SerializedDictionaryKVPPropsRef<TKey, TValue>(KeyValuePair<TKey, TValue> kvp)
            => new SerializedDictionaryKVPPropsRef<TKey, TValue>(kvp.Key, kvp.Value);
        public static implicit operator KeyValuePair<TKey, TValue>(SerializedDictionaryKVPPropsRef<TKey, TValue> kvp)
            => new KeyValuePair<TKey, TValue>(kvp.Key, kvp.Value);

        TKey ISerializedKVP<TKey, TValue>.Key { get => Key; set => Key = value; }
        TValue ISerializedKVP<TKey, TValue>.Value { get => Value; set => Value = value; }
        int ISerializedKVP<TKey, TValue>.Index { get => Index; set => Index = value; }
        bool ISerializedKVP<TKey, TValue>.IsKeyDuplicated { get => IsKeyDuplicated; set => IsKeyDuplicated = value; }
    }



    //------------------------------------------------------------------------------------------




    // 基类，处理序列化逻辑
    [Serializable]
    public abstract class SerializableDictionaryBase<TKey, TValue, TSerializedKVP> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
        where TSerializedKVP : class, ISerializedKVP<TKey, TValue>, new()
    {
        [SerializeField] protected List<TSerializedKVP> dictionaryList = new();

        public void OnBeforeSerialize()
        {
            foreach (var kv in this)
            {
                bool found = false;
                for (int i = 0; i < dictionaryList.Count; i++)
                {
                    if (this.Comparer.Equals(dictionaryList[i].Key, kv.Key))
                    {
                        dictionaryList[i].Value = kv.Value;
                        found = true;
                        break;
                    }
                }
                if (!found)
                    dictionaryList.Add(CreateSerializedKVP(kv.Key, kv.Value));
            }

            dictionaryList.RemoveAll(v => v == null || !ContainsKey(v.Key));

            for (int i = 0; i < dictionaryList.Count; i++)
                dictionaryList[i].Index = i;
        }

        public void OnAfterDeserialize()
        {
            Clear();
            HashSet<TKey> seenKeys = new();
            foreach (var kvp in dictionaryList)
            {
                if (seenKeys.Contains(kvp.Key))
                {
                    kvp.IsKeyDuplicated = true;
                    kvp.Value = default;
                }
                else
                {
                    kvp.IsKeyDuplicated = false;
                    Add(kvp.Key, kvp.Value);
                    seenKeys.Add(kvp.Key);
                }
            }

            dictionaryList.RemoveAll(v => v == null || !ContainsKey(v.Key));
        }

        protected virtual TSerializedKVP CreateSerializedKVP(TKey key, TValue value)
        {
            return new TSerializedKVP { Key = key, Value = value };
        }

        public new TValue this[TKey key]
        {
            get => TryGetValue(key, out var v) ? v : default;
            set
            {
                if (TryGetValue(key, out _))
                {
                    foreach (var kv in dictionaryList)
                    {
                        if (this.Comparer.Equals(kv.Key, key))
                        {
                            kv.Value = value;
                            break;
                        }
                    }
                }
                else
                {
                    dictionaryList.Add(CreateSerializedKVP(key, value));
                }
                base[key] = value;
            }
        }
    }

    // 普通字典
    [Serializable]
    public class SerializableDictionary<TKey, TValue> : SerializableDictionaryBase<TKey, TValue, SerializedDictionaryKVPProps<TKey, TValue>>
    {
    }

    // 引用字典
    [Serializable]
    public class SerializableDictionaryRef<TKey, TValue> : SerializableDictionaryBase<TKey, TValue, SerializedDictionaryKVPPropsRef<TKey, TValue>>
    {
    }

}
