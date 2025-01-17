﻿// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using LibreLancer.World.Components;

namespace LibreLancer.Net.Protocol
{
    public interface IPacket
    {
        void WriteContents(PacketWriter outPacket);
    }

    public static class Packets
    {
        static List<Func<PacketReader, object>> parsers = new List<Func<PacketReader,object>>();
        static List<Type> packetTypes = new List<Type>();
        public static void Register<T>(Func<PacketReader,object> parser) where T : IPacket
        {
            packetTypes.Add(typeof(T));
            parsers.Add(parser);
        }

        public static void Write(PacketWriter outPacket, IPacket p)
        {
            var pkt = packetTypes.IndexOf(p.GetType());
            if(pkt == -1) throw new Exception($"Packet type not registered {p.GetType().Name}");
            outPacket.PutVariableUInt32((uint) pkt);
            p.WriteContents(outPacket);
        }

        public static IPacket Read(PacketReader inPacket)
        { 
            return (IPacket)parsers[(int)inPacket.GetVariableUInt32()](inPacket);
        }

#if DEBUG
        public static void CheckRegistered(IPacket p)
        {
            if (p is SPUpdatePacket) return;
            var idx = packetTypes.IndexOf(p.GetType());
            if(idx == -1) throw new Exception($"Packet type not registered {p.GetType().Name}");
        }
#endif
        static Packets()
        {
            //Authentication
            Register<GuidAuthenticationPacket>(GuidAuthenticationPacket.Read);
            Register<AuthenticationReplyPacket>(AuthenticationReplyPacket.Read);
            Register<LoginSuccessPacket>(LoginSuccessPacket.Read);
            //Menu
            Register<OpenCharacterListPacket>(OpenCharacterListPacket.Read);
            Register<NewCharacterDBPacket>(NewCharacterDBPacket.Read);
            Register<AddCharacterPacket>(AddCharacterPacket.Read);
            //Space
            Register<InputUpdatePacket>(InputUpdatePacket.Read);
            Register<PackedUpdatePacket>(PackedUpdatePacket.Read);
            //Protocol
            GeneratedProtocol.RegisterPackets();
            //String Updates (low priority)
            Register<SetStringsPacket>(SetStringsPacket.Read);
            Register<AddStringPacket>(AddStringPacket.Read);
        }
    }

    public class AddStringPacket : IPacket
    {
        public string ToAdd;

        public static AddStringPacket Read(PacketReader message) => new()
        {
            ToAdd = message.GetString()
        };

        public void WriteContents(PacketWriter outPacket)
        {
            outPacket.Put(ToAdd);
        }
    }
    
    public class SetStringsPacket : IPacket
    {
        public byte[] Data;
        public static object Read(PacketReader message)
        {
            return new SetStringsPacket() { Data = message.GetRemainingBytes() };
        }

        public void WriteContents(PacketWriter outPacket)
        {
            outPacket.Put(Data, 0, Data.Length);
        }
    }
    
    public class LoginSuccessPacket : IPacket
    {
        public static object Read(PacketReader message)
        {
            return new LoginSuccessPacket();
        }

        public void WriteContents(PacketWriter outPacket)
        {
        }
    }
    public class GuidAuthenticationPacket : IPacket
    {
        public static GuidAuthenticationPacket Read(PacketReader message)
        {
            return new GuidAuthenticationPacket() { };
        }
        public void WriteContents(PacketWriter outPacket)
        {
        }
    }

    public class AuthenticationReplyPacket : IPacket
    {
        public Guid Guid;
        public static AuthenticationReplyPacket Read(PacketReader message)
        {
            return new AuthenticationReplyPacket() { Guid = message.GetGuid() };
        }
        public void WriteContents(PacketWriter outPacket)
        {
            outPacket.Put(Guid);
        }

    }
    
    public class SolarInfo
    {
        public int ID;
        public string Archetype;
        public Vector3 Position;
        public Quaternion Orientation;
        public static SolarInfo Read(PacketReader message)
        {
            return new SolarInfo
            {
                ID = message.GetInt(),
                Archetype = message.GetString(),
                Position = message.GetVector3(),
                Orientation = message.GetQuaternion()
            };
        }
        public void Put(PacketWriter message)
        {
            message.Put(ID);
            message.Put(Archetype);
            message.Put(Position);
            message.Put(Orientation);
        }
    }

