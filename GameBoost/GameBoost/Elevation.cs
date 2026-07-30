using System.Security.Principal;

namespace GameBoost
{
    public static class Elevation
    {
        /// <summary>True when the process can stop services and change the power plan.</summary>
        public static bool IsElevated
        {
            get
            {
                using (var identity = WindowsIdentity.GetCurrent())
                {
                    return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
                }
            }
        }
    }
}
