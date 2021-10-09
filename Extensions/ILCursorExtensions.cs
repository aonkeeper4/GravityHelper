// "Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text."

using System;
using Microsoft.Xna.Framework;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.Utils;

namespace Celeste.Mod.GravityHelper.Extensions
{
    public static class ILCursorExtensions
    {
        public static bool AdditionPredicate(Instruction instr) => instr.MatchCall<Vector2>("op_Addition");
        public static bool SubtractionPredicate(Instruction instr) => instr.MatchCall<Vector2>("op_Subtraction");
        public static bool UnitYPredicate(Instruction instr) => instr.MatchCall<Vector2>("get_UnitY");
        public static bool MinPredicate(Instruction instr) => instr.MatchCall("System.Math", "Min");
        public static bool MaxPredicate(Instruction instr) => instr.MatchCall("System.Math", "Max");
        public static bool SignPredicate(Instruction instr) => instr.MatchCall("System.Math", "Sign");
        public static bool BottomPredicate(Instruction instr) => instr.MatchCallOrCallvirt<Entity>("get_Bottom");
        public static bool TopPredicate(Instruction instr) => instr.MatchCallOrCallvirt<Entity>("get_Top");
        public static bool BottomCenterPredicate(Instruction instr) => instr.MatchCallOrCallvirt<Entity>("get_BottomCenter");

        public static readonly Func<Vector2, Vector2, Vector2> AdditionDelegate = (lhs, rhs) =>
            lhs + (GravityHelperModule.ShouldInvert ? new Vector2(rhs.X, -rhs.Y) : rhs);

        public static readonly Func<Vector2, Vector2, Vector2> SubtractionDelegate = (lhs, rhs) =>
            lhs - (GravityHelperModule.ShouldInvert ? new Vector2(rhs.X, -rhs.Y) : rhs);

        public static readonly Func<float, float, float> MinDelegate = (a, b) =>
            GravityHelperModule.ShouldInvert ? Math.Max(a, b) : Math.Min(a, b);

        public static readonly Func<float, float, float> MaxDelegate = (a, b) =>
            GravityHelperModule.ShouldInvert ? Math.Min(a, b) : Math.Max(a, b);

        public static readonly Func<float, float> SignDelegate = a =>
            GravityHelperModule.ShouldInvert ? -Math.Sign(a) : Math.Sign(a);

        public static readonly Func<Entity, float> BottomDelegate = e =>
            GravityHelperModule.ShouldInvert ? e.Top : e.Bottom;

        public static readonly Func<Entity, float> TopDelegate = e =>
            GravityHelperModule.ShouldInvert ? e.Bottom : e.Top;

        public static readonly Func<Entity, Vector2> BottomCenterDelegate = e =>
            GravityHelperModule.ShouldInvert ? e.TopCenter : e.BottomCenter;

        public static void ReplaceWithDelegate<T>(this ILCursor cursor, Func<Instruction, bool> predicate, T del, int count = 1)
            where T : Delegate
        {
            while (count != 0)
            {
                if (count == -1 && !cursor.TryGotoNext(predicate)) break;
                if (count >= 0) cursor.GotoNext(predicate);
                if (count > 0) count--;

                cursor.Remove();
                cursor.EmitDelegate(del);
            }
        }

        public static void ReplaceAdditionWithDelegate(this ILCursor cursor, int count = 1) => cursor.ReplaceWithDelegate(AdditionPredicate, AdditionDelegate, count);
        public static void ReplaceSubtractionWithDelegate(this ILCursor cursor, int count = 1) => cursor.ReplaceWithDelegate(SubtractionPredicate, SubtractionDelegate, count);
        public static void ReplaceMinWithDelegate(this ILCursor cursor, int count = 1) => cursor.ReplaceWithDelegate(MinPredicate, MinDelegate, count);
        public static void ReplaceMaxWithDelegate(this ILCursor cursor, int count = 1) => cursor.ReplaceWithDelegate(MaxPredicate, MaxDelegate, count);
        public static void ReplaceSignWithDelegate(this ILCursor cursor, int count = 1) => cursor.ReplaceWithDelegate(SignPredicate, SignDelegate, count);
        public static void ReplaceBottomWithDelegate(this ILCursor cursor, int count = 1) => cursor.ReplaceWithDelegate(BottomPredicate, BottomDelegate, count);
        public static void ReplaceTopWithDelegate(this ILCursor cursor, int count = 1) => cursor.ReplaceWithDelegate(TopPredicate, TopDelegate, count);
        public static void ReplaceBottomCenterWithDelegate(this ILCursor cursor, int count = 1) => cursor.ReplaceWithDelegate(BottomCenterPredicate, BottomCenterDelegate, count);

