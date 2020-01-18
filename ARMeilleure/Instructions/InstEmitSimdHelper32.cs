﻿using ARMeilleure.Decoders;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.Translation;
using System;

using static ARMeilleure.Instructions.InstEmitHelper;
using static ARMeilleure.Instructions.InstEmitSimdHelper;
using static ARMeilleure.IntermediateRepresentation.OperandHelper;

namespace ARMeilleure.Instructions
{
    using Func1I = Func<Operand, Operand>;
    using Func2I = Func<Operand, Operand, Operand>;
    using Func3I = Func<Operand, Operand, Operand, Operand>;

    static class InstEmitSimdHelper32
    {
        public static (int, int) GetQuadwordAndSubindex(int index, RegisterSize size)
        {
            switch (size)
            {
                case RegisterSize.Simd128:
                    return (index >> 1, 0);
                case RegisterSize.Simd64:
                    return (index >> 1, index & 1);
                case RegisterSize.Simd32:
                    return (index >> 2, index & 3);
            }

            throw new NotImplementedException("Unrecognized Vector Register Size!");
        }

        public static Operand ExtractScalar(ArmEmitterContext context, OperandType type, int reg)
        {
            if (type == OperandType.FP64 || type == OperandType.I64)
            {
                // from dreg
                return context.VectorExtract(type, GetVecA32(reg >> 1), reg & 1);
            } 
            else
            {
                // from sreg
                return context.VectorExtract(type, GetVecA32(reg >> 2), reg & 3);
            }
        }

        public static void InsertScalar(ArmEmitterContext context, int reg, Operand value)
        {
            Operand vec, insert;
            if (value.Type == OperandType.FP64 || value.Type == OperandType.I64)
            {
                // from dreg
                vec = GetVecA32(reg >> 1);
                insert = context.VectorInsert(vec, value, reg & 1);
                
            }
            else
            {
                // from sreg
                vec = GetVecA32(reg >> 2);
                insert = context.VectorInsert(vec, value, reg & 3);
            }
            context.Copy(vec, insert);
        }

        public static void EmitVectorImmUnaryOp32(ArmEmitterContext context, Func1I emit)
        {
            IOpCode32SimdImm op = (IOpCode32SimdImm)context.CurrOp;

            Operand imm = Const(op.Immediate);

            int elems = op.Elems;
            (int index, int subIndex) = GetQuadwordAndSubindex(op.Vd, op.RegisterSize);

            Operand vec = GetVecA32(index);
            Operand res = vec;

            for (int item = 0; item < elems; item++)
            {
                res = EmitVectorInsert(context, res, emit(imm), item + subIndex * elems, op.Size);
            }

            context.Copy(vec, res);
        }

        public static void EmitScalarUnaryOpF32(ArmEmitterContext context, Func1I emit)
        {
            OpCode32SimdS op = (OpCode32SimdS)context.CurrOp;

            OperandType type = (op.Size & 1) != 0 ? OperandType.FP64 : OperandType.FP32;

            Operand m = ExtractScalar(context, type, op.Vm);

            InsertScalar(context, op.Vd, emit(m));
        }

        public static void EmitScalarBinaryOpF32(ArmEmitterContext context, Func2I emit)
        {
            OpCode32SimdRegS op = (OpCode32SimdRegS)context.CurrOp;

            OperandType type = (op.Size & 1) != 0 ? OperandType.FP64 : OperandType.FP32;

            Operand n = ExtractScalar(context, type, op.Vn);
            Operand m = ExtractScalar(context, type, op.Vm);

            InsertScalar(context, op.Vd, emit(n, m));
        }

        public static void EmitScalarBinaryOpI32(ArmEmitterContext context, Func2I emit)
        {
            OpCode32SimdRegS op = (OpCode32SimdRegS)context.CurrOp;

            OperandType type = (op.Size & 1) != 0 ? OperandType.I64 : OperandType.I32;

            if (op.Size < 2) throw new Exception("Not supported right now");

            Operand n = ExtractScalar(context, type, op.Vn);
            Operand m = ExtractScalar(context, type, op.Vm);

            InsertScalar(context, op.Vd, emit(n, m));
        }

