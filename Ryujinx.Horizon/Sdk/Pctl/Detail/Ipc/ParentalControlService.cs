using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Pctl.Detail.Service.Watcher;
using Ryujinx.Horizon.Sdk.Sf;
using Ryujinx.Horizon.Sdk.Sf.Hipc;
using Ryujinx.Horizon.Sdk.Time;
using System;
using System.Collections.Generic;
using ApplicationId = Ryujinx.Horizon.Sdk.Ncm.ApplicationId;

namespace Ryujinx.Horizon.Sdk.Pctl.Detail.Ipc
{
    class ParentalControlService : IParentalControlService
    {
        [CmifCommand(1)]
        public Result Initialize()
        {
            return Result.Success;
        }

        [CmifCommand(1001)]
        public Result TryBeginFreeCommunication()
        {
            return Result.Success;
        }

        Result IParentalControlService.ConfirmResumeApplicationPermission(ApplicationId arg0, ReadOnlySpan<sbyte> arg1, bool arg2)
        {
            return ConfirmResumeApplicationPermission(arg0, arg1, arg2);
        }

        [CmifCommand(1002)]
        public Result ConfirmLaunchApplicationPermission(ApplicationId arg0, [Buffer(HipcBufferFlags.In | HipcBufferFlags.Pointer)] ReadOnlySpan<sbyte> arg1, bool arg2)
        {
            return Result.Success;
        }

        [CmifCommand(1003)]
        public Result ConfirmResumeApplicationPermission(ApplicationId arg0, [Buffer(HipcBufferFlags.In | HipcBufferFlags.Pointer)] ReadOnlySpan<sbyte> arg1, bool arg2)
        {
            return Result.Success;
        }

        [CmifCommand(1004)]
        public Result ConfirmSnsPostPermission()
        {
            return Result.Success;
        }

        [CmifCommand(1005)]
        public Result ConfirmSystemSettingsPermission()
        {
            return Result.Success;
        }

        [CmifCommand(1006)]
        public Result IsRestrictionTemporaryUnlocked(out bool arg0)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(1007)]
        public Result RevertRestrictionTemporaryUnlocked()
        {
            return Result.Success;
        }

        [CmifCommand(1008)]
        public Result EnterRestrictedSystemSettings()
        {
            return Result.Success;
        }

        [CmifCommand(1009)]
        public Result LeaveRestrictedSystemSettings()
        {
            return Result.Success;
        }

