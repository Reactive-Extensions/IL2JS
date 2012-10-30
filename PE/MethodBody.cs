//
// Method bodies (§25.4)
//

using System;
using System.Linq;
using Microsoft.LiveLabs.Extras;

namespace Microsoft.LiveLabs.PE
{
    public class ExceptionHandlingClause
    {
        public CorILExceptionClause Flags;
        public int TryOffset;
        public int TryLength;
        public int HandlerOffset;
        public int HandlerLength;
        public object Class;
        public int FilterOffset;

        public void Read(ReaderContext ctxt, BlobReader reader, bool isFat, Func<OpCode, Row, object> resolveRow)
        {
            if (isFat)
            {
                Flags = (CorILExceptionClause)reader.ReadUInt32();
                TryOffset = (int)reader.ReadUInt32();
                TryLength = (int)reader.ReadUInt32();
                HandlerOffset = (int)reader.ReadUInt32();
                HandlerLength = (int)reader.ReadUInt32();
            }
            else
            {
                Flags = (CorILExceptionClause)reader.ReadUInt16();
                TryOffset = (int)reader.ReadUInt16();
                TryLength = (int)reader.ReadByte();
                HandlerOffset = (int)reader.ReadUInt16();
                HandlerLength = (int)reader.ReadByte();
            }
            var rowRef = default(TokenRef);
            rowRef.Read(ctxt, reader);
            rowRef.ResolveIndexes(ctxt);
            Class = rowRef.Value == null ? null : resolveRow(OpCode.Ldobj, rowRef.Value);
            if (Flags == CorILExceptionClause.Filter)
                FilterOffset = (int)reader.ReadUInt32();
        }

        public bool IsFat()
        {
            return TryOffset > 0xffff || TryLength > 0xff || HandlerOffset > 0xffff || HandlerLength > 0xff;
        }

        public void Write(WriterContext ctxt, BlobWriter writer, bool isFat, Func<OpCode, object, Row> findRow)
        {
            if (isFat)
            {
                writer.WriteUInt32((uint)Flags);
                writer.WriteUInt32((uint)TryOffset);
                writer.WriteUInt32((uint)TryLength);
                writer.WriteUInt32((uint)HandlerOffset);
                writer.WriteUInt32((uint)HandlerLength);
            }
            else
            {
                writer.WriteUInt16((ushort)Flags);
                writer.WriteUInt16((ushort)TryOffset);
                writer.WriteByte((byte)TryLength);
                writer.WriteUInt16((ushort)HandlerOffset);
                writer.WriteByte((byte)HandlerLength);
            }
            var rowRef = default(TokenRef);
            rowRef.Value = Class == null ? null : findRow(OpCode.Ldobj, Class);
            rowRef.PersistIndexes(ctxt);
            rowRef.Write(ctxt, writer);
            if (Flags == CorILExceptionClause.Filter)
                writer.WriteUInt32((uint)FilterOffset);
        }
    }

    public struct Instruction
    {
        public int Offset;
        public OpCode OpCode;
        public object Value;

