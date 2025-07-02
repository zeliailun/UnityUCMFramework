using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnknownCreator.Modules
{

    public class TextureTriggerCfgSO : CustomScriptableObject
    {
        /*
        [LabelText("层")]
        public LayerMask mask;

        [LabelText("默认触发"), HideReferenceObjectPicker, SerializeField]
        private TextureTriggerData inDef = new();
        public TextureTriggerData def => inDef;

        [DictionaryDrawerSettings(DisplayMode= DictionaryDisplayOptions.Foldout)]
        [LabelText("触发字典"), SerializeField]
        private Dictionary<string, List<TextureTriggerData>> inDic = new();
        public Dictionary<string, List<TextureTriggerData>> ttd => inDic;

        public TextureTriggerData FindTexture(string key,string tn)
        {
            if (ttd.TryGetValue(key, out var value))
            {
                foreach (TextureTriggerData data in value)
                {
                    if (data?.texture == null) return null;
                    if (tn == data.texture.name) return data;
                }
            }
            return null;
        }

        public TextureTriggerData FindTextureByCustomName(string key, string tn)
        {
            if (ttd.TryGetValue(key, out var value))
            {
                foreach (TextureTriggerData data in value)
                {
                    if (data?.texture == null) return null;
                    if (tn == data.customName) return data;
                }
            }
            return null;
        }

    }
    

    public class TextureTriggerData
    {
        //private SoundMGR soundMGR => GodMGR.ins.GetMgr<SoundMGR>("SoundMGR");

        private GameObject inObj;

        private int indexAc, indexFx;

        public string customName;

        public Texture texture;

        [FoldoutGroup("声音")]
        public bool isPlaySound = false;

        [FoldoutGroup("声音")]
        public TextureTriggerPlay soundEnum = TextureTriggerPlay.Sequence;

        [FoldoutGroup("声音"), SerializeField]
        private List<SoundData> acList = new();
        public List<SoundData> ac => acList;

        [FoldoutGroup("特效")]
        public bool isPlayFx = false;

        [FoldoutGroup("特效")]
        public TextureTriggerPlay fxEnum = TextureTriggerPlay.Sequence;

        [FoldoutGroup("特效"), SerializeField]
        private List<GameObject> fxList = new();
        public List<GameObject> fx => fxList;

        public void Play(Vector3 pos)
        {
            PlaySound(pos);
            PlayFx(pos);
        }

        private void PlaySound(Vector3 pos)
        {
            if (isPlaySound&& acList!=null && acList.Count>0)
            {
                switch (soundEnum)
                {
                    case TextureTriggerPlay.Sequence:
                        if (indexAc >= ac.Count) indexAc = 0;
                        StartPlaySound(ac[indexAc], pos);
                        indexAc++;
                        break;
                    case TextureTriggerPlay.Random:
                        StartPlaySound(ac[UnityEngine.Random.Range(0, ac.Count)], pos);
                        break;
                }
            }
        }

        private void StartPlaySound(SoundData sd, Vector3 pos)
        {
            if (sd != null)
            {
                sd.pos = pos;
               // soundMGR.PlaySoundPos(sd);
            }
        }


        private void PlayFx(Vector3 pos)
        {
            if (isPlayFx && fxList != null && fxList.Count > 0)
            {
                switch (fxEnum)
                {
                    case TextureTriggerPlay.Sequence:
                        if (indexFx >= fx.Count) indexFx = 0;
                        inObj = fx[indexFx];
                        if (inObj != null)
                        {

                        }
                        indexFx++;
                        break;
                    case TextureTriggerPlay.Random:
                        inObj = fx[UnityEngine.Random.Range(0, fx.Count)];
                        if (inObj != null)
                        {

                        }
                        break;
                }
            }
        }

    }


    public enum TextureTriggerPlay
    {
        Random,
        Sequence
    }
        */
    }


}