using ChocolArm64.Memory;
using Ryujinx.Audio;
using Ryujinx.HLE.Logging;
using Ryujinx.HLE.OsHle.Handles;
using Ryujinx.HLE.OsHle.Ipc;
using System.Collections.Generic;
using System.Text;

using static Ryujinx.HLE.OsHle.ErrorCode;

namespace Ryujinx.HLE.OsHle.Services.Aud
{
    class IAudioOutManager : IpcService
    {
        private const string DefaultAudioOutput = "DeviceOut";

        private Dictionary<int, ServiceProcessRequest> m_Commands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => m_Commands;

        public IAudioOutManager()
        {
            m_Commands = new Dictionary<int, ServiceProcessRequest>()
            {
                { 0, ListAudioOuts     },
                { 1, OpenAudioOut      },
                { 2, ListAudioOutsAuto },
                { 3, OpenAudioOutAuto  }
            };
        }

        public long ListAudioOuts(ServiceCtx Context)
        {
            return ListAudioOutsImpl(
                Context,
                Context.Request.ReceiveBuff[0].Position,
                Context.Request.ReceiveBuff[0].Size);
        }

        public long OpenAudioOut(ServiceCtx Context)
        {
            return OpenAudioOutImpl(
                Context,
                Context.Request.SendBuff[0].Position,
                Context.Request.SendBuff[0].Size,
                Context.Request.ReceiveBuff[0].Position,
                Context.Request.ReceiveBuff[0].Size);
        }

        public long ListAudioOutsAuto(ServiceCtx Context)
        {
            (long RecvPosition, long RecvSize) = Context.Request.GetBufferType0x22();

            return ListAudioOutsImpl(Context, RecvPosition, RecvSize);
        }

        public long OpenAudioOutAuto(ServiceCtx Context)
        {
            (long SendPosition, long SendSize) = Context.Request.GetBufferType0x21();
            (long RecvPosition, long RecvSize) = Context.Request.GetBufferType0x22();

            return OpenAudioOutImpl(
                Context,
                SendPosition,
                SendSize,
                RecvPosition,
                RecvSize);
        }

        public long ListAudioOutsImpl(ServiceCtx Context, long Position, long Size)
        {
            int NameCount = 0;

            byte[] DeviceNameBuffer = Encoding.ASCII.GetBytes(DefaultAudioOutput + "\0");

            if ((ulong)DeviceNameBuffer.Length <= (ulong)Size)
            {
                Context.Memory.WriteBytes(Position, DeviceNameBuffer);

                NameCount++;
            }
            else
            {
                Context.Ns.Log.PrintError(LogClass.ServiceAudio, $"Output buffer size {Size} too small!");
            }

            Context.ResponseData.Write(NameCount);

            return 0;
        }

        public long OpenAudioOutImpl(ServiceCtx Context, long SendPosition, long SendSize, long ReceivePosition, long ReceiveSize)
        {
            string DeviceName = AMemoryHelper.ReadAsciiString(
                Context.Memory,
                SendPosition,
                SendSize);

            if (DeviceName == string.Empty)
            {
                DeviceName = DefaultAudioOutput;
            }

            if (DeviceName != DefaultAudioOutput)
            {
                Context.Ns.Log.PrintWarning(LogClass.Audio, "Invalid device name!");

                return MakeError(ErrorModule.Audio, AudErr.DeviceNotFound);
            }

            byte[] DeviceNameBuffer = Encoding.ASCII.GetBytes(DeviceName + "\0");

            if ((ulong)DeviceNameBuffer.Length <= (ulong)ReceiveSize)
            {
                Context.Memory.WriteBytes(ReceivePosition, DeviceNameBuffer);
            }
            else
            {
                Context.Ns.Log.PrintError(LogClass.ServiceAudio, $"Output buffer size {ReceiveSize} too small!");
            }

            int SampleRate = Context.RequestData.ReadInt32();
            int Channels   = Context.RequestData.ReadInt32();

            if (SampleRate != 48000)
            {
                Context.Ns.Log.PrintWarning(LogClass.Audio, "Invalid sample rate!");

                return MakeError(ErrorModule.Audio, AudErr.UnsupportedSampleRate);
            }

            Channels = (ushort)Channels;

            if (Channels == 0)
            {
                Channels = 2;
            }

            KEvent ReleaseEvent = new KEvent();

            ReleaseCallback Callback = () =>
            {
                ReleaseEvent.WaitEvent.Set();
            };

            IAalOutput AudioOut = Context.Ns.AudioOut;

            int Track = AudioOut.OpenTrack(SampleRate, 2, Callback, out AudioFormat Format);

            MakeObject(Context, new IAudioOut(AudioOut, ReleaseEvent, Track));

            Context.ResponseData.Write(SampleRate);
            Context.ResponseData.Write(Channels);
            Context.ResponseData.Write((int)Format);
            Context.ResponseData.Write((int)PlaybackState.Stopped);

            return 0;
        }
    }
}
