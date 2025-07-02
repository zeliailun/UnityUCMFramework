using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;

namespace UnknownCreator.Modules
{
    public sealed class JsonDataMgr : IJsonDataMgr
    {
        private readonly Dictionary<string, object> _dataDict = new();
        private readonly ReaderWriterLockSlim _dictionaryLock = new();
        private static readonly object _fileLock = new();

        public string path { get; private set; }
        public string folderName { get; set; } = "GameData";

        [SerializeField]
        private List<CustomJsonDataInfo> _jsonData = new();

        // 初始化方法
        void IDearMgr.WorkWork()
        {
            CustomTypeBindings.Register();
            path = Path.Combine(Application.persistentDataPath, folderName);

            foreach (var result in _jsonData)
            {
                AddData(result.asset, result.name);
            }

            EnsureDirectoryExists();
        }

        void IDearMgr.DoNothing()
        {
            ClearAllData();
            _jsonData.Clear();
        }

        // 保存数据（支持覆盖）
        public void SaveData<T>(string fileName, T data, bool isOverwrite)
        {
            if (data == null) return;

            EnsureDirectoryExists();

            string safeFileName = Path.GetFileName(fileName);
            string filePath = Path.Combine(path, safeFileName);

            lock (_fileLock)
            {
                try
                {
                    // 处理备份
                    if (!isOverwrite && File.Exists(filePath))
                    {
                        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                        string backupFileName = $"{Path.GetFileNameWithoutExtension(safeFileName)}_backup_{timestamp}{Path.GetExtension(safeFileName)}";
                        string backupFilePath = Path.Combine(path, backupFileName);
                        File.Copy(filePath, backupFilePath);
                    }

                    // 使用 LitJson 序列化
                    string json = JsonMapper.ToJson(data);
                    File.WriteAllText(filePath, json);
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    Debug.LogError($"Save failed: {ex.Message}");
                    throw;
                }
            }
        }

        // 加载数据
        public T LoadData<T>(string fileName)
        {
            string filePath = GetFilePath(fileName);
            if (!File.Exists(filePath)) return default;

            try
            {
                string json = File.ReadAllText(filePath);
                T obj = JsonMapper.ToObject<T>(json);

                _dictionaryLock.EnterWriteLock();
                try
                {
                    _dataDict[fileName] = obj;
                }
                finally
                {
                    _dictionaryLock.ExitWriteLock();
                }

                return obj;
            }
            catch (JsonException ex)
            {
                Debug.LogError($"JSON parse error: {ex.Message}");
                return default;
            }
        }

        // 添加数据（泛型）
        public T AddData<T>(object textFile, string name = null)
        {
            if (textFile is not TextAsset textAsset) return default;

            try
            {
                T data = JsonMapper.ToObject<T>(textAsset.text);
                string key = string.IsNullOrWhiteSpace(name) ? textAsset.name : name;

                _dictionaryLock.EnterWriteLock();
                try
                {
                    _dataDict[key] = data;
                }
                finally
                {
                    _dictionaryLock.ExitWriteLock();
                }

                return data;
            }
            catch (JsonException ex)
            {
                Debug.LogError($"Deserialization failed: {ex.Message}");
                return default;
            }
        }

        // 添加数据（动态类型）
        public object AddData(Type type, object textFile, string name = null)
        {
            if (textFile is not TextAsset textAsset) return null;

            try
            {
                object data = JsonMapper.ToObject(textAsset.text, type);
                string key = string.IsNullOrWhiteSpace(name) ? textAsset.name : name;

                _dictionaryLock.EnterWriteLock();
                try
                {
                    _dataDict[key] = data;
                }
                finally
                {
                    _dictionaryLock.ExitWriteLock();
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
            return AddData<object>(textFile, name);
        }

        // 获取数据
        public T GetData<T>(string name)
        {
            _dictionaryLock.EnterReadLock();
            try
            {
                return _dataDict.TryGetValue(name, out object data) ? (T)data : default;
            }
            finally
            {
                _dictionaryLock.ExitReadLock();
            }
        }

        // 删除数据
        public void DeleteData(string fileName)
        {
            string filePath = GetFilePath(fileName);

            _dictionaryLock.EnterWriteLock();
            try
            {
                if (File.Exists(filePath))
                {
                    _dataDict.Remove(fileName);
                    File.Delete(filePath);
                }
            }
            finally
            {
                _dictionaryLock.ExitWriteLock();
            }
        }

        // 清空所有数据
        public void DeleteAllData()
        {
            ClearAllData();

            try
            {
                string[] files = Directory.GetFiles(path, "*.json");
                foreach (string file in files)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Failed to delete {file}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"DeleteAllData failed: {ex.Message}");
            }
        }

        // 清除内存数据
        public void ClearAllData()
        {
            _dictionaryLock.EnterWriteLock();
            try
            {
                _dataDict.Clear();
            }
            finally
            {
                _dictionaryLock.ExitWriteLock();
            }
        }

        // 检查文件是否存在
        public bool HasFile(string name)
        {
            string filePath = GetFilePath(name);
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
            _dictionaryLock.EnterReadLock();
            try
            {
                return _dataDict.ContainsKey(name);
            }
            finally
            {
                _dictionaryLock.ExitReadLock();
            }
        }

        // 确保目录存在
        private void EnsureDirectoryExists()
        {
          
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        // 获取完整文件路径
        private string GetFilePath(string fileName)
        {
            return Path.Combine(path, Path.GetFileName(fileName));
        }
    }
}