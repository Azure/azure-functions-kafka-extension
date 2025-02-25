﻿// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: user.proto
// </auto-generated>
#pragma warning disable 1591, 0612, 3021
#region Designer generated code

using PB = global::Google.Protobuf;
using PBR = global::Google.Protobuf.Reflection;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.EndToEndTests
{

    /// <summary>Holder for reflection information generated from user.proto</summary>
    public static partial class ProtoUserReflection
    {

        #region Descriptor
        /// <summary>File descriptor for user.proto</summary>
        public static PBR::FileDescriptor Descriptor
        {
            get { return descriptor; }
        }
        private static PBR::FileDescriptor descriptor;

        static ProtoUserReflection()
        {
            byte[] descriptorData = global::System.Convert.FromBase64String(
                string.Concat(
                  "Cgp1c2VyLnByb3RvEiFjb25mbHVlbnQua2Fma2EuZXhhbXBsZXMucHJvdG9i",
                  "dWYiQwoEVXNlchIMCgROYW1lGAEgASgJEhYKDkZhdm9yaXRlTnVtYmVyGAIg",
                  "ASgFEhUKDUZhdm9yaXRlQ29sb3IYAyABKAliBnByb3RvMw=="));
            descriptor = PBR::FileDescriptor.FromGeneratedCode(descriptorData,
                new PBR::FileDescriptor[] { },
                new PBR::GeneratedClrTypeInfo(null, new PBR::GeneratedClrTypeInfo[] {
            new PBR::GeneratedClrTypeInfo(typeof(global::Microsoft.Azure.WebJobs.Extensions.Kafka.EndToEndTests.ProtoUser), global::Microsoft.Azure.WebJobs.Extensions.Kafka.EndToEndTests.ProtoUser.Parser, new[]{ "Name", "FavoriteNumber", "FavoriteColor" }, null, null, null)
                }));
        }
        #endregion

    }
    #region Messages
    public sealed partial class ProtoUser : PB::IMessage<ProtoUser>
    {
        private static readonly PB::MessageParser<ProtoUser> _parser = new PB::MessageParser<ProtoUser>(() => new ProtoUser());
        private PB::UnknownFieldSet _unknownFields;
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static PB::MessageParser<ProtoUser> Parser { get { return _parser; } }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static PBR::MessageDescriptor Descriptor
        {
            get { return global::Microsoft.Azure.WebJobs.Extensions.Kafka.EndToEndTests.ProtoUserReflection.Descriptor.MessageTypes[0]; }
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        PBR::MessageDescriptor PB::IMessage.Descriptor
        {
            get { return Descriptor; }
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public ProtoUser()
        {
            OnConstruction();
        }

        partial void OnConstruction();

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public ProtoUser(ProtoUser other) : this()
        {
            name_ = other.name_;
            favoriteNumber_ = other.favoriteNumber_;
            favoriteColor_ = other.favoriteColor_;
            _unknownFields = PB::UnknownFieldSet.Clone(other._unknownFields);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public ProtoUser Clone()
        {
            return new ProtoUser(this);
        }

        /// <summary>Field number for the "Name" field.</summary>
        public const int NameFieldNumber = 1;
        private string name_ = "";
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public string Name
        {
            get { return name_; }
            set
            {
                name_ = PB::ProtoPreconditions.CheckNotNull(value, "value");
            }
        }

        /// <summary>Field number for the "FavoriteNumber" field.</summary>
        public const int FavoriteNumberFieldNumber = 2;
        private int favoriteNumber_;
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public int FavoriteNumber
        {
            get { return favoriteNumber_; }
            set
            {
                favoriteNumber_ = value;
            }
        }

        /// <summary>Field number for the "FavoriteColor" field.</summary>
        public const int FavoriteColorFieldNumber = 3;
        private string favoriteColor_ = "";
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public string FavoriteColor
        {
            get { return favoriteColor_; }
            set
            {
                favoriteColor_ = PB::ProtoPreconditions.CheckNotNull(value, "value");
            }
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public override bool Equals(object other)
        {
            return Equals(other as ProtoUser);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public bool Equals(ProtoUser other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }
            if (ReferenceEquals(other, this))
            {
                return true;
            }
            if (Name != other.Name) return false;
            if (FavoriteNumber != other.FavoriteNumber) return false;
            if (FavoriteColor != other.FavoriteColor) return false;
            return Equals(_unknownFields, other._unknownFields);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public override int GetHashCode()
        {
            int hash = 1;
            if (Name.Length != 0) hash ^= Name.GetHashCode();
            if (FavoriteNumber != 0) hash ^= FavoriteNumber.GetHashCode();
            if (FavoriteColor.Length != 0) hash ^= FavoriteColor.GetHashCode();
            if (_unknownFields != null)
            {
                hash ^= _unknownFields.GetHashCode();
            }
            return hash;
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public override string ToString()
        {
            return PB::JsonFormatter.ToDiagnosticString(this);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public void WriteTo(PB::CodedOutputStream output)
        {
            if (Name.Length != 0)
            {
                output.WriteRawTag(10);
                output.WriteString(Name);
            }
            if (FavoriteNumber != 0)
            {
                output.WriteRawTag(16);
                output.WriteInt32(FavoriteNumber);
            }
            if (FavoriteColor.Length != 0)
            {
                output.WriteRawTag(26);
                output.WriteString(FavoriteColor);
            }
            if (_unknownFields != null)
            {
                _unknownFields.WriteTo(output);
            }
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public int CalculateSize()
        {
            int size = 0;
            if (Name.Length != 0)
            {
                size += 1 + PB::CodedOutputStream.ComputeStringSize(Name);
            }
            if (FavoriteNumber != 0)
            {
                size += 1 + PB::CodedOutputStream.ComputeInt32Size(FavoriteNumber);
            }
            if (FavoriteColor.Length != 0)
            {
                size += 1 + PB::CodedOutputStream.ComputeStringSize(FavoriteColor);
            }
            if (_unknownFields != null)
            {
                size += _unknownFields.CalculateSize();
            }
            return size;
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public void MergeFrom(ProtoUser other)
        {
            if (other == null)
            {
                return;
            }
            if (other.Name.Length != 0)
            {
                Name = other.Name;
            }
            if (other.FavoriteNumber != 0)
            {
                FavoriteNumber = other.FavoriteNumber;
            }
            if (other.FavoriteColor.Length != 0)
            {
                FavoriteColor = other.FavoriteColor;
            }
            _unknownFields = PB::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public void MergeFrom(PB::CodedInputStream input)
        {
            uint tag;
            while ((tag = input.ReadTag()) != 0)
            {
                switch (tag)
                {
                    default:
                        _unknownFields = PB::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
                        break;
                    case 10:
                        {
                            Name = input.ReadString();
                            break;
                        }
                    case 16:
                        {
                            FavoriteNumber = input.ReadInt32();
                            break;
                        }
                    case 26:
                        {
                            FavoriteColor = input.ReadString();
                            break;
                        }
                }
            }
        }

    }

    #endregion

}

#endregion Designer generated code