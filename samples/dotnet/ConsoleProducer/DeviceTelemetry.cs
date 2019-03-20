using System;
namespace ConsoleProducer
{
    /// <summary>
    /// Proto buff implementation of Device telemetry
    /// </summary>
    /// <remarks>
    /// This is the proto buff specification:
    /// syntax = "proto3";
    /// 
    /// message DeviceTelemetry
    /// {
    ///     string DeviceID = 1;
    ///     float Temperature = 2;
    ///     float Humidity = 3;
    ///     int64 Time = 4; 
    /// }
    /// </remarks>
    #pragma warning disable CS1591, CS0612, CS3021, IDE1006
    [global::ProtoBuf.ProtoContract()]
    public partial class DeviceTelemetry : global::ProtoBuf.IExtensible
    {
        private global::ProtoBuf.IExtension __pbn__extensionData;
        global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
            => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

        [global::ProtoBuf.ProtoMember(1)]
        [global::System.ComponentModel.DefaultValue("")]
        public string DeviceID { get; set; } = "";

        [global::ProtoBuf.ProtoMember(2)]
        public float Temperature { get; set; }

        [global::ProtoBuf.ProtoMember(3)]
        public float Humidity { get; set; }

        [global::ProtoBuf.ProtoMember(4)]
        public long Time { get; set; }

    }

    #pragma warning restore CS1591, CS0612, CS3021, IDE1006
}