        public static void Skip(BlobReader reader)
        {
            var opCode = (OpCode)reader.ReadByte();
            if (opCode == OpCode.Prefix1)
                opCode = (OpCode)((ushort)opCode << 8 | reader.ReadByte());
            switch (opCode)
            {
            case OpCode.Nop:
            case OpCode.Break:
            case OpCode.Ldarg_0:
            case OpCode.Ldarg_1:
            case OpCode.Ldarg_2:
            case OpCode.Ldarg_3:
            case OpCode.Ldloc_0:
            case OpCode.Ldloc_1:
            case OpCode.Ldloc_2:
            case OpCode.Ldloc_3:
            case OpCode.Stloc_0:
            case OpCode.Stloc_1:
            case OpCode.Stloc_2:
            case OpCode.Stloc_3:
            case OpCode.Ldnull:
            case OpCode.Ldc_i4_m1:
            case OpCode.Ldc_i4_0:
            case OpCode.Ldc_i4_1:
            case OpCode.Ldc_i4_2:
            case OpCode.Ldc_i4_3:
            case OpCode.Ldc_i4_4:
            case OpCode.Ldc_i4_5:
            case OpCode.Ldc_i4_6:
            case OpCode.Ldc_i4_7:
            case OpCode.Ldc_i4_8:
            case OpCode.Dup:
            case OpCode.Pop:
            case OpCode.Ret:
            case OpCode.Ldind_i1:
            case OpCode.Ldind_u1:
            case OpCode.Ldind_i2:
            case OpCode.Ldind_u2:
            case OpCode.Ldind_i4:
            case OpCode.Ldind_u4:
            case OpCode.Ldind_i8:
            case OpCode.Ldind_i:
            case OpCode.Ldind_r4:
            case OpCode.Ldind_r8:
            case OpCode.Ldind_ref:
            case OpCode.Stind_ref:
            case OpCode.Stind_i1:
            case OpCode.Stind_i2:
            case OpCode.Stind_i4:
            case OpCode.Stind_i8:
            case OpCode.Stind_r4:
            case OpCode.Stind_r8:
            case OpCode.Add:
            case OpCode.Sub:
            case OpCode.Mul:
            case OpCode.Div:
            case OpCode.Div_un:
            case OpCode.Rem:
            case OpCode.Rem_un:
            case OpCode.And:
            case OpCode.Or:
            case OpCode.Xor:
            case OpCode.Shl:
            case OpCode.Shr:
            case OpCode.Shr_un:
            case OpCode.Neg:
            case OpCode.Not:
            case OpCode.Conv_i1:
            case OpCode.Conv_i2:
            case OpCode.Conv_i4:
            case OpCode.Conv_i8:
            case OpCode.Conv_r4:
            case OpCode.Conv_r8:
            case OpCode.Conv_u4:
            case OpCode.Conv_u8:
            case OpCode.Conv_r_un:
            case OpCode.Throw:
            case OpCode.Conv_ovf_i1_un:
            case OpCode.Conv_ovf_i2_un:
            case OpCode.Conv_ovf_i4_un:
            case OpCode.Conv_ovf_i8_un:
            case OpCode.Conv_ovf_u1_un:
            case OpCode.Conv_ovf_u2_un:
            case OpCode.Conv_ovf_u4_un:
            case OpCode.Conv_ovf_u8_un:
            case OpCode.Conv_ovf_i_un:
            case OpCode.Conv_ovf_u_un:
            case OpCode.Ldlen:
            case OpCode.Ldelem_i1:
            case OpCode.Ldelem_u1:
            case OpCode.Ldelem_i2:
            case OpCode.Ldelem_u2:
            case OpCode.Ldelem_i4:
            case OpCode.Ldelem_u4:
            case OpCode.Ldelem_i8:
            case OpCode.Ldelem_i:
            case OpCode.Ldelem_r4:
            case OpCode.Ldelem_r8:
            case OpCode.Ldelem_ref:
            case OpCode.Stelem_i:
            case OpCode.Stelem_i1:
            case OpCode.Stelem_i2:
            case OpCode.Stelem_i4:
            case OpCode.Stelem_i8:
            case OpCode.Stelem_r4:
            case OpCode.Stelem_r8:
            case OpCode.Stelem_ref:
            case OpCode.Conv_ovf_i1:
            case OpCode.Conv_ovf_u1:
            case OpCode.Conv_ovf_i2:
            case OpCode.Conv_ovf_u2:
            case OpCode.Conv_ovf_i4:
            case OpCode.Conv_ovf_u4:
            case OpCode.Conv_ovf_i8:
            case OpCode.Conv_ovf_u8:
            case OpCode.Ckfinite:
            case OpCode.Conv_u2:
            case OpCode.Conv_u1:
            case OpCode.Conv_i:
            case OpCode.Conv_ovf_i:
            case OpCode.Conv_ovf_u:
            case OpCode.Add_ovf:
            case OpCode.Add_ovf_un:
            case OpCode.Mul_ovf:
            case OpCode.Mul_ovf_un:
            case OpCode.Sub_ovf:
            case OpCode.Sub_ovf_un:
            case OpCode.Endfinally:
            case OpCode.Stind_i:
            case OpCode.Conv_u:
            case OpCode.Prefix7:
            case OpCode.Prefix6:
            case OpCode.Prefix5:
            case OpCode.Prefix4:
            case OpCode.Prefix3:
            case OpCode.Prefix2:
            case OpCode.Prefix1:
            case OpCode.Prefixref:
            case OpCode.Arglist:
            case OpCode.Ceq:
            case OpCode.Cgt:
            case OpCode.Cgt_un:
            case OpCode.Clt:
            case OpCode.Clt_un:
            case OpCode.Localloc:
            case OpCode.Endfilter:
            case OpCode.Volatile:
            case OpCode.Tailcall:
            case OpCode.Cpblk:
            case OpCode.Initblk:
            case OpCode.Rethrow:
            case OpCode.Refanytype:
            case OpCode.Readonly:
                break;
            case OpCode.Br_s:
            case OpCode.Brfalse_s:
            case OpCode.Brtrue_s:
            case OpCode.Beq_s:
            case OpCode.Bge_s:
            case OpCode.Bgt_s:
            case OpCode.Ble_s:
            case OpCode.Blt_s:
            case OpCode.Bne_un_s:
            case OpCode.Bge_un_s:
            case OpCode.Bgt_un_s:
            case OpCode.Ble_un_s:
            case OpCode.Blt_un_s:
            case OpCode.Leave_s:
            case OpCode.Ldc_i4_s:
                reader.ReadSByte();
                break;
            case OpCode.Unaligned:
            case OpCode.Ldarg_s:
            case OpCode.Ldarga_s:
            case OpCode.Starg_s:
            case OpCode.Ldloc_s:
            case OpCode.Ldloca_s:
            case OpCode.Stloc_s:
                reader.ReadByte();
                break;
            case OpCode.Br:
            case OpCode.Brfalse:
            case OpCode.Brtrue:
            case OpCode.Beq:
            case OpCode.Bge:
            case OpCode.Bgt:
            case OpCode.Ble:
            case OpCode.Blt:
            case OpCode.Bne_un:
            case OpCode.Bge_un:
            case OpCode.Bgt_un:
            case OpCode.Ble_un:
            case OpCode.Blt_un:
            case OpCode.Leave:
            case OpCode.Ldc_i4:
                reader.ReadInt32();
                break;
            case OpCode.Ldfld:
            case OpCode.Ldflda:
            case OpCode.Stfld:
            case OpCode.Ldsfld:
            case OpCode.Ldsflda:
            case OpCode.Stsfld:
            case OpCode.Calli:
            case OpCode.Ldstr:
            case OpCode.Ldtoken:
            case OpCode.Cpobj:
            case OpCode.Ldobj:
            case OpCode.Castclass:
            case OpCode.Isinst:
            case OpCode.Unbox:
            case OpCode.Stobj:
            case OpCode.Box:
            case OpCode.Newarr:
            case OpCode.Ldelema:
            case OpCode.Ldelem:
            case OpCode.Stelem:
            case OpCode.Unbox_any:
            case OpCode.Refanyval:
            case OpCode.Mkrefany:
            case OpCode.Initobj:
            case OpCode.Constrained:
            case OpCode.Sizeof:
            case OpCode.Ldarg:
            case OpCode.Ldarga:
            case OpCode.Starg:
            case OpCode.Ldloc:
            case OpCode.Ldloca:
            case OpCode.Stloc:
            case OpCode.Jmp:
            case OpCode.Call:
            case OpCode.Callvirt:
            case OpCode.Newobj:
            case OpCode.Ldftn:
            case OpCode.Ldvirtftn:
                reader.ReadUInt32();
                break;
            case OpCode.Ldc_i8:
                reader.ReadInt64();
                break;
            case OpCode.Ldc_r4:
                reader.ReadSingle();
                break;
            case OpCode.Ldc_r8:
                reader.ReadDouble();
                break;
            case OpCode.Switch:
                {
                    var numTargets = (int)reader.ReadUInt32();
                    for (var i = 0; i < numTargets; i++)
                        reader.ReadInt32();
                    break;
                }
            default:
                throw new PEException("unrecognised opcode");
            }
        }

        private static Row ReadToken(ReaderContext ctxt, BlobReader reader)
        {
            var rowRef = default(TokenRef);
            rowRef.Read(ctxt, reader);
            rowRef.ResolveIndexes(ctxt);
            return rowRef.Value;
        }

