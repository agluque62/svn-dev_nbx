using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace U5ki.Infrastructure
{
    public class GlobalTypes
    {
        /** 20170217. AGL. Nueva interfaz de comandos. Orientada a estructuras definidas en 'Infraestructure' */
        public class radioSessionData
        {
            /* datos de frecuencia */
            public string frec { get; set; }
            public int ftipo { get; set; }
            public int prio { get; set; }
            public int fstd { get; set; }
            public int fp_climax_mc { get; set; }
            public int fp_bss_win { get; set; }
            public string selected_site { get; set; }
            public int selected_site_qidx { get; set; }
            /** 20180618. Funcion Transmisor seleccionado */
            public string selected_tx { get; set; }

            /* datos de sesion */
            public string uri { get; set; }
            public string tipo { get; set; }       // 0: TX, 1: RX, 2: RXTX
            public int std { get; set; }           // 0: Desconectado, 1: Conectado.
            public int tx_rtp { get; set; }
            public int tx_cld { get; set; }
            public int tx_owd { get; set; }
            public int rx_rtp { get; set; }
            public int rx_qidx { get; set; }
            /** 20170807 */
            public string site { get; set; }
        }
        public class equipoMNData
        {
            public string equ { get; set; }
            public int grp { get; set; }    // 0: VHF, 1: UHF
            public int mod { get; set; }    // 0: TX, 1: RX
            public int tip { get; set; }    // 0: MAIN, 1: RSVA
            public int std { get; set; }    // 0: Desconectado, 1: Conectado, 2: Desahabilitado
            public string frec { get; set; }
            public int prio { get; set; }
            public int sip { get; set; }
            public string ip { get; set; }
            public string emp { get; set; }
            public int tfrec { get; set; }   // Tipo de Frecuencia. 0: Normal, 1: 1+1, 2: FD, 3: EM       
        }

        /** 20171009. Incluimos las pantallas de Gestion HF*/
        public class txHF
        {
            public string id { get; set; }
            public string gestor { get; set; }
            public string oid { get; set; }
            public int std { get; set; }
            public string user { get; set; }
            public string fre { get; set; }
            public string uri { get; set; }
        }


    }
}