        public static void GotoNextAddition(this ILCursor cursor, MoveType moveType = MoveType.Before) => cursor.GotoNext(moveType, AdditionPredicate);
        public static void GotoNextSubtraction(this ILCursor cursor, MoveType moveType = MoveType.Before) => cursor.GotoNext(moveType, SubtractionPredicate);
        public static void GotoNextMin(this ILCursor cursor, MoveType moveType = MoveType.Before) => cursor.GotoNext(moveType, MinPredicate);
        public static void GotoNextMax(this ILCursor cursor, MoveType moveType = MoveType.Before) => cursor.GotoNext(moveType, MaxPredicate);
        public static void GotoNextSign(this ILCursor cursor, MoveType moveType = MoveType.Before) => cursor.GotoNext(moveType, SignPredicate);
        public static void GotoNextUnitY(this ILCursor cursor, MoveType moveType = MoveType.Before) => cursor.GotoNext(moveType, UnitYPredicate);
        public static void GotoNextBottom(this ILCursor cursor, MoveType moveType = MoveType.Before) => cursor.GotoNext(moveType, BottomPredicate);
        public static void GotoNextBottomCenter(this ILCursor cursor, MoveType moveType = MoveType.Before) => cursor.GotoNext(moveType, BottomCenterPredicate);

        public static void EmitActorInvertVectorDelegate(this ILCursor cursor, OpCode loadActorOpCode)
        {
            cursor.Emit(loadActorOpCode);
            cursor.EmitDelegate<Func<Vector2, Actor, Vector2>>((v, a) =>
                GravityHelperModule.ShouldInvertActor(a) ? new Vector2(v.X, -v.Y) : v);
        }

        public static void EmitInvertVectorDelegate(this ILCursor cursor) =>
            cursor.EmitDelegate<Func<Vector2, Vector2>>(v => GravityHelperModule.ShouldInvert ? new Vector2(v.X, -v.Y) : v);

        public static void EmitActorInvertFloatDelegate(this ILCursor cursor, OpCode loadActorOpCode)
        {
            cursor.Emit(loadActorOpCode);
            cursor.EmitDelegate<Func<float, Actor, float>>((f, a) =>
                GravityHelperModule.ShouldInvertActor(a) ? -f : f);
        }

        public static void EmitInvertFloatDelegate(this ILCursor cursor) =>
            cursor.EmitDelegate<Func<float, float>>(f => GravityHelperModule.ShouldInvert ? -f : f);

        public static void EmitActorInvertIntDelegate(this ILCursor cursor, OpCode loadActorOpCode)
        {
            cursor.Emit(loadActorOpCode);
            cursor.EmitDelegate<Func<int, Actor, int>>((i, a) =>
                GravityHelperModule.ShouldInvertActor(a) ? -i : i);
        }

        public static void EmitInvertIntDelegate(this ILCursor cursor) =>
            cursor.EmitDelegate<Func<int, int>>(i => GravityHelperModule.ShouldInvert ? -i : i);

        public static void EmitLoadShouldInvert(this ILCursor cursor) =>
            cursor.Emit(OpCodes.Call, typeof(GravityHelperModule).GetProperty(nameof(GravityHelperModule.ShouldInvert)).GetGetMethod());