        private static string ReadUserString(ReaderContext ctxt, BlobReader reader)
        {
            var strref = default(UserStringRef);
            strref.Read(ctxt, reader);
            strref.ResolveIndexes(ctxt);
            return strref.Value;
        }


        public void Read(ReaderContext ctxt, BlobReader reader, uint beginOffset, Func<OpCode, Row, object> resolveRow)
        {
            Offset = (int)(reader.Offset - beginOffset);
            OpCode = (OpCode)reader.ReadByte();
            if (OpCode == OpCode.Prefix1)
                OpCode = (OpCode)((ushort)OpCode << 8 | reader.ReadByte());
            Value = default(object);
            switch (OpCode)
            {
            case OpCode.Nop:
            case OpCode.Break:
            case OpCode.Ldarg_0:
            case OpCode.Ldarg_1:
            case OpCode.Ldarg_2:
            case OpCode.Ldarg_3:
            case OpCode.Ldloc_0:
            case OpCode.Ldloc_1:
            case OpCode.Ldloc_2:
            case OpCode.Ldloc_3:
            case OpCode.Stloc_0:
            case OpCode.Stloc_1:
            case OpCode.Stloc_2:
            case OpCode.Stloc_3:
            case OpCode.Ldnull:
            case OpCode.Ldc_i4_m1:
            case OpCode.Ldc_i4_0:
            case OpCode.Ldc_i4_1:
            case OpCode.Ldc_i4_2:
            case OpCode.Ldc_i4_3:
            case OpCode.Ldc_i4_4:
            case OpCode.Ldc_i4_5:
            case OpCode.Ldc_i4_6:
            case OpCode.Ldc_i4_7:
            case OpCode.Ldc_i4_8:
            case OpCode.Dup:
            case OpCode.Pop:
            case OpCode.Ret:
            case OpCode.Ldind_i1:
            case OpCode.Ldind_u1:
            case OpCode.Ldind_i2:
            case OpCode.Ldind_u2:
            case OpCode.Ldind_i4:
            case OpCode.Ldind_u4:
            case OpCode.Ldind_i8:
            case OpCode.Ldind_i:
            case OpCode.Ldind_r4:
            case OpCode.Ldind_r8:
            case OpCode.Ldind_ref:
            case OpCode.Stind_ref:
            case OpCode.Stind_i1:
            case OpCode.Stind_i2:
            case OpCode.Stind_i4:
            case OpCode.Stind_i8:
            case OpCode.Stind_r4:
            case OpCode.Stind_r8:
            case OpCode.Add:
            case OpCode.Sub:
            case OpCode.Mul:
            case OpCode.Div:
            case OpCode.Div_un:
            case OpCode.Rem:
            case OpCode.Rem_un:
            case OpCode.And:
            case OpCode.Or:
            case OpCode.Xor:
            case OpCode.Shl:
            case OpCode.Shr:
            case OpCode.Shr_un:
            case OpCode.Neg:
            case OpCode.Not:
            case OpCode.Conv_i1:
            case OpCode.Conv_i2:
            case OpCode.Conv_i4:
            case OpCode.Conv_i8:
            case OpCode.Conv_r4:
            case OpCode.Conv_r8:
            case OpCode.Conv_u4:
            case OpCode.Conv_u8:
            case OpCode.Conv_r_un:
            case OpCode.Throw:
            case OpCode.Conv_ovf_i1_un:
            case OpCode.Conv_ovf_i2_un:
            case OpCode.Conv_ovf_i4_un:
            case OpCode.Conv_ovf_i8_un:
            case OpCode.Conv_ovf_u1_un:
            case OpCode.Conv_ovf_u2_un:
            case OpCode.Conv_ovf_u4_un:
            case OpCode.Conv_ovf_u8_un:
            case OpCode.Conv_ovf_i_un:
            case OpCode.Conv_ovf_u_un:
            case OpCode.Ldlen:
            case OpCode.Ldelem_i1:
            case OpCode.Ldelem_u1:
            case OpCode.Ldelem_i2:
            case OpCode.Ldelem_u2:
            case OpCode.Ldelem_i4:
            case OpCode.Ldelem_u4:
            case OpCode.Ldelem_i8:
            case OpCode.Ldelem_i:
            case OpCode.Ldelem_r4:
            case OpCode.Ldelem_r8:
            case OpCode.Ldelem_ref:
            case OpCode.Stelem_i:
            case OpCode.Stelem_i1:
            case OpCode.Stelem_i2:
            case OpCode.Stelem_i4:
            case OpCode.Stelem_i8:
            case OpCode.Stelem_r4:
            case OpCode.Stelem_r8:
            case OpCode.Stelem_ref:
            case OpCode.Conv_ovf_i1:
            case OpCode.Conv_ovf_u1:
            case OpCode.Conv_ovf_i2:
            case OpCode.Conv_ovf_u2:
            case OpCode.Conv_ovf_i4:
            case OpCode.Conv_ovf_u4:
            case OpCode.Conv_ovf_i8:
            case OpCode.Conv_ovf_u8:
            case OpCode.Ckfinite:
            case OpCode.Conv_u2:
            case OpCode.Conv_u1:
            case OpCode.Conv_i:
            case OpCode.Conv_ovf_i:
            case OpCode.Conv_ovf_u:
            case OpCode.Add_ovf:
            case OpCode.Add_ovf_un:
            case OpCode.Mul_ovf:
            case OpCode.Mul_ovf_un:
            case OpCode.Sub_ovf:
            case OpCode.Sub_ovf_un:
            case OpCode.Endfinally:
            case OpCode.Stind_i:
            case OpCode.Conv_u:
            case OpCode.Prefix7:
            case OpCode.Prefix6:
            case OpCode.Prefix5:
            case OpCode.Prefix4:
            case OpCode.Prefix3:
            case OpCode.Prefix2:
            case OpCode.Prefix1:
            case OpCode.Prefixref:
            case OpCode.Arglist:
            case OpCode.Ceq:
            case OpCode.Cgt:
            case OpCode.Cgt_un:
            case OpCode.Clt:
            case OpCode.Clt_un:
            case OpCode.Localloc:
            case OpCode.Endfilter:
            case OpCode.Volatile:
            case OpCode.Tailcall:
            case OpCode.Cpblk:
            case OpCode.Initblk:
            case OpCode.Rethrow:
            case OpCode.Refanytype:
            case OpCode.Readonly:
                break;
            case OpCode.Br:
            case OpCode.Brfalse:
            case OpCode.Brtrue:
            case OpCode.Beq:
            case OpCode.Bge:
            case OpCode.Bgt:
            case OpCode.Ble:
            case OpCode.Blt:
            case OpCode.Bne_un:
            case OpCode.Bge_un:
            case OpCode.Bgt_un:
            case OpCode.Ble_un:
            case OpCode.Blt_un:
            case OpCode.Leave:
                {
                    // NOTE: Delta is w.r.t. start of next instruction
                    var delta = reader.ReadInt32();
                    Value = (int)(reader.Offset - beginOffset) + delta;
                }
                break;
            case OpCode.Br_s:
            case OpCode.Brfalse_s:
            case OpCode.Brtrue_s:
            case OpCode.Beq_s:
            case OpCode.Bge_s:
            case OpCode.Bgt_s:
            case OpCode.Ble_s:
            case OpCode.Blt_s:
            case OpCode.Bne_un_s:
            case OpCode.Bge_un_s:
            case OpCode.Bgt_un_s:
            case OpCode.Ble_un_s:
            case OpCode.Blt_un_s:
            case OpCode.Leave_s:
                {
                    var delta = reader.ReadSByte();
                    Value = (int)(reader.Offset - beginOffset) + delta;
                }
                break;
            case OpCode.Ldc_i4_s:
                Value = (int)reader.ReadSByte();
                break;
            case OpCode.Unaligned:
            case OpCode.Ldarg_s:
            case OpCode.Ldarga_s:
            case OpCode.Starg_s:
            case OpCode.Ldloc_s:
            case OpCode.Ldloca_s:
            case OpCode.Stloc_s:
                Value = (int)reader.ReadByte();
                break;
            case OpCode.Ldc_i4:
                Value = reader.ReadInt32();
                break;
            case OpCode.Ldarg:
            case OpCode.Ldarga:
            case OpCode.Starg:
            case OpCode.Ldloc:
            case OpCode.Ldloca:
            case OpCode.Stloc:
                Value = (int)reader.ReadUInt32();
                break;
            case OpCode.Ldc_i8:
                Value = reader.ReadInt64();
                break;
            case OpCode.Ldc_r4:
                Value = reader.ReadSingle();
                break;
            case OpCode.Ldc_r8:
                Value = reader.ReadDouble();
                break;
            case OpCode.Ldstr:
                Value = ReadUserString(ctxt, reader);
                break;
            case OpCode.Switch:
                {
                    var numTargets = (int)reader.ReadUInt32();
                    var targets = new Seq<int>(numTargets);
                    // Read as offsets from end of switch, then fixup to offsets from start of instructions
                    for (var i = 0; i < numTargets; i++)
                        targets.Add(reader.ReadInt32());
                    for (var i = 0; i < numTargets; i++)
                        targets[i] = (int)(reader.Offset - beginOffset) + targets[i];
                    Value = targets;
                }
                break;
            case OpCode.Calli:
            case OpCode.Ldfld:
            case OpCode.Ldflda:
            case OpCode.Stfld:
            case OpCode.Ldsfld:
            case OpCode.Ldsflda:
            case OpCode.Stsfld:
            case OpCode.Jmp:
            case OpCode.Call:
            case OpCode.Callvirt:
            case OpCode.Newobj:
            case OpCode.Ldftn:
            case OpCode.Ldvirtftn:
            case OpCode.Ldtoken:
            case OpCode.Cpobj:
            case OpCode.Ldobj:
            case OpCode.Castclass:
            case OpCode.Isinst:
            case OpCode.Unbox:
            case OpCode.Stobj:
            case OpCode.Box:
            case OpCode.Newarr:
            case OpCode.Ldelema:
            case OpCode.Ldelem:
            case OpCode.Stelem:
            case OpCode.Unbox_any:
            case OpCode.Refanyval:
            case OpCode.Mkrefany:
            case OpCode.Initobj:
            case OpCode.Constrained:
            case OpCode.Sizeof:
                Value = resolveRow(OpCode, ReadToken(ctxt, reader));
                break;
            default:
                throw new PEException("unrecognised opcode");
            }
        }

