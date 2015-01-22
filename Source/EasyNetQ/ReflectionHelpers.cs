﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace EasyNetQ
{
    public static class ReflectionHelpers
    {
        private static readonly ConcurrentDictionary<Type, Dictionary<Type, Attribute[]>> _attributes = new ConcurrentDictionary<Type, Dictionary<Type, Attribute[]>>();

        private static Dictionary<Type, Attribute[]> GetOrAddTypeAttributeDictionary(Type type)
        {
            return _attributes.GetOrAdd(type, t => t.GetCustomAttributes(true)
                                                    .Cast<Attribute>()
                                                    .GroupBy(attr => attr.GetType())
                                                    .ToDictionary(group => group.Key, group => group.ToArray()));
        }

        public static IEnumerable<TAttribute> GetAttributes<TAttribute>(this Type type) where TAttribute : Attribute
        {
            Attribute[] attributes;
            if (GetOrAddTypeAttributeDictionary(type).TryGetValue(typeof(TAttribute), out attributes))
            {
                return attributes.Cast<TAttribute>().ToArray();
            }
            return new TAttribute[0];
        }

        public static TAttribute GetAttribute<TAttribute>(this Type type) where TAttribute : Attribute
        {
            Attribute[] attributes;
            if (GetOrAddTypeAttributeDictionary(type).TryGetValue(typeof(TAttribute), out attributes) && attributes.Length > 0)
            {
                return (TAttribute)attributes[0];
            }
            return default(TAttribute);
        }

        /// <summary>
        /// A factory method that creates an instance of <paramref name="{T}"/> using a public parameterless constructor.
        /// If no such constructor is found on <paramref name="{T}"/>, a <see cref="MissingMethodException"/> will be thrown.
        /// </summary>
        public static T CreateInstance<T>()
        {
            return DefaultFactories<T>.Get();
        }

        private static class DefaultFactories<T>
        {
            private static Func<T> factory;

            public static T Get()
            {
                if (factory == null)
                {
                    var constructorInfo = typeof(T).GetConstructor(Type.EmptyTypes);
                    if (constructorInfo == null)
                    {
                        throw new MissingMethodException("The type that is specified for T does not have a public parameterless constructor.");
                    }
                    factory = Expression.Lambda<Func<T>>(Expression.New(constructorInfo)).Compile();
                }
                return factory();
            }
        }

        private static readonly ConcurrentDictionary<Type, Func<object>> _parameterlessConstructorMap =
            new ConcurrentDictionary<Type, Func<object>>();

        /// <summary>
        /// A factory method that creates an instance of the specified <see cref="Type"/> using a public parameterless constructor.
        /// If no such constructor is found, a <see cref="MissingMethodException"/> will be thrown.
        /// </summary>
        public static object CreateInstance(Type objectType)
        {
            var ctor = _parameterlessConstructorMap.GetOrAdd(objectType, t =>
            {
                var innerCtor = objectType.GetConstructor(Type.EmptyTypes);
                if (innerCtor == null)
                {
                    throw new MissingMethodException(String.Format("Type {0} doesn't have a public parameterless constructor.", objectType));
                }
                return () => innerCtor.Invoke(null);
            });
            return ctor();
        }

        private static readonly ConcurrentDictionary<Type, Dictionary<Type, Func<object, object>>> _singleParameterConstructorMap = 
            new ConcurrentDictionary<Type, Dictionary<Type, Func<object, object>>>();
        private static readonly Func<Type, Type, Func<object, object>> _singleParameterConstructorMapUpdate = ((objectType, argType) =>
        {
            var ctor = objectType.GetConstructor(new[] { argType });
            if (ctor == null)
            {
                throw new MissingMethodException(String.Format("Type {0} doesn't have a public constructor that take one parameter of type {1}."
                                                               , objectType, argType));
            }
            return obj => ctor.Invoke(new[] { obj });
        });

        /// <summary>
        /// A factory method that creates an instance of the specified <see cref="Type"/>
        /// using a public constructor that accepts one argument of the type of <paramref name="arg"/>.
        /// If no such constructor is found on type of <paramref name="objectType"/>, a <see cref="MissingMethodException"/> will be thrown.
        /// </summary>
        public static object CreateInstance(Type objectType, object arg)
        {
            var argType = arg.GetType();
            var constructors = _singleParameterConstructorMap.GetOrAdd(objectType, t => new Dictionary<Type, Func<object, object>>
            {
                { argType, _singleParameterConstructorMapUpdate(objectType, argType) }
            });

            Func<object, object> ctor;
            if (!constructors.TryGetValue(argType, out ctor))
            {
                ctor = _singleParameterConstructorMapUpdate(objectType, argType);
                constructors.Add(argType, ctor);
            }
            return ctor(arg);
        }

        private static readonly ConcurrentDictionary<Type, Dictionary<Type, Dictionary<Type, Func<object, object, object>>>> _dualParameterConstructorMap =
            new ConcurrentDictionary<Type, Dictionary<Type, Dictionary<Type, Func<object, object, object>>>>();
        private static readonly Func<Type, Type, Type, Func<object, object, object>> _dualParameterConstructorMapUpdate = ((objectType, firstArgType, secondArgType) =>
        {
            var ctor = objectType.GetConstructor(new[] { firstArgType, secondArgType });
            if (ctor == null)
            {
                throw new MissingMethodException(String.Format("Type {0} doesn't have a public constructor that take two parametesr of type {1} and {2}.",
                                                               objectType, firstArgType, secondArgType));
            }
            return (arg1, arg2) => ctor.Invoke(new[] { arg1, arg2 });
        });

        /// <summary>
        /// A factory method that creates an instance of the specified <see cref="Type"/>
        /// using a public constructor that accepts two arguments of the type of <paramref name="firstArg"/> and <paramref name="secondArg"/> in that order.
        /// If no such constructor is found on type of <paramref name="objectType"/>, a <see cref="MissingMethodException"/> will be thrown.
        /// </summary>
        public static object CreateInstance(Type objectType, object firstArg, object secondArg)
        {
            var firstArgType = firstArg.GetType();
            var secondArgType = secondArg.GetType();

            var constructors = _dualParameterConstructorMap.GetOrAdd(objectType, t => new Dictionary<Type, Dictionary<Type, Func<object, object, object>>>
            {
                {
                    firstArgType, new Dictionary<Type, Func<object, object, object>>
                    {
                        { secondArgType, _dualParameterConstructorMapUpdate(objectType, firstArgType, secondArgType) }
                    }
                }
            });

            Dictionary<Type, Func<object, object, object>> firstArgConstructorMap;
            if (!constructors.TryGetValue(firstArgType, out firstArgConstructorMap))
            {
                firstArgConstructorMap = new Dictionary<Type, Func<object, object, object>>
                {
                    { secondArgType, _dualParameterConstructorMapUpdate(objectType, firstArgType, secondArgType) }
                };
                constructors.Add(firstArgType, firstArgConstructorMap);
            }

            Func<object, object, object> ctor;
            if (!firstArgConstructorMap.TryGetValue(secondArgType, out ctor))
            {
                ctor = _dualParameterConstructorMapUpdate(objectType, firstArgType, secondArgType);
                firstArgConstructorMap.Add(secondArgType, ctor);
            }
            return ctor(firstArg, secondArg);
        }
    }
}