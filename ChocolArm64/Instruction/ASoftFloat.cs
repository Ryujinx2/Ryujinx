using ChocolArm64.State;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ChocolArm64.Instruction
{
    static class ASoftFloat
    {
        static ASoftFloat()
        {
            RecipEstimateTable   = BuildRecipEstimateTable();
            InvSqrtEstimateTable = BuildInvSqrtEstimateTable();
        }

        private static readonly byte[] RecipEstimateTable;
        private static readonly byte[] InvSqrtEstimateTable;

        private static byte[] BuildRecipEstimateTable()
        {
            byte[] Table = new byte[256];
            for (ulong index = 0; index < 256; index++)
            {
                ulong a = index | 0x100;

                a = (a << 1) + 1;
                ulong b = 0x80000 / a;
                b = (b + 1) >> 1;

                Table[index] = (byte)(b & 0xFF);
            }
            return Table;
        }

        private static byte[] BuildInvSqrtEstimateTable()
        {
            byte[] Table = new byte[512];
            for (ulong index = 128; index < 512; index++)
            {
                ulong a = index;
                if (a < 256)
                {
                    a = (a << 1) + 1;
                }
                else
                {
                    a = (a | 1) << 1;
                }

                ulong b = 256;
                while (a * (b + 1) * (b + 1) < (1ul << 28))
                {
                    b++;
                }
                b = (b + 1) >> 1;

                Table[index] = (byte)(b & 0xFF);
            }
            return Table;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float RecipEstimate(float x)
        {
            return (float)RecipEstimate((double)x);
        }

        public static double RecipEstimate(double x)
        {
            ulong x_bits = (ulong)BitConverter.DoubleToInt64Bits(x);
            ulong x_sign = x_bits & 0x8000000000000000;
            ulong x_exp = (x_bits >> 52) & 0x7FF;
            ulong scaled = x_bits & ((1ul << 52) - 1);

            if (x_exp >= 2045)
            {
                if (x_exp == 0x7ff && scaled != 0)
                {
                    // NaN
                    return BitConverter.Int64BitsToDouble((long)(x_bits | 0x0008000000000000));
                }

                // Infinity, or Out of range -> Zero
                return BitConverter.Int64BitsToDouble((long)x_sign);
            }

            if (x_exp == 0)
            {
                if (scaled == 0)
                {
                    // Zero -> Infinity
                    return BitConverter.Int64BitsToDouble((long)(x_sign | 0x7FF0000000000000));
                }

                // Denormal
                if ((scaled & (1ul << 51)) == 0)
                {
                    x_exp = ~0ul;
                    scaled <<= 2;
                }
                else
                {
                    scaled <<= 1;
                }
            }

            scaled >>= 44;
            scaled &= 0xFF;

            ulong result_exp = (2045 - x_exp) & 0x7FF;
            ulong estimate = (ulong)RecipEstimateTable[scaled];
            ulong fraction = estimate << 44;

            if (result_exp == 0)
            {
                fraction >>= 1;
                fraction |= 1ul << 51;
            }
            else if (result_exp == 0x7FF)
            {
                result_exp = 0;
                fraction >>= 2;
                fraction |= 1ul << 50;
            }

            ulong result = x_sign | (result_exp << 52) | fraction;
            return BitConverter.Int64BitsToDouble((long)result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float InvSqrtEstimate(float x)
        {
            return (float)InvSqrtEstimate((double)x);
        }

        public static double InvSqrtEstimate(double x)
        {
            ulong x_bits = (ulong)BitConverter.DoubleToInt64Bits(x);
            ulong x_sign = x_bits & 0x8000000000000000;
            long x_exp = (long)((x_bits >> 52) & 0x7FF);
            ulong scaled = x_bits & ((1ul << 52) - 1);

            if (x_exp == 0x7FF && scaled != 0)
            {
                // NaN
                return BitConverter.Int64BitsToDouble((long)(x_bits | 0x0008000000000000));
            }

            if (x_exp == 0)
            {
                if (scaled == 0)
                {
                    // Zero -> Infinity
                    return BitConverter.Int64BitsToDouble((long)(x_sign | 0x7FF0000000000000));
                }

                // Denormal
                while ((scaled & (1 << 51)) == 0)
                {
                    scaled <<= 1;
                    x_exp--;
                }
                scaled <<= 1;
            }

            if (x_sign != 0)
            {
                // Negative -> NaN
                return BitConverter.Int64BitsToDouble((long)0x7FF8000000000000);
            }

            if (x_exp == 0x7ff && scaled == 0)
            {
                // Infinity -> Zero
                return BitConverter.Int64BitsToDouble((long)x_sign);
            }

            if (((ulong)x_exp & 1) == 1)
            {
                scaled >>= 45;
                scaled &= 0xFF;
                scaled |= 0x80;
            }
            else
            {
                scaled >>= 44;
                scaled &= 0xFF;
                scaled |= 0x100;
            }

            ulong result_exp = ((ulong)(3068 - x_exp) / 2) & 0x7FF;
            ulong estimate = (ulong)InvSqrtEstimateTable[scaled];
            ulong fraction = estimate << 44;

            ulong result = x_sign | (result_exp << 52) | fraction;
            return BitConverter.Int64BitsToDouble((long)result);
        }

        public static float ConvertHalfToSingle(ushort x)
        {
            uint x_sign = (uint)(x >> 15) & 0x0001;
            uint x_exp = (uint)(x >> 10) & 0x001F;
            uint x_mantissa = (uint)x & 0x03FF;

            if (x_exp == 0 && x_mantissa == 0)
            {
                // Zero
                return BitConverter.Int32BitsToSingle((int)(x_sign << 31));
            }

            if (x_exp == 0x1F)
            {
                // NaN or Infinity
                return BitConverter.Int32BitsToSingle((int)((x_sign << 31) | 0x7F800000 | (x_mantissa << 13)));
            }

            int exponent = (int)x_exp - 15;

            if (x_exp == 0)
            {
                // Denormal
                x_mantissa <<= 1;
                while ((x_mantissa & 0x0400) == 0)
                {
                    x_mantissa <<= 1;
                    exponent--;
                }
                x_mantissa &= 0x03FF;
            }

            uint new_exp = (uint)((exponent + 127) & 0xFF) << 23;
            return BitConverter.Int32BitsToSingle((int)((x_sign << 31) | new_exp | (x_mantissa << 13)));
        }
    }

    static class ASoftFloat_32
    {
        public static float FPAdd(float Value1, float Value2, AThreadState State)
        {
            Debug.WriteLineIf(State.Fpcr != 0, $"ASoftFloat_32.FPAdd: State.Fpcr = 0x{State.Fpcr:X8}");

            Value1 = Value1.FPUnpack(out FPType Type1, out bool Sign1, out uint Op1);
            Value2 = Value2.FPUnpack(out FPType Type2, out bool Sign2, out uint Op2);

            float Result = FPProcessNaNs(Type1, Type2, Op1, Op2, State, out bool Done);

            if (!Done)
            {
                bool Inf1 = Type1 == FPType.Infinity; bool Zero1 = Type1 == FPType.Zero;
                bool Inf2 = Type2 == FPType.Infinity; bool Zero2 = Type2 == FPType.Zero;

                if (Inf1 && Inf2 && Sign1 == !Sign2)
                {
                    Result = FPDefaultNaN();

                    FPProcessException(FPExc.InvalidOp, State);
                }
                else if ((Inf1 && !Sign1) || (Inf2 && !Sign2))
                {
                    Result = FPInfinity(false);
                }
                else if ((Inf1 && Sign1) || (Inf2 && Sign2))
                {
                    Result = FPInfinity(true);
                }
                else if (Zero1 && Zero2 && Sign1 == Sign2)
                {
                    Result = FPZero(Sign1);
                }
                else
                {
                    Result = Value1 + Value2;
                }
            }

            return Result;
        }

        public static float FPDiv(float Value1, float Value2, AThreadState State)
        {
            Debug.WriteLineIf(State.Fpcr != 0, $"ASoftFloat_32.FPDiv: State.Fpcr = 0x{State.Fpcr:X8}");

            Value1 = Value1.FPUnpack(out FPType Type1, out bool Sign1, out uint Op1);
            Value2 = Value2.FPUnpack(out FPType Type2, out bool Sign2, out uint Op2);

            float Result = FPProcessNaNs(Type1, Type2, Op1, Op2, State, out bool Done);

            if (!Done)
            {
                bool Inf1 = Type1 == FPType.Infinity; bool Zero1 = Type1 == FPType.Zero;
                bool Inf2 = Type2 == FPType.Infinity; bool Zero2 = Type2 == FPType.Zero;

                if ((Inf1 && Inf2) || (Zero1 && Zero2))
                {
                    Result = FPDefaultNaN();

                    FPProcessException(FPExc.InvalidOp, State);
                }
                else if (Inf1 || Zero2)
                {
                    Result = FPInfinity(Sign1 ^ Sign2);

                    if (!Inf1) FPProcessException(FPExc.DivideByZero, State);
                }
                else if (Zero1 || Inf2)
                {
                    Result = FPZero(Sign1 ^ Sign2);
                }
                else
                {
                    Result = Value1 / Value2;
                }
            }

            return Result;
        }

        public static float FPMax(float Value1, float Value2, AThreadState State)
        {
            Debug.WriteLineIf(State.Fpcr != 0, $"ASoftFloat_32.FPMax: State.Fpcr = 0x{State.Fpcr:X8}");

            Value1 = Value1.FPUnpack(out FPType Type1, out bool Sign1, out uint Op1);
            Value2 = Value2.FPUnpack(out FPType Type2, out bool Sign2, out uint Op2);

            float Result = FPProcessNaNs(Type1, Type2, Op1, Op2, State, out bool Done);

            if (!Done)
            {
                if (Value1 > Value2)
                {
                    if (Type1 == FPType.Infinity)
                    {
                        Result = FPInfinity(Sign1);
                    }
                    else if (Type1 == FPType.Zero)
                    {
                        Result = FPZero(Sign1 && Sign2);
                    }
                    else
                    {
                        Result = Value1;
                    }
                }
                else
                {
                    if (Type2 == FPType.Infinity)
                    {
                        Result = FPInfinity(Sign2);
                    }
                    else if (Type2 == FPType.Zero)
                    {
                        Result = FPZero(Sign1 && Sign2);
                    }
                    else
                    {
                        Result = Value2;
                    }
                }
            }

            return Result;
        }

        public static float FPMaxNum(float Value1, float Value2, AThreadState State)
        {
            Debug.WriteIf(State.Fpcr != 0, "ASoftFloat_32.FPMaxNum: ");

            Value1.FPUnpack(out FPType Type1, out bool Sign1, out uint Op1);
            Value2.FPUnpack(out FPType Type2, out bool Sign2, out uint Op2);

            if (Type1 == FPType.QNaN && Type2 != FPType.QNaN)
            {
                Value1 = FPInfinity(true);
            }
            else if (Type1 != FPType.QNaN && Type2 == FPType.QNaN)
            {
                Value2 = FPInfinity(true);
            }

            return FPMax(Value1, Value2, State);
        }

        public static float FPMin(float Value1, float Value2, AThreadState State)
        {
            Debug.WriteLineIf(State.Fpcr != 0, $"ASoftFloat_32.FPMin: State.Fpcr = 0x{State.Fpcr:X8}");

            Value1 = Value1.FPUnpack(out FPType Type1, out bool Sign1, out uint Op1);
            Value2 = Value2.FPUnpack(out FPType Type2, out bool Sign2, out uint Op2);

            float Result = FPProcessNaNs(Type1, Type2, Op1, Op2, State, out bool Done);

            if (!Done)
            {
                if (Value1 < Value2)
                {
                    if (Type1 == FPType.Infinity)
                    {
                        Result = FPInfinity(Sign1);
                    }
                    else if (Type1 == FPType.Zero)
                    {
                        Result = FPZero(Sign1 || Sign2);
                    }
                    else
                    {
                        Result = Value1;
                    }
                }
                else
                {
                    if (Type2 == FPType.Infinity)
                    {
                        Result = FPInfinity(Sign2);
                    }
                    else if (Type2 == FPType.Zero)
                    {
                        Result = FPZero(Sign1 || Sign2);
                    }
                    else
                    {
                        Result = Value2;
                    }
                }
            }

            return Result;
        }

        public static float FPMinNum(float Value1, float Value2, AThreadState State)
        {
            Debug.WriteIf(State.Fpcr != 0, "ASoftFloat_32.FPMinNum: ");

            Value1.FPUnpack(out FPType Type1, out bool Sign1, out uint Op1);
            Value2.FPUnpack(out FPType Type2, out bool Sign2, out uint Op2);

            if (Type1 == FPType.QNaN && Type2 != FPType.QNaN)
            {
                Value1 = FPInfinity(false);
            }
            else if (Type1 != FPType.QNaN && Type2 == FPType.QNaN)
            {
                Value2 = FPInfinity(false);
            }

            return FPMin(Value1, Value2, State);
        }

        public static float FPMul(float Value1, float Value2, AThreadState State)
        {
            Debug.WriteLineIf(State.Fpcr != 0, $"ASoftFloat_32.FPMul: State.Fpcr = 0x{State.Fpcr:X8}");

            Value1 = Value1.FPUnpack(out FPType Type1, out bool Sign1, out uint Op1);
            Value2 = Value2.FPUnpack(out FPType Type2, out bool Sign2, out uint Op2);

            float Result = FPProcessNaNs(Type1, Type2, Op1, Op2, State, out bool Done);

            if (!Done)
            {
                bool Inf1 = Type1 == FPType.Infinity; bool Zero1 = Type1 == FPType.Zero;
                bool Inf2 = Type2 == FPType.Infinity; bool Zero2 = Type2 == FPType.Zero;

                if ((Inf1 && Zero2) || (Zero1 && Inf2))
                {
                    Result = FPDefaultNaN();

                    FPProcessException(FPExc.InvalidOp, State);
                }
                else if (Inf1 || Inf2)
                {
                    Result = FPInfinity(Sign1 ^ Sign2);
                }
                else if (Zero1 || Zero2)
                {
                    Result = FPZero(Sign1 ^ Sign2);
                }
                else
                {
                    Result = Value1 * Value2;
                }
            }

            return Result;
        }

        public static float FPMulAdd(float ValueA, float Value1, float Value2, AThreadState State)
        {
            Debug.WriteLineIf(State.Fpcr != 0, $"ASoftFloat_32.FPMulAdd: State.Fpcr = 0x{State.Fpcr:X8}");

            ValueA = ValueA.FPUnpack(out FPType TypeA, out bool SignA, out uint Addend);
            Value1 = Value1.FPUnpack(out FPType Type1, out bool Sign1, out uint Op1);
            Value2 = Value2.FPUnpack(out FPType Type2, out bool Sign2, out uint Op2);

            bool Inf1 = Type1 == FPType.Infinity; bool Zero1 = Type1 == FPType.Zero;
            bool Inf2 = Type2 == FPType.Infinity; bool Zero2 = Type2 == FPType.Zero;

            float Result = FPProcessNaNs3(TypeA, Type1, Type2, Addend, Op1, Op2, State, out bool Done);

            if (TypeA == FPType.QNaN && ((Inf1 && Zero2) || (Zero1 && Inf2)))
            {
                Result = FPDefaultNaN();

                FPProcessException(FPExc.InvalidOp, State);
            }

            if (!Done)
            {
                bool InfA = TypeA == FPType.Infinity; bool ZeroA = TypeA == FPType.Zero;

                bool SignP = Sign1 ^  Sign2;
                bool InfP  = Inf1  || Inf2;
                bool ZeroP = Zero1 || Zero2;

                if ((Inf1 && Zero2) || (Zero1 && Inf2) || (InfA && InfP && SignA != SignP))
                {
                    Result = FPDefaultNaN();

                    FPProcessException(FPExc.InvalidOp, State);
                }
                else if ((InfA && !SignA) || (InfP && !SignP))
                {
                    Result = FPInfinity(false);
                }
                else if ((InfA && SignA) || (InfP && SignP))
                {
                    Result = FPInfinity(true);
                }
                else if (ZeroA && ZeroP && SignA == SignP)
                {
                    Result = FPZero(SignA);
                }
                else
                {
                    // TODO: When available, use: T MathF.FusedMultiplyAdd(T, T, T);
                    // https://github.com/dotnet/corefx/issues/31903

                    Result = ValueA + (Value1 * Value2);
                }
            }

            return Result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float FPMulSub(float ValueA, float Value1, float Value2, AThreadState State)
        {
            Debug.WriteIf(State.Fpcr != 0, "ASoftFloat_32.FPMulSub: ");

            Value1 = Value1.FPNeg();

            return FPMulAdd(ValueA, Value1, Value2, State);
        }

        public static float FPMulX(float Value1, float Value2, AThreadState State)
        {
            Debug.WriteLineIf(State.Fpcr != 0, $"ASoftFloat_32.FPMulX: State.Fpcr = 0x{State.Fpcr:X8}");

            Value1 = Value1.FPUnpack(out FPType Type1, out bool Sign1, out uint Op1);
            Value2 = Value2.FPUnpack(out FPType Type2, out bool Sign2, out uint Op2);

            float Result = FPProcessNaNs(Type1, Type2, Op1, Op2, State, out bool Done);

            if (!Done)
            {
                bool Inf1 = Type1 == FPType.Infinity; bool Zero1 = Type1 == FPType.Zero;
                bool Inf2 = Type2 == FPType.Infinity; bool Zero2 = Type2 == FPType.Zero;

                if ((Inf1 && Zero2) || (Zero1 && Inf2))
                {
                    Result = FPTwo(Sign1 ^ Sign2);
                }
                else if (Inf1 || Inf2)
                {
                    Result = FPInfinity(Sign1 ^ Sign2);
                }
                else if (Zero1 || Zero2)
                {
                    Result = FPZero(Sign1 ^ Sign2);
                }
                else
                {
                    Result = Value1 * Value2;
                }
            }

            return Result;
        }

        public static float FPRecipStepFused(float Value1, float Value2, AThreadState State)
        {
            Debug.WriteLineIf(State.Fpcr != 0, $"ASoftFloat_32.FPRecipStepFused: State.Fpcr = 0x{State.Fpcr:X8}");

            Value1 = Value1.FPNeg();

            Value1 = Value1.FPUnpack(out FPType Type1, out bool Sign1, out uint Op1);
            Value2 = Value2.FPUnpack(out FPType Type2, out bool Sign2, out uint Op2);

            float Result = FPProcessNaNs(Type1, Type2, Op1, Op2, State, out bool Done);

            if (!Done)
            {
                bool Inf1 = Type1 == FPType.Infinity; bool Zero1 = Type1 == FPType.Zero;
                bool Inf2 = Type2 == FPType.Infinity; bool Zero2 = Type2 == FPType.Zero;

                if ((Inf1 && Zero2) || (Zero1 && Inf2))
                {
                    Result = FPTwo(false);
                }
                else if (Inf1 || Inf2)
                {
                    Result = FPInfinity(Sign1 ^ Sign2);
                }
                else
                {
                    // TODO: When available, use: T MathF.FusedMultiplyAdd(T, T, T);
                    // https://github.com/dotnet/corefx/issues/31903

                    Result = 2f + (Value1 * Value2);
                }
            }

            return Result;
        }

        public static float FPRSqrtStepFused(float Value1, float Value2, AThreadState State)
        {
            Debug.WriteLineIf(State.Fpcr != 0, $"ASoftFloat_32.FPRSqrtStepFused: State.Fpcr = 0x{State.Fpcr:X8}");

            Value1 = Value1.FPNeg();

            Value1 = Value1.FPUnpack(out FPType Type1, out bool Sign1, out uint Op1);
            Value2 = Value2.FPUnpack(out FPType Type2, out bool Sign2, out uint Op2);

            float Result = FPProcessNaNs(Type1, Type2, Op1, Op2, State, out bool Done);

            if (!Done)
            {
                bool Inf1 = Type1 == FPType.Infinity; bool Zero1 = Type1 == FPType.Zero;
                bool Inf2 = Type2 == FPType.Infinity; bool Zero2 = Type2 == FPType.Zero;

                if ((Inf1 && Zero2) || (Zero1 && Inf2))
                {
                    Result = FPOnePointFive(false);
                }
                else if (Inf1 || Inf2)
                {
                    Result = FPInfinity(Sign1 ^ Sign2);
                }
                else
                {
                    // TODO: When available, use: T MathF.FusedMultiplyAdd(T, T, T);
                    // https://github.com/dotnet/corefx/issues/31903

                    Result = (3f + (Value1 * Value2)) / 2f;
                }
            }

            return Result;
        }

        public static float FPSqrt(float Value, AThreadState State)
        {
            Debug.WriteLineIf(State.Fpcr != 0, $"ASoftFloat_32.FPSqrt: State.Fpcr = 0x{State.Fpcr:X8}");

            Value = Value.FPUnpack(out FPType Type, out bool Sign, out uint Op);

            float Result;

            if (Type == FPType.SNaN || Type == FPType.QNaN)
            {
                Result = FPProcessNaN(Type, Op, State);
            }
            else if (Type == FPType.Zero)
            {
                Result = FPZero(Sign);
            }
            else if (Type == FPType.Infinity && !Sign)
            {
                Result = FPInfinity(Sign);
            }
            else if (Sign)
            {
                Result = FPDefaultNaN();

                FPProcessException(FPExc.InvalidOp, State);
            }
            else
            {
                Result = MathF.Sqrt(Value);
            }

            return Result;
        }

        public static float FPSub(float Value1, float Value2, AThreadState State)
        {
            Debug.WriteLineIf(State.Fpcr != 0, $"ASoftFloat_32.FPSub: State.Fpcr = 0x{State.Fpcr:X8}");

            Value1 = Value1.FPUnpack(out FPType Type1, out bool Sign1, out uint Op1);
            Value2 = Value2.FPUnpack(out FPType Type2, out bool Sign2, out uint Op2);

            float Result = FPProcessNaNs(Type1, Type2, Op1, Op2, State, out bool Done);

            if (!Done)
            {
                bool Inf1 = Type1 == FPType.Infinity; bool Zero1 = Type1 == FPType.Zero;
                bool Inf2 = Type2 == FPType.Infinity; bool Zero2 = Type2 == FPType.Zero;

                if (Inf1 && Inf2 && Sign1 == Sign2)
                {
                    Result = FPDefaultNaN();

                    FPProcessException(FPExc.InvalidOp, State);
                }
                else if ((Inf1 && !Sign1) || (Inf2 && Sign2))
                {
                    Result = FPInfinity(false);
                }
                else if ((Inf1 && Sign1) || (Inf2 && !Sign2))
                {
                    Result = FPInfinity(true);
                }
                else if (Zero1 && Zero2 && Sign1 == !Sign2)
                {
                    Result = FPZero(Sign1);
                }
                else
                {
                    Result = Value1 - Value2;
                }
            }

            return Result;
        }

        private enum FPType
        {
            Nonzero,
            Zero,
            Infinity,
            QNaN,
            SNaN
        }

        private enum FPExc
        {
            InvalidOp,
            DivideByZero,
            Overflow,
            Underflow,
            Inexact,
            InputDenorm = 7
        }

        private enum FPRounding
        {
            TIEEVEN,
            POSINF,
            NEGINF,
            ZERO
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float FPDefaultNaN()
        {
            return -float.NaN;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float FPInfinity(bool Sign)
        {
            return Sign ? float.NegativeInfinity : float.PositiveInfinity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float FPZero(bool Sign)
        {
            return Sign ? -0f : +0f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float FPTwo(bool Sign)
        {
            return Sign ? -2f : +2f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float FPOnePointFive(bool Sign)
        {
            return Sign ? -1.5f : +1.5f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float FPNeg(this float Value)
        {
            return -Value;
        }

        private static float FPUnpack(this float Value, out FPType Type, out bool Sign, out uint ValueBits)
        {
            ValueBits = (uint)BitConverter.SingleToInt32Bits(Value);

            Sign = (~ValueBits & 0x80000000u) == 0u;

            if ((ValueBits & 0x7F800000u) == 0u)
            {
                if ((ValueBits & 0x007FFFFFu) == 0u)
                {
                    Type = FPType.Zero;
                }
                else
                {
                    Type = FPType.Nonzero;
                }
            }
            else if ((~ValueBits & 0x7F800000u) == 0u)
            {
                if ((ValueBits & 0x007FFFFFu) == 0u)
                {
                    Type = FPType.Infinity;
                }
                else
                {
                    Type = (~ValueBits & 0x00400000u) == 0u
                        ? FPType.QNaN
                        : FPType.SNaN;

                    return FPZero(Sign);
                }
            }
            else
            {
                Type = FPType.Nonzero;
            }

            return Value;
        }

        private static float FPProcessNaNs(
            FPType Type1,
            FPType Type2,
            uint Op1,
            uint Op2,
            AThreadState State,
            out bool Done)
        {
            Done = true;

            if (Type1 == FPType.SNaN)
            {
                return FPProcessNaN(Type1, Op1, State);
            }
            else if (Type2 == FPType.SNaN)
            {
                return FPProcessNaN(Type2, Op2, State);
            }
            else if (Type1 == FPType.QNaN)
            {
                return FPProcessNaN(Type1, Op1, State);
            }
            else if (Type2 == FPType.QNaN)
            {
                return FPProcessNaN(Type2, Op2, State);
            }

            Done = false;

            return FPZero(false);
        }

        private static float FPProcessNaNs3(
            FPType Type1,
            FPType Type2,
            FPType Type3,
            uint Op1,
            uint Op2,
            uint Op3,
            AThreadState State,
            out bool Done)
        {
            Done = true;

            if (Type1 == FPType.SNaN)
            {
                return FPProcessNaN(Type1, Op1, State);
            }
            else if (Type2 == FPType.SNaN)
            {
                return FPProcessNaN(Type2, Op2, State);
            }
            else if (Type3 == FPType.SNaN)
            {
                return FPProcessNaN(Type3, Op3, State);
            }
            else if (Type1 == FPType.QNaN)
            {
                return FPProcessNaN(Type1, Op1, State);
            }
            else if (Type2 == FPType.QNaN)
            {
                return FPProcessNaN(Type2, Op2, State);
            }
            else if (Type3 == FPType.QNaN)
            {
                return FPProcessNaN(Type3, Op3, State);
            }

            Done = false;

            return FPZero(false);
        }

        private static float FPProcessNaN(FPType Type, uint Op, AThreadState State)
        {
            const int DNBit = 25; // Default NaN mode control bit.

            if (Type == FPType.SNaN)
            {
                Op |= 1u << 22;

                FPProcessException(FPExc.InvalidOp, State);
            }

            if ((State.Fpcr & (1 << DNBit)) != 0)
            {
                return FPDefaultNaN();
            }

            return BitConverter.Int32BitsToSingle((int)Op);
        }

        private static void FPProcessException(FPExc Exc, AThreadState State)
        {
            int Enable = (int)Exc + 8;

            if ((State.Fpcr & (1 << Enable)) != 0)
            {
                throw new NotImplementedException("floating-point trap handling");
            }
            else
            {
                State.Fpsr |= 1 << (int)Exc;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static FPRounding FPRoundingMode(AThreadState State)
        {
            const int RModeBits = 22; // Rounding Mode control field.

            return (FPRounding)((State.Fpcr >> RModeBits) & 0b11);
        }
    }

    static class ASoftFloat_64
    {
        public static double FPAdd(double Value1, double Value2, AThreadState State)
        {
            Debug.WriteLineIf(State.Fpcr != 0, $"ASoftFloat_64.FPAdd: State.Fpcr = 0x{State.Fpcr:X8}");

            Value1 = Value1.FPUnpack(out FPType Type1, out bool Sign1, out ulong Op1);
            Value2 = Value2.FPUnpack(out FPType Type2, out bool Sign2, out ulong Op2);

            double Result = FPProcessNaNs(Type1, Type2, Op1, Op2, State, out bool Done);

            if (!Done)
            {
                bool Inf1 = Type1 == FPType.Infinity; bool Zero1 = Type1 == FPType.Zero;
                bool Inf2 = Type2 == FPType.Infinity; bool Zero2 = Type2 == FPType.Zero;

                if (Inf1 && Inf2 && Sign1 == !Sign2)
                {
                    Result = FPDefaultNaN();

                    FPProcessException(FPExc.InvalidOp, State);
                }
                else if ((Inf1 && !Sign1) || (Inf2 && !Sign2))
                {
                    Result = FPInfinity(false);
                }
                else if ((Inf1 && Sign1) || (Inf2 && Sign2))
                {
                    Result = FPInfinity(true);
                }
                else if (Zero1 && Zero2 && Sign1 == Sign2)
                {
                    Result = FPZero(Sign1);
                }
                else
                {
                    Result = Value1 + Value2;
                }
            }

            return Result;
        }

        public static double FPDiv(double Value1, double Value2, AThreadState State)
        {
            Debug.WriteLineIf(State.Fpcr != 0, $"ASoftFloat_64.FPDiv: State.Fpcr = 0x{State.Fpcr:X8}");

            Value1 = Value1.FPUnpack(out FPType Type1, out bool Sign1, out ulong Op1);
            Value2 = Value2.FPUnpack(out FPType Type2, out bool Sign2, out ulong Op2);

            double Result = FPProcessNaNs(Type1, Type2, Op1, Op2, State, out bool Done);

            if (!Done)
            {
                bool Inf1 = Type1 == FPType.Infinity; bool Zero1 = Type1 == FPType.Zero;
                bool Inf2 = Type2 == FPType.Infinity; bool Zero2 = Type2 == FPType.Zero;

                if ((Inf1 && Inf2) || (Zero1 && Zero2))
                {
                    Result = FPDefaultNaN();

                    FPProcessException(FPExc.InvalidOp, State);
                }
                else if (Inf1 || Zero2)
                {
                    Result = FPInfinity(Sign1 ^ Sign2);

                    if (!Inf1) FPProcessException(FPExc.DivideByZero, State);
                }
                else if (Zero1 || Inf2)
                {
                    Result = FPZero(Sign1 ^ Sign2);
                }
                else
                {
                    Result = Value1 / Value2;
                }
            }

            return Result;
        }

        public static double FPMax(double Value1, double Value2, AThreadState State)
        {
            Debug.WriteLineIf(State.Fpcr != 0, $"ASoftFloat_64.FPMax: State.Fpcr = 0x{State.Fpcr:X8}");

            Value1 = Value1.FPUnpack(out FPType Type1, out bool Sign1, out ulong Op1);
            Value2 = Value2.FPUnpack(out FPType Type2, out bool Sign2, out ulong Op2);

            double Result = FPProcessNaNs(Type1, Type2, Op1, Op2, State, out bool Done);

            if (!Done)
            {
                if (Value1 > Value2)
                {
                    if (Type1 == FPType.Infinity)
                    {
                        Result = FPInfinity(Sign1);
                    }
                    else if (Type1 == FPType.Zero)
                    {
                        Result = FPZero(Sign1 && Sign2);
                    }
                    else
                    {
                        Result = Value1;
                    }
                }
                else
                {
                    if (Type2 == FPType.Infinity)
                    {
                        Result = FPInfinity(Sign2);
                    }
                    else if (Type2 == FPType.Zero)
                    {
                        Result = FPZero(Sign1 && Sign2);
                    }
                    else
                    {
                        Result = Value2;
                    }
                }
            }

            return Result;
        }

        public static double FPMaxNum(double Value1, double Value2, AThreadState State)
        {
            Debug.WriteIf(State.Fpcr != 0, "ASoftFloat_64.FPMaxNum: ");

            Value1.FPUnpack(out FPType Type1, out bool Sign1, out ulong Op1);
            Value2.FPUnpack(out FPType Type2, out bool Sign2, out ulong Op2);

            if (Type1 == FPType.QNaN && Type2 != FPType.QNaN)
            {
                Value1 = FPInfinity(true);
            }
            else if (Type1 != FPType.QNaN && Type2 == FPType.QNaN)
            {
                Value2 = FPInfinity(true);
            }

            return FPMax(Value1, Value2, State);
        }

        public static double FPMin(double Value1, double Value2, AThreadState State)
        {
            Debug.WriteLineIf(State.Fpcr != 0, $"ASoftFloat_64.FPMin: State.Fpcr = 0x{State.Fpcr:X8}");

            Value1 = Value1.FPUnpack(out FPType Type1, out bool Sign1, out ulong Op1);
            Value2 = Value2.FPUnpack(out FPType Type2, out bool Sign2, out ulong Op2);

            double Result = FPProcessNaNs(Type1, Type2, Op1, Op2, State, out bool Done);

            if (!Done)
            {
                if (Value1 < Value2)
                {
                    if (Type1 == FPType.Infinity)
                    {
                        Result = FPInfinity(Sign1);
                    }
                    else if (Type1 == FPType.Zero)
                    {
                        Result = FPZero(Sign1 || Sign2);
                    }
                    else
                    {
                        Result = Value1;
                    }
                }
                else
                {
                    if (Type2 == FPType.Infinity)
                    {
                        Result = FPInfinity(Sign2);
                    }
                    else if (Type2 == FPType.Zero)
                    {
                        Result = FPZero(Sign1 || Sign2);
                    }
                    else
                    {
                        Result = Value2;
                    }
                }
            }

            return Result;
        }

        public static double FPMinNum(double Value1, double Value2, AThreadState State)
        {
            Debug.WriteIf(State.Fpcr != 0, "ASoftFloat_64.FPMinNum: ");

            Value1.FPUnpack(out FPType Type1, out bool Sign1, out ulong Op1);
            Value2.FPUnpack(out FPType Type2, out bool Sign2, out ulong Op2);

            if (Type1 == FPType.QNaN && Type2 != FPType.QNaN)
            {
                Value1 = FPInfinity(false);
            }
            else if (Type1 != FPType.QNaN && Type2 == FPType.QNaN)
            {
                Value2 = FPInfinity(false);
            }

            return FPMin(Value1, Value2, State);
        }

        public static double FPMul(double Value1, double Value2, AThreadState State)
        {
            Debug.WriteLineIf(State.Fpcr != 0, $"ASoftFloat_64.FPMul: State.Fpcr = 0x{State.Fpcr:X8}");

            Value1 = Value1.FPUnpack(out FPType Type1, out bool Sign1, out ulong Op1);
            Value2 = Value2.FPUnpack(out FPType Type2, out bool Sign2, out ulong Op2);

            double Result = FPProcessNaNs(Type1, Type2, Op1, Op2, State, out bool Done);

            if (!Done)
            {
                bool Inf1 = Type1 == FPType.Infinity; bool Zero1 = Type1 == FPType.Zero;
                bool Inf2 = Type2 == FPType.Infinity; bool Zero2 = Type2 == FPType.Zero;

                if ((Inf1 && Zero2) || (Zero1 && Inf2))
                {
                    Result = FPDefaultNaN();

                    FPProcessException(FPExc.InvalidOp, State);
                }
                else if (Inf1 || Inf2)
                {
                    Result = FPInfinity(Sign1 ^ Sign2);
                }
                else if (Zero1 || Zero2)
                {
                    Result = FPZero(Sign1 ^ Sign2);
                }
                else
                {
                    Result = Value1 * Value2;
                }
            }

            return Result;
        }

        public static double FPMulAdd(double ValueA, double Value1, double Value2, AThreadState State)
        {
            Debug.WriteLineIf(State.Fpcr != 0, $"ASoftFloat_64.FPMulAdd: State.Fpcr = 0x{State.Fpcr:X8}");

            ValueA = ValueA.FPUnpack(out FPType TypeA, out bool SignA, out ulong Addend);
            Value1 = Value1.FPUnpack(out FPType Type1, out bool Sign1, out ulong Op1);
            Value2 = Value2.FPUnpack(out FPType Type2, out bool Sign2, out ulong Op2);

            bool Inf1 = Type1 == FPType.Infinity; bool Zero1 = Type1 == FPType.Zero;
            bool Inf2 = Type2 == FPType.Infinity; bool Zero2 = Type2 == FPType.Zero;

            double Result = FPProcessNaNs3(TypeA, Type1, Type2, Addend, Op1, Op2, State, out bool Done);

            if (TypeA == FPType.QNaN && ((Inf1 && Zero2) || (Zero1 && Inf2)))
            {
                Result = FPDefaultNaN();

                FPProcessException(FPExc.InvalidOp, State);
            }

            if (!Done)
            {
                bool InfA = TypeA == FPType.Infinity; bool ZeroA = TypeA == FPType.Zero;

                bool SignP = Sign1 ^  Sign2;
                bool InfP  = Inf1  || Inf2;
                bool ZeroP = Zero1 || Zero2;

                if ((Inf1 && Zero2) || (Zero1 && Inf2) || (InfA && InfP && SignA != SignP))
                {
                    Result = FPDefaultNaN();

                    FPProcessException(FPExc.InvalidOp, State);
                }
                else if ((InfA && !SignA) || (InfP && !SignP))
                {
                    Result = FPInfinity(false);
                }
                else if ((InfA && SignA) || (InfP && SignP))
                {
                    Result = FPInfinity(true);
                }
                else if (ZeroA && ZeroP && SignA == SignP)
                {
                    Result = FPZero(SignA);
                }
                else
                {
                    // TODO: When available, use: T Math.FusedMultiplyAdd(T, T, T);
                    // https://github.com/dotnet/corefx/issues/31903

                    Result = ValueA + (Value1 * Value2);
                }
            }

            return Result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double FPMulSub(double ValueA, double Value1, double Value2, AThreadState State)
        {
            Debug.WriteIf(State.Fpcr != 0, "ASoftFloat_64.FPMulSub: ");

            Value1 = Value1.FPNeg();

            return FPMulAdd(ValueA, Value1, Value2, State);
        }

        public static double FPMulX(double Value1, double Value2, AThreadState State)
        {
            Debug.WriteLineIf(State.Fpcr != 0, $"ASoftFloat_64.FPMulX: State.Fpcr = 0x{State.Fpcr:X8}");

            Value1 = Value1.FPUnpack(out FPType Type1, out bool Sign1, out ulong Op1);
            Value2 = Value2.FPUnpack(out FPType Type2, out bool Sign2, out ulong Op2);

            double Result = FPProcessNaNs(Type1, Type2, Op1, Op2, State, out bool Done);

            if (!Done)
            {
                bool Inf1 = Type1 == FPType.Infinity; bool Zero1 = Type1 == FPType.Zero;
                bool Inf2 = Type2 == FPType.Infinity; bool Zero2 = Type2 == FPType.Zero;

                if ((Inf1 && Zero2) || (Zero1 && Inf2))
                {
                    Result = FPTwo(Sign1 ^ Sign2);
                }
                else if (Inf1 || Inf2)
                {
                    Result = FPInfinity(Sign1 ^ Sign2);
                }
                else if (Zero1 || Zero2)
                {
                    Result = FPZero(Sign1 ^ Sign2);
                }
                else
                {
                    Result = Value1 * Value2;
                }
            }

            return Result;
        }

        public static double FPRecipStepFused(double Value1, double Value2, AThreadState State)
        {
            Debug.WriteLineIf(State.Fpcr != 0, $"ASoftFloat_64.FPRecipStepFused: State.Fpcr = 0x{State.Fpcr:X8}");

            Value1 = Value1.FPNeg();

            Value1 = Value1.FPUnpack(out FPType Type1, out bool Sign1, out ulong Op1);
            Value2 = Value2.FPUnpack(out FPType Type2, out bool Sign2, out ulong Op2);

            double Result = FPProcessNaNs(Type1, Type2, Op1, Op2, State, out bool Done);

            if (!Done)
            {
                bool Inf1 = Type1 == FPType.Infinity; bool Zero1 = Type1 == FPType.Zero;
                bool Inf2 = Type2 == FPType.Infinity; bool Zero2 = Type2 == FPType.Zero;

                if ((Inf1 && Zero2) || (Zero1 && Inf2))
                {
                    Result = FPTwo(false);
                }
                else if (Inf1 || Inf2)
                {
                    Result = FPInfinity(Sign1 ^ Sign2);
                }
                else
                {
                    // TODO: When available, use: T Math.FusedMultiplyAdd(T, T, T);
                    // https://github.com/dotnet/corefx/issues/31903

                    Result = 2d + (Value1 * Value2);
                }
            }

            return Result;
        }

        public static double FPRSqrtStepFused(double Value1, double Value2, AThreadState State)
        {
            Debug.WriteLineIf(State.Fpcr != 0, $"ASoftFloat_64.FPRSqrtStepFused: State.Fpcr = 0x{State.Fpcr:X8}");

            Value1 = Value1.FPNeg();

            Value1 = Value1.FPUnpack(out FPType Type1, out bool Sign1, out ulong Op1);
            Value2 = Value2.FPUnpack(out FPType Type2, out bool Sign2, out ulong Op2);

            double Result = FPProcessNaNs(Type1, Type2, Op1, Op2, State, out bool Done);

            if (!Done)
            {
                bool Inf1 = Type1 == FPType.Infinity; bool Zero1 = Type1 == FPType.Zero;
                bool Inf2 = Type2 == FPType.Infinity; bool Zero2 = Type2 == FPType.Zero;

                if ((Inf1 && Zero2) || (Zero1 && Inf2))
                {
                    Result = FPOnePointFive(false);
                }
                else if (Inf1 || Inf2)
                {
                    Result = FPInfinity(Sign1 ^ Sign2);
                }
                else
                {
                    // TODO: When available, use: T Math.FusedMultiplyAdd(T, T, T);
                    // https://github.com/dotnet/corefx/issues/31903

                    Result = (3d + (Value1 * Value2)) / 2d;
                }
            }

            return Result;
        }

        public static double FPSqrt(double Value, AThreadState State)
        {
            Debug.WriteLineIf(State.Fpcr != 0, $"ASoftFloat_64.FPSqrt: State.Fpcr = 0x{State.Fpcr:X8}");

            Value = Value.FPUnpack(out FPType Type, out bool Sign, out ulong Op);

            double Result;

            if (Type == FPType.SNaN || Type == FPType.QNaN)
            {
                Result = FPProcessNaN(Type, Op, State);
            }
            else if (Type == FPType.Zero)
            {
                Result = FPZero(Sign);
            }
            else if (Type == FPType.Infinity && !Sign)
            {
                Result = FPInfinity(Sign);
            }
            else if (Sign)
            {
                Result = FPDefaultNaN();

                FPProcessException(FPExc.InvalidOp, State);
            }
            else
            {
                Result = Math.Sqrt(Value);
            }

            return Result;
        }

        public static double FPSub(double Value1, double Value2, AThreadState State)
        {
            Debug.WriteLineIf(State.Fpcr != 0, $"ASoftFloat_64.FPSub: State.Fpcr = 0x{State.Fpcr:X8}");

            Value1 = Value1.FPUnpack(out FPType Type1, out bool Sign1, out ulong Op1);
            Value2 = Value2.FPUnpack(out FPType Type2, out bool Sign2, out ulong Op2);

            double Result = FPProcessNaNs(Type1, Type2, Op1, Op2, State, out bool Done);

            if (!Done)
            {
                bool Inf1 = Type1 == FPType.Infinity; bool Zero1 = Type1 == FPType.Zero;
                bool Inf2 = Type2 == FPType.Infinity; bool Zero2 = Type2 == FPType.Zero;

                if (Inf1 && Inf2 && Sign1 == Sign2)
                {
                    Result = FPDefaultNaN();

                    FPProcessException(FPExc.InvalidOp, State);
                }
                else if ((Inf1 && !Sign1) || (Inf2 && Sign2))
                {
                    Result = FPInfinity(false);
                }
                else if ((Inf1 && Sign1) || (Inf2 && !Sign2))
                {
                    Result = FPInfinity(true);
                }
                else if (Zero1 && Zero2 && Sign1 == !Sign2)
                {
                    Result = FPZero(Sign1);
                }
                else
                {
                    Result = Value1 - Value2;
                }
            }

            return Result;
        }

        private enum FPType
        {
            Nonzero,
            Zero,
            Infinity,
            QNaN,
            SNaN
        }

        private enum FPExc
        {
            InvalidOp,
            DivideByZero,
            Overflow,
            Underflow,
            Inexact,
            InputDenorm = 7
        }

        private enum FPRounding
        {
            TIEEVEN,
            POSINF,
            NEGINF,
            ZERO
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double FPDefaultNaN()
        {
            return -double.NaN;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double FPInfinity(bool Sign)
        {
            return Sign ? double.NegativeInfinity : double.PositiveInfinity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double FPZero(bool Sign)
        {
            return Sign ? -0d : +0d;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double FPTwo(bool Sign)
        {
            return Sign ? -2d : +2d;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double FPOnePointFive(bool Sign)
        {
            return Sign ? -1.5d : +1.5d;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double FPNeg(this double Value)
        {
            return -Value;
        }

        private static double FPUnpack(this double Value, out FPType Type, out bool Sign, out ulong ValueBits)
        {
            ValueBits = (ulong)BitConverter.DoubleToInt64Bits(Value);

            Sign = (~ValueBits & 0x8000000000000000ul) == 0ul;

            if ((ValueBits & 0x7FF0000000000000ul) == 0ul)
            {
                if ((ValueBits & 0x000FFFFFFFFFFFFFul) == 0ul)
                {
                    Type = FPType.Zero;
                }
                else
                {
                    Type = FPType.Nonzero;
                }
            }
            else if ((~ValueBits & 0x7FF0000000000000ul) == 0ul)
            {
                if ((ValueBits & 0x000FFFFFFFFFFFFFul) == 0ul)
                {
                    Type = FPType.Infinity;
                }
                else
                {
                    Type = (~ValueBits & 0x0008000000000000ul) == 0ul
                        ? FPType.QNaN
                        : FPType.SNaN;

                    return FPZero(Sign);
                }
            }
            else
            {
                Type = FPType.Nonzero;
            }

            return Value;
        }

        private static double FPProcessNaNs(
            FPType Type1,
            FPType Type2,
            ulong Op1,
            ulong Op2,
            AThreadState State,
            out bool Done)
        {
            Done = true;

            if (Type1 == FPType.SNaN)
            {
                return FPProcessNaN(Type1, Op1, State);
            }
            else if (Type2 == FPType.SNaN)
            {
                return FPProcessNaN(Type2, Op2, State);
            }
            else if (Type1 == FPType.QNaN)
            {
                return FPProcessNaN(Type1, Op1, State);
            }
            else if (Type2 == FPType.QNaN)
            {
                return FPProcessNaN(Type2, Op2, State);
            }

            Done = false;

            return FPZero(false);
        }

        private static double FPProcessNaNs3(
            FPType Type1,
            FPType Type2,
            FPType Type3,
            ulong Op1,
            ulong Op2,
            ulong Op3,
            AThreadState State,
            out bool Done)
        {
            Done = true;

            if (Type1 == FPType.SNaN)
            {
                return FPProcessNaN(Type1, Op1, State);
            }
            else if (Type2 == FPType.SNaN)
            {
                return FPProcessNaN(Type2, Op2, State);
            }
            else if (Type3 == FPType.SNaN)
            {
                return FPProcessNaN(Type3, Op3, State);
            }
            else if (Type1 == FPType.QNaN)
            {
                return FPProcessNaN(Type1, Op1, State);
            }
            else if (Type2 == FPType.QNaN)
            {
                return FPProcessNaN(Type2, Op2, State);
            }
            else if (Type3 == FPType.QNaN)
            {
                return FPProcessNaN(Type3, Op3, State);
            }

            Done = false;

            return FPZero(false);
        }

        private static double FPProcessNaN(FPType Type, ulong Op, AThreadState State)
        {
            const int DNBit = 25; // Default NaN mode control bit.

            if (Type == FPType.SNaN)
            {
                Op |= 1ul << 51;

                FPProcessException(FPExc.InvalidOp, State);
            }

            if ((State.Fpcr & (1 << DNBit)) != 0)
            {
                return FPDefaultNaN();
            }

            return BitConverter.Int64BitsToDouble((long)Op);
        }

        private static void FPProcessException(FPExc Exc, AThreadState State)
        {
            int Enable = (int)Exc + 8;

            if ((State.Fpcr & (1 << Enable)) != 0)
            {
                throw new NotImplementedException("floating-point trap handling");
            }
            else
            {
                State.Fpsr |= 1 << (int)Exc;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static FPRounding FPRoundingMode(AThreadState State)
        {
            const int RModeBits = 22; // Rounding Mode control field.

            return (FPRounding)((State.Fpcr >> RModeBits) & 0b11);
        }
    }
}
