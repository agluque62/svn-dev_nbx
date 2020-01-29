/***************************************************************************

   Nombre: COMUN.H
   Descripcion: Declaracion de defines comunes a nucleo y aplic
   
   $Revision: 1.3 $
   $Date: 2010/07/27 13:00:47 $
   
   Fecha ultima modificacion: 22/02/2010                                                   
   
   Notas:
   
   
***************************************************************************/

#ifndef INCLUIDO_PUBLICA
#define INCLUIDO_PUBLICA

#include "cfgpasa.hpp"
#include "cfgsistema.h"

/* tipo msg multicast */
#define PUBLICA_EST_CGW	01

/* tipos de recursos que se publican */
#define PUBLICA_TLF	1
#define PUBLICA_LC	2
/* */
#define TM_SUPERVISION_PUBLICA	30	/* se activará el temp cada 30 x 100ms=3 sg */
#define CONT_SUPERVISION_PUBLICA	5	/* cinco decrementaciones de la variable timer para que se de por caida  */

/* cabecera del mensaje PublicaRecursos */

struct headerMsgPublicaRecursos
{
    unsigned int tipo_msg;
    char CGWnombre[CFG_MAX_LONG_NOMBRE_PASARELA+1];
    unsigned int nrecursos;
    unsigned int version;	/* cambia de valor con los cambios */
};

/* struct de la info de unrecurso en mensaje PublicaRecursos */

struct st_PBInfoEstadoRecurso
{
    char nombre[CFG_MAX_LONG_NOMBRE_RECURSO+1];
    unsigned int tipo;		/*publica_tlf, publica_lc */
    unsigned int version;	/* cambia de valor con los cambios */
    unsigned int estado;	/* estado del recurso */
    unsigned int prio_cpipl;
    unsigned int ContadorTransitos;
    unsigned int tiempo;
};

/* estructura de datos interna para publicar los estados de un recurso */

struct stMiEstadoPublicoRecurso
{
    char nombre[CFG_MAX_LONG_NOMBRE_RECURSO+1];
    unsigned int tipo;
    unsigned int version;
    unsigned int estado;
    unsigned int prio_cpipl;
    unsigned int ContadorTransitos;
    unsigned int tiempo;
    unsigned char canal;
    unsigned char cambio;
};

struct st_PBInfoEstadosCGW
{
    char nombre[CFG_MAX_LONG_NOMBRE_PASARELA+1];
    unsigned int nrecursos;
    unsigned int version;
    unsigned int timer;
    struct st_PBInfoEstadoRecurso aInfoEstadoRecurso[MAX_RECURSOS_TIFX];
};


class CPublica
{
    public:
    	CPublica(char *);
	~CPublica();
        // Los hilos de cada entidad radio.
        void HiloPublica(void);
	
	int Funcion_Supervisor(void);
	int BajaPublicaRecurso(char *cgwname);
	int BajaPublicaRecurso(unsigned int numRec);
	int AltaPublicaRecurso(char *cgwname, int tipo, unsigned int numRec);
	int ActualizaPublicaRecurso(char *, struct DatosEstadoRecurso *, int tipo, unsigned int numRec);
	int PublicaRecursos();
	void RecibeInfoEstadoCGW(char *buf);
	int PintaPublicacionRecursos(void);
	struct st_PBInfoEstadoRecurso *LocalizaInfoEstadoRecurso(char *CGWnombre, char *RECnombre);
    
    protected:	       
	char CGWNombre[CFG_MAX_LONG_NOMBRE_PASARELA+1];
	unsigned int cambioCGW;
	unsigned int version;
	struct stMiEstadoPublicoRecurso MiEstadoPublicoRecurso[MAX_RECURSOS_TIFX];

	struct st_PBInfoEstadosCGW infoEstadosCGW[N_MAX_TIFX];
	
	int sock;

	pthread_t tHiloPublica;
	pthread_mutex_t tMutexPublica;
	pthread_cond_t tCondVarPublica;
	pthread_mutex_t tMutexDatos;
	
	struct stMiEstadoPublicoRecurso *LocalizaRecurso(char *ptnombre);
	int FormaPublicaRecursos();
	int EnviaPublicacionRecursos(unsigned char *buf, int len);
	struct st_PBInfoEstadoRecurso *LocalizaRecEnMsgRx(unsigned int nrecmsg, char *buf,char *RECnombre);
	struct st_PBInfoEstadosCGW *AltaInfoEstadosCGW(char *CGWnombre);
	int BajaInfoEstadosCGW(char *CGWnombre);
	struct st_PBInfoEstadosCGW *LocalizaInfoCGW(char *CGWnombre);

	struct st_PBInfoEstadoRecurso *AltaInfoEstadoRecurso(char *CGWnombre, struct st_PBInfoEstadoRecurso *ptinfoREC);
	struct st_PBInfoEstadoRecurso *ActualizaInfoEstadoRecurso(char *CGWnombre, struct st_PBInfoEstadoRecurso *ptinfoREC);
	int BajaInfoEstadoRecurso(char *CGWnombre, char *RECnombre );
};

#endif


/****************************************************************

    $Log: publica.h,v $
    Revision 1.3  2010/07/27 13:00:47  mariajo


    	Al detener un recurso: dar de baja el recurso y miIdUsrGus=-1, para que
    	cuando se de de alta otro nuevo, se de de alta en el SIPUA.
    	Al arrancar, detener y reconfigurar el recurso actualizar la publicacion
    	de recursos.
    	Al reconfigurar recurso, dar la orden de detener y arrancar, para que el nucleo
    	tambien se actualice.

    Revision 1.2  2010/05/20 07:25:33  mariajo

    	Recepcion del msg Multicast donde se publican los recursos de cada pasarela
    	nueva clase gestorencamina para encaminar las llamadas entrantes y salientes
    	de la pasarela con un destino que es un numero.
    	añadido un parametro al evtringing para indicar que el recurso esta en Int.Warning

    Revision 1.1  2010/05/06 08:33:11  mariajo

    	se sustituye la publicación de los recursos que se hacia con spread por
    	un mensaje multicast periodico

    Revision 1.17  2010/04/06 10:13:47  mariajo

    	Nuevo cliente de configuracion
    	publicacion de los recursos para que el resto del sistema vea si estan presentes

    Revision 1.16  2010/02/26 01:33:24  fran
    "CODINFO, causa de rechazo e interrupcion por prioridad desde el lado SIP implementados"

    Revision 1.15  2010/02/25 23:58:13  mariajo

    	codigos de info

    Revision 1.14  2010/02/25 22:54:21  mariajo

    	nuevos codigos de Info-Notify entre nucleo y recurso

    Revision 1.13  2010/02/25 16:17:36  fran
    "comun.h con causas de rechazo de llamada"

    Revision 1.12  2010/02/25 15:29:43  mariajo

    	comun.h con las causas de fallo de llamada qsig

    Revision 1.11  2010/02/25 10:11:01  jmgarcia
    Anade codigo para tratamiento del 'INFO "Intrusion in progress"'
    Anade codigo MOTIVO_VARIADO, que aglutina varios motivos de rechazo de llamada que deben provocar una liberacion en llamadas ATS-R2 -> ATS-IP.

    Revision 1.10  2010/02/21 17:16:48  mariajo

    	nueva clase audio qsig
    	nuevos mensajes sip-qsig

    Revision 1.9  2010/02/21 12:29:12  fran
    "ats-qsig llamada basica 1"

    Revision 1.8  2010/02/05 13:21:12  mariajo

        Se añaden mensajes de INFO para enviar de la pasarela a los usuarios
        con los que tiene una sesion establecida
        se anade un parametro mas a los datos para crear una nueva sesion RTP,
        para relaccionar la sesión con la llamada sip que corresponda
        se corrigen casos de encamina.

    Revision 1.7  2010/01/27 12:14:58  mariajo

        se aumenta la struct DatosLLamada para incluir parametros mecesarios para QSIG
        Se añade el algunos casos un motivo del comando FInLLamada del recurso al nucleo
        se incluye en el Makefile un nuevo recurso QSIG.
        Se crea el recurso QSIG,
        se recibe procedente de los recursos de ATS la notificación de su estado.

    Revision 1.6  2010/01/21 12:03:55  mariajo

        modificaciones de R2 para que cumpla los plugtest de eurocae.
        Se realiza un encaminamiento de las llam. salientes de R2.
        se aborta una interrupción por prioridad cuando se queda libre otra linea.
        falta que el dsp responda.

    Revision 1.5  2009/12/22 11:37:37  mariajo

        Nuevas contestaciones SIP segun Eurocae para R2
        depuracion de llamadas R2
        reduccion del retardo de audio en tlf y radio. Quitado el mezclador.
        Nuevo mensaje del DSP cuando el otro libera la llamada R2

    Revision 1.4  2009/11/27 12:54:32  mariajo

        Arreglos de escenarios de R2
        notifica el estado de un recurso al nucleo a traves de un comando (colas)
        el estado del recurso se extrae de de la info de sip (modificacion en pasagus)

    Revision 1.3  2009/11/17 10:51:30  mariajo

        mejoras encamina, incluye cd40-resurce en el invite de las llamadas r2
        los recursos de r2 guardan el origen y destino de la llamada
        unificacion de los estados de los recursos de tlf con los de r2

    Revision 1.2  2009/10/30 15:23:01  mariajo


        modificaciones para depurar casos de R2 e incluir interrupcion por prioridad

    Revision 1.1  2009/10/16 13:56:11  mariajo

        defines comunes de nucleo y recursos en un .h comun

    Revision 1.21  2009/06/25 09:11:34  mariajo

        Arreglos en las lineas telefonicas BL, BC.

    Revision 1.20  2009/06/18 13:58:28  jmgarcia

    Repone lo qye habia del WG67 Key-in package


****************************************************************/

