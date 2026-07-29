using System;
using System.Windows.Forms;

namespace ExitClone.UI
{
    /// <summary>
    /// Marshals work onto the UI thread, tolerating controls whose handle does not exist yet
    /// (events can fire while a page is still being constructed).
    /// </summary>
    public static class UiDispatch
    {
        public static void Post(Control control, Action action)
        {
            if (control == null || control.IsDisposed) return;
            if (!control.IsHandleCreated)
            {
                action();
                return;
            }
            try
            {
                if (control.InvokeRequired) control.BeginInvoke(action);
                else action();
            }
            catch (Exception)
            {
                // The control can be torn down between the check and the invoke.
            }
        }
    }
}