        public static void EmitScalarTernaryOpF32(ArmEmitterContext context, Func3I emit)
        {
            OpCode32SimdRegS op = (OpCode32SimdRegS)context.CurrOp;

            OperandType type = (op.Size & 1) != 0 ? OperandType.FP64 : OperandType.FP32;

            Operand a = ExtractScalar(context, type, op.Vd);
            Operand n = ExtractScalar(context, type, op.Vn);
            Operand m = ExtractScalar(context, type, op.Vm);

            InsertScalar(context, op.Vd, emit(a, n, m));
        }

        public static void EmitVectorUnaryOpF32(ArmEmitterContext context, Func1I emit)
        {
            OpCode32Simd op = (OpCode32Simd)context.CurrOp;

            int sizeF = op.Size & 1;

            OperandType type = sizeF != 0 ? OperandType.FP64 : OperandType.FP32;

            int elems = op.GetBytesCount() >> sizeF + 2;

            Operand res = GetVecA32(op.Qd);

            for (int index = 0; index < elems; index++)
            {
                Operand me = context.VectorExtract(type, GetVecA32(op.Qm), op.Fm + index);

                res = context.VectorInsert(res, emit(me), op.Fd + index);
            }

            context.Copy(GetVecA32(op.Qd), res);
        }

        public static void EmitVectorBinaryOpF32(ArmEmitterContext context, Func2I emit)
        {
            OpCode32SimdReg op = (OpCode32SimdReg)context.CurrOp;

            int sizeF = op.Size & 1;

            OperandType type = sizeF != 0 ? OperandType.FP64 : OperandType.FP32;

            int elems = op.GetBytesCount() >> (sizeF + 2);

            Operand res = GetVecA32(op.Qd);

            for (int index = 0; index < elems; index++)
            {
                Operand ne = context.VectorExtract(type, GetVecA32(op.Qn), op.Fn + index);
                Operand me = context.VectorExtract(type, GetVecA32(op.Qm), op.Fm + index);

                res = context.VectorInsert(res, emit(ne, me), op.Fd + index);
            }

            context.Copy(GetVecA32(op.Qd), res);
        }

        public static void EmitVectorTernaryOpF32(ArmEmitterContext context, Func3I emit)
        {
            OpCode32SimdReg op = (OpCode32SimdReg)context.CurrOp;

            int sizeF = op.Size & 1;

            OperandType type = sizeF != 0 ? OperandType.FP64 : OperandType.FP32;

            int elems = op.GetBytesCount() >> sizeF + 2;

            Operand res = GetVecA32(op.Qd);

            for (int index = 0; index < elems; index++)
            {
                Operand de = context.VectorExtract(type, GetVecA32(op.Qd), op.Fd + index);
                Operand ne = context.VectorExtract(type, GetVecA32(op.Qn), op.Fn + index);
                Operand me = context.VectorExtract(type, GetVecA32(op.Qm), op.Fm + index);

                res = context.VectorInsert(res, emit(de, ne, me), op.Fd + index);
            }

            context.Copy(GetVecA32(op.Qd), res);
        }

        // INTEGER

        public static void EmitVectorUnaryOpI32(ArmEmitterContext context, Func1I emit, bool signed)
        {
            OpCode32Simd op = (OpCode32Simd)context.CurrOp;

            Operand res = GetVecA32(op.Qd);

            int elems = op.GetBytesCount() >> op.Size;

            for (int index = 0; index < elems; index++)
            {
                Operand me = EmitVectorExtract32(context, op.Qm, op.Im + index, op.Size, signed);

                res = EmitVectorInsert(context, res, emit(me), op.Id + index, op.Size);
            }

            context.Copy(GetVecA32(op.Qd), res);
        }

