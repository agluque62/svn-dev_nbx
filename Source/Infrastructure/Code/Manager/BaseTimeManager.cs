using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Utilities;

namespace U5ki.Infrastructure
{

    /// <summary>
    /// Clase que añade un timer al manager basico.
    /// </summary>
    public abstract class BaseTimeManager : BaseManager
    {

        #region Declarations

        /// <summary>
        /// Usado para registrar la ultima modificacion a la inforamcion que maneja el Manager.
        /// </summary>
        public DateTime LastUpdate { get; set; }
        /// <summary>
        /// Usado para registrar el ultimo tick del Timer. 
        /// </summary>
        public DateTime LastValidation { get; set; }

        //private Int32 _timerInterval = 10000; // 10 segundos.
        //private Int32 _timerInterval = 5000;  // 5 segundos
        //private Int32 _timerInterval = 2000;  // 2 segundos
        private Int32 _timerInterval = 1000;    // 1 segundos

        //private Int32 _timerInterval = 500; // 0,5 segundo
        /// <summary>
        /// Determina el nivel de iteración del timer, en milisegundos.
        /// </summary>
        public Int32 TimerInterval
        {
            get { return _timerInterval; }
            set
            { 
                _timerInterval = value;
                WorkingTimer.Interval = value;
            }
        }

        private Timer _workingTimer;
        /// <summary>
        /// Usado para gestionar el trabajo periodico priuncipal del Manager.
        /// </summary>
        public Timer WorkingTimer
        {
            get
            {
                if (null == _workingTimer) 
                {
                    _workingTimer = new Timer(_timerInterval);
                    _workingTimer.Elapsed += OnElapsed;
                }
                return _workingTimer;
            }
        }

        #endregion

        protected abstract void OnElapsed(object sender, ElapsedEventArgs e);

        /// <summary>
        /// Funcion de inicio del servicio del manager.
        /// </summary>
        protected override void StartManager()
        {
            /** TEST.. */
            if (U5ki.Infrastructure.Code.Globals.Test.IsRCNDFSimuladoRunning)
                _timerInterval = 1000;

            if (Status != ServiceStatus.Running)
            {
                base.StartManager();
                WorkingTimer.Start();
                Console.WriteLine(" ---- TIMER MANAGER START ---- ");
            }
        }
        /// <summary>
        /// Funcion de finalización del servicio del manager, parametrizada por tipo de status de parada.
        /// </summary>
        protected override void StopManager()
        {
            if (Status == ServiceStatus.Running)
            {
                base.StopManager();
                WorkingTimer.Stop();
                Console.WriteLine(" ---- TIMER MANAGER STOP ---- ");
            }
        }

    }
}
