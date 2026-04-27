using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace UnknownCreator.Modules
{
    public sealed class JsonDataMgr : IJsonDataMgr
    {
        private readonly Dictionary<string, object> _dataDict = new();

        // 简化锁设计：
        // 1. 移除 ReaderWriterLockSlim，避免过度设计和额外维护成本
        // 2. 使用一个实例级锁统一保护内存字典和文件操作，逻辑更一致，也更不容易出并发问题
        private readonly object _lock = new();

        public string path { get; private set; }
        public string folderName { get; set; } = "JsonData";

        [SerializeField]
        private List<CustomJsonDataInfo> _jsonData = new();


        void IDearMgr.WorkWork()
        {
            CustomTypeBindings.Register();
            path = Path.Combine(Application.persistentDataPath, folderName);

            EnsureDirectoryExists();

            foreach (var result in _jsonData)
            {
                AddData(result.asset, result.name);
            }
        }

        void IDearMgr.DoNothing()
        {
            ClearAllData();
        }

        // 保存数据（支持覆盖）
        public void SaveData<T>(string fileName, T data, bool isOverwrite)
        {
            if (data == null) return;

            EnsureDirectoryExists();

            string key = NormalizeKey(fileName);
            string filePath = GetFilePath(key);

            lock (_lock)
            {
                try
                {
                    // 不覆盖时，如已存在则先备份
                    if (!isOverwrite && File.Exists(filePath))
                    {
                        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                        string backupFileName = $"{Path.GetFileNameWithoutExtension(key)}_backup_{timestamp}{Path.GetExtension(key)}";
                        string backupFilePath = Path.Combine(path, backupFileName);
                        File.Copy(filePath, backupFilePath, true);
                    }

                    string json = JsonMapper.ToJson(data);
                    File.WriteAllText(filePath, json);

                    // 保存成功后同步更新内存缓存，避免磁盘和内存不一致
                    _dataDict[key] = data;

                    UCMDebug.Log($"保存数据成功: {key}");
                }
                catch (JsonException ex)
                {
                    UCMDebug.LogError($"序列化失败: {ex.Message}");
                    throw;
                }
                catch (IOException ex)
                {
                    UCMDebug.LogError($"保存失败: {ex.Message}");
                    throw;
                }
                catch (UnauthorizedAccessException ex)
                {
                    UCMDebug.LogError($"保存失败: {ex.Message}");
                    throw;
                }
                catch (Exception ex)
                {
                    UCMDebug.LogError($"未知错误: {ex.Message}");
                    throw;
                }
            }
        }

        // 加载数据
        public T LoadData<T>(string fileName)
        {
            string key = NormalizeKey(fileName);
            string filePath = GetFilePath(key);

            lock (_lock)
            {
                if (!File.Exists(filePath)) return default;

                try
                {
                    string json = File.ReadAllText(filePath);
                    T obj = JsonMapper.ToObject<T>(json);

                    // 重新加载后刷新内存缓存
                    _dataDict[key] = obj;

                    return obj;
                }
                catch (JsonException ex)
                {
                    UCMDebug.LogError($"json语法错误: {ex.Message}");
                    return default;
                }
                catch (IOException ex)
                {
                    UCMDebug.LogError($"读取文件失败: {ex.Message}");
                    return default;
                }
                catch (UnauthorizedAccessException ex)
                {
                    UCMDebug.LogError($"无权限读取文件: {ex.Message}");
                    return default;
                }
            }
        }

        // 添加数据（泛型）
        public T AddData<T>(object textFile, string name = null)
        {
            if (textFile is not TextAsset textAsset) return default;

            try
            {
                T data = JsonMapper.ToObject<T>(textAsset.text);
                string key = string.IsNullOrWhiteSpace(name) ? textAsset.name : NormalizeKey(name);

                lock (_lock)
                {
                    _dataDict[key] = data;
                }

                return data;
            }
            catch (JsonException ex)
            {
                Debug.LogError($"序列化失败: {ex.Message}");
                return default;
            }
        }

        // 添加数据（动态类型）
        public object AddData(Type type, object textFile, string name = null)
        {
            if (textFile is not TextAsset textAsset) return null;
            if (type == null) return null;

            try
            {
                object data = JsonMapper.ToObject(textAsset.text, type);
                string key = string.IsNullOrWhiteSpace(name) ? textAsset.name : NormalizeKey(name);

                lock (_lock)
                {
                    _dataDict[key] = data;
                }

                return data;
            }
            catch (JsonException ex)
            {
                Debug.LogError($"Deserialization failed: {ex.Message}");
                return null;
            }
        }

        // 添加 JsonData 类型数据
        public JsonData AddJsonData(object textFile, string name = null)
        {
            return (JsonData)AddData(typeof(JsonData), textFile, name);
        }

        // 添加数据（非泛型）
        public object AddData(object textFile, string name = null)
        {
            // 保留原函数，不删除
            // 这里沿用你原本设计，但本质上更推荐外部明确指定类型
            return AddData<object>(textFile, name);
        }

        // 获取数据
        public T GetData<T>(string name)
        {
            string key = NormalizeKey(name);

            lock (_lock)
            {
                if (_dataDict.TryGetValue(key, out object data))
                {
                    if (data is T typedData)
                    {
                        return typedData;
                    }

                    Debug.LogWarning($"获取数据失败，类型不匹配: {key}");
                }

                return default;
            }
        }

        // 删除数据
        public void DeleteData(string fileName)
        {
            string key = NormalizeKey(fileName);
            string filePath = GetFilePath(key);

            lock (_lock)
            {
                try
                {
                    _dataDict.Remove(key);

                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }
                catch (IOException ex)
                {
                    Debug.LogError($"删除文件失败: {ex.Message}");
                }
                catch (UnauthorizedAccessException ex)
                {
                    Debug.LogError($"无权限删除文件: {ex.Message}");
                }
            }
        }

        // 清空所有数据
        public void DeleteAllData()
        {
            lock (_lock)
            {
                _dataDict.Clear();

                try
                {
                    EnsureDirectoryExists();

                    string[] files = Directory.GetFiles(path, "*.json");
                    foreach (string file in files)
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"删除失败 {file}: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"删除所有数据失败: {ex.Message}");
                }
            }
        }

        // 清除内存数据
        public void ClearAllData()
        {
            lock (_lock)
            {
                _dataDict.Clear();
            }
        }

        // 检查文件是否存在
        public bool HasFile(string name)
        {
            string key = NormalizeKey(name);
            string filePath = GetFilePath(key);

            try
            {
                FileInfo info = new FileInfo(filePath);
                info.Refresh();
                return info.Exists;
            }
            catch
            {
                return false;
            }
        }

        // 检查内存中是否有数据
        public bool HasData(string name)
        {
            string key = NormalizeKey(name);

            lock (_lock)
            {
                return _dataDict.ContainsKey(key);
            }
        }

        // 确保目录存在
        private void EnsureDirectoryExists()
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new InvalidOperationException("path 尚未初始化，请先调用 WorkWork()");
            }

            Directory.CreateDirectory(path);
        }

        // 获取完整文件路径
        private string GetFilePath(string fileName)
        {
            string key = NormalizeKey(fileName);
            return Path.Combine(path, key);
        }

        // 统一 key，避免传入带路径时缓存 key 混乱
        private string NormalizeKey(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("fileName 不能为空", nameof(fileName));
            }

            return Path.GetFileName(fileName);
        }
    }
}