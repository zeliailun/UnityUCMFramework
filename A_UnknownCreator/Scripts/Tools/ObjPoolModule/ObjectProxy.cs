
using System;
using UnityEngine;
namespace UnknownCreator.Modules
{

    public struct ObjectProxy<T> where T : class
    {
        private Type _type;
        private bool isGameObject;
        private string gameObjectName;

        private T _target;
        public T Target
        {
            get
            {
                if (_target == null) return null;

                bool isPooled = isGameObject
                    ? Mgr.GPool.HasObject(_target as GameObject, gameObjectName)
                    : Mgr.RPool.HasObject(_type, _target);

                if (isPooled)
                {
                    Clear();
                    UCMDebug.LogWarning("�޷����ö�����еĶ���");
                    return null;
                }

                return _target;
            }
        }

        internal ObjectProxy<T> Set(T value, string name)
        {
            if (value == null)
            {
                UCMDebug.LogWarning("�޷����ÿ�ֵ");
                return this;
            }

            if (_target != null)
            {
                UCMDebug.LogWarning("ֻ������һ��ֵ");
                return this;
            }


            _target = value;

            if (string.IsNullOrWhiteSpace(name))
            {
                isGameObject = false;
                _type = _target.GetType();
            }
            else
            {
                isGameObject = true;
                gameObjectName = name;
            }

            return this;
        }

        public bool IsVaild() => _target != null;

        public void Clear()
        {
            _type = null;
            _target = null;
            gameObjectName = null;
            isGameObject = false;
        }
    }
}