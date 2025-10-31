//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Serial_Com.Services
//{
//    //UI ---> Fluidics
//    public abstract record FluidicsCommand;
//    public record ConnectSerial(string port, TaskCompletionSource<bool> Reply) : FluidicsCommand;
//    public record DisconnectSerial(TaskCompletionSource<bool> Reply) : FluidicsCommand;

//    //Fluidics ---> UI
//    public abstract record FluidicsEvent;
//    public record ConnectionChanged(bool IsConnected) : FluidicsEvent;
//    public record FluidicsMessageIn(DeviceMessage msg) : FluidicsEvent;
//    public record FluidicsError(string Where, string Message) : FluidicsEvent;
//}