        [CmifCommand(1010)]
        public Result IsRestrictedSystemSettingsEntered(out bool arg0)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(1011)]
        public Result RevertRestrictedSystemSettingsEntered()
        {
            return Result.Success;
        }

        [CmifCommand(1012)]
        public Result GetRestrictedFeatures(out int arg0)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(1013)]
        public Result ConfirmStereoVisionPermission()
        {
            return Result.Success;
        }

        [CmifCommand(1014)]
        public Result ConfirmPlayableApplicationVideoOld([Buffer(HipcBufferFlags.In | HipcBufferFlags.Pointer)] ReadOnlySpan<sbyte> arg0)
        {
            return Result.Success;
        }

        Result IParentalControlService.ConfirmPlayableApplicationVideo(ApplicationId arg0, ReadOnlySpan<sbyte> arg1)
        {
            return ConfirmPlayableApplicationVideo(arg0, arg1);
        }

        [CmifCommand(1015)]
        public Result ConfirmPlayableApplicationVideo(ApplicationId arg0, [Buffer(HipcBufferFlags.In | HipcBufferFlags.Pointer)] ReadOnlySpan<sbyte> arg1)
        {
            return Result.Success;
        }

        [CmifCommand(1016)]
        public Result ConfirmShowNewsPermission([Buffer(HipcBufferFlags.In | HipcBufferFlags.Pointer)] ReadOnlySpan<sbyte> arg0)
        {
            return Result.Success;
        }

        [CmifCommand(1017)]
        public Result EndFreeCommunication()
        {
            return Result.Success;
        }

        [CmifCommand(1018)]
        public Result IsFreeCommunicationAvailable()
        {
            return Result.Success;
        }

        [CmifCommand(1031)]
        public Result IsRestrictionEnabled(out bool arg0)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(1032)]
        public Result GetSafetyLevel(out int arg0)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(1033)]
        public Result SetSafetyLevel(int arg0)
        {
            return Result.Success;
        }

        [CmifCommand(1034)]
        public Result GetSafetyLevelSettings(out RestrictionSettings arg0, int arg1)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(1035)]
        public Result GetCurrentSettings(out RestrictionSettings arg0)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(1036)]
        public Result SetCustomSafetyLevelSettings(RestrictionSettings arg0)
        {
            return Result.Success;
        }

        [CmifCommand(1037)]
        public Result GetDefaultRatingOrganization(out int arg0)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(1038)]
        public Result SetDefaultRatingOrganization(int arg0)
        {
            return Result.Success;
        }

        [CmifCommand(1039)]
        public Result GetFreeCommunicationApplicationListCount(out int arg0)
        {
            arg0 = default;

            return Result.Success;
        }

        Result IParentalControlService.AddToFreeCommunicationApplicationList(ApplicationId arg0)
        {
            return AddToFreeCommunicationApplicationList(arg0);
        }

        [CmifCommand(1042)]
        public Result AddToFreeCommunicationApplicationList(ApplicationId arg0)
        {
            return Result.Success;
        }

        [CmifCommand(1043)]
        public Result DeleteSettings()
        {
            return Result.Success;
        }

        [CmifCommand(1044)]
        public Result GetFreeCommunicationApplicationList(out int arg0, [Buffer(HipcBufferFlags.Out | HipcBufferFlags.MapAlias)] Span<FreeCommunicationApplicationInfo> arg1, int arg2)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(1045)]
        public Result UpdateFreeCommunicationApplicationList([Buffer(HipcBufferFlags.In | HipcBufferFlags.MapAlias)] ReadOnlySpan<FreeCommunicationApplicationInfo> arg0)
        {
            return Result.Success;
        }

        [CmifCommand(1046)]
        public Result DisableFeaturesForReset()
        {
            return Result.Success;
        }

        Result IParentalControlService.NotifyApplicationDownloadStarted(ApplicationId arg0)
        {
            return NotifyApplicationDownloadStarted(arg0);
        }

        [CmifCommand(1047)]
        public Result NotifyApplicationDownloadStarted(ApplicationId arg0)
        {
            return Result.Success;
        }

        [CmifCommand(1048)]
        public Result NotifyNetworkProfileCreated()
        {
            return Result.Success;
        }

        [CmifCommand(1049)]
        public Result ResetFreeCommunicationApplicationList()
        {
            return Result.Success;
        }

        [CmifCommand(1061)]
        public Result ConfirmStereoVisionRestrictionConfigurable()
        {
            return Result.Success;
        }

        [CmifCommand(1062)]
        public Result GetStereoVisionRestriction(out bool arg0)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(1063)]
        public Result SetStereoVisionRestriction(bool arg0)
        {
            return Result.Success;
        }

        [CmifCommand(1064)]
        public Result ResetConfirmedStereoVisionPermission()
        {
            return Result.Success;
        }

        [CmifCommand(1065)]
        public Result IsStereoVisionPermitted(out bool arg0)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(1201)]
        public Result UnlockRestrictionTemporarily([Buffer(HipcBufferFlags.In | HipcBufferFlags.Pointer)] ReadOnlySpan<sbyte> arg0)
        {
            return Result.Success;
        }

        [CmifCommand(1202)]
        public Result UnlockSystemSettingsRestriction([Buffer(HipcBufferFlags.In | HipcBufferFlags.Pointer)] ReadOnlySpan<sbyte> arg0)
        {
            return Result.Success;
        }

        [CmifCommand(1203)]
        public Result SetPinCode([Buffer(HipcBufferFlags.In | HipcBufferFlags.Pointer)] ReadOnlySpan<sbyte> arg0)
        {
            return Result.Success;
        }

        [CmifCommand(1204)]
        public Result GenerateInquiryCode(out InquiryCode arg0)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(1205)]
        public Result CheckMasterKey(out bool arg0, InquiryCode arg1, [Buffer(HipcBufferFlags.In | HipcBufferFlags.Pointer)] ReadOnlySpan<sbyte> arg2)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(1206)]
        public Result GetPinCodeLength(out int arg0)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(1207)]
        public Result GetPinCodeChangedEvent([CopyHandle] out int arg0)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(1208)]
        public Result GetPinCode(out int arg0, [Buffer(HipcBufferFlags.Out | HipcBufferFlags.Pointer)] Span<sbyte> arg1)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(1403)]
        public Result IsPairingActive(out bool arg0)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(1406)]
        public Result GetSettingsLastUpdated(out PosixTime arg0)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(1411)]
        public Result GetPairingAccountInfo(out PairingAccountInfoBase arg0, PairingInfoBase arg1)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(1421)]
        public Result GetAccountNickname(out uint arg0, [Buffer(HipcBufferFlags.Out | HipcBufferFlags.Pointer)] Span<sbyte> arg1, PairingAccountInfoBase arg2)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(1424)]
        public Result GetAccountState(out int arg0, PairingAccountInfoBase arg1)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(1425)]
        public Result RequestPostEvents(out int arg0, [Buffer(HipcBufferFlags.Out | HipcBufferFlags.MapAlias)] Span<EventData> arg1)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(1426)]
        public Result GetPostEventInterval(out int arg0)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(1427)]
        public Result SetPostEventInterval(int arg0)
        {
            return Result.Success;
        }

        [CmifCommand(1432)]
        public Result GetSynchronizationEvent([CopyHandle] out int arg0)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(1451)]
        public Result StartPlayTimer()
        {
            return Result.Success;
        }

        [CmifCommand(1452)]
        public Result StopPlayTimer()
        {
            return Result.Success;
        }

        [CmifCommand(1453)]
        public Result IsPlayTimerEnabled(out bool arg0)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(1454)]
        public Result GetPlayTimerRemainingTime(out TimeSpanType arg0)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(1455)]
        public Result IsRestrictedByPlayTimer(out bool arg0)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(1456)]
        public Result GetPlayTimerSettings(out PlayTimerSettings arg0)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(1457)]
        public Result GetPlayTimerEventToRequestSuspension([CopyHandle] out int arg0)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(1458)]
        public Result IsPlayTimerAlarmDisabled(out bool arg0)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(1471)]
        public Result NotifyWrongPinCodeInputManyTimes()
        {
            return Result.Success;
        }

        [CmifCommand(1472)]
        public Result CancelNetworkRequest()
        {
            return Result.Success;
        }

        [CmifCommand(1473)]
        public Result GetUnlinkedEvent([CopyHandle] out int arg0)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(1474)]
        public Result ClearUnlinkedEvent()
        {
            return Result.Success;
        }

        [CmifCommand(1601)]
        public Result DisableAllFeatures(out bool arg0)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(1602)]
        public Result PostEnableAllFeatures(out bool arg0)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(1603)]
        public Result IsAllFeaturesDisabled(out bool arg0, out bool arg1)
        {
            arg0 = default;
            arg1 = default;

            return Result.Success;
        }

        Result IParentalControlService.DeleteFromFreeCommunicationApplicationListForDebug(ApplicationId arg0)
        {
            return DeleteFromFreeCommunicationApplicationListForDebug(arg0);
        }

        [CmifCommand(1901)]
        public Result DeleteFromFreeCommunicationApplicationListForDebug(ApplicationId arg0)
        {
            return Result.Success;
        }

        [CmifCommand(1902)]
        public Result ClearFreeCommunicationApplicationListForDebug()
        {
            return Result.Success;
        }

        [CmifCommand(1903)]
        public Result GetExemptApplicationListCountForDebug(out int arg0)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(1904)]
        public Result GetExemptApplicationListForDebug(out int arg0, [Buffer(HipcBufferFlags.Out | HipcBufferFlags.MapAlias)] Span<ExemptApplicationInfo> arg1, int arg2)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(1905)]
        public Result UpdateExemptApplicationListForDebug([Buffer(HipcBufferFlags.In | HipcBufferFlags.MapAlias)] ReadOnlySpan<ExemptApplicationInfo> arg0)
        {
            return Result.Success;
        }

        Result IParentalControlService.AddToExemptApplicationListForDebug(ApplicationId arg0)
        {
            return AddToExemptApplicationListForDebug(arg0);
        }

        Result IParentalControlService.DeleteFromExemptApplicationListForDebug(ApplicationId arg0)
        {
            return DeleteFromExemptApplicationListForDebug(arg0);
        }

        [CmifCommand(1906)]
        public Result AddToExemptApplicationListForDebug(ApplicationId arg0)
        {
            return Result.Success;
        }

        [CmifCommand(1907)]
        public Result DeleteFromExemptApplicationListForDebug(ApplicationId arg0)
        {
            return Result.Success;
        }

        [CmifCommand(1908)]
        public Result ClearExemptApplicationListForDebug()
        {
            return Result.Success;
        }

        [CmifCommand(1941)]
        public Result DeletePairing()
        {
            return Result.Success;
        }

        [CmifCommand(1951)]
        public Result SetPlayTimerSettingsForDebug(PlayTimerSettings arg0)
        {
            return Result.Success;
        }

        [CmifCommand(1952)]
        public Result GetPlayTimerSpentTimeForTest(out TimeSpanType arg0)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(1953)]
        public Result SetPlayTimerAlarmDisabledForDebug(bool arg0)
        {
            return Result.Success;
        }

        [CmifCommand(2001)]
        public Result RequestPairingAsync(out AsyncData arg0, [CopyHandle] out int arg1, [Buffer(HipcBufferFlags.In | HipcBufferFlags.Pointer)] ReadOnlySpan<sbyte> arg2)
        {
            arg0 = default;
            arg1 = default;

            return Result.Success;
        }

        [CmifCommand(2002)]
        public Result FinishRequestPairing(out PairingInfoBase arg0, AsyncData arg1)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(2003)]
        public Result AuthorizePairingAsync(out AsyncData arg0, [CopyHandle] out int arg1, PairingInfoBase arg2)
        {
            arg0 = default;
            arg1 = default;

            return Result.Success;
        }

        [CmifCommand(2004)]
        public Result FinishAuthorizePairing(out PairingInfoBase arg0, AsyncData arg1)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(2005)]
        public Result RetrievePairingInfoAsync(out AsyncData arg0, [CopyHandle] out int arg1)
        {
            arg0 = default;
            arg1 = default;

            return Result.Success;
        }

        [CmifCommand(2006)]
        public Result FinishRetrievePairingInfo(out PairingInfoBase arg0, AsyncData arg1)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(2007)]
        public Result UnlinkPairingAsync(out AsyncData arg0, [CopyHandle] out int arg1, bool arg2)
        {
            arg0 = default;
            arg1 = default;

            return Result.Success;
        }

        [CmifCommand(2008)]
        public Result FinishUnlinkPairing(AsyncData arg0, bool arg1)
        {
            return Result.Success;
        }

        [CmifCommand(2009)]
        public Result GetAccountMiiImageAsync(out AsyncData arg0, [CopyHandle] out int arg1, out uint arg2, [Buffer(HipcBufferFlags.Out | HipcBufferFlags.MapAlias)] Span<byte> arg3, PairingAccountInfoBase arg4)
        {
            arg0 = default;
            arg1 = default;
            arg2 = default;

            return Result.Success;
        }

        [CmifCommand(2010)]
        public Result FinishGetAccountMiiImage(out uint arg0, [Buffer(HipcBufferFlags.Out | HipcBufferFlags.MapAlias)] Span<byte> arg1, AsyncData arg2)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(2011)]
        public Result GetAccountMiiImageContentTypeAsync(out AsyncData arg0, [CopyHandle] out int arg1, out uint arg2, [Buffer(HipcBufferFlags.Out | HipcBufferFlags.Pointer)] Span<sbyte> arg3, PairingAccountInfoBase arg4)
        {
            arg0 = default;
            arg1 = default;
            arg2 = default;

            return Result.Success;
        }

        [CmifCommand(2012)]
        public Result FinishGetAccountMiiImageContentType(out uint arg0, [Buffer(HipcBufferFlags.Out | HipcBufferFlags.Pointer)] Span<sbyte> arg1, AsyncData arg2)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(2013)]
        public Result SynchronizeParentalControlSettingsAsync(out AsyncData arg0, [CopyHandle] out int arg1)
        {
            arg0 = default;
            arg1 = default;

            return Result.Success;
        }

        [CmifCommand(2014)]
        public Result FinishSynchronizeParentalControlSettings(AsyncData arg0)
        {
            return Result.Success;
        }

        [CmifCommand(2015)]
        public Result FinishSynchronizeParentalControlSettingsWithLastUpdated(out PosixTime arg0, AsyncData arg1)
        {
            arg0 = default;

            return Result.Success;
        }

        Result IParentalControlService.RequestUpdateExemptionListAsync(out AsyncData arg0, out int arg1, ApplicationId arg2, bool arg3)
        {
            return RequestUpdateExemptionListAsync(out arg0, out arg1, arg2, arg3);
        }

        [CmifCommand(2016)]
        public Result RequestUpdateExemptionListAsync(out AsyncData arg0, [CopyHandle] out int arg1, ApplicationId arg2, bool arg3)
        {
            arg0 = default;
            arg1 = default;

            return Result.Success;
        }

        public IReadOnlyDictionary<int, CommandHandler> GetCommandHandlers()
        {
            throw new NotImplementedException();
        }
    }
}