        private static void WriteToken(WriterContext ctxt, BlobWriter writer, Row row)
        {
            var rowRef = default(TokenRef);
            rowRef.Value = row;
            rowRef.PersistIndexes(ctxt);
            rowRef.Write(ctxt, writer);
        }

        private static void WriteUserString(WriterContext ctxt, BlobWriter writer, string str)
        {
            var strref = default(UserStringRef);
            strref.Value = str;
            strref.PersistIndexes(ctxt);
            strref.Write(ctxt, writer);
        }

        public int Size()
        {
            var n = 0;
            var highByte = (ushort)OpCode >> 8;
            if (highByte > 0)
                n++;
            n++;
            switch (OpCode)
            {
            case OpCode.Nop:
            case OpCode.Break:
            case OpCode.Ldarg_0:
            case OpCode.Ldarg_1:
            case OpCode.Ldarg_2:
            case OpCode.Ldarg_3:
            case OpCode.Ldloc_0:
            case OpCode.Ldloc_1:
            case OpCode.Ldloc_2:
            case OpCode.Ldloc_3:
            case OpCode.Stloc_0:
            case OpCode.Stloc_1:
            case OpCode.Stloc_2:
            case OpCode.Stloc_3:
            case OpCode.Ldnull:
            case OpCode.Ldc_i4_m1:
            case OpCode.Ldc_i4_0:
            case OpCode.Ldc_i4_1:
            case OpCode.Ldc_i4_2:
            case OpCode.Ldc_i4_3:
            case OpCode.Ldc_i4_4:
            case OpCode.Ldc_i4_5:
            case OpCode.Ldc_i4_6:
            case OpCode.Ldc_i4_7:
            case OpCode.Ldc_i4_8:
            case OpCode.Dup:
            case OpCode.Pop:
            case OpCode.Ret:
            case OpCode.Ldind_i1:
            case OpCode.Ldind_u1:
            case OpCode.Ldind_i2:
            case OpCode.Ldind_u2:
            case OpCode.Ldind_i4:
            case OpCode.Ldind_u4:
            case OpCode.Ldind_i8:
            case OpCode.Ldind_i:
            case OpCode.Ldind_r4:
            case OpCode.Ldind_r8:
            case OpCode.Ldind_ref:
            case OpCode.Stind_ref:
            case OpCode.Stind_i1:
            case OpCode.Stind_i2:
            case OpCode.Stind_i4:
            case OpCode.Stind_i8:
            case OpCode.Stind_r4:
            case OpCode.Stind_r8:
            case OpCode.Add:
            case OpCode.Sub:
            case OpCode.Mul:
            case OpCode.Div:
            case OpCode.Div_un:
            case OpCode.Rem:
            case OpCode.Rem_un:
            case OpCode.And:
            case OpCode.Or:
            case OpCode.Xor:
            case OpCode.Shl:
            case OpCode.Shr:
            case OpCode.Shr_un:
            case OpCode.Neg:
            case OpCode.Not:
            case OpCode.Conv_i1:
            case OpCode.Conv_i2:
            case OpCode.Conv_i4:
            case OpCode.Conv_i8:
            case OpCode.Conv_r4:
            case OpCode.Conv_r8:
            case OpCode.Conv_u4:
            case OpCode.Conv_u8:
            case OpCode.Conv_r_un:
            case OpCode.Throw:
            case OpCode.Conv_ovf_i1_un:
            case OpCode.Conv_ovf_i2_un:
            case OpCode.Conv_ovf_i4_un:
            case OpCode.Conv_ovf_i8_un:
            case OpCode.Conv_ovf_u1_un:
            case OpCode.Conv_ovf_u2_un:
            case OpCode.Conv_ovf_u4_un:
            case OpCode.Conv_ovf_u8_un:
            case OpCode.Conv_ovf_i_un:
            case OpCode.Conv_ovf_u_un:
            case OpCode.Ldlen:
            case OpCode.Ldelem_i1:
            case OpCode.Ldelem_u1:
            case OpCode.Ldelem_i2:
            case OpCode.Ldelem_u2:
            case OpCode.Ldelem_i4:
            case OpCode.Ldelem_u4:
            case OpCode.Ldelem_i8:
            case OpCode.Ldelem_i:
            case OpCode.Ldelem_r4:
            case OpCode.Ldelem_r8:
            case OpCode.Ldelem_ref:
            case OpCode.Stelem_i:
            case OpCode.Stelem_i1:
            case OpCode.Stelem_i2:
            case OpCode.Stelem_i4:
            case OpCode.Stelem_i8:
            case OpCode.Stelem_r4:
            case OpCode.Stelem_r8:
            case OpCode.Stelem_ref:
            case OpCode.Conv_ovf_i1:
            case OpCode.Conv_ovf_u1:
            case OpCode.Conv_ovf_i2:
            case OpCode.Conv_ovf_u2:
            case OpCode.Conv_ovf_i4:
            case OpCode.Conv_ovf_u4:
            case OpCode.Conv_ovf_i8:
            case OpCode.Conv_ovf_u8:
            case OpCode.Ckfinite:
            case OpCode.Conv_u2:
            case OpCode.Conv_u1:
            case OpCode.Conv_i:
            case OpCode.Conv_ovf_i:
            case OpCode.Conv_ovf_u:
            case OpCode.Add_ovf:
            case OpCode.Add_ovf_un:
            case OpCode.Mul_ovf:
            case OpCode.Mul_ovf_un:
            case OpCode.Sub_ovf:
            case OpCode.Sub_ovf_un:
            case OpCode.Endfinally:
            case OpCode.Stind_i:
            case OpCode.Conv_u:
            case OpCode.Prefix7:
            case OpCode.Prefix6:
            case OpCode.Prefix5:
            case OpCode.Prefix4:
            case OpCode.Prefix3:
            case OpCode.Prefix2:
            case OpCode.Prefix1:
            case OpCode.Prefixref:
            case OpCode.Arglist:
            case OpCode.Ceq:
            case OpCode.Cgt:
            case OpCode.Cgt_un:
            case OpCode.Clt:
            case OpCode.Clt_un:
            case OpCode.Localloc:
            case OpCode.Endfilter:
            case OpCode.Volatile:
            case OpCode.Tailcall:
            case OpCode.Cpblk:
            case OpCode.Initblk:
            case OpCode.Rethrow:
            case OpCode.Refanytype:
            case OpCode.Readonly:
                break;
            case OpCode.Br:
            case OpCode.Brfalse:
            case OpCode.Brtrue:
            case OpCode.Beq:
            case OpCode.Bge:
            case OpCode.Bgt:
            case OpCode.Ble:
            case OpCode.Blt:
            case OpCode.Bne_un:
            case OpCode.Bge_un:
            case OpCode.Bgt_un:
            case OpCode.Ble_un:
            case OpCode.Blt_un:
            case OpCode.Leave:
                n += 4;
                break;
            case OpCode.Br_s:
            case OpCode.Brfalse_s:
            case OpCode.Brtrue_s:
            case OpCode.Beq_s:
            case OpCode.Bge_s:
            case OpCode.Bgt_s:
            case OpCode.Ble_s:
            case OpCode.Blt_s:
            case OpCode.Bne_un_s:
            case OpCode.Bge_un_s:
            case OpCode.Bgt_un_s:
            case OpCode.Ble_un_s:
            case OpCode.Blt_un_s:
            case OpCode.Leave_s:
                n++;
                break;
            case OpCode.Ldc_i4_s:
                n++;
                break;
            case OpCode.Ldarg_s:
            case OpCode.Ldarga_s:
            case OpCode.Starg_s:
            case OpCode.Ldloc_s:
            case OpCode.Ldloca_s:
            case OpCode.Stloc_s:
            case OpCode.Unaligned:
                n++;
                break;
            case OpCode.Ldc_i4:
                n += 4;
                break;
            case OpCode.Ldarg:
            case OpCode.Ldarga:
            case OpCode.Starg:
            case OpCode.Ldloc:
            case OpCode.Ldloca:
            case OpCode.Stloc:
                n += 4;
                break;
            case OpCode.Ldc_i8:
                n += 8;
                break;
            case OpCode.Ldc_r4:
                n += 4;
                break;
            case OpCode.Ldc_r8:
                n += 8;
                break;
            case OpCode.Ldstr:
                n += 4;
                break;
            case OpCode.Switch:
                {
                    var targets = (Seq<int>)Value;
                    n += (1 + targets.Count)*4;
                    break;
                }
            case OpCode.Calli:
            case OpCode.Jmp:
            case OpCode.Call:
            case OpCode.Callvirt:
            case OpCode.Newobj:
            case OpCode.Ldftn:
            case OpCode.Ldvirtftn:
            case OpCode.Ldfld:
            case OpCode.Ldflda:
            case OpCode.Stfld:
            case OpCode.Ldsfld:
            case OpCode.Ldsflda:
            case OpCode.Stsfld:
            case OpCode.Ldtoken:
            case OpCode.Cpobj:
            case OpCode.Ldobj:
            case OpCode.Castclass:
            case OpCode.Isinst:
            case OpCode.Unbox:
            case OpCode.Stobj:
            case OpCode.Box:
            case OpCode.Newarr:
            case OpCode.Ldelema:
            case OpCode.Ldelem:
            case OpCode.Stelem:
            case OpCode.Unbox_any:
            case OpCode.Refanyval:
            case OpCode.Mkrefany:
            case OpCode.Initobj:
            case OpCode.Constrained:
            case OpCode.Sizeof:
                n += 4;
                break;
            default:
                throw new PEException("unrecognised opcode");
            }
            return n;
        }

