using System.Collections.Generic;

namespace PonyuDev.SherpaOnnx.Common.Data
{
    public interface ISettingsData<TProfile> where TProfile : IProfileData
    {
        int ActiveProfileIndex { get; set; }
        List<TProfile> Profiles { get; }
    }
}