        public static void EmitVectorBinaryOpI32(ArmEmitterContext context, Func2I emit, bool signed)
        {
            OpCode32SimdReg op = (OpCode32SimdReg)context.CurrOp;

            Operand res = GetVecA32(op.Qd);

            int elems = op.GetBytesCount() >> op.Size;

            for (int index = 0; index < elems; index++)
            {
                Operand ne = EmitVectorExtract32(context, op.Qn, op.In + index, op.Size, signed);
                Operand me = EmitVectorExtract32(context, op.Qm, op.Im + index, op.Size, signed);

                res = EmitVectorInsert(context, res, emit(ne, me), op.Id + index, op.Size);
            }

            context.Copy(GetVecA32(op.Qd), res);
        }

        public static void EmitVectorTernaryOpI32(ArmEmitterContext context, Func3I emit, bool signed)
        {
            OpCode32SimdReg op = (OpCode32SimdReg)context.CurrOp;

            Operand res = GetVecA32(op.Qd);

            int elems = op.GetBytesCount() >> op.Size;

            for (int index = 0; index < elems; index++)
            {
                Operand de = EmitVectorExtract32(context, op.Qd, op.Id + index, op.Size, signed);
                Operand ne = EmitVectorExtract32(context, op.Qn, op.In + index, op.Size, signed);
                Operand me = EmitVectorExtract32(context, op.Qm, op.Im + index, op.Size, signed);

                res = EmitVectorInsert(context, res, emit(de, ne, me), index + op.Id, op.Size);
            }

            context.Copy(GetVecA32(op.Qd), res);
        }

        public static void EmitVectorUnaryOpSx32(ArmEmitterContext context, Func1I emit)
        {
            EmitVectorUnaryOpI32(context, emit, true);
        }

        public static void EmitVectorBinaryOpSx32(ArmEmitterContext context, Func2I emit)
        {
            EmitVectorBinaryOpI32(context, emit, true);
        }

        public static void EmitVectorTernaryOpSx32(ArmEmitterContext context, Func3I emit)
        {
            EmitVectorTernaryOpI32(context, emit, true);
        }

        public static void EmitVectorUnaryOpZx32(ArmEmitterContext context, Func1I emit)
        {
            EmitVectorUnaryOpI32(context, emit, false);
        }

        public static void EmitVectorBinaryOpZx32(ArmEmitterContext context, Func2I emit)
        {
            EmitVectorBinaryOpI32(context, emit, false);
        }

        public static void EmitVectorTernaryOpZx32(ArmEmitterContext context, Func3I emit)
        {
            EmitVectorTernaryOpI32(context, emit, false);
        }

        // VEC BY SCALAR

        public static void EmitVectorByScalarOpF32(ArmEmitterContext context, Func2I emit)
        {
            OpCode32SimdRegElem op = (OpCode32SimdRegElem)context.CurrOp;

            int sizeF = op.Size & 1;

            OperandType type = sizeF != 0 ? OperandType.FP64 : OperandType.FP32;
            if (op.Size < 2) throw new Exception("FP ops <32 bit unimplemented!");

            int elems = op.GetBytesCount() >> sizeF + 2;

            Operand m = ExtractScalar(context, type, op.Vm);

            Operand res = GetVecA32(op.Qd);

            for (int index = 0; index < elems; index++)
            {
                Operand ne = context.VectorExtract(type, GetVecA32(op.Qn), index + op.Fn);

                res = context.VectorInsert(res, emit(ne, m), index + op.Fd);
            }

            context.Copy(GetVecA32(op.Qd), res);
        }

        public static void EmitVectorByScalarOpI32(ArmEmitterContext context, Func2I emit, bool signed)
        {
            OpCode32SimdRegElem op = (OpCode32SimdRegElem)context.CurrOp;

            if (op.Size < 1) throw new Exception("Undefined");
            Operand m = EmitVectorExtract32(context, op.Vm >> (4 - op.Size), op.Vm & ((1 << (4 - op.Size)) - 1), op.Size, signed);

            Operand res = GetVecA32(op.Qd);

            int elems = op.GetBytesCount() >> op.Size;

            for (int index = 0; index < elems; index++)
            {
                Operand ne = EmitVectorExtract32(context, op.Qn, index + op.In, op.Size, signed);

                res = EmitVectorInsert(context, res, emit(ne, m), index + op.Id, op.Size);
            }

            context.Copy(GetVecA32(op.Qd), res);
        }