        public void Write(WriterContext ctxt, BlobWriter writer, uint beginOffset, Func<OpCode, object, Row> findRow)
        {
            var offset = (int)(writer.Offset - beginOffset);
            if (offset != Offset)
                throw new PEException("invalid instruction offset");
            var highByte = (ushort)OpCode >> 8;
            if (highByte > 0)
                writer.WriteByte((byte)highByte);
            writer.WriteByte((byte)OpCode);
            switch (OpCode)
            {
            case OpCode.Nop:
            case OpCode.Break:
            case OpCode.Ldarg_0:
            case OpCode.Ldarg_1:
            case OpCode.Ldarg_2:
            case OpCode.Ldarg_3:
            case OpCode.Ldloc_0:
            case OpCode.Ldloc_1:
            case OpCode.Ldloc_2:
            case OpCode.Ldloc_3:
            case OpCode.Stloc_0:
            case OpCode.Stloc_1:
            case OpCode.Stloc_2:
            case OpCode.Stloc_3:
            case OpCode.Ldnull:
            case OpCode.Ldc_i4_m1:
            case OpCode.Ldc_i4_0:
            case OpCode.Ldc_i4_1:
            case OpCode.Ldc_i4_2:
            case OpCode.Ldc_i4_3:
            case OpCode.Ldc_i4_4:
            case OpCode.Ldc_i4_5:
            case OpCode.Ldc_i4_6:
            case OpCode.Ldc_i4_7:
            case OpCode.Ldc_i4_8:
            case OpCode.Dup:
            case OpCode.Pop:
            case OpCode.Ret:
            case OpCode.Ldind_i1:
            case OpCode.Ldind_u1:
            case OpCode.Ldind_i2:
            case OpCode.Ldind_u2:
            case OpCode.Ldind_i4:
            case OpCode.Ldind_u4:
            case OpCode.Ldind_i8:
            case OpCode.Ldind_i:
            case OpCode.Ldind_r4:
            case OpCode.Ldind_r8:
            case OpCode.Ldind_ref:
            case OpCode.Stind_ref:
            case OpCode.Stind_i1:
            case OpCode.Stind_i2:
            case OpCode.Stind_i4:
            case OpCode.Stind_i8:
            case OpCode.Stind_r4:
            case OpCode.Stind_r8:
            case OpCode.Add:
            case OpCode.Sub:
            case OpCode.Mul:
            case OpCode.Div:
            case OpCode.Div_un:
            case OpCode.Rem:
            case OpCode.Rem_un:
            case OpCode.And:
            case OpCode.Or:
            case OpCode.Xor:
            case OpCode.Shl:
            case OpCode.Shr:
            case OpCode.Shr_un:
            case OpCode.Neg:
            case OpCode.Not:
            case OpCode.Conv_i1:
            case OpCode.Conv_i2:
            case OpCode.Conv_i4:
            case OpCode.Conv_i8:
            case OpCode.Conv_r4:
            case OpCode.Conv_r8:
            case OpCode.Conv_u4:
            case OpCode.Conv_u8:
            case OpCode.Conv_r_un:
            case OpCode.Throw:
            case OpCode.Conv_ovf_i1_un:
            case OpCode.Conv_ovf_i2_un:
            case OpCode.Conv_ovf_i4_un:
            case OpCode.Conv_ovf_i8_un:
            case OpCode.Conv_ovf_u1_un:
            case OpCode.Conv_ovf_u2_un:
            case OpCode.Conv_ovf_u4_un:
            case OpCode.Conv_ovf_u8_un:
            case OpCode.Conv_ovf_i_un:
            case OpCode.Conv_ovf_u_un:
            case OpCode.Ldlen:
            case OpCode.Ldelem_i1:
            case OpCode.Ldelem_u1:
            case OpCode.Ldelem_i2:
            case OpCode.Ldelem_u2:
            case OpCode.Ldelem_i4:
            case OpCode.Ldelem_u4:
            case OpCode.Ldelem_i8:
            case OpCode.Ldelem_i:
            case OpCode.Ldelem_r4:
            case OpCode.Ldelem_r8:
            case OpCode.Ldelem_ref:
            case OpCode.Stelem_i:
            case OpCode.Stelem_i1:
            case OpCode.Stelem_i2:
            case OpCode.Stelem_i4:
            case OpCode.Stelem_i8:
            case OpCode.Stelem_r4:
            case OpCode.Stelem_r8:
            case OpCode.Stelem_ref:
            case OpCode.Conv_ovf_i1:
            case OpCode.Conv_ovf_u1:
            case OpCode.Conv_ovf_i2:
            case OpCode.Conv_ovf_u2:
            case OpCode.Conv_ovf_i4:
            case OpCode.Conv_ovf_u4:
            case OpCode.Conv_ovf_i8:
            case OpCode.Conv_ovf_u8:
            case OpCode.Ckfinite:
            case OpCode.Conv_u2:
            case OpCode.Conv_u1:
            case OpCode.Conv_i:
            case OpCode.Conv_ovf_i:
            case OpCode.Conv_ovf_u:
            case OpCode.Add_ovf:
            case OpCode.Add_ovf_un:
            case OpCode.Mul_ovf:
            case OpCode.Mul_ovf_un:
            case OpCode.Sub_ovf:
            case OpCode.Sub_ovf_un:
            case OpCode.Endfinally:
            case OpCode.Stind_i:
            case OpCode.Conv_u:
            case OpCode.Prefix7:
            case OpCode.Prefix6:
            case OpCode.Prefix5:
            case OpCode.Prefix4:
            case OpCode.Prefix3:
            case OpCode.Prefix2:
            case OpCode.Prefix1:
            case OpCode.Prefixref:
            case OpCode.Arglist:
            case OpCode.Ceq:
            case OpCode.Cgt:
            case OpCode.Cgt_un:
            case OpCode.Clt:
            case OpCode.Clt_un:
            case OpCode.Localloc:
            case OpCode.Endfilter:
            case OpCode.Volatile:
            case OpCode.Tailcall:
            case OpCode.Cpblk:
            case OpCode.Initblk:
            case OpCode.Rethrow:
            case OpCode.Refanytype:
            case OpCode.Readonly:
                break;
            case OpCode.Br:
            case OpCode.Brfalse:
            case OpCode.Brtrue:
            case OpCode.Beq:
            case OpCode.Bge:
            case OpCode.Bgt:
            case OpCode.Ble:
            case OpCode.Blt:
            case OpCode.Bne_un:
            case OpCode.Bge_un:
            case OpCode.Bgt_un:
            case OpCode.Ble_un:
            case OpCode.Blt_un:
            case OpCode.Leave:
                {
                    var target = (int)Value;
                    // NOTE: Delta is relatative to start of next instruction
                    var delta = (int)beginOffset + target - ((int)writer.Offset + 4);
                    writer.WriteInt32(delta);
                }
                break;
            case OpCode.Br_s:
            case OpCode.Brfalse_s:
            case OpCode.Brtrue_s:
            case OpCode.Beq_s:
            case OpCode.Bge_s:
            case OpCode.Bgt_s:
            case OpCode.Ble_s:
            case OpCode.Blt_s:
            case OpCode.Bne_un_s:
            case OpCode.Bge_un_s:
            case OpCode.Bgt_un_s:
            case OpCode.Ble_un_s:
            case OpCode.Blt_un_s:
            case OpCode.Leave_s:
                {
                    var target = (int)Value;
                    // NOTE: Delta is w.r.t. begining of next instruction
                    var delta = (int)beginOffset + target - ((int)writer.Offset + 1);
                    if (delta > 0xff)
                        throw new PEException("cannot use small form for this instruction");
                    writer.WriteSByte((sbyte)delta);
                }
                break;
            case OpCode.Ldc_i4_s:
                writer.WriteSByte((sbyte)(int)Value);
                break;
            case OpCode.Ldarg_s:
            case OpCode.Ldarga_s:
            case OpCode.Starg_s:
            case OpCode.Ldloc_s:
            case OpCode.Ldloca_s:
            case OpCode.Stloc_s:
            case OpCode.Unaligned:
                writer.WriteByte((byte)(int)Value);
                break;
            case OpCode.Ldc_i4:
                writer.WriteInt32((int)Value);
                break;
            case OpCode.Ldarg:
            case OpCode.Ldarga:
            case OpCode.Starg:
            case OpCode.Ldloc:
            case OpCode.Ldloca:
            case OpCode.Stloc:
                writer.WriteUInt32((uint)(int)Value);
                break;
            case OpCode.Ldc_i8:
                writer.WriteInt64((long)Value);
                break;
            case OpCode.Ldc_r4:
                writer.WriteSingle((float)Value);
                break;
            case OpCode.Ldc_r8:
                writer.WriteDouble((double)Value);
                break;
            case OpCode.Ldstr:
                WriteUserString(ctxt, writer, (string)Value);
                break;
            case OpCode.Switch:
                {
                    var targets = (Seq<int>)Value;
                    writer.WriteUInt32((uint)targets.Count);
                    // NOTE: Deltas are w.r.t. start of next instruction
                    for (var i = 0; i < targets.Count; i++)
                    {
                        var delta = (int)beginOffset + targets[i] - ((int)writer.Offset + (targets.Count * 4));
                        writer.WriteInt32(delta);
                    }
                }
                break;
            case OpCode.Calli:
            case OpCode.Jmp:
            case OpCode.Call:
            case OpCode.Callvirt:
            case OpCode.Newobj:
            case OpCode.Ldftn:
            case OpCode.Ldvirtftn:
            case OpCode.Ldfld:
            case OpCode.Ldflda:
            case OpCode.Stfld:
            case OpCode.Ldsfld:
            case OpCode.Ldsflda:
            case OpCode.Stsfld:
            case OpCode.Ldtoken:
            case OpCode.Cpobj:
            case OpCode.Ldobj:
            case OpCode.Castclass:
            case OpCode.Isinst:
            case OpCode.Unbox:
            case OpCode.Stobj:
            case OpCode.Box:
            case OpCode.Newarr:
            case OpCode.Ldelema:
            case OpCode.Ldelem:
            case OpCode.Stelem:
            case OpCode.Unbox_any:
            case OpCode.Refanyval:
            case OpCode.Mkrefany:
            case OpCode.Initobj:
            case OpCode.Constrained:
            case OpCode.Sizeof:
                WriteToken(ctxt, writer, findRow(OpCode, Value));
                break;
            default:
                throw new PEException("unrecognised opcode");
            }
        }
    }

