// Copyright (c) 2009 http://grommet.codeplex.com
// This source is subject to the Microsoft Public License.
// See http://www.opensource.org/licenses/ms-pl.html
// All other rights reserved.

using System;

namespace STLK
{

    public enum ApiFrameName : byte
    {
        ModemStatus = 0x8A,
        ATCommand = 0x08,
        ATCommandQueueParameterValue = 0x09,
        ATCommandResponse = 0x88,
        RemoteCommandRequest = 0x17,
        RemoteCommandResponse = 0x97,
        ZigBeeTransmitRequest = 0x10,
        ExplicitAddressingZigBee = 0x11,
        ZigBeeTransmitStatus = 0x8B,
        ZigBeeReceivePacket = 0x90,
        ZigBeeExplicitRxIndicator = 0x91,
        ZigBeeIODataSampleRxIndicator = 0x92,
        XBeeSensorReadIndicator = 0x94,
        NodeIdentificationIndicator = 0x95,
    };

    public enum XBeeCommand : ushort
    {
        // Special
        Write = 0x5752,                     // WR
        RestoreDefaults = 0x5245,           // RE
        SoftwareReset = 0x4652,             // FR
        NetworkReset = 0x4E52,              // NR
        // Addressing
        Address = 0x4D59,                   // MY
        ParentAddress = 0x4D50,             // MP
        NumberOfRemainingChildrean = 0x4E43,// NC
        SerialNumberHigh = 0x5348,          // SH
        SerialNumberLow = 0x534C,           // SL
        NodeIdentifier = 0x4E49,            // NI
        DeviceTypeIdentifier = 0x4444,      // DD
        MaximumRFPayloadBytes = 0x4E50,     // NP
        // Networking
        OperatingChannel = 0x4348,          // CH
        ExtendedPanID = 0x4944,             // ID
        OperatingExtendedPanID = 0x4F50,    // OP
        MaximumUnicastHops = 0x4E48,        // NH
        BroadcastHops = 0x4248,             // BH
        OperatingPanID = 0x4F49,            // OI
        NodeDiscoverTimeout = 0x4E54,       // NT
        NetworkDiscoveryOptions = 0x4E4F,   // NO
        NodeDiscover = 0x4E44,              // ND
        DestinationNode = 0x444E,           // DN
        ScanChannels = 0x5343,              // SC
        ScanDuration = 0x5344,              // SD
        ZigBeeStackProfile = 0x5A53,        // ZS
        NodeJoinTime = 0x4E4A,              // NJ
        ChannelVerification = 0x4A56,       // JV
        JoinNotification = 0x4A4E,          // JN
        AggregateRoutingNotification = 0x4152,// AR
        AssociationIndication = 0x4149,     // AI
        // Security
        EncryptionEnable = 0x4545,          // EE
        EncryptionOptions = 0x454F,         // EO
        EncryptionKey = 0x4E4B,             // NK
        LinkKey = 0x4B59,                   // KY
        // RF Interfacing
        PowerLevel = 0x504C,                // PL
        PowerMode = 0x504D,                 // PM
        ReceivedSignalStrength = 0x4442,    // DB
        // Serial Interfacing (I/O)
        ApiEnable = 0x4150,                 // AP
        ApiOptions = 0x414F,                // AO
        InterfaceDataRate = 0x4244,         // BD
        SerialParity = 0x4E42,              // NB
        PacketizationTimeout = 0x524F,      // RO
        Dio7Configuration = 0x4437,         // D7
        Dio6Configuration = 0x4436,         // D6
        // I/O Commands
        ForceSample = 0x4953,               // IS
        XBeeSensorSample = 0x3153,          // 1S
        IOSampleRate = 0x4952,              // IR
        IODigitalChangeDirection = 0x4943,  // IC
        Pwm0Configuration = 0x4C30,         // P0
        Dio11Configuration = 0x4C31,        // P1
        Dio12Configuration = 0x4C32,        // P2
        Dio13Configuration = 0x4C33,        // P3
        ADio0Configuration = 0x4430,        // D0
        ADio1Configuration = 0x4431,        // D1
        ADio2Configuration = 0x4432,        // D2
        ADio3Configuration = 0x4433,        // D3
        Dio4Configuration = 0x4434,         // D4
        Dio5Configuration = 0x4435,         // D5
        AssociateLedBlinkTime = 0x4C54,     // LT
        Dio8Configuration = 0x4438,         // D8
        PullUpRegister = 0x4C52,            // PR
        RssiPwmTimer = 0x524C,              // RP
        CommissioningPushButton = 0x4342,   // CB
        // Diagnostics
        FirmwareVersion = 0x5652,           // VR
        HardwareVersion = 0x4856,           // HV
        SupplyVoltage = 0x2556,             // %V
        // Sleep Commands
        SleepMode = 0x534D,                 // SM
        NumberOfSleepPeriods = 0x534E,      // SN
        SleepPeriod = 0x534C,               // SP
        TimeBeforeSleep = 0x5354,           // ST
        SleepOptions = 0x534F,              // SO
    };

    public enum DeliveryStatus : byte
    {
        Success = 0x00,
        ClearChannelAssessmentFailure = 0x02,
        InvalidDestinationEndpoint = 0x15,
        NetworkAckFailure = 0x21,
        NotJoinedToNetwork = 0x22,
        SelfAddresses = 0x23,
        AddressNotFound = 0x24,
        RouteNotFound = 0x25
    };

    public enum DiscoveryStatus : byte
    {
        NoDiscoveryOverhead = 0x00,
        AddressDiscovery = 0x01,
        RouteDiscovery = 0x02,
        AddressAndRouteDiscovery = 0x03
    };

    //public enum DeviceType : byte
    //{
    //    Coordinator = 0,
    //    Router,
    //    EndDevice,
    //};

    //public enum ModemStatus : byte
    //{
    //    HardwareReset = 0,
    //    WatchdogTimerReset,
    //    Associated,
    //    Disassociated,
    //    SynchronizationLost,
    //    CoordinatorRealignment,
    //    CoordinatorStarted
    //};

    //public enum PacketOptions : byte
    //{
    //    None = 0,
    //    Acknowledged = 1,
    //    Broadcast = 2
    //};

}
