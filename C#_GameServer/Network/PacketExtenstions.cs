using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Protocol;

public interface _IPacket
{
    _PacketID PacketId { get; }
}

namespace Protocol
{
    public partial class PlayerMoveRequestProto : _IPacket
    {
        public _PacketID PacketId => _PacketID.PlayerMoveRequestId;
    }

    public partial class PlayerMoveResponseProto : _IPacket
    {
        public _PacketID PacketId => _PacketID.PlayerMoveResponseId;
    }

    public partial class PlayerMoveListResponseProto : _IPacket
    {
        public _PacketID PacketId => _PacketID.PlayerMoveListResponseId;
    }

    public partial class TimeSyncRequestProto : _IPacket
    {
        public _PacketID PacketId => _PacketID.TimeSyncRequestId;
    }

    public partial class TimeSyncResponseProto : _IPacket
    {
        public _PacketID PacketId => _PacketID.TimeSyncResponseId;
    }

    //PlayerMoveRequest = 11,
    //PlayerMoveResponse = 12,
    //TimeSyncRequest = 900,
    //TimeSyncResponse = 901
}
