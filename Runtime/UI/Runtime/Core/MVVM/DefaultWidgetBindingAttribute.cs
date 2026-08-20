using System;

namespace DM.UI
{
    /// <summary>
    ///     Биндинг виджета по умолчанию: какой вьюмодель и какой лэйаут использовать,
    ///     если они не переданы явно при открытии.
    ///     Любой из параметров может быть null — тогда соответствующее значение обязано прийти извне.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class DefaultWidgetBindingAttribute : Attribute
    {
        public DefaultWidgetBindingAttribute(Type viewModelType = null, string layoutId = null)
        {
            ViewModelType = viewModelType;
            LayoutId = layoutId;
        }

        public string LayoutId { get; }

        // Раньше был публичный сеттер: атрибут мог быть изменён после чтения.
        public Type ViewModelType { get; }
    }
}
