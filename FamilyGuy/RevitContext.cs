/****************************************************************\

 Convinient ExternalEven wrapper

\****************************************************************/

using System;

using Autodesk.Revit.UI;


namespace FamilyGuy
{
  internal sealed class RevitContext : IExternalEventHandler, IDisposable
  {
    public delegate void CommandInContext(UIApplication app);

    private string name;
    private CommandInContext cmd;

    private readonly ExternalEvent externalEvent;

    public RevitContext()
    {
      externalEvent = ExternalEvent.Create(this);
    }

    public void Call(string name, CommandInContext cmd)
    {
      if (cmd == null)
        return;

      if (externalEvent.IsPending)
      {
        System.Windows.MessageBox.Show("Previous command not yet executed", "External Event");
        return;
      }

      this.cmd = cmd;
      this.name = name;

      var res = externalEvent.Raise();
      if (res == ExternalEventRequest.Denied || res == ExternalEventRequest.TimedOut)
        System.Windows.MessageBox.Show("Revit denied to execute command", "External Event");
    }

    public void Execute(UIApplication app)
    {
      if (cmd != null)
      {
        try
        {
          cmd.Invoke(app);
        }
        catch (Exception e)
        {
          System.Windows.MessageBox.Show(e.ToString(), "Something went wrong", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.None);
          throw;
        }
      }
    }

    public string GetName()
    {
      return name;
    }

    public void Dispose()
    {
      externalEvent.Dispose();
    }
  }
}
