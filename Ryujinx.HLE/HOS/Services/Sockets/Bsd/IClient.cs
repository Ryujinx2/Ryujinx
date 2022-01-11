﻿using Ryujinx.Common;
using Ryujinx.Common.Logging;
using Ryujinx.Memory;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;

namespace Ryujinx.HLE.HOS.Services.Sockets.Bsd
{
    [Service("bsd:s", true)]
    [Service("bsd:u", false)]
    class IClient : IpcService
    {
        private BsdContext _context;
        private bool _isPrivileged;

        public IClient(ServiceCtx context, bool isPrivileged) : base(context.Device.System.BsdServer)
        {
            _isPrivileged = isPrivileged;
        }

        private ResultCode WriteBsdResult(ServiceCtx context, int result, LinuxError errorCode = LinuxError.SUCCESS)
        {
            if (errorCode != LinuxError.SUCCESS)
            {
                result = -1;
            }

            context.ResponseData.Write(result);
            context.ResponseData.Write((int)errorCode);

            return ResultCode.Success;
        }

        private static AddressFamily ConvertBsdAddressFamily(BsdAddressFamily family)
        {
            switch (family)
            {
                case BsdAddressFamily.Unspecified:
                    return AddressFamily.Unspecified;
                case BsdAddressFamily.InterNetwork:
                    return AddressFamily.InterNetwork;
                case BsdAddressFamily.InterNetworkV6:
                    return AddressFamily.InterNetworkV6;
                case BsdAddressFamily.Unknown:
                    return AddressFamily.Unknown;
                default:
                    throw new NotImplementedException(family.ToString());
            }
        }

        private LinuxError SetResultErrno(IBsdSocket socket, int result)
        {
            return result == 0 && !socket.Blocking ? LinuxError.EWOULDBLOCK : LinuxError.SUCCESS;
        }

        private ResultCode SocketInternal(ServiceCtx context, bool exempt)
        {
            BsdAddressFamily domain   = (BsdAddressFamily)context.RequestData.ReadInt32();
            SocketType       type     = (SocketType)context.RequestData.ReadInt32();
            ProtocolType     protocol = (ProtocolType)context.RequestData.ReadInt32();

            if (domain == BsdAddressFamily.Unknown)
            {
                return WriteBsdResult(context, -1, LinuxError.EPROTONOSUPPORT);
            }
            else if ((type == SocketType.Seqpacket || type == SocketType.Raw) && !_isPrivileged)
            {
                if (domain != BsdAddressFamily.InterNetwork || type != SocketType.Raw || protocol != ProtocolType.Icmp)
                {
                    return WriteBsdResult(context, -1, LinuxError.ENOENT);
                }
            }

            AddressFamily netDomain = ConvertBsdAddressFamily(domain);

            if (protocol == ProtocolType.IP)
            {
                if (type == SocketType.Stream)
                {
                    protocol = ProtocolType.Tcp;
                }
                else if (type == SocketType.Dgram)
                {
                    protocol = ProtocolType.Udp;
                }
            }

            BsdSocket newBsdSocket = new BsdSocket
            {
                Family   = (int)domain,
                Type     = (int)type,
                Protocol = (int)protocol,
                Handle   = new ManagedSocket(netDomain, type, protocol),
                Refcount = 1
            };

            LinuxError errno = LinuxError.SUCCESS;

            int newSockFd = _context.RegisterSocket(newBsdSocket);

            if (newSockFd == -1)
            {
                errno = LinuxError.EBADF;
            }

            if (exempt)
            {
                newBsdSocket.Handle.Disconnect();
            }

            return WriteBsdResult(context, newSockFd, errno);
        }

        private void WriteSockAddr(ServiceCtx context, ulong bufferPosition, BsdSocket socket, bool isRemote)
        {
            IPEndPoint endPoint = isRemote ? socket.Handle.RemoteEndPoint : socket.Handle.LocalEndPoint;

            context.Memory.Write(bufferPosition, BsdSockAddr.FromIPEndPoint(endPoint));
        }

        [CommandHipc(0)]
        // Initialize(nn::socket::BsdBufferConfig config, u64 pid, u64 transferMemorySize, KObject<copy, transfer_memory>, pid) -> u32 bsd_errno
        public ResultCode RegisterClient(ServiceCtx context)
        {
            _context = BsdContext.GetOrRegister(context.Request.HandleDesc.PId);

            /*
            typedef struct  {
                u32 version;                // Observed 1 on 2.0 LibAppletWeb, 2 on 3.0.
                u32 tcp_tx_buf_size;        // Size of the TCP transfer (send) buffer (initial or fixed).
                u32 tcp_rx_buf_size;        // Size of the TCP recieve buffer (initial or fixed).
                u32 tcp_tx_buf_max_size;    // Maximum size of the TCP transfer (send) buffer. If it is 0, the size of the buffer is fixed to its initial value.
                u32 tcp_rx_buf_max_size;    // Maximum size of the TCP receive buffer. If it is 0, the size of the buffer is fixed to its initial value.
                u32 udp_tx_buf_size;        // Size of the UDP transfer (send) buffer (typically 0x2400 bytes).
                u32 udp_rx_buf_size;        // Size of the UDP receive buffer (typically 0xA500 bytes).
                u32 sb_efficiency;          // Number of buffers for each socket (standard values range from 1 to 8).
            } BsdBufferConfig;
            */

            // bsd_error
            context.ResponseData.Write(0);

            Logger.Stub?.PrintStub(LogClass.ServiceBsd);

            // Close transfer memory immediately as we don't use it.
            context.Device.System.KernelContext.Syscall.CloseHandle(context.Request.HandleDesc.ToCopy[0]);

            return ResultCode.Success;
        }

        [CommandHipc(1)]
        // StartMonitoring(u64, pid)
        public ResultCode StartMonitoring(ServiceCtx context)
        {
            ulong unknown0 = context.RequestData.ReadUInt64();

            Logger.Stub?.PrintStub(LogClass.ServiceBsd, new { unknown0 });

            return ResultCode.Success;
        }

        [CommandHipc(2)]
        // Socket(u32 domain, u32 type, u32 protocol) -> (i32 ret, u32 bsd_errno)
        public ResultCode Socket(ServiceCtx context)
        {
            return SocketInternal(context, false);
        }

        [CommandHipc(3)]
        // SocketExempt(u32 domain, u32 type, u32 protocol) -> (i32 ret, u32 bsd_errno)
        public ResultCode SocketExempt(ServiceCtx context)
        {
            return SocketInternal(context, true);
        }

        [CommandHipc(4)]
        // Open(u32 flags, array<unknown, 0x21> path) -> (i32 ret, u32 bsd_errno)
        public ResultCode Open(ServiceCtx context)
        {
            (ulong bufferPosition, ulong bufferSize) = context.Request.GetBufferType0x21();

            int flags = context.RequestData.ReadInt32();

            byte[] rawPath = new byte[bufferSize];

            context.Memory.Read(bufferPosition, rawPath);

            string path = Encoding.ASCII.GetString(rawPath);

            WriteBsdResult(context, -1, LinuxError.EOPNOTSUPP);

            Logger.Stub?.PrintStub(LogClass.ServiceBsd, new { path, flags });

            return ResultCode.Success;
        }

        [CommandHipc(5)]
        // Select(u32 nfds, nn::socket::timeout timeout, buffer<nn::socket::fd_set, 0x21, 0> readfds_in, buffer<nn::socket::fd_set, 0x21, 0> writefds_in, buffer<nn::socket::fd_set, 0x21, 0> errorfds_in) -> (i32 ret, u32 bsd_errno, buffer<nn::socket::fd_set, 0x22, 0> readfds_out, buffer<nn::socket::fd_set, 0x22, 0> writefds_out, buffer<nn::socket::fd_set, 0x22, 0> errorfds_out)
        public ResultCode Select(ServiceCtx context)
        {
            WriteBsdResult(context, -1, LinuxError.EOPNOTSUPP);

            Logger.Stub?.PrintStub(LogClass.ServiceBsd);

            return ResultCode.Success;
        }

        [CommandHipc(6)]
        // Poll(u32 nfds, u32 timeout, buffer<unknown, 0x21, 0> fds) -> (i32 ret, u32 bsd_errno, buffer<unknown, 0x22, 0>)
        public ResultCode Poll(ServiceCtx context)
        {
            int fdsCount = context.RequestData.ReadInt32();
            int timeout  = context.RequestData.ReadInt32();

            (ulong bufferPosition, ulong bufferSize) = context.Request.GetBufferType0x21();


            if (timeout < -1 || fdsCount < 0 || (ulong)(fdsCount * 8) > bufferSize)
            {
                return WriteBsdResult(context, -1, LinuxError.EINVAL);
            }

            PollEvent[] events = new PollEvent[fdsCount];

            for (int i = 0; i < fdsCount; i++)
            {
                PollEventData pollEventData = context.Memory.Read<PollEventData>(bufferPosition + (ulong)(i * Unsafe.SizeOf<PollEventData>()));

                IBsdSocket socket = _context.RetrieveSocket(pollEventData.SocketFd);

                if (socket == null)
                {
                    return WriteBsdResult(context, -1, LinuxError.EBADF);
                }

                events[i] = new PollEvent(pollEventData, socket);
            }

            List<PollEvent> managedSockets = new List<PollEvent>();
            List<PollEvent> eventFdSockets = new List<PollEvent>();

            foreach (PollEvent evnt in events)
            {
                IBsdSocket sock = evnt.Socket;

                if (sock is BsdSocket bsdSocket)
                {
                    if (bsdSocket.Handle is not ManagedSocket)
                    {
                        Logger.Error?.Print(LogClass.ServiceBsd, $"Poll operation is only supported on {typeof(ManagedSocket).Name} at present, skipping");

                        continue;
                    }

                    managedSockets.Add(evnt);
                }
                else if (sock is EventSocket)
                {
                    eventFdSockets.Add(evnt);
                }
                else
                {
                    Logger.Error?.Print(LogClass.ServiceBsd, $"Poll operation is only supported on {sock.GetType().Name}, returning");

                    return WriteBsdResult(context, -1, LinuxError.EBADF);
                }
            }

            int updateCount = 0;

            LinuxError errno = LinuxError.SUCCESS;

            if (fdsCount != 0)
            {
                if (managedSockets.Count != 0)
                {
                    if (eventFdSockets.Count == 0)
                    {
                        errno = ManagedSocket.Poll(managedSockets, timeout, out updateCount);
                    }
                    else
                    {
                        bool IsUnexpectedLinuxError(LinuxError error)
                        {
                            return errno != LinuxError.SUCCESS && errno != LinuxError.ETIMEDOUT;
                        }

                        // Hybrid approach
                        long budgetLeftMilliseconds;

                        if (timeout == -1)
                        {
                            budgetLeftMilliseconds = PerformanceCounter.ElapsedMilliseconds + uint.MaxValue;
                        }
                        else
                        {
                            budgetLeftMilliseconds = PerformanceCounter.ElapsedMilliseconds + timeout;
                        }

                        while (PerformanceCounter.ElapsedMilliseconds < budgetLeftMilliseconds)
                        {
                            // First try to poll event sockets
                            errno = EventSocket.Poll(eventFdSockets, 0, out updateCount);

                            if (IsUnexpectedLinuxError(errno))
                            {
                                break;
                            }

                            if (updateCount > 0)
                            {
                                break;
                            }

                            // Then try managed sockets
                            errno = ManagedSocket.Poll(managedSockets, timeout, out updateCount);

                            if (IsUnexpectedLinuxError(errno))
                            {
                                break;
                            }

                            if (updateCount > 0)
                            {
                                break;
                            }

                            // If we are here, that mean nothing was availaible, sleep for 50ms
                            context.Device.System.KernelContext.Syscall.SleepThread(50 * 1000000);
                        }
                    }
                }
                else if (eventFdSockets.Count != 0)
                {
                    // Only poll event fds
                    errno = EventSocket.Poll(eventFdSockets, timeout, out updateCount);
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
            else if (timeout == -1)
            {
                // FIXME: If we get a timeout of -1 and there is no fds to wait on, this should kill the KProces. (need to check that with re)
                throw new InvalidOperationException();
            }
            else
            {
                context.Device.System.KernelContext.Syscall.SleepThread(timeout);
            }

            // TODO: Spanify
            for (int i = 0; i < fdsCount; i++)
            {
                context.Memory.Write(bufferPosition + (ulong)i * 8, events[i].Data);
            }

            return WriteBsdResult(context, updateCount, errno);
        }

        [CommandHipc(7)]
        // Sysctl(buffer<unknown, 0x21, 0>, buffer<unknown, 0x21, 0>) -> (i32 ret, u32 bsd_errno, u32, buffer<unknown, 0x22, 0>)
        public ResultCode Sysctl(ServiceCtx context)
        {
            WriteBsdResult(context, -1, LinuxError.EOPNOTSUPP);

            Logger.Stub?.PrintStub(LogClass.ServiceBsd);

            return ResultCode.Success;
        }

        [CommandHipc(8)]
        // Recv(u32 socket, u32 flags) -> (i32 ret, u32 bsd_errno, array<i8, 0x22> message)
        public ResultCode Recv(ServiceCtx context)
        {
            int            socketFd    = context.RequestData.ReadInt32();
            BsdSocketFlags socketFlags = (BsdSocketFlags)context.RequestData.ReadInt32();

            (ulong receivePosition, ulong receiveLength) = context.Request.GetBufferType0x22();

            WritableRegion receiveRegion = context.Memory.GetWritableRegion(receivePosition, (int)receiveLength);

            LinuxError errno  = LinuxError.EBADF;
            BsdSocket  socket = _context.RetrieveBsdSocket(socketFd);
            int        result = -1;

            if (socket != null)
            {
                errno = socket.Handle.Receive(out result, receiveRegion.Memory.Span, socketFlags);

                if (errno == LinuxError.SUCCESS)
                {
                    SetResultErrno(socket, result);

                    receiveRegion.Dispose();
                }
            }

            return WriteBsdResult(context, result, errno);
        }

        [CommandHipc(9)]
        // RecvFrom(u32 sock, u32 flags) -> (i32 ret, u32 bsd_errno, u32 addrlen, buffer<i8, 0x22, 0> message, buffer<nn::socket::sockaddr_in, 0x22, 0x10>)
        public ResultCode RecvFrom(ServiceCtx context)
        {
            int            socketFd    = context.RequestData.ReadInt32();
            BsdSocketFlags socketFlags = (BsdSocketFlags)context.RequestData.ReadInt32();

            (ulong receivePosition,     ulong receiveLength)   = context.Request.GetBufferType0x22(0);
            (ulong sockAddrOutPosition, ulong sockAddrOutSize) = context.Request.GetBufferType0x22(1);

            WritableRegion receiveRegion = context.Memory.GetWritableRegion(receivePosition, (int)receiveLength);

            LinuxError errno  = LinuxError.EBADF;
            BsdSocket  socket = _context.RetrieveBsdSocket(socketFd);
            int        result = -1;

            if (socket != null)
            {
                errno = socket.Handle.ReceiveFrom(out result, receiveRegion.Memory.Span, receiveRegion.Memory.Span.Length, socketFlags, out IPEndPoint endPoint);

                if (errno == LinuxError.SUCCESS)
                {
                    SetResultErrno(socket, result);

                    receiveRegion.Dispose();

                    context.Memory.Write(sockAddrOutPosition, BsdSockAddr.FromIPEndPoint(endPoint));
                }
            }

            return WriteBsdResult(context, result, errno);
        }

        [CommandHipc(10)]
        // Send(u32 socket, u32 flags, buffer<i8, 0x21, 0>) -> (i32 ret, u32 bsd_errno)
        public ResultCode Send(ServiceCtx context)
        {
            int            socketFd    = context.RequestData.ReadInt32();
            BsdSocketFlags socketFlags = (BsdSocketFlags)context.RequestData.ReadInt32();

            (ulong sendPosition, ulong sendSize) = context.Request.GetBufferType0x21();

            ReadOnlySpan<byte> sendBuffer = context.Memory.GetSpan(sendPosition, (int)sendSize);

            LinuxError errno  = LinuxError.EBADF;
            BsdSocket  socket = _context.RetrieveBsdSocket(socketFd);
            int        result = -1;

            if (socket != null)
            {
                errno = socket.Handle.Send(out result, sendBuffer, socketFlags);

                if (errno == LinuxError.SUCCESS)
                {
                    SetResultErrno(socket, result);
                }
            }

            return WriteBsdResult(context, result, errno);
        }

        [CommandHipc(11)]
        // SendTo(u32 socket, u32 flags, buffer<i8, 0x21, 0>, buffer<nn::socket::sockaddr_in, 0x21, 0x10>) -> (i32 ret, u32 bsd_errno)
        public ResultCode SendTo(ServiceCtx context)
        {
            int            socketFd    = context.RequestData.ReadInt32();
            BsdSocketFlags socketFlags = (BsdSocketFlags)context.RequestData.ReadInt32();

            (ulong sendPosition,   ulong sendSize)   = context.Request.GetBufferType0x21(0);
            (ulong bufferPosition, ulong bufferSize) = context.Request.GetBufferType0x21(1);

            ReadOnlySpan<byte> sendBuffer = context.Memory.GetSpan(sendPosition, (int)sendSize);

            LinuxError errno  = LinuxError.EBADF;
            BsdSocket  socket = _context.RetrieveBsdSocket(socketFd);
            int        result = -1;

            if (socket != null)
            {
                IPEndPoint endPoint = context.Memory.Read<BsdSockAddr>(bufferPosition).ToIPEndPoint();

                errno = socket.Handle.SendTo(out result, sendBuffer, sendBuffer.Length, socketFlags, endPoint);

                if (errno == LinuxError.SUCCESS)
                {
                    SetResultErrno(socket, result);
                }
            }

            return WriteBsdResult(context, result, errno);
        }

        [CommandHipc(12)]
        // Accept(u32 socket) -> (i32 ret, u32 bsd_errno, u32 addrlen, buffer<nn::socket::sockaddr_in, 0x22, 0x10> addr)
        public ResultCode Accept(ServiceCtx context)
        {
            int socketFd = context.RequestData.ReadInt32();

            (ulong bufferPos, ulong bufferSize) = context.Request.GetBufferType0x22();

            LinuxError errno  = LinuxError.EBADF;
            BsdSocket  socket = _context.RetrieveBsdSocket(socketFd);

            if (socket != null)
            {
                errno = socket.Handle.Accept(out ISocket newSocket);

                if (newSocket == null && errno == LinuxError.SUCCESS)
                {
                    errno = LinuxError.EWOULDBLOCK;
                }
                else if (errno == LinuxError.SUCCESS)
                {
                    BsdSocket newBsdSocket = new BsdSocket
                    {
                        Family   = (int)newSocket.AddressFamily,
                        Type     = (int)newSocket.SocketType,
                        Protocol = (int)newSocket.ProtocolType,
                        Handle   = newSocket,
                        Refcount = 1
                    };

                    int newSockFd = _context.RegisterSocket(newBsdSocket);

                    if (newSockFd == -1)
                    {
                        errno = LinuxError.EBADF;
                    }
                    else
                    {
                        WriteSockAddr(context, bufferPos, newBsdSocket, true);
                    }

                    WriteBsdResult(context, newSockFd, errno);

                    context.ResponseData.Write(0x10);

                    return ResultCode.Success;
                }
            }

            return WriteBsdResult(context, -1, errno);
        }

        [CommandHipc(13)]
        // Bind(u32 socket, buffer<nn::socket::sockaddr_in, 0x21, 0x10> addr) -> (i32 ret, u32 bsd_errno)
        public ResultCode Bind(ServiceCtx context)
        {
            int socketFd = context.RequestData.ReadInt32();

            (ulong bufferPosition, ulong bufferSize) = context.Request.GetBufferType0x21();

            LinuxError errno  = LinuxError.EBADF;
            BsdSocket  socket = _context.RetrieveBsdSocket(socketFd);

            if (socket != null)
            {
                IPEndPoint endPoint = context.Memory.Read<BsdSockAddr>(bufferPosition).ToIPEndPoint();

                errno = socket.Handle.Bind(endPoint);
            }

            return WriteBsdResult(context, 0, errno);
        }

        [CommandHipc(14)]
        // Connect(u32 socket, buffer<nn::socket::sockaddr_in, 0x21, 0x10>) -> (i32 ret, u32 bsd_errno)
        public ResultCode Connect(ServiceCtx context)
        {
            int socketFd = context.RequestData.ReadInt32();

            (ulong bufferPosition, ulong bufferSize) = context.Request.GetBufferType0x21();

            LinuxError errno  = LinuxError.EBADF;
            BsdSocket  socket = _context.RetrieveBsdSocket(socketFd);

            if (socket != null)
            {
                IPEndPoint endPoint = context.Memory.Read<BsdSockAddr>(bufferPosition).ToIPEndPoint();

                errno = socket.Handle.Connect(endPoint);
            }

            return WriteBsdResult(context, 0, errno);
        }

        [CommandHipc(15)]
        // GetPeerName(u32 socket) -> (i32 ret, u32 bsd_errno, u32 addrlen, buffer<nn::socket::sockaddr_in, 0x22, 0x10> addr)
        public ResultCode GetPeerName(ServiceCtx context)
        {
            int socketFd = context.RequestData.ReadInt32();

            (ulong bufferPosition, ulong bufferSize) = context.Request.GetBufferType0x22();

            LinuxError  errno  = LinuxError.EBADF;
            BsdSocket socket = _context.RetrieveBsdSocket(socketFd);

            if (socket != null)
            {
                errno = LinuxError.SUCCESS;

                WriteSockAddr(context, bufferPosition, socket, true);
                WriteBsdResult(context, 0, errno);
                context.ResponseData.Write(Unsafe.SizeOf<BsdSockAddr>());
            }

            return WriteBsdResult(context, 0, errno);
        }

        [CommandHipc(16)]
        // GetSockName(u32 socket) -> (i32 ret, u32 bsd_errno, u32 addrlen, buffer<nn::socket::sockaddr_in, 0x22, 0x10> addr)
        public ResultCode GetSockName(ServiceCtx context)
        {
            int socketFd = context.RequestData.ReadInt32();

            (ulong bufferPos, ulong bufferSize) = context.Request.GetBufferType0x22();

            LinuxError errno  = LinuxError.EBADF;
            BsdSocket  socket = _context.RetrieveBsdSocket(socketFd);

            if (socket != null)
            {
                errno = LinuxError.SUCCESS;

                WriteSockAddr(context, bufferPos, socket, false);
                WriteBsdResult(context, 0, errno);
                context.ResponseData.Write(Unsafe.SizeOf<BsdSockAddr>());
            }

            return WriteBsdResult(context, 0, errno);
        }

        [CommandHipc(17)]
        // GetSockOpt(u32 socket, u32 level, u32 option_name) -> (i32 ret, u32 bsd_errno, u32, buffer<unknown, 0x22, 0>)
        public ResultCode GetSockOpt(ServiceCtx context)
        {
            int               socketFd = context.RequestData.ReadInt32();
            SocketOptionLevel level    = (SocketOptionLevel)context.RequestData.ReadInt32();
            BsdSocketOption   option   = (BsdSocketOption)context.RequestData.ReadInt32();

            (ulong bufferPosition, ulong bufferSize) = context.Request.GetBufferType0x22();
            WritableRegion optionValue = context.Memory.GetWritableRegion(bufferPosition, (int)bufferSize);

            LinuxError errno  = LinuxError.EBADF;
            BsdSocket  socket = _context.RetrieveBsdSocket(socketFd);

            if (socket != null)
            {
                errno = socket.Handle.GetSocketOption(option, level, optionValue.Memory.Span);

                if (errno == LinuxError.SUCCESS)
                {
                    optionValue.Dispose();
                }
            }

            return WriteBsdResult(context, 0, errno);
        }

        [CommandHipc(18)]
        // Listen(u32 socket, u32 backlog) -> (i32 ret, u32 bsd_errno)
        public ResultCode Listen(ServiceCtx context)
        {
            int socketFd = context.RequestData.ReadInt32();
            int backlog  = context.RequestData.ReadInt32();

            LinuxError errno  = LinuxError.EBADF;
            BsdSocket  socket = _context.RetrieveBsdSocket(socketFd);

            if (socket != null)
            {
                errno = socket.Handle.Listen(backlog);
            }

            return WriteBsdResult(context, 0, errno);
        }

        [CommandHipc(19)]
        // Ioctl(u32 fd, u32 request, u32 bufcount, buffer<unknown, 0x21, 0>, buffer<unknown, 0x21, 0>, buffer<unknown, 0x21, 0>, buffer<unknown, 0x21, 0>) -> (i32 ret, u32 bsd_errno, buffer<unknown, 0x22, 0>, buffer<unknown, 0x22, 0>, buffer<unknown, 0x22, 0>, buffer<unknown, 0x22, 0>)
        public ResultCode Ioctl(ServiceCtx context)
        {
            int      socketFd    = context.RequestData.ReadInt32();
            BsdIoctl cmd         = (BsdIoctl)context.RequestData.ReadInt32();
            int      bufferCount = context.RequestData.ReadInt32();

            LinuxError errno  = LinuxError.EBADF;
            BsdSocket  socket = _context.RetrieveBsdSocket(socketFd);

            if (socket != null)
            {
                switch (cmd)
                {
                    case BsdIoctl.AtMark:
                        errno = LinuxError.SUCCESS;

                        (ulong bufferPosition, ulong bufferSize) = context.Request.GetBufferType0x22();

                        // FIXME: OOB not implemented.
                        context.Memory.Write(bufferPosition, 0);
                        break;

                    default:
                        errno = LinuxError.EOPNOTSUPP;

                        Logger.Warning?.Print(LogClass.ServiceBsd, $"Unsupported Ioctl Cmd: {cmd}");
                        break;
                }
            }

            return WriteBsdResult(context, 0, errno);
        }

        [CommandHipc(20)]
        // Fcntl(u32 socket, u32 cmd, u32 arg) -> (i32 ret, u32 bsd_errno)
        public ResultCode Fcntl(ServiceCtx context)
        {
            int socketFd = context.RequestData.ReadInt32();
            int cmd      = context.RequestData.ReadInt32();
            int arg      = context.RequestData.ReadInt32();

            int        result = 0;
            LinuxError errno  = LinuxError.EBADF;
            BsdSocket  socket = _context.RetrieveBsdSocket(socketFd);

            if (socket != null)
            {
                errno = LinuxError.SUCCESS;

                if (cmd == 0x3)
                {
                    result = !socket.Handle.Blocking ? 0x800 : 0;
                }
                else if (cmd == 0x4 && arg == 0x800)
                {
                    socket.Handle.Blocking = false;
                    result = 0;
                }
                else
                {
                    errno = LinuxError.EOPNOTSUPP;
                }
            }

            return WriteBsdResult(context, result, errno);
        }

        [CommandHipc(21)]
        // SetSockOpt(u32 socket, u32 level, u32 option_name, buffer<unknown, 0x21, 0> option_value) -> (i32 ret, u32 bsd_errno)
        public ResultCode SetSockOpt(ServiceCtx context)
        {
            int               socketFd = context.RequestData.ReadInt32();
            SocketOptionLevel level    = (SocketOptionLevel)context.RequestData.ReadInt32();
            BsdSocketOption   option   = (BsdSocketOption)context.RequestData.ReadInt32();

            (ulong bufferPos, ulong bufferSize) = context.Request.GetBufferType0x21();

            ReadOnlySpan<byte> optionValue = context.Memory.GetSpan(bufferPos, (int)bufferSize);

            LinuxError errno  = LinuxError.EBADF;
            BsdSocket  socket = _context.RetrieveBsdSocket(socketFd);

            if (socket != null)
            {
                errno = socket.Handle.SetSocketOption(option, level, optionValue);
            }

            return WriteBsdResult(context, 0, errno);
        }

        [CommandHipc(22)]
        // Shutdown(u32 socket, u32 how) -> (i32 ret, u32 bsd_errno)
        public ResultCode Shutdown(ServiceCtx context)
        {
            int socketFd = context.RequestData.ReadInt32();
            int how      = context.RequestData.ReadInt32();

            LinuxError errno  = LinuxError.EBADF;
            BsdSocket  socket = _context.RetrieveBsdSocket(socketFd);

            if (socket != null)
            {
                errno = LinuxError.EINVAL;

                if (how >= 0 && how <= 2)
                {
                    errno = socket.Handle.Shutdown((BsdSocketShutdownFlags)how);
                }
            }

            return WriteBsdResult(context, 0, errno);
        }

        [CommandHipc(23)]
        // ShutdownAllSockets(u32 how) -> (i32 ret, u32 bsd_errno)
        public ResultCode ShutdownAllSockets(ServiceCtx context)
        {
            int how = context.RequestData.ReadInt32();

            LinuxError errno = LinuxError.EINVAL;

            if (how >= 0 && how <= 2)
            {
                errno = _context.ShutdownAll((BsdSocketShutdownFlags)how);
            }

            return WriteBsdResult(context, 0, errno);
        }

        [CommandHipc(24)]
        // Write(u32 socket, buffer<i8, 0x21, 0> message) -> (i32 ret, u32 bsd_errno)
        public ResultCode Write(ServiceCtx context)
        {
            int socketFd = context.RequestData.ReadInt32();

            (ulong sendPosition, ulong sendSize) = context.Request.GetBufferType0x21();

            ReadOnlySpan<byte> sendBuffer = context.Memory.GetSpan(sendPosition, (int)sendSize);

            LinuxError errno  = LinuxError.EBADF;
            IBsdSocket socket = _context.RetrieveSocket(socketFd);
            int        result = -1;

            if (socket != null)
            {
                errno = socket.Write(out result, sendBuffer);

                if (errno == LinuxError.SUCCESS)
                {
                    SetResultErrno(socket, result);
                }
            }

            return WriteBsdResult(context, result, errno);
        }

        [CommandHipc(25)]
        // Read(u32 socket) -> (i32 ret, u32 bsd_errno, buffer<i8, 0x22, 0> message)
        public ResultCode Read(ServiceCtx context)
        {
            int socketFd = context.RequestData.ReadInt32();

            (ulong receivePosition, ulong receiveLength) = context.Request.GetBufferType0x22();

            WritableRegion receiveRegion = context.Memory.GetWritableRegion(receivePosition, (int)receiveLength);

            LinuxError errno  = LinuxError.EBADF;
            IBsdSocket socket = _context.RetrieveSocket(socketFd);
            int        result = -1;

            if (socket != null)
            {
                errno = socket.Read(out result, receiveRegion.Memory.Span);

                if (errno == LinuxError.SUCCESS)
                {
                    SetResultErrno(socket, result);

                    receiveRegion.Dispose();
                }
            }

            return WriteBsdResult(context, result, errno);
        }

        [CommandHipc(26)]
        // Close(u32 socket) -> (i32 ret, u32 bsd_errno)
        public ResultCode Close(ServiceCtx context)
        {
            int socketFd = context.RequestData.ReadInt32();

            LinuxError errno = LinuxError.EBADF;

            if (_context.Close(socketFd))
            {
                errno = LinuxError.SUCCESS;
            }

            return WriteBsdResult(context, 0, errno);
        }

        [CommandHipc(27)]
        // DuplicateSocket(u32 socket, u64 reserved) -> (i32 ret, u32 bsd_errno)
        public ResultCode DuplicateSocket(ServiceCtx context)
        {
            int socketFd = context.RequestData.ReadInt32();
            ulong reserved = context.RequestData.ReadUInt64();

            LinuxError errno = LinuxError.ENOENT;
            int newSockFd = -1;

            if (_isPrivileged)
            {
                errno = LinuxError.SUCCESS;

                newSockFd = _context.DuplicateSocket(socketFd);

                if (newSockFd == -1)
                {
                    errno = LinuxError.EBADF;
                }
            }

            return WriteBsdResult(context, newSockFd, errno);
        }

        [CommandHipc(31)] // 7.0.0+
        // EventFd(u64 initval, nn::socket::EventFdFlags flags) -> (i32 ret, u32 bsd_errno)
        public ResultCode EventFd(ServiceCtx context)
        {
            ulong initialValue = context.RequestData.ReadUInt64();
            EventFdFlags flags = (EventFdFlags)context.RequestData.ReadUInt32();

            EventSocket newEventSocket = new EventSocket(initialValue, flags);

            LinuxError errno = LinuxError.SUCCESS;

            int newSockFd = _context.RegisterSocket(newEventSocket);

            if (newSockFd == -1)
            {
                errno = LinuxError.EBADF;
            }

            return WriteBsdResult(context, newSockFd, errno);
        }
    }
}