    public class MethodBody
    {
        public int MaxStack;
        public bool IsInitLocals;
        public TokenRef LocalVarRef;
        public Seq<ExceptionHandlingClause> ExceptionHandlingClauses;
        public Instruction[] Instructions;

        public LocalVarMemberSig LocalVariables
        {
            get
            {
                if (LocalVarRef.IsNull)
                    return null;
                else
                {
                    var standAloneSigRow = LocalVarRef.Value as StandAloneSigRow;
                    if (standAloneSigRow == null)
                        throw new PEException("expecting StandAloneSigRow");
                    var memberSig = standAloneSigRow.Signature.Value;
                    var res = memberSig as LocalVarMemberSig;
                    if (res == null)
                        throw new PEException("expecting LocalVarMemberSig");
                    return res;
                }
            }
        }

        public void Read(ReaderContext ctxt, BlobReader reader, Func<OpCode, Row, object> resolveRow)
        {
            ExceptionHandlingClauses = new Seq<ExceptionHandlingClause>();

            var firstByte = reader.ReadByte();
            var formatKind = (CorILMethod)(firstByte & 0x3);
            var bodySize = default(uint);
            var more = default(bool);
            switch (formatKind)
            {
            case CorILMethod.TinyFormat:
                {
                    MaxStack = 8;
                    bodySize = (uint)(firstByte >> 2);
                    break;
                }
            case CorILMethod.FatFormat:
                {
                    var secondByte = reader.ReadByte();
                    var flags = (CorILMethod)(((ushort)(secondByte & 0x7) << 8) | (ushort)firstByte);
                    IsInitLocals = (flags & CorILMethod.InitLocals) != 0;
                    var headerSize = (secondByte >> 4) & 0x7;
                    if (headerSize != 3)
                        throw new PEException("unexpected method body header size");
                    MaxStack = (int)reader.ReadUInt16();
                    bodySize = reader.ReadUInt32();
                    LocalVarRef.Read(ctxt, reader);
                    LocalVarRef.ResolveIndexes(ctxt);
                    more = (flags & CorILMethod.MoreSects) != 0;
                    break;
                }
            default:
                throw new InvalidOperationException("invalid method body format");
            }

            if (bodySize > 0)
            {
                var beginOffset = reader.Offset;
                var endOffset = reader.Offset + bodySize;
                var n = 0;
                while (reader.Offset < endOffset)
                {
                    n++;
                    Instruction.Skip(reader);
                }
                reader.Offset = beginOffset;
                Instructions = new Instruction[n];
                for (var i = 0; i < n; i++)
                    Instructions[i].Read(ctxt, reader, beginOffset, resolveRow);
            }

            while (more)
                more = ReadMethodDataSection(ctxt, reader, resolveRow);
        }

