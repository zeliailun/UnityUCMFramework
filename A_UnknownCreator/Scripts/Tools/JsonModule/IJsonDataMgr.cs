using System;
namespace UnknownCreator.Modules
{
    public interface IJsonDataMgr : IDearMgr
    {
        string path { get; }
        string folderName { set; get; }
        void SaveData<T>(string fileName, T data, bool isOverwrite);
        T LoadData<T>(string fileName);
        T AddData<T>(object textFile, string name = null);
        object AddData(Type type, object textFile, string name = null);
        JsonData AddJsonData(object textFile, string name = null);
        object AddData(object textFile, string name = null);
        void DeleteData(string fileName);
        void DeleteAllData();
        void ClearAllData();
        bool HasFile(string name);
        bool HasData(string name);
        T GetData<T>(string name);
    }
}