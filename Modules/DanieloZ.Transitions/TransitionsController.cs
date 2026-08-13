using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DanieloZ.Transitions
{
    public class TransitionsController : MonoBehaviour/*, IRecoverItem*/
    {
        public SerializedDictionary<string, TransitionBase> transitions;

        private void Awake()
        {
            CollectTransitions();
        }

        [ContextMenu("Collect transitions")]
        public void CollectTransitions()
        {
            transitions.Clear();
            foreach (Transform child in transform)
            {
                TransitionBase transition = child.GetComponent<TransitionBase>();
                if (transition != null && !transitions.ContainsKey(transition.name))
                {
                    transitions.Add(transition.name, transition);
                }
            }
        }

        public void RegisterTransition(string name, TransitionBase transition)
        {
            transitions[name] = transition;
        }

        public void UnregisterTransition(string name)
        {
            if (transitions.ContainsKey(name))
            {
                transitions.Remove(name);
            }
        }

        public void UnregisterTransition(TransitionBase transition)
        {
            var pair = transitions
                .FirstOrDefault(x => x.Value == transition);

            if (pair.Key != null)
            {
                transitions.Remove(pair.Key);
            }
        }

        public void CallTransition(string key, bool instantly = false)
        {
            if (transitions.ContainsKey(key))
            {
                if (transitions[key] == null)
                {
                    UIManager.Instance.DebugWarning($"Transition {key} is null. Skipping...");
                    return;
                }
                transitions[key].CallTransition(this, instantly);
            }
        }


        //        private string recoverGUID;

        //        private void OnValidate()
        //        {
        //#if UNITY_EDITOR
        //            if (Application.isPlaying) return;

        //            if (!DataMigrationManager.HaveInstance()) return;

        //            var usedGuids = DataMigrationManager.Instance.CollectAllGUIDs(this);

        //            if (string.IsNullOrEmpty(recoverGUID) || usedGuids.Contains(recoverGUID))
        //            {
        //                string oldGuid = recoverGUID;
        //                string newGuid;

        //                do
        //                {
        //                    newGuid = GUID.Generate().ToString();
        //                }
        //                while (usedGuids.Contains(newGuid));

        //                recoverGUID = newGuid;
        //            }
        //#endif
        //        }

        //        public string GetGUID()
        //        {
        //            return recoverGUID;
        //        }

        //        public RecoverData GetState()
        //        {
        //            var rd = new RecoverData
        //            {
        //                GUID = recoverGUID
        //            };

        //            // Будем хранить словарь "Transitions" 
        //            // в виде ключ -> GUID перехода
        //            // (или специальная "метка", если null/не реализует IRecoverItem).

        //            // Собираем все пары:
        //            //   key = ключ в Transitions
        //            //   value = GUID, если TransitionBase тоже IRecoverItem, 
        //            //            иначе "(null)" или "(no recover)"
        //            Dictionary<string, string> transitionsMap = new Dictionary<string, string>();

        //            foreach (var kvp in transitions)
        //            {
        //                string transitionKey = kvp.Key;
        //                TransitionBase transitionVal = kvp.Value;

        //                if (transitionVal == null)
        //                {
        //                    transitionsMap[transitionKey] = "(null)";
        //                }
        //                else
        //                {
        //                    // Проверяем, реализует ли TransitionBase интерфейс IRecoverItem
        //                    IRecoverItem recover = transitionVal as IRecoverItem;
        //                    if (recover != null)
        //                    {
        //                        transitionsMap[transitionKey] = recover.GetGUID();
        //                    }
        //                    else
        //                    {
        //                        transitionsMap[transitionKey] = "(no recover)";
        //                    }
        //                }
        //            }

        //            // Сериализуем transitionsMap в JSON, чтобы уложить в rd.Data
        //            string transitionsJson = JsonUtility.ToJson(new StringStringDictionary(transitionsMap));
        //            rd.Data["Transitions"] = transitionsJson;

        //            // Можете сохранить и другие поля контроллера, если нужно.
        //            // Например, rd.Data["SomeField"] = something;

        //            return rd;
        //        }

        //        public void SetState(RecoverData data)
        //        {
        //            if (data == null || data.Data == null) return;

        //            // Сохраняем/обновляем GUID контроллера.
        //            if (!string.IsNullOrEmpty(data.GUID))
        //            {
        //                recoverGUID = data.GUID;
        //            }

        //            // Пытаемся прочесть transitionsJson
        //            if (data.Data.TryGetValue("Transitions", out string transitionsJson))
        //            {
        //                // Распарсим JSON обратно в словарь string->string
        //                var container = JsonUtility.FromJson<StringStringDictionary>(transitionsJson);
        //                if (container != null && container.Map != null)
        //                {
        //                    // Собираем все IRecoverItem, чтобы искать их по GUID
        //                    var allItems = GatherAllRecoverItemsInScene();

        //                    foreach (var kvp in container.Map)
        //                    {
        //                        string transitionKey = kvp.Key;
        //                        string storedGuid = kvp.Value; // может быть "(null)", "(no recover)", или реальный GUID

        //                        if (storedGuid == "(null)")
        //                        {
        //                            // Переход был null — обнуляем и сейчас
        //                            transitions[transitionKey] = null;
        //                        }
        //                        else if (storedGuid == "(no recover)")
        //                        {
        //                            // Раньше был TransitionBase без IRecoverItem — 
        //                            // непонятно, как восстанавливать. Обнулим или оставим как есть?
        //                            transitions[transitionKey] = null;
        //                        }
        //                        else
        //                        {
        //                            // Пытаемся найти IRecoverItem с таким GUID
        //                            if (allItems.TryGetValue(storedGuid, out IRecoverItem foundItem))
        //                            {
        //                                // Преобразуем обратно к TransitionBase
        //                                TransitionBase transitionObj = foundItem as TransitionBase;
        //                                transitions[transitionKey] = transitionObj;
        //                            }
        //                            else
        //                            {
        //                                // GUID не найден — возможно переход потерян/удалён
        //                                transitions[transitionKey] = null;
        //                            }
        //                        }
        //                    }
        //                }
        //            }
        //        }
    }
}