        private bool ReadMethodDataSection(ReaderContext ctxt, BlobReader reader, Func<OpCode, Row, object> resolveRow)
        {
            reader.Align(4);
            var flags = (CorILMethodSect)reader.ReadByte();
            if ((flags & CorILMethodSect.EHTable) == 0)
                throw new PEException("unrecognised method data section");

            var isFat = (flags & CorILMethodSect.FatFormat) != 0;
            var count = default(uint);
            if (isFat)
            {
                var size = reader.ReadUInt24();
                if (size < 4 || (size - 4) % 24 != 0)
                    throw new InvalidOperationException("invalid method data section");
                count = (size - 4) / 24;
            }
            else
            {
                var size = (uint)reader.ReadByte();
                // NOTE: Looks live VB emits size without including the 4 byte header...
                if (size < 4 || (size - 4) % 12 != 0)
                    throw new InvalidOperationException("invalid method data section");
                var padding = reader.ReadUInt16();
                if (padding != 0)
                    throw new PEException("unexpected data");
                count = (size - 4) / 12;
            }

            for (var i = 0; i < count; i++)
            {
                var c = new ExceptionHandlingClause();
                c.Read(ctxt, reader, isFat, resolveRow);
                ExceptionHandlingClauses.Add(c);
            }

            return (flags & CorILMethodSect.MoreSects) != 0;
        }

