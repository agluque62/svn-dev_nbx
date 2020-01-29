using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace U5ki.PresenceService.Interfaces
{
    public enum AgentEngineEvents
    {
        Open, Closed, Error,
        UserRegistered, UserUnregistered,
        UserAvailable, UserBusy, UserBusyUninterrupted
    }
    public class AgentEngineEventArgs : EventArgs
    {
        public AgentEngineEvents ev { get; set; }
        public string idsub { get; set; }
    }
    public interface IAgentEngine : IDisposable
    {
        event EventHandler<AgentEngineEventArgs> EventOccurred;
        bool Available { get; }
        void Init(EventHandler<AgentEngineEventArgs> OnEventOccurred);
        void Start();
        void Stop();
    }
}
