#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;

namespace DanieloZ.InstancePromise.Editor
{
    /// <summary>
    /// Custom drawer для атрибута PromiseField
    /// Отображает статус промиса и дополнительную информацию в инспекторе
    /// </summary>
    [CustomPropertyDrawer(typeof(PromiseFieldAttribute))]
    public class PromiseFieldDrawer : PropertyDrawer
    {
        private const float IconSize = 16f;
        private const float Spacing = 4f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            PromiseFieldAttribute promiseAttr = attribute as PromiseFieldAttribute;
            
            EditorGUI.BeginProperty(position, label, property);

            // Позиция для иконки
            Rect iconRect = new Rect(position.x, position.y, IconSize, IconSize);
            
            // Позиция для основного поля
            Rect propertyRect = new Rect(
                position.x + IconSize + Spacing,
                position.y,
                position.width - IconSize - Spacing,
                EditorGUIUtility.singleLineHeight
            );

            // Рисуем иконку промиса
            DrawPromiseIcon(iconRect, promiseAttr, property);

            // Рисуем основное поле
            EditorGUI.PropertyField(propertyRect, property, label, true);

            // Рисуем статус промиса, если включено
            if (promiseAttr.ShowStatus)
            {
                DrawPromiseStatus(position, promiseAttr, property);
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            PromiseFieldAttribute promiseAttr = attribute as PromiseFieldAttribute;
            float baseHeight = EditorGUI.GetPropertyHeight(property, label, true);
            
            // Добавляем высоту для статуса, если он включен
            if (promiseAttr.ShowStatus)
            {
                baseHeight += EditorGUIUtility.singleLineHeight + 2f;
            }

            return baseHeight;
        }

        private void DrawPromiseIcon(Rect rect, PromiseFieldAttribute attr, SerializedProperty property)
        {
            Color iconColor = GetColorForPromiseField(attr.Color);
            
            // Рисуем круг
            Texture2D circleTexture = EditorGUIUtility.IconContent("sv_icon_dot0_pix16_gizmo").image as Texture2D;
            
            if (circleTexture != null)
            {
                Color originalColor = GUI.color;
                GUI.color = iconColor;
                GUI.DrawTexture(rect, circleTexture);
                GUI.color = originalColor;
            }
            else
            {
                // Fallback: рисуем простой круг
                EditorGUI.DrawRect(rect, iconColor);
            }
        }

        private void DrawPromiseStatus(Rect position, PromiseFieldAttribute attr, SerializedProperty property)
        {
            // Получаем информацию о статусе промиса через рефлексию
            string statusText = GetPromiseStatus(property);
            
            if (!string.IsNullOrEmpty(statusText))
            {
                Rect statusRect = new Rect(
                    position.x + IconSize + Spacing,
                    position.y + EditorGUI.GetPropertyHeight(property, GUIContent.none, true) + 2f,
                    position.width - IconSize - Spacing,
                    EditorGUIUtility.singleLineHeight
                );

                GUIStyle statusStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    fontStyle = FontStyle.Italic,
                    normal = { textColor = Color.gray }
                };

                EditorGUI.LabelField(statusRect, statusText, statusStyle);
            }

            // Отображаем custom сообщение, если есть
            if (!string.IsNullOrEmpty(attr.Message))
            {
                Rect messageRect = new Rect(
                    position.x + IconSize + Spacing,
                    position.y + EditorGUIUtility.singleLineHeight + 2f,
                    position.width - IconSize - Spacing,
                    EditorGUIUtility.singleLineHeight
                );

                GUIStyle messageStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    fontSize = 10,
                    normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
                };

                EditorGUI.LabelField(messageRect, attr.Message, messageStyle);
            }
        }

        private string GetPromiseStatus(SerializedProperty property)
        {
            try
            {
                object targetObject = property.serializedObject.targetObject;
                
                // Получаем тип поля
                FieldInfo fieldInfo = targetObject.GetType().GetField(
                    property.name,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                );

                if (fieldInfo != null)
                {
                    object fieldValue = fieldInfo.GetValue(targetObject);
                    
                    if (fieldValue != null)
                    {
                        // Проверяем, является ли это InstancePromise
                        Type fieldType = fieldValue.GetType();
                        
                        if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition().Name.Contains("InstancePromise"))
                        {
                            PropertyInfo isResolvedProp = fieldType.GetProperty("IsResolved");
                            
                            if (isResolvedProp != null)
                            {
                                bool isResolved = (bool)isResolvedProp.GetValue(fieldValue);
                                return isResolved ? "✓ Промис выполнен" : "⏳ Ожидание экземпляра...";
                            }
                        }
                    }
                    else
                    {
                        return "⚠ Поле не инициализировано";
                    }
                }
            }
            catch (Exception)
            {
                // Игнорируем ошибки рефлексии
            }

            return string.Empty;
        }

        private Color GetColorForPromiseField(PromiseFieldColor color)
        {
            switch (color)
            {
                case PromiseFieldColor.Blue:
                    return new Color(0.3f, 0.6f, 1f);
                case PromiseFieldColor.Green:
                    return new Color(0.3f, 0.8f, 0.3f);
                case PromiseFieldColor.Yellow:
                    return new Color(1f, 0.9f, 0.2f);
                case PromiseFieldColor.Red:
                    return new Color(1f, 0.3f, 0.3f);
                case PromiseFieldColor.Gray:
                    return new Color(0.6f, 0.6f, 0.6f);
                default:
                    return Color.white;
            }
        }
    }
}
#endif

