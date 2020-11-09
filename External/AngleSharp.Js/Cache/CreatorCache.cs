using AngleSharp.Attributes;
using Jint.Native.Object;
using Jint.Runtime.Descriptors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AngleSharp.Js.Cache
{
    internal static class CreatorCache
    {
        private static readonly Dictionary<Type, Action<EngineInstance, ObjectInstance>> _constructorActions = new Dictionary<Type, Action<EngineInstance, ObjectInstance>>();

        public static Action<EngineInstance, ObjectInstance> GetConstructorAction(this Type type)
        {
            if (!_constructorActions.TryGetValue(type, out var action))
            {
                var ti = type.GetTypeInfo();
                var names = ti.GetCustomAttributes<DomNameAttribute>();
                var name = names.FirstOrDefault();

                if (name != null && !ti.IsEnum)
                {
                    var info = ti.DeclaredConstructors.FirstOrDefault(m => m.GetCustomAttributes<DomConstructorAttribute>().Any());
                    action = (engine, obj) =>
                    {
                        var constructor = info != null ? new DomConstructorInstance(engine, info) : new DomConstructorInstance(engine, type);
                        obj.FastSetProperty(name.OfficialName, new PropertyDescriptor(constructor, false, true, false));
                    };
                }
                else
                {
                    return null;

                    action = (e, o) =>
                    {
                    };
                }

                _constructorActions.Add(type, action);
            }

            return action;
        }

        private static readonly Dictionary<Type, Action<EngineInstance, ObjectInstance>> _instanceActions = new Dictionary<Type, Action<EngineInstance, ObjectInstance>>();

        public static Action<EngineInstance, ObjectInstance> GetInstanceAction(this Type type)
        {
            if (!_instanceActions.TryGetValue(type, out var action))
            {
                var info = type.GetTypeInfo().DeclaredConstructors.FirstOrDefault(m => m.GetParameters().Length == 0);

                if (info != null)
                {
                    var attributes = type.GetTypeInfo().GetCustomAttributes<DomInstanceAttribute>();
                    action = (engine, obj) =>
                    {
                        foreach (var attribute in attributes)
                        {
                            var instance = info.Invoke(null);

                            if (instance != null)
                            {
                                var node = engine.GetDomNode(instance);
                                obj.FastSetProperty(attribute.Name, new PropertyDescriptor(node, false, true, false));
                            }
                        }
                    };
                }
                else
                {
                    action = (e, o) => { };
                }

                _instanceActions.Add(type, action);
            }

            return action;
        }
    }
}