using DG.Tweening;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEngine;

namespace DanieloZ.Transitions
{
    [Serializable]
    public abstract class TransitionBase : MonoBehaviour, IRecoverItem
    {
        public bool allowChainOfTransitions = false;
        [Sirenix.OdinInspector.ShowIf(nameof(allowChainOfTransitions))]
        public string nextTransition;

        [Space]
        public bool UnregisterAfterTransition = false;

        public TransitionBase() { }

        public virtual void CallTransition(TransitionsController controller, bool instantly = false)
        {
            if (allowChainOfTransitions)
            {
                controller.CallTransition(nextTransition);
            }

            if (UnregisterAfterTransition) { controller.UnregisterTransition(this); }
        }


        // ==============================================
        //                  IRecoverItem
        // ==============================================

        private string recoverGUID;

        private void OnValidate()
        {
#if UNITY_EDITOR
            if (Application.isPlaying) return;

            if (!DataMigrationManager.HaveInstance()) return;

            var usedGuids = DataMigrationManager.Instance.CollectAllGUIDs(this);

            if (string.IsNullOrEmpty(recoverGUID) || usedGuids.Contains(recoverGUID))
            {
                string oldGuid = recoverGUID;
                string newGuid;

                do
                {
                    newGuid = GUID.Generate().ToString();
                }
                while (usedGuids.Contains(newGuid));

                recoverGUID = newGuid;
            }
#endif
        }

        public string GetGUID()
        {
            return recoverGUID;
        }

        public RecoverData GetState()
        {
            var rd = new RecoverData
            {
                GUID = recoverGUID,
                Data = new Dictionary<string, string>()
            };

            FieldInfo[] fields = GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var field in fields)
            {
                if (field.IsStatic)
                    continue;

                if (field.Name == nameof(recoverGUID))
                    continue;

                object value = field.GetValue(this);
                if (value == null)
                    continue;

                string strVal = DataMigrationManager.ConvertTypeToString(value);
                rd.Data[field.Name] = strVal;
            }

            return rd;
        }

        public void SetState(RecoverData data)
        {
            if (data == null || data.Data == null)
                return;

            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            FieldInfo[] fields = this.GetType().GetFields(flags);

            foreach (var field in fields)
            {
                if (field.IsStatic)
                    continue;

                string newFieldName = field.Name;
                var attr = field.GetCustomAttribute<RecoverAsAttribute>();

                if (!data.Data.TryGetValue(newFieldName, out string valueStr))
                {
                    if (attr != null && attr.OldNames != null)
                    {
                        foreach (var oldName in attr.OldNames)
                        {
                            if (data.Data.TryGetValue(oldName, out valueStr))
                            {
                                break;
                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(valueStr))
                    continue;

                object parsedVal = DataMigrationManager.ConvertStringToType(valueStr, field.FieldType);
                field.SetValue(this, parsedVal);
            }
        }
    }
}