        private bool IsFat()
        {
            return false;
        }

        public void Write(WriterContext ctxt, BlobWriter writer, Func<OpCode, object, Row> findRow)
        {
            var bodySize = 0;
            for (var i = 0; i < Instructions.Length; i++)
                bodySize += Instructions[i].Size();

            var isFat = bodySize > 0x3f;
            if (LocalVariables != null && LocalVariables.Variables.Count > 0)
                isFat = true;
            if (ExceptionHandlingClauses.Count > 0 || MaxStack > 8)
                isFat = true;

            if (isFat)
            {
                var flags = CorILMethod.FatFormat;
                if (IsInitLocals)
                    flags |= CorILMethod.InitLocals;
                if (ExceptionHandlingClauses.Count > 0)
                    flags |= CorILMethod.MoreSects;
                var firstWord = (ushort)((uint)flags | (3 << 12));
                writer.WriteUInt16(firstWord);
                writer.WriteUInt16((ushort)MaxStack);
                writer.WriteUInt32((uint)bodySize);
                LocalVarRef.PersistIndexes(ctxt);
                LocalVarRef.Write(ctxt, writer);
            }
            else
            {
                var firstByte = (byte)CorILMethod.TinyFormat;
                firstByte |= (byte)(bodySize << 2);
                writer.WriteByte(firstByte);
            }

            if (Instructions != null && Instructions.Length > 0)
            {
                var beginOffset = writer.Offset;
                for (var i = 0; i < Instructions.Length; i++)
                    Instructions[i].Write(ctxt, writer, beginOffset, findRow);
            }

            if (ExceptionHandlingClauses.Count > 0)
                WriteMethodDataSection(ctxt, writer, false, findRow);
        }

        private void WriteMethodDataSection(WriterContext ctxt, BlobWriter writer, bool isMoreSects, Func<OpCode, object, Row> findRow)
        {
            writer.Align(4);

            var size = ExceptionHandlingClauses.Count * 12 + 4;
            var isFat = size > 0xff || ExceptionHandlingClauses.Any(m => m.IsFat());

            var flags = CorILMethodSect.EHTable;
            if (isFat)
                flags |= CorILMethodSect.FatFormat;
            if (isMoreSects)
                flags |= CorILMethodSect.MoreSects;
            writer.WriteByte((byte)flags);

            if (isFat)
            {
                size = ExceptionHandlingClauses.Count * 24 + 4;
                writer.WriteUInt24((uint)size);
            }
            else
            {
                writer.WriteByte((byte)size);
                writer.WriteUInt16(0);
            }

            for (var i = 0; i < ExceptionHandlingClauses.Count; i++)
                ExceptionHandlingClauses[i].Write(ctxt, writer, isFat, findRow);
        }
    }
}
