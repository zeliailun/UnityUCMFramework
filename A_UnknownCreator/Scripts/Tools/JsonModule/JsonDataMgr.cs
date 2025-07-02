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

        // ��ʼ������
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

        // �������ݣ�֧�ָ��ǣ�
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
                    // ������
                    if (!isOverwrite && File.Exists(filePath))
                    {
                        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                        string backupFileName = $"{Path.GetFileNameWithoutExtension(safeFileName)}_backup_{timestamp}{Path.GetExtension(safeFileName)}";
                        string backupFilePath = Path.Combine(path, backupFileName);
                        File.Copy(filePath, backupFilePath);
                    }

                    // ʹ�� LitJson ���л�
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

        // ��������
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

        // ������ݣ����ͣ�
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

        // ������ݣ���̬���ͣ�
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

        // ��� JsonData ��������
        public JsonData AddJsonData(object textFile, string name = null)
        {
            return (JsonData)AddData(typeof(JsonData), textFile, name);
        }

        // ������ݣ��Ƿ��ͣ�
        public object AddData(object textFile, string name = null)
        {
            return AddData<object>(textFile, name);
        }

        // ��ȡ����
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

        // ɾ������
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

        // �����������
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

        // ����ڴ�����
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

        // ����ļ��Ƿ����
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

        // ����ڴ����Ƿ�������
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

        // ȷ��Ŀ¼����
        private void EnsureDirectoryExists()
        {
          
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        // ��ȡ�����ļ�·��
        private string GetFilePath(string fileName)
        {
            return Path.Combine(path, Path.GetFileName(fileName));
        }
    }
}