    public class NetShipCargo
    {
        public int ID;
        public uint EquipCRC;
        public string Hardpoint;
        public byte Health;
        public int Count;
        
        public NetShipCargo(int id, uint crc, string hp, byte health, int count)
        {
            ID = id;
            EquipCRC = crc;
            Hardpoint = hp;
            Health = health;
            Count = count;
        }
    }

    public class NetShipLoadout
    {
        public uint ShipCRC;
        public float Health;
        public List<NetShipCargo> Items;
        public static NetShipLoadout Read(PacketReader message)
        {
            var s = new NetShipLoadout();
            s.ShipCRC = message.GetUInt();
            s.Health = message.GetFloat();
            var cargoCount = (int)message.GetVariableUInt32();
            s.Items = new List<NetShipCargo>(cargoCount);
            for (int i = 0; i < cargoCount; i++)
            {
                s.Items.Add(new NetShipCargo(
                    message.GetVariableInt32(), 
                    message.GetUInt(), 
                    message.GetHpid(), 
                    message.GetByte(), 
                    (int)message.GetVariableUInt32()
                    ));
            }
            return s;
        }
        public void Put(PacketWriter message)
        {
            message.Put(ShipCRC);
            message.Put(Health);
            message.PutVariableUInt32((uint) Items.Count);
            foreach (var c in Items)
            {
                message.PutVariableInt32(c.ID);
                message.Put(c.EquipCRC);
                message.PutHpid(c.Hardpoint);
                message.Put(c.Health);
                message.PutVariableUInt32((uint)c.Count);
            }
        }
    }

    public struct PlayerAuthState
    {
        public Vector3 Position;
        public Quaternion Orientation;
        public Vector3 LinearVelocity;
        public Vector3 AngularVelocity;
        
        public float Health;
        public float Shield;
        public float CruiseChargePct;
        public float CruiseAccelPct;
        public static PlayerAuthState Read(ref BitReader reader, PlayerAuthState src)
        {
            var pa = new PlayerAuthState();
            pa.Position = reader.GetVector3();
            //Extra precision
            pa.Orientation = reader.GetQuaternion(18);
            pa.LinearVelocity = reader.GetVector3();
            pa.AngularVelocity = reader.GetVector3();
            pa.Health = reader.GetBool() ? reader.GetFloat() : src.Health;
            pa.Shield = reader.GetBool() ? reader.GetFloat() : src.Shield;
            pa.CruiseChargePct = reader.GetBool() ? reader.GetRangedFloat(0, 1, 12) : src.CruiseChargePct;
            pa.CruiseAccelPct = reader.GetBool() ? reader.GetRangedFloat(0, 1, 12) : src.CruiseAccelPct;
            return pa;
        }

        [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
        public void Write(ref BitWriter writer, PlayerAuthState prev)
        {
            writer.PutVector3(Position);
            //Extra precision
            writer.PutQuaternion(Orientation, 18);
            writer.PutVector3(LinearVelocity);
            writer.PutVector3(AngularVelocity);
            
            if (Health == prev.Health) {
                writer.PutBool(false);
            }
            else {
                writer.PutBool(true);
                writer.PutFloat(Health);
            }
            if (Shield == prev.Shield) {
                writer.PutBool(false);
            }
            else {
                writer.PutBool(true);
                writer.PutFloat(Shield);
            }
            if(NetPacking.QuantizedEqual(CruiseChargePct, prev.CruiseChargePct, 0, 1, 12))
                writer.PutBool(false);
            else
            {
                writer.PutBool(true);
                writer.PutRangedFloat(CruiseChargePct, 0, 1, 12);
            }
            if(NetPacking.QuantizedEqual(CruiseAccelPct, prev.CruiseAccelPct, 0, 1, 12))
                writer.PutBool(false);
            else
            {
                writer.PutBool(true);
                writer.PutRangedFloat(CruiseAccelPct, 0, 1, 12);
            }
        }
    }

    public struct NetInputControls
    {
        public int Sequence;
        public Vector3 Steering;
        public StrafeControls Strafe;
        public float Throttle;
        public bool Cruise;
        public bool Thrust;    
    }
    
    public class InputUpdatePacket : IPacket
    {
        public uint AckTick;
        public NetInputControls Current;
        public NetInputControls HistoryA;
        public NetInputControls HistoryB;
        public NetInputControls HistoryC;

        [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
        static void WriteDelta(ref BitWriter writer, ref NetInputControls baseline, ref NetInputControls cur)
        {
            writer.PutVarInt32(cur.Sequence - baseline.Sequence);
            writer.PutUInt((uint)cur.Strafe, 4);
            writer.PutBool(cur.Cruise);
            writer.PutBool(cur.Thrust);
            writer.PutBool(cur.Throttle != baseline.Throttle);
            writer.PutBool(cur.Steering != baseline.Steering);
            if(cur.Throttle != baseline.Throttle)
                writer.PutFloat(cur.Throttle);
            if(cur.Steering != baseline.Steering)
                writer.PutVector3(cur.Steering);
        }
        
        static NetInputControls ReadDelta(ref BitReader reader, ref NetInputControls baseline)
        {
            var nc = new NetInputControls();
            nc.Sequence = baseline.Sequence + reader.GetVarInt32();
            nc.Strafe = (StrafeControls)reader.GetUInt(4);
            nc.Cruise = reader.GetBool();
            nc.Thrust = reader.GetBool();
            bool readThrottle = reader.GetBool();
            bool readSteering = reader.GetBool();
            nc.Throttle = readThrottle ? reader.GetFloat() : baseline.Throttle;
            nc.Steering = readSteering ? reader.GetVector3() : baseline.Steering;
            return nc;
        }

        public static object Read(PacketReader message)
        {
            var br = new BitReader(message.GetRemainingBytes(), 0);
            var p = new InputUpdatePacket();
            p.AckTick = br.GetVarUInt32();
            p.Current.Sequence = br.GetVarInt32();
            p.Current.Steering = br.GetVector3();
            p.Current.Strafe = (StrafeControls) br.GetUInt(4);
            p.Current.Cruise = br.GetBool();
            p.Current.Thrust = br.GetBool();
            var throttle = br.GetUInt(2);
            if (throttle == 0) p.Current.Throttle = 0;
            else if (throttle == 1) p.Current.Throttle = 1;
            else {
                p.Current.Throttle = br.GetFloat();
            }
            p.HistoryA = ReadDelta(ref br, ref p.Current);
            p.HistoryB = ReadDelta(ref br, ref p.HistoryA);
            p.HistoryC = ReadDelta(ref br, ref p.HistoryB);
            return p;
        }
        
        [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
        public void WriteContents(PacketWriter outPacket)
        {
            var bw = new BitWriter();
            bw.PutVarUInt32(AckTick);
            bw.PutVarInt32(Current.Sequence);
            bw.PutVector3(Current.Steering);
            bw.PutUInt((uint)Current.Strafe, 4);
            bw.PutBool(Current.Cruise);
            bw.PutBool(Current.Thrust);
            if (Current.Throttle == 0){
                bw.PutUInt(0, 2);
            } else if (Current.Throttle >= 1){
                bw.PutUInt(1,2);
            }else {
                bw.PutUInt(2, 2);
                bw.PutFloat(Current.Throttle);
            }
            
            WriteDelta(ref bw, ref Current, ref HistoryA);
            WriteDelta(ref bw, ref HistoryA, ref HistoryB);
            WriteDelta(ref bw, ref HistoryB, ref HistoryC);
            bw.WriteToPacket(outPacket);
        }
    }
    

    public class NetDlgLine
    {
        public string Voice;
        public uint Hash;
        public static NetDlgLine Read(PacketReader message) => new NetDlgLine()
            {Voice = message.GetString(), Hash = message.GetUInt()};
        public void Put(PacketWriter message)
        {
            message.Put(Voice);
            message.Put(Hash);
        }
    }
}