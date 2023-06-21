using Ryujinx.Common.Utilities;
using System.Text.Json.Serialization;

namespace Ryujinx.Common.Logging
{
    [JsonConverter(typeof(TypedStringEnumConverter<LogClass>))]
    public enum LogClass
    {
        Application,
        Audio,
        AudioRenderer,
        Configuration,
        Cpu,
        Emulation,
        FFmpeg,
        Font,
        Gpu,
        Hid,
        Host1x,
        Kernel,
        KernelIpc,
        KernelScheduler,
        KernelSvc,
        Loader,
        ModLoader,
        Nvdec,
        Ptc,
        Service,
        ServiceAcc,
        ServiceAm,
        ServiceApm,
        ServiceAudio,
        ServiceBcat,
        ServiceBsd,
        ServiceBtm,
        ServiceCaps,
        ServiceFatal,
        ServiceFriend,
        ServiceFs,
        ServiceHid,
        ServiceIrs,
        ServiceLdn,
        ServiceLdr,
        ServiceLm,
        ServiceMii,
        ServiceMm,
        ServiceMnpp,
        ServiceNfc,
        ServiceNfp,
        ServiceNgct,
        ServiceNifm,
        ServiceNim,
        ServiceNs,
        ServiceNsd,
        ServiceNtc,
        ServiceNv,
        ServiceOlsc,
        ServicePctl,
        ServicePcv,
        ServicePl,
        ServicePrepo,
        ServicePsm,
        ServicePtm,
        ServiceSet,
        ServiceSfdnsres,
        ServiceSm,
        ServiceSsl,
        ServiceSss,
        ServiceTime,
        ServiceVi,
        SurfaceFlinger,
        TamperMachine,
        Ui,
        Vic
    }
}
