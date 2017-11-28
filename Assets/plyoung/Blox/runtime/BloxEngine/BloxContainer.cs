using BloxEngine.Variables;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BloxEngine
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Blox/Blox Visual Script")]
    [HelpURL("http://www.plyoung.com/blox/container.html")]
    public class BloxContainer : MonoBehaviour, ISerializationCallbackReceiver
    {
        [SerializeField]
        public GameObject bloxGlobalPrefab;

        [SerializeField]
        public List<string> bloxIdents = new List<string>();

        [SerializeField]
        private List<BloxVariables> bloxVars = new List<BloxVariables>();

        private Dictionary<string, List<BloxEvent>> customEvents;

        private Dictionary<string, BloxVariables> bloxVarCache;

        protected void Awake()
        {
            Debug.Log("Container awake", this);
            BloxGlobal.Create(this.bloxGlobalPrefab);
            this.customEvents = new Dictionary<string, List<BloxEvent>>();
            this.bloxVarCache = new Dictionary<string, BloxVariables>();
            Debug.Log("生成事件和变量缓存");
            for (int i = 0; i < this.bloxVars.Count; i++)
            {
                Debug.Log("添加变量缓存" + this.bloxVars[i].bloxIdent + " 值 " + this.bloxVars[i]);
                this.bloxVarCache.Add(this.bloxVars[i].bloxIdent, this.bloxVars[i]);
                this.CheckVariables(this.bloxVars[i], BloxGlobal.Instance.FindBloxDef(this.bloxIdents[i]));
                this.bloxVars[i].BuildCache();
            }
            for (int j = 0; j < this.bloxIdents.Count; j++)
            {
                Blox blox = BloxGlobal.Instance.FindBloxDef(this.bloxIdents[j]);
                if (!((UnityEngine.Object)blox == (UnityEngine.Object)null))
                {
                    if (!this.bloxVarCache.ContainsKey(this.bloxIdents[j]))
                    {
                        BloxVariables bloxVariables = new BloxVariables();
                        bloxVariables.bloxIdent = this.bloxIdents[j];
                        this.bloxVars.Add(bloxVariables);
                        this.bloxVarCache.Add(bloxVariables.bloxIdent, bloxVariables);
                        this.CheckVariables(bloxVariables, blox);
                        bloxVariables.BuildCache();
                    }
                    this.AttachComponents(blox);
                }
            }
        }

        private void AttachComponents(Blox b)
        {
            if (b.scriptDirty || b.scriptType == null)
            {
                this.AttachEventHandlers(b);
            }
            else
            {
                base.gameObject.AddComponent(b.scriptType);
            }
        }

        private void AttachEventHandlers(Blox b)
        {
            if (!b.bloxLoaded)
            {
                b.Deserialize();
            }
            Common_BloxEventHandler common_BloxEventHandler = null;
            for (int i = 0; i < b.events.Length; i++)
            {
                if (b.events[i].active)
                {
                    b.events[i].Init(this);
                    if (b.events[i].ident.Equals("Custom"))
                    {
                        if (!this.customEvents.ContainsKey(b.events[i].screenName))
                        {
                            this.customEvents.Add(b.events[i].screenName, new List<BloxEvent>());
                        }
                        this.customEvents[b.events[i].screenName].Add(b.events[i]);
                    }
                    else
                    {
                        Type type = BloxGlobal.FindEventHandlerType(b.events[i].ident);
                        if (type == null)
                        {
                            Debug.LogWarning("Could not find a handler for: " + b.events[i].ident, base.gameObject);
                        }
                        else
                        {
                            BloxEventHandler bloxEventHandler = (BloxEventHandler)base.gameObject.GetComponent(type);
                            if ((UnityEngine.Object)bloxEventHandler == (UnityEngine.Object)null)
                            {
                                bloxEventHandler = (BloxEventHandler)base.gameObject.AddComponent(type);
                                bloxEventHandler.container = this;
                                if (bloxEventHandler.GetType() == typeof(Common_BloxEventHandler))
                                {
                                    common_BloxEventHandler = (Common_BloxEventHandler)bloxEventHandler;
                                }
                            }
                            bloxEventHandler.AddEvent(b.events[i]);
                        }
                    }
                }
            }
            if ((UnityEngine.Object)common_BloxEventHandler != (UnityEngine.Object)null)
            {
                common_BloxEventHandler.Awake();
                common_BloxEventHandler.OnEnable();
            }
        }

        public void TriggerEvent(string eventName)
        {
            base.gameObject.SendMessage(eventName, new BloxEventArg[0], SendMessageOptions.DontRequireReceiver);
            List<BloxEvent> list = default(List<BloxEvent>);
            if (this.customEvents == null)
            {
                Debug.LogError("The Event Cache is not yet initialized.", base.gameObject);
            }
            else if (this.customEvents.TryGetValue(eventName, out list))
            {
                for (int i = 0; i < list.Count; i++)
                {
                    this.RunEvent(list[i]);
                }
            }
        }

        public void TriggerEvent(string eventName, BloxEventArg[] args)
        {
            if (args == null || args.Length == 0)
            {
                this.TriggerEvent(eventName);
            }
            else
            {
                base.gameObject.SendMessage(eventName, args, SendMessageOptions.DontRequireReceiver);
                List<BloxEvent> list = default(List<BloxEvent>);
                if (this.customEvents == null)
                {
                    Debug.LogError("The Event Cache is not yet initialized.", base.gameObject);
                }
                else if (this.customEvents.TryGetValue(eventName, out list))
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        this.RunEvent(list[i], args);
                    }
                }
            }
        }

        public void TriggerEvent(string eventName, float afterSeconds)
        {
            if (afterSeconds > 0.0)
            {
                BloxGlobal.Instance.AddDelayedEvent(this, eventName, afterSeconds, (BloxEventArg[])null);
            }
            else
            {
                this.TriggerEvent(eventName);
            }
        }

        public void TriggerEvent(string eventName, float afterSeconds, BloxEventArg[] args)
        {
            if (afterSeconds > 0.0)
            {
                BloxGlobal.Instance.AddDelayedEvent(this, eventName, afterSeconds, args);
            }
            else
            {
                this.TriggerEvent(eventName, args);
            }
        }

        private void RunEvent(BloxEvent ev, params BloxEventArg[] args)
        {
            ev.GetReadyToRun(args);
            if (ev.canYield)
            {
                BloxGlobal.Instance.StartCoroutine(ev.RunYield(this));
            }
            else
            {
                ev.Run(this);
            }
        }

        public plyVar FindVariable(string bloxIdent, string varName)
        {
            Debug.Log("查找变量 FindVariable " + "bloxIdent:" + bloxIdent + "varName:" + varName);
            BloxVariables bloxVariables = this.GetBloxVariables(bloxIdent, null);
            if (bloxVariables == null)
            {
                return null;
            }
            return bloxVariables.FindVariable(varName);
        }

        public BloxVariables GetBloxVariables(string bloxIdent, Blox b = null)
        {
            Debug.Log("获取变量GetBloxVariables " + "bloxIdent: "+ bloxIdent);
            BloxVariables bloxVariables = null;
            if ((UnityEngine.Object)b == (UnityEngine.Object)null)
            {
                if (this.bloxVarCache.TryGetValue(bloxIdent, out bloxVariables))
                {
                    return bloxVariables;
                }
                Debug.LogError("This Blox Container does not contain variables for a Blox with ident: " + bloxIdent, base.gameObject);
            }
            else
            {
                if (this.bloxVars.Count > 0)
                {
                    for (int num = this.bloxVars.Count - 1; num >= 0; num--)
                    {
                        if (!this.bloxIdents.Contains(this.bloxVars[num].bloxIdent))
                        {
                            this.bloxVars.RemoveAt(num);
                        }
                    }
                }
                if (this.bloxVarCache == null)
                {
                    this.bloxVarCache = new Dictionary<string, BloxVariables>();
                    for (int i = 0; i < this.bloxVars.Count; i++)
                    {
                        this.bloxVarCache.Add(this.bloxVars[i].bloxIdent, this.bloxVars[i]);
                    }
                }
                this.bloxVarCache.TryGetValue(bloxIdent, out bloxVariables);
                if (bloxVariables == null)
                {
                    bloxVariables = new BloxVariables();
                    bloxVariables.bloxIdent = bloxIdent;
                    this.bloxVars.Add(bloxVariables);
                    this.bloxVarCache.Add(bloxIdent, bloxVariables);
                }
                else
                {
                    bloxVariables.Deserialize(false);
                }
                this.CheckVariables(bloxVariables, b);
            }
            return bloxVariables;
        }

        public void RemoveBloxVariables(string bloxIdent)
        {
            if (this.bloxVarCache != null && this.bloxVarCache.ContainsKey(bloxIdent))
            {
                this.bloxVarCache.Remove(bloxIdent);
            }
            int num = 0;
            while (true)
            {
                if (num < this.bloxVars.Count)
                {
                    if (!this.bloxVars[num].bloxIdent.Equals(bloxIdent))
                    {
                        num++;
                        continue;
                    }
                    break;
                }
                return;
            }
            this.bloxVars.RemoveAt(num);
        }

        private void CheckVariables(BloxVariables v, Blox b)
        {
            Debug.Log("CheckVariables " + " BloxVariables: " + v + "Blox: " + b);
            if (v != null && !((UnityEngine.Object)b == (UnityEngine.Object)null))
            {
                if (v.varDefs.Count > 0)
                {
                    for (int num = v.varDefs.Count - 1; num >= 0; num--)
                    {
                        plyVar plyVar = v.varDefs[num];
                        bool flag = false;
                        for (int i = 0; i < b.variables.varDefs.Count; i++)
                        {
                            plyVar plyVar2 = b.variables.varDefs[i];
                            if (plyVar.ident == plyVar2.ident)
                            {
                                plyVar.name = plyVar2.name;
                                if (plyVar.variableType != plyVar2.variableType)
                                {
                                    plyVar2.CopyTo(plyVar);
                                }
                                flag = true;
                                break;
                            }
                        }
                        if (!flag)
                        {
                            v.varDefs.RemoveAt(num);
                        }
                    }
                }
                for (int j = 0; j < b.variables.varDefs.Count; j++)
                {
                    plyVar plyVar3 = b.variables.varDefs[j];
                    bool flag2 = false;
                    int num2 = 0;
                    while (num2 < v.varDefs.Count)
                    {
                        if (plyVar3.ident != v.varDefs[num2].ident)
                        {
                            num2++;
                            continue;
                        }
                        flag2 = true;
                        break;
                    }
                    if (!flag2)
                    {
                        v.varDefs.Add(plyVar3.Copy());
                    }
                }
            }
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            for (int i = 0; i < this.bloxVars.Count; i++)
            {
                this.bloxVars[i].Deserialize(true);
            }
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            for (int i = 0; i < this.bloxVars.Count; i++)
            {
                this.bloxVars[i].Serialize();
            }
        }
    }
}
