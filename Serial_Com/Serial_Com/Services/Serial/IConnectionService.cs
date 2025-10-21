using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Serial_Com.Services.Serial
{
    //This is like a contract that another class uses to make sure it implements all of the metods in here 
    //This enforces a shape and behavior of a class.
    public interface IConnectionService
    {
        bool IsConnected { get; }
        string? Endpoint { get; } //Com port & baud rate
        public event EventHandler<ReadOnlyMemory<byte>>? DataReceived;
        public event EventHandler<bool>? ConnectionChanged;

        Task ConnectAsync(string portName, int baud, string endline, int timeout, CancellationToken cancellationToken);
        Task DisconnectAsync();

        ValueTask WriteAsync(ReadOnlyMemory<byte> data);

    }
}
