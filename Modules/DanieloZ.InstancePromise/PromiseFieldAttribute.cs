using System;
using UnityEngine;

namespace DanieloZ.InstancePromise
{
    /// <summary>
    /// Атрибут для полей, которые должны быть инициализированы через InstancePromise
    /// Помогает визуально идентифицировать поля, которые используют паттерн промисов
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class PromiseFieldAttribute : PropertyAttribute
    {
        /// <summary>
        /// Сообщение для отображения в инспекторе
        /// </summary>
        public string Message { get; private set; }

        /// <summary>
        /// Цвет иконки в инспекторе
        /// </summary>
        public PromiseFieldColor Color { get; private set; }

        /// <summary>
        /// Показывать ли статус промиса в инспекторе
        /// </summary>
        public bool ShowStatus { get; private set; }

        /// <summary>
        /// Создает новый атрибут PromiseField
        /// </summary>
        /// <param name="message">Опциональное сообщение</param>
        /// <param name="color">Цвет иконки</param>
        /// <param name="showStatus">Показывать статус</param>
        public PromiseFieldAttribute(string message = "", PromiseFieldColor color = PromiseFieldColor.Blue, bool showStatus = true)
        {
            Message = message;
            Color = color;
            ShowStatus = showStatus;
        }
    }

    /// <summary>
    /// Цвета для визуализации PromiseField в инспекторе
    /// </summary>
    public enum PromiseFieldColor
    {
        Blue,
        Green,
        Yellow,
        Red,
        Gray
    }
}

