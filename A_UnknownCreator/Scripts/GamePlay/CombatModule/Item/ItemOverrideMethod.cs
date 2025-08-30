using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnknownCreator.Modules
{
    public abstract partial class ItemBase
    {
        /// <summary>
        /// 初始化完成时调用,还未添加到列表中
        /// </summary>
        public virtual void OnInitialized() { }
        public virtual void OnEquipToBody() { }
        public virtual void OnEquipToHand() { }
        public virtual void OnStoraged() { }
        public virtual void OnDrop() { }
        protected virtual void OnRelease() { }
    }
}