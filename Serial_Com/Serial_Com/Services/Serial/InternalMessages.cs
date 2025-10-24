using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Serial_Com.InternalMessages
{
    //UI ---> Backend
    public abstract record BackendCommand;
    public record ConnectSerial(string port, TaskCompletionSource<bool> Reply) : BackendCommand;
    public record DisconnectSerial(TaskCompletionSource<bool> Reply) : BackendCommand;
    public record SendHostCommand(HostMessage msg, TaskCompletionSource<bool> Reply) : BackendCommand;

    //Backend ---> UI
    public abstract record BackendEvent;
    public record ConnectionChanged(bool IsConnected) : BackendEvent;
    public record DeviceMessageIn(DeviceMessage msg) : BackendEvent;
    public record BackendError(string Where, string Message) : BackendEvent;

}
