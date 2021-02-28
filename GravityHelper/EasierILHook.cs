﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Celeste;
using Celeste.Mod;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;

namespace GravityHelper
{
    internal static class EasierILHook
    {
        public static void ReplaceStrings(this ILCursor cursor, Dictionary<string, string> toReplace)
        {
            int lastIndex = cursor.Index;
            cursor.Index = 0;

            while (cursor.TryGotoNext(MoveType.After, instr => matchStringInDict(instr, toReplace.Keys.ToList())))
            {
                string old = (string)cursor.Prev.Operand;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldstr, toReplace[old]);
            }

            cursor.Index = lastIndex;
        }

        public static void ReplaceStrings(this ILCursor cursor, Dictionary<string, Func<string>> toReplace)
        {
            int lastIndex = cursor.Index;
            cursor.Index = 0;

            while (cursor.TryGotoNext(MoveType.After, instr => matchStringInDict(instr, toReplace.Keys.ToList())))
            {
                string old = (string)cursor.Prev.Operand;
                cursor.Emit(OpCodes.Pop);
                cursor.EmitDelegate(toReplace[old]);
            }

            cursor.Index = lastIndex;
        }

        private static bool matchStringInDict(Instruction instr, List<string> keys)
        {
            foreach (string val in keys)
            {
                if (instr.MatchLdstr(val))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// The following method is written by max480. Thanks max!
        /// Utility method to patch "coroutine" kinds of methods with IL.
        /// Those methods' code reside in a compiler-generated method, and IL.Celeste.* do not allow manipulating them directly.
        /// </summary>
        /// <returns>The IL hook if the actual code was found, null otherwise</returns>
        public static ILHook HookCoroutine(string typeName, string methodName, ILContext.Manipulator manipulator)
        {
            // get the Celeste.exe module definition Everest loaded for us
            ModuleDefinition celeste = Everest.Relinker.SharedRelinkModuleMap["Celeste.Mod.mm"];

            // get the type
            TypeDefinition type = celeste.GetType(typeName);
            if (type == null) return null;

            // the "coroutine" method is actually a nested type tracking the coroutine's state
            // (to make it restart from where it stopped when MoveNext() is called).
            // what we see in ILSpy and what we want to hook is actually the MoveNext() method in this nested type.
            foreach (TypeDefinition nest in type.NestedTypes)
            {
                if (nest.Name.StartsWith("<" + methodName + ">d__", StringComparison.Ordinal))
                {
                    // check that this nested type contains a MoveNext() method
                    MethodDefinition method = nest.FindMethod("System.Boolean MoveNext()");
                    if (method == null) return null;

                    // we found it! let's convert it into basic System.Reflection stuff and hook it.
                    //Logger.Log("ExtendedVariantMode/ExtendedVariantsModule", $"Building IL hook for method {method.FullName} in order to mod {typeName}.{methodName}()");
                    Type reflectionType = typeof(Player).Assembly.GetType(typeName);
                    Type reflectionNestedType = reflectionType.GetNestedType(nest.Name, BindingFlags.NonPublic);
                    MethodBase moveNextMethod = reflectionNestedType.GetMethod("MoveNext", BindingFlags.NonPublic | BindingFlags.Instance);
                    return new ILHook(moveNextMethod, manipulator);
                }
            }

            return null;
        }
    }
}