        public static void DumpIL(this ILCursor cursor, int instructions = 1, int offset = 0)
        {
            var thisCursor = cursor.Clone();
            thisCursor.Index = Calc.Clamp(thisCursor.Index + offset, 0, thisCursor.Instrs.Count - 1);

            for (int it = 0; it < instructions; it++)
            {
                var instr = thisCursor.Next;

                var str = $"{instr.Offset:x4}{(it == -offset ? "*" : " ")}: {instr.OpCode.ToString().PadRight(10)}";

                if (instr.MatchLdarg(out var i)) str += $"   {i}";
                else if (instr.MatchLdloc(out i)) str += $"   {i}";
                else if (instr.MatchStloc(out i)) str += $"   {i}";
                else if (instr.MatchCall(out var mr)) str += $"   {mr.FullName}";
                else if (instr.MatchCallvirt(out mr)) str += $"   {mr.FullName}";
                else if (instr.MatchLdfld(out var fr)) str += $"   {fr.FullName}";
                else if (instr.MatchStfld(out fr)) str += $"   {fr.FullName}";
                else if (instr.MatchLdcR4(out var f)) str += $"   {f}";
                else if (instr.MatchLdcI4(out i)) str += $"   {i}";
                else if (instr.MatchBeq(out var label)) str += $"   {label.Target.Offset:x4}";
                else if (instr.MatchBneUn(out label)) str += $"   {label.Target.Offset:x4}";
                else if (instr.MatchBge(out label)) str += $"   {label.Target.Offset:x4}";
                else if (instr.MatchBgeUn(out label)) str += $"   {label.Target.Offset:x4}";
                else if (instr.MatchBgt(out label)) str += $"   {label.Target.Offset:x4}";
                else if (instr.MatchBgtUn(out label)) str += $"   {label.Target.Offset:x4}";
                else if (instr.MatchBle(out label)) str += $"   {label.Target.Offset:x4}";
                else if (instr.MatchBleUn(out label)) str += $"   {label.Target.Offset:x4}";
                else if (instr.MatchBlt(out label)) str += $"   {label.Target.Offset:x4}";
                else if (instr.MatchBltUn(out label)) str += $"   {label.Target.Offset:x4}";

                Logger.Log(nameof(GravityHelperModule), str);

                thisCursor.Index++;
                if (thisCursor.Next == null) break;
            }
        }

        public static bool MatchCallGeneric<T>(this Instruction instr, string name, out GenericInstanceMethod method)
        {
            method = instr.Operand as GenericInstanceMethod;
            if (method == null || instr.OpCode != OpCodes.Call) return false;
            return method.DeclaringType.Is(typeof(T)) && method.Name == name;
        }

        public static bool MatchCallGeneric<TType, TFirst>(this Instruction instr, string name, out GenericInstanceMethod method)
        {
            method = instr.Operand as GenericInstanceMethod;
            if (method == null || instr.OpCode != OpCodes.Call) return false;
            return method.DeclaringType.Is(typeof(TType)) &&
                   method.Name == name &&
                   method.GenericArguments.Count == 1 &&
                   method.GenericArguments[0].ResolveReflection() == typeof(TFirst);
        }

        public static bool MatchCallvirtGeneric<T>(this Instruction instr, string name, out GenericInstanceMethod method)
        {
            method = instr.Operand as GenericInstanceMethod;
            if (method == null || instr.OpCode != OpCodes.Callvirt) return false;
            return method.DeclaringType.Is(typeof(T)) && method.Name == name;
        }

        public static bool MatchCallvirtGeneric<TType, TFirst>(this Instruction instr, string name, out GenericInstanceMethod method)
        {
            method = instr.Operand as GenericInstanceMethod;
            if (method == null || instr.OpCode != OpCodes.Callvirt) return false;
            return method.DeclaringType.Is(typeof(TType)) &&
                   method.Name == name &&
                   method.GenericArguments.Count == 1 &&
                   method.GenericArguments[0].ResolveReflection() == typeof(TFirst);
        }
    }
}
