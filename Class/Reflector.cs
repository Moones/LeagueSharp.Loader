﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Reflector.cs" company="LeagueSharp.Loader">
//   Copyright (c) LeagueSharp.Loader. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace LeagueSharp.Loader.Class
{
    using System;
    using System.Reflection;

    /// <summary>
    ///     This class is from the Front-End for Dosbox and is used to present a 'vista' dialog box to select folders.
    ///     Being able to use a vista style dialog box to select folders is much better then using the shell folder browser.
    ///     http://code.google.com/p/fed/
    ///     Example:
    ///     var r = new Reflector("System.Windows.Forms");
    /// </summary>
    public class Reflector
    {
        private Assembly m_asmb;

        private string m_ns;

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="ns">The namespace containing types to be used</param>
        public Reflector(string ns)
            : this(ns, ns)
        {
        }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="an">A specific assembly name (used if the assembly name does not tie exactly with the namespace)</param>
        /// <param name="ns">The namespace containing types to be used</param>
        public Reflector(string an, string ns)
        {
            this.m_ns = ns;
            this.m_asmb = null;
            foreach (var aN in Assembly.GetExecutingAssembly().GetReferencedAssemblies())
            {
                if (aN.FullName.StartsWith(an))
                {
                    this.m_asmb = Assembly.Load(aN);
                    break;
                }
            }
        }

        /// <summary>
        ///     Calls method 'func' on object 'obj' passing parameters 'parameters'
        /// </summary>
        /// <param name="obj">The object on which to excute function 'func'</param>
        /// <param name="func">The function to execute</param>
        /// <param name="parameters">The parameters to pass to function 'func'</param>
        /// <returns>The result of the function invocation</returns>
        public object Call(object obj, string func, params object[] parameters)
        {
            return this.Call2(obj, func, parameters);
        }

        /// <summary>
        ///     Calls method 'func' on object 'obj' passing parameters 'parameters'
        /// </summary>
        /// <param name="obj">The object on which to excute function 'func'</param>
        /// <param name="func">The function to execute</param>
        /// <param name="parameters">The parameters to pass to function 'func'</param>
        /// <returns>The result of the function invocation</returns>
        public object Call2(object obj, string func, object[] parameters)
        {
            return this.CallAs2(obj.GetType(), obj, func, parameters);
        }

        /// <summary>
        ///     Calls method 'func' on object 'obj' which is of type 'type' passing parameters 'parameters'
        /// </summary>
        /// <param name="type">The type of 'obj'</param>
        /// <param name="obj">The object on which to excute function 'func'</param>
        /// <param name="func">The function to execute</param>
        /// <param name="parameters">The parameters to pass to function 'func'</param>
        /// <returns>The result of the function invocation</returns>
        public object CallAs(Type type, object obj, string func, params object[] parameters)
        {
            return this.CallAs2(type, obj, func, parameters);
        }

        /// <summary>
        ///     Calls method 'func' on object 'obj' which is of type 'type' passing parameters 'parameters'
        /// </summary>
        /// <param name="type">The type of 'obj'</param>
        /// <param name="obj">The object on which to excute function 'func'</param>
        /// <param name="func">The function to execute</param>
        /// <param name="parameters">The parameters to pass to function 'func'</param>
        /// <returns>The result of the function invocation</returns>
        public object CallAs2(Type type, object obj, string func, object[] parameters)
        {
            var methInfo = type.GetMethod(func, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return methInfo.Invoke(obj, parameters);
        }

        /// <summary>
        ///     Returns the value of property 'prop' of object 'obj'
        /// </summary>
        /// <param name="obj">The object containing 'prop'</param>
        /// <param name="prop">The property name</param>
        /// <returns>The property value</returns>
        public object Get(object obj, string prop)
        {
            return this.GetAs(obj.GetType(), obj, prop);
        }

        /// <summary>
        ///     Returns the value of property 'prop' of object 'obj' which has type 'type'
        /// </summary>
        /// <param name="type">The type of 'obj'</param>
        /// <param name="obj">The object containing 'prop'</param>
        /// <param name="prop">The property name</param>
        /// <returns>The property value</returns>
        public object GetAs(Type type, object obj, string prop)
        {
            var propInfo = type.GetProperty(prop, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return propInfo.GetValue(obj, null);
        }

        /// <summary>
        ///     Returns an enum value
        /// </summary>
        /// <param name="typeName">The name of enum type</param>
        /// <param name="name">The name of the value</param>
        /// <returns>The enum value</returns>
        public object GetEnum(string typeName, string name)
        {
            var type = this.GetType(typeName);
            var fieldInfo = type.GetField(name);
            return fieldInfo.GetValue(null);
        }

        /// <summary>
        ///     Return a Type instance for a type 'typeName'
        /// </summary>
        /// <param name="typeName">The name of the type</param>
        /// <returns>A type instance</returns>
        public Type GetType(string typeName)
        {
            Type type = null;
            var names = typeName.Split('.');

            if (names.Length > 0)
            {
                type = this.m_asmb.GetType(this.m_ns + "." + names[0]);
            }

            for (var i = 1; i < names.Length; ++i)
            {
                type = type.GetNestedType(names[i], BindingFlags.NonPublic);
            }

            return type;
        }

        /// <summary>
        ///     Create a new object of a named type passing along any params
        /// </summary>
        /// <param name="name">The name of the type to create</param>
        /// <param name="parameters"></param>
        /// <returns>An instantiated type</returns>
        public object New(string name, params object[] parameters)
        {
            var type = this.GetType(name);

            var ctorInfos = type.GetConstructors();
            foreach (var ci in ctorInfos)
            {
                try
                {
                    return ci.Invoke(parameters);
                }
                catch
                {
                }
            }

            return null;
        }
    }
}