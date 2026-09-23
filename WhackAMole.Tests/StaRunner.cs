using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows.Forms;

namespace WhackAMole.Tests;

public class StaRunner
{
    public void Run(Action action)
    {
        Exception? failure = null;
        Thread thread = new Thread(() =>
        {
            try
            {
                action();
                Application.DoEvents();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        if (!thread.Join(TimeSpan.FromSeconds(15)))
        {
            throw new TimeoutException("STA UI test did not finish");
        }

        if (failure != null)
        {
            ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }
}