        public static void EmitVectorsByScalarOpF32(ArmEmitterContext context, Func3I emit)
        {
            OpCode32SimdRegElem op = (OpCode32SimdRegElem)context.CurrOp;

            int sizeF = op.Size & 1;

            OperandType type = sizeF != 0 ? OperandType.FP64 : OperandType.FP32;
            if (op.Size < 2) throw new Exception("FP ops <32 bit unimplemented!");

            int elems = op.GetBytesCount() >> sizeF + 2;

            Operand m = ExtractScalar(context, type, op.Vm);

            Operand res = GetVecA32(op.Qd);

            for (int index = 0; index < elems; index++)
            {
                Operand de = context.VectorExtract(type, GetVecA32(op.Qd), index + op.Fd);
                Operand ne = context.VectorExtract(type, GetVecA32(op.Qn), index + op.Fn);

                res = context.VectorInsert(res, emit(de, ne, m), index + op.Fd);
            }

            context.Copy(GetVecA32(op.Qd), res);
        }

        public static void EmitVectorsByScalarOpI32(ArmEmitterContext context, Func3I emit, bool signed)
        {
            OpCode32SimdRegElem op = (OpCode32SimdRegElem)context.CurrOp;

            if (op.Size < 1) throw new Exception("Undefined");
            Operand m = EmitVectorExtract32(context, op.Vm >> (4 - op.Size), op.Vm & ((1 << (4 - op.Size)) - 1), op.Size, signed);

            Operand res = GetVecA32(op.Qd);

            int elems = op.GetBytesCount() >> op.Size;

            for (int index = 0; index < elems; index++)
            {
                Operand de = EmitVectorExtract32(context, op.Qd, index + op.Id, op.Size, signed);
                Operand ne = EmitVectorExtract32(context, op.Qn, index + op.In, op.Size, signed);

                res = EmitVectorInsert(context, res, emit(de, ne, m), index + op.Id, op.Size);
            }

            context.Copy(GetVecA32(op.Qd), res);
        }

        // PAIRWISE

        public static void EmitVectorPairwiseOpF32(ArmEmitterContext context, Func2I emit)
        {
            OpCode32SimdReg op = (OpCode32SimdReg)context.CurrOp;

            if (op.Q)
            {
                throw new Exception("Q mode not supported for pairwise");
            }
            int sizeF = op.Size & 1;

            OperandType type = sizeF != 0 ? OperandType.FP64 : OperandType.FP32;

            int elems = op.GetBytesCount() >> (sizeF + 2);
            int pairs = elems >> 1;

            Operand res = GetVecA32(op.Qd);
            Operand mvec = GetVecA32(op.Qm);
            Operand nvec = GetVecA32(op.Qn);

            for (int index = 0; index < pairs; index++)
            {
                int pairIndex = index << 1;

                Operand n1 = context.VectorExtract(type, nvec, pairIndex + op.Fn);
                Operand n2 = context.VectorExtract(type, nvec, pairIndex + 1 + op.Fn);

                res = context.VectorInsert(res, emit(n1, n2), index + op.Fd);

                Operand m1 = context.VectorExtract(type, mvec, pairIndex + op.Fm);
                Operand m2 = context.VectorExtract(type, mvec, pairIndex + 1 + op.Fm);

                res = context.VectorInsert(res, emit(m1, m2), index + pairs + op.Fd);
            }

            context.Copy(GetVecA32(op.Qd), res);
        }

        public static void EmitVectorPairwiseOpI32(ArmEmitterContext context, Func2I emit, bool signed)
        {
            OpCode32SimdReg op = (OpCode32SimdReg)context.CurrOp;

            if (op.Q)
            {
                throw new Exception("Q mode not supported for pairwise");
            }

            int elems = op.GetBytesCount() >> op.Size;
            int pairs = elems >> 1;

            Operand res = GetVecA32(op.Qd);

            for (int index = 0; index < pairs; index++)
            {
                int pairIndex = index << 1;
                Operand n1 = EmitVectorExtract32(context, op.Qn, pairIndex + op.In, op.Size, signed);
                Operand n2 = EmitVectorExtract32(context, op.Qn, pairIndex + 1 + op.In, op.Size, signed);

                Operand m1 = EmitVectorExtract32(context, op.Qm, pairIndex + op.Im, op.Size, signed);
                Operand m2 = EmitVectorExtract32(context, op.Qm, pairIndex + 1 + op.Im, op.Size, signed);

                res = EmitVectorInsert(context, res, emit(n1, n2), index + op.Id, op.Size);
                res = EmitVectorInsert(context, res, emit(m1, m2), index + pairs + op.Id, op.Size);
            }

            context.Copy(GetVecA32(op.Qd), res);
        }

        // Generic Functions

        public static Operand EmitSoftFloatCallDefaultFpscr(
            ArmEmitterContext context,
            _F32_F32_Bool f32,
            _F64_F64_Bool f64,
            params Operand[] callArgs)
        {
            IOpCodeSimd op = (IOpCodeSimd)context.CurrOp;

            Delegate dlg = (op.Size & 1) == 0 ? (Delegate)f32 : (Delegate)f64;

            Array.Resize(ref callArgs, callArgs.Length + 1);
            callArgs[callArgs.Length - 1] = Const(1);

            return context.Call(dlg, callArgs);
        }

        public static Operand EmitSoftFloatCallDefaultFpscr(
            ArmEmitterContext context,
            _F32_F32_F32_Bool f32,
            _F64_F64_F64_Bool f64,
            params Operand[] callArgs)
        {
            IOpCodeSimd op = (IOpCodeSimd)context.CurrOp;

            Delegate dlg = (op.Size & 1) == 0 ? (Delegate)f32 : (Delegate)f64;

            Array.Resize(ref callArgs, callArgs.Length + 1);
            callArgs[callArgs.Length - 1] = Const(1);

            return context.Call(dlg, callArgs);
        }

        public static Operand EmitSoftFloatCallDefaultFpscr(
            ArmEmitterContext context,
            _F32_F32_F32_F32_Bool f32,
            _F64_F64_F64_F64_Bool f64,
            params Operand[] callArgs)
        {
            IOpCodeSimd op = (IOpCodeSimd)context.CurrOp;

            Delegate dlg = (op.Size & 1) == 0 ? (Delegate)f32 : (Delegate)f64;

            Array.Resize(ref callArgs, callArgs.Length + 1);
            callArgs[callArgs.Length - 1] = Const(1);

            return context.Call(dlg, callArgs);
        }

        public static Operand EmitVectorExtractSx32(ArmEmitterContext context, int reg, int index, int size)
        {
            return EmitVectorExtract32(context, reg, index, size, true);
        }

        public static Operand EmitVectorExtractZx32(ArmEmitterContext context, int reg, int index, int size)
        {
            return EmitVectorExtract32(context, reg, index, size, false);
        }

        public static Operand EmitVectorExtract32(ArmEmitterContext context, int reg, int index, int size, bool signed)
        {
            ThrowIfInvalid(index, size);

            Operand res = null;

            switch (size)
            {
                case 0:
                    res = context.VectorExtract8(GetVec(reg), index);
                    break;

                case 1:
                    res = context.VectorExtract16(GetVec(reg), index);
                    break;

                case 2:
                    res = context.VectorExtract(OperandType.I32, GetVec(reg), index);
                    break;

                case 3:
                    res = context.VectorExtract(OperandType.I64, GetVec(reg), index);
                    break;
            }

            if (signed)
            {
                switch (size)
                {
                    case 0: res = context.SignExtend8(OperandType.I32, res); break;
                    case 1: res = context.SignExtend16(OperandType.I32, res); break;
                }
            }
            else
            {
                switch (size)
                {
                    case 0: res = context.ZeroExtend8(OperandType.I32, res); break;
                    case 1: res = context.ZeroExtend16(OperandType.I32, res); break;
                }
            }

            return res;
        }

    }
}
