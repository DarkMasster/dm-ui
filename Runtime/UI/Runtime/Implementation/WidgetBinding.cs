using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace DM.UI
{
    public class WidgetBinding
    {
        public readonly string LayoutId;
        public readonly Type ViewModelType;
        public readonly Type WidgetType;

        public WidgetBinding(Type widgetType, Type viewModelType, string layoutId)
        {
            WidgetType = widgetType;
            ViewModelType = viewModelType;
            LayoutId = layoutId;
        }

        /// <summary>
        ///     Собирает биндинги виджетов из указанных сборок.
        ///     Если сборки не заданы — обходит весь AppDomain (медленно на старте, оставлено для совместимости).
        ///     Динамические сборки пропускаются, ReflectionTypeLoadException больше не роняет инициализацию UI.
        /// </summary>
        public static HashSet<WidgetBinding> GetBindings(IEnumerable<Assembly> assemblies = null)
        {
            var targetAssemblies = assemblies?.ToArray() ?? AppDomain.CurrentDomain.GetAssemblies();
            var bindings = new HashSet<WidgetBinding>();

            foreach (var assembly in targetAssemblies)
            {
                if (assembly == null || assembly.IsDynamic) continue;

                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException exception)
                {
                    // Часть типов сборки может не грузиться — берём то, что загрузилось.
                    types = exception.Types.Where(t => t != null).ToArray();
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"[{nameof(WidgetBinding)}] Skipped assembly {assembly.FullName}: {exception.Message}");
                    continue;
                }

                foreach (var type in types)
                {
                    if (type.IsAbstract || !typeof(Widget).IsAssignableFrom(type)) continue;

                    var attribute = (DefaultWidgetBindingAttribute)Attribute.GetCustomAttribute(
                        type, typeof(DefaultWidgetBindingAttribute));

                    if (attribute == null) continue;

                    bindings.Add(new WidgetBinding(type, attribute.ViewModelType, attribute.LayoutId));
                }
            }

            return bindings;
        }
    }
}
