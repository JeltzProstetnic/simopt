using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace SimOpt.Basics.Utilities
{
    public static class DelegateFactory
    {
        private static readonly Type[] actionTypes = new[] {
                                                            typeof(Action),
                                                            typeof(Action<>),
                                                            typeof(Action<,>),
                                                            typeof(Action<,,>),
                                                            typeof(Action<,,,>),
                                                            typeof(Action<,,,,>),
                                                            typeof(Action<,,,,,>),
                                                            typeof(Action<,,,,,,>),
                                                            typeof(Action<,,,,,,,>),
                                                            typeof(Action<,,,,,,,,>),
                                                            typeof(Action<,,,,,,,,,>),
                                                            typeof(Action<,,,,,,,,,,>),
                                                            typeof(Action<,,,,,,,,,,,>),
                                                            typeof(Action<,,,,,,,,,,,,>),
                                                            typeof(Action<,,,,,,,,,,,,,>),
                                                            typeof(Action<,,,,,,,,,,,,,,>),
                                                            typeof(Action<,,,,,,,,,,,,,,,>),
                                                        };

        private static readonly Type[] functionTypes = new[] {
                                                            typeof(Func<>),
                                                            typeof(Func<,>),
                                                            typeof(Func<,,>),
                                                            typeof(Func<,,,>),
                                                            typeof(Func<,,,,>),
                                                            typeof(Func<,,,,,>),
                                                            typeof(Func<,,,,,,>),
                                                            typeof(Func<,,,,,,,>),
                                                            typeof(Func<,,,,,,,,>),
                                                            typeof(Func<,,,,,,,,,>),
                                                            typeof(Func<,,,,,,,,,,>),
                                                            typeof(Func<,,,,,,,,,,,>),
                                                            typeof(Func<,,,,,,,,,,,,>),
                                                            typeof(Func<,,,,,,,,,,,,,>),
                                                            typeof(Func<,,,,,,,,,,,,,,>),
                                                            typeof(Func<,,,,,,,,,,,,,,,>),
                                                            typeof(Func<,,,,,,,,,,,,,,,,>),
                                                        };

        public static Delegate CreateOpenDelegate(MethodInfo method)
        {
            var closedType = GetClosedDelegateType(method);

            return closedType != null ?
                     Delegate.CreateDelegate(closedType, method, true)
                     : null;
        }

        private static Type GetClosedDelegateType(MethodInfo method)
        {
            var openType = GetOpenDelegateType(method);

            if (openType != null)
            {
                var parameterTypes =
                    new[] { method.DeclaringType }.Union(method.GetParameters().Select(p => p.ParameterType));

                if (method.ReturnType != typeof(void))
                {
                    parameterTypes = parameterTypes.Union(new[] { method.ReturnType });
                }

                return openType.MakeGenericType(parameterTypes.ToArray());
            }

            return null;
        }

        private static Type GetOpenDelegateType(MethodInfo method)
        {
            var parameterCount = method.GetParameters().Length + 1;

            if (parameterCount < functionTypes.Length && parameterCount < actionTypes.Length)
            {

                return method.ReturnType != typeof(void)
                           ? functionTypes[parameterCount]
                           : actionTypes[parameterCount];
            }

            return null;
        }
    }
}