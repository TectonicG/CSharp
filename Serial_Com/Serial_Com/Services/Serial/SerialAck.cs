using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Serial_Com.Services.Serial
{

    /*
     * This class 
     */
    public sealed class AckLatch
    {

        private readonly object _lock = new object();   //protects the fields below
        private TaskCompletionSource<bool>? _tcs;   //waiter ther writer awaits
        private uint _token;    //token of the current in-flight write

        //Writer calls this before sending a message over serial to arm the latch for next token
        //Call from serialWriter
        public (uint token, Task<bool> task) Arm(uint nextToken)
        {

            lock (_lock)
            {
                _token = nextToken; //This is the token we are waiting for 
                _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously); //Do nothing
                return (_token, _tcs.Task);//Writer awaits this taks
            }

        }

        //Try to signal the writer that the ack came in
        //Call from serialReader
        public bool TrySignal(uint refToken)
        {
            lock (_lock) //Lock the object
            {
                //if the taskCompletionSource matches a non null tcs, and the reference token is the one we are looking for,
                //Basically if tcs is real (called) and the ref token came in
                if(_tcs is { } tcs && refToken == _token) //Does the token that came in match the _token we are looking for
                {
                    _tcs = null; //Disarm waiting task
                    tcs.TrySetResult(true); //Let the writer know the ack came in
                    return true;
                }
                return false; //The ack didn't match or it was already completed
            }
        }
        
        //Use on disconnect / teardown / timeout 
        //Disarms the waiting task
        public void Cancel()
        {
            lock (_lock)
            {
                _tcs?.TrySetCanceled();
                _tcs = null;
            }
        }


    }
}
