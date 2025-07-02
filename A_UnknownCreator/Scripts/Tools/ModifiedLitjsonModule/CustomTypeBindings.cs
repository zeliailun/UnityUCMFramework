
using System;


using Animancer;



#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Audio;


namespace UnknownCreator.Modules
{

#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoad]
#endif
    /// <summary>
    /// Unity内建类型拓展
    /// </summary>
    public static class CustomTypeBindings
    {

        static bool registerd;

        static CustomTypeBindings()
        {
            Register();
        }

        // [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Register()
        {

            if (registerd) return;
            registerd = true;


            CustomBuilt();

            PluginsBuilt();

            UnityBuilt();
        }

        private static void PluginsBuilt()
        {
            RegisterUnityObject<TransitionAsset>();
        }

        private static void CustomBuilt()
        {
            RegisterUnityObject<ScriptableObject>();
        }

        private static void UnityBuilt()
        {
            RegisterUnityObject<AudioClip>();
            RegisterUnityObject<Texture2D>();
            RegisterUnityObject<AvatarMask>();

            // 注册Vector2类型的Exporter
            static void writeVector2(Vector2 v, JsonWriter w)
            {
                w.WriteObjectStart();
                w.WriteProperty("x", v.x);
                w.WriteProperty("y", v.y);
                w.WriteObjectEnd();
            }

            JsonMapper.RegisterExporter<Vector2>((v, w) =>
            {
                writeVector2(v, w);
            });

            // 注册Vector3类型的Exporter
            static void writeVector3(Vector3 v, JsonWriter w)
            {
                w.WriteObjectStart();
                w.WriteProperty("x", v.x);
                w.WriteProperty("y", v.y);
                w.WriteProperty("z", v.z);
                w.WriteObjectEnd();
            }

            JsonMapper.RegisterExporter<Vector3>((v, w) =>
            {
                writeVector3(v, w);
            });

            // 注册Vector4类型的Exporter
            JsonMapper.RegisterExporter<Vector4>((v, w) =>
            {
                w.WriteObjectStart();
                w.WriteProperty("x", v.x);
                w.WriteProperty("y", v.y);
                w.WriteProperty("z", v.z);
                w.WriteProperty("w", v.w);
                w.WriteObjectEnd();
            });

            // 注册Quaternion类型的Exporter
            JsonMapper.RegisterExporter<Quaternion>((v, w) =>
            {
                w.WriteObjectStart();
                w.WriteProperty("x", v.x);
                w.WriteProperty("y", v.y);
                w.WriteProperty("z", v.z);
                w.WriteProperty("w", v.w);
                w.WriteObjectEnd();
            });

            // 注册Color类型的Exporter
            JsonMapper.RegisterExporter<Color>((v, w) =>
            {
                w.WriteObjectStart();
                w.WriteProperty("r", v.r);
                w.WriteProperty("g", v.g);
                w.WriteProperty("b", v.b);
                w.WriteProperty("a", v.a);
                w.WriteObjectEnd();
            });

            // 注册Color32类型的Exporter
            JsonMapper.RegisterExporter<Color32>((v, w) =>
            {
                w.WriteObjectStart();
                w.WriteProperty("r", v.r);
                w.WriteProperty("g", v.g);
                w.WriteProperty("b", v.b);
                w.WriteProperty("a", v.a);
                w.WriteObjectEnd();
            });

            // 注册Bounds类型的Exporter
            JsonMapper.RegisterExporter<Bounds>((v, w) =>
            {
                w.WriteObjectStart();

                w.WritePropertyName("center");
                writeVector3(v.center, w);

                w.WritePropertyName("size");
                writeVector3(v.size, w);

                w.WriteObjectEnd();
            });

            // 注册Rect类型的Exporter
            JsonMapper.RegisterExporter<Rect>((v, w) =>
            {
                w.WriteObjectStart();
                w.WriteProperty("x", v.x);
                w.WriteProperty("y", v.y);
                w.WriteProperty("width", v.width);
                w.WriteProperty("height", v.height);
                w.WriteObjectEnd();
            });

            // 注册RectOffset类型的Exporter
            JsonMapper.RegisterExporter<RectOffset>((v, w) =>
            {
                w.WriteObjectStart();
                w.WriteProperty("top", v.top);
                w.WriteProperty("left", v.left);
                w.WriteProperty("bottom", v.bottom);
                w.WriteProperty("right", v.right);
                w.WriteObjectEnd();
            });

        }

        public static void RegisterUnityObject<T>() where T : UnityEngine.Object
        {

            // 注册导出器
            JsonMapper.RegisterExporter<T>((v, w) =>
            {
                if (v == null)
                    w.Write(null);
                else
                    w.Write(v.name);
            });

            // 注册导入器
            JsonMapper.RegisterImporter<string, T>((objName) =>
            {
                if (string.IsNullOrWhiteSpace(objName))
                    return null;

#if UNITY_EDITOR
                string[] guids = AssetDatabase.FindAssets($"{objName} t:{typeof(T).Name}", null);
                if (guids.Length > 0)
                {
                    T so;
                    foreach (string guid in guids)
                    {
                        so = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                        if (so != null && so.name == objName) return so;
                    }
                    return null;
                }
                else
                {
                    Debug.LogWarning($"{typeof(T).Name} with name {objName} not found in AssetDatabase.");
                    return null;
                }
#else
            return Addressables.LoadAssetAsync<T>(objName).WaitForCompletion();
#endif
            });
        }


    }
}
