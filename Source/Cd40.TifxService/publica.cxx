/***************************************************************************

   Nombre: publica.cxx
   Descripcion: Publicacion de los estados de los recursos de una pasarela
   
   Fecha ultima modificacion: 22/06/2010                                                       
   
   Notas:
   
   
***************************************************************************/


#include <unistd.h>
#include <pthread.h>
#include <string.h>
#include <sys/types.h>
#include <sys/wait.h>
#include <sys/time.h>
#include <signal.h>
#include <iostream>
#include <stdio.h>
#include <stdlib.h>
#include <arpa/inet.h>
#include <sys/socket.h>
#include <errno.h>

#include "general.h"
#include "trazapid.h"
#include "perrilos.h"
#include "general.h"
#include "uptime.h"
#include "publica.h"
#include "gesmutex.h"
#include "comun.h"
#include "main.h"
#include  "ctemp.hpp"
#include "cgestordual.hpp"

//-----------------------------------------------------------
//  Definiciones del diagnostico de una funcion llamada a traves de las colas
//-----------------------------------------------------------

/*
 * Retorno de funciones.
 */
 

static void *ArrancaHiloPublica( void *pvObj )
{
     ((class CPublica*)pvObj)->HiloPublica();
     return NULL;
}



/*
Funciones para tratar el timout del supervisor de pasarelas publicadas
*/

static void VenceTemp_Supervision(long int pvObj, long int nul1, long int nul2)
{
     ((class CPublica*)pvObj)->Funcion_Supervisor();
     return;
}

	
/*
 * Funcion: CPublica::CPublica
 * Descripcion: Constructor de CPublica.
 * Parametros ->
 * Retorna: Nada.
 */

CPublica::CPublica(char *ptnombre)
{
   char sz[40];
   
   memset(MiEstadoPublicoRecurso, 0, (sizeof(struct stMiEstadoPublicoRecurso) * MAX_RECURSOS_TIFX));
   memset(infoEstadosCGW, 0, (sizeof(struct st_PBInfoEstadosCGW) * N_MAX_TIFX));   

    sprintf( sz, "Publica.tMutexDatos");
    gmtxIniciaMutex( sz, &tMutexDatos, NULL );
      
    sprintf( sz, "Publica.tMutexPublica");
    gmtxIniciaMutex( sz, &tMutexPublica, NULL );
    pthread_cond_init (&tCondVarPublica,NULL);
       
   if ( 0!= pthread_create (&tHiloPublica, NULL, ::ArrancaHiloPublica, this) )
      {
      tcpcPrintfNiv( CONSOLA_BASICA,  "- Error creando hilo Publica\n");
      INCIDENCIA_N0( "Error creando hilo Publica", 0, 0, 0 );
      return;
      }
   strcpy((char *)CGWNombre, ptnombre); 
   version =0;
   cambioCGW =1;
}

/*
 * Funcion: CPublica::~CPublica
 * Descripcion: Destructor de CPublica.
 * Parametros ->
 * Retorna: Nada.
 */

CPublica::~CPublica()
{
}

/*
 * Funcion: CPublica::AltaPublicaRecurso
 * Descripcion: Alta de recurso para publicar los recursos de una pasarela
 * Parametros: nombre del recurso
 * tipo: tipo del recurso (PUBLICA_TLF / PUBLICA_LC)
 * numRec: numero del recurso en la pasarela 0 a MAX_RECURSOS_TIFX -1
 * Retorna: resultado
 */

int CPublica::AltaPublicaRecurso(char *cgwname, int tipo, unsigned int numRec)
{
  struct stMiEstadoPublicoRecurso *ptrecurso;
    
	pthread_mutex_lock( &tMutexDatos );
	ptrecurso = LocalizaRecurso(cgwname);
	if (ptrecurso !=NULL)
	{
		pthread_mutex_unlock( &tMutexDatos );
		return 0;
	}
	pthread_mutex_unlock( &tMutexDatos );
	if (numRec < MAX_RECURSOS_TIFX)
	{
		pthread_mutex_lock( &tMutexDatos );
		//no compruebo si hay algo
		//if (MiEstadoPublicoRecurso[numRec].nombre[0] == 0)
		{
   			memset(&MiEstadoPublicoRecurso[numRec], 0, sizeof(struct stMiEstadoPublicoRecurso));
			strcpy((char *)MiEstadoPublicoRecurso[numRec].nombre, (char*)cgwname);
			MiEstadoPublicoRecurso[numRec].tipo = tipo;
			MiEstadoPublicoRecurso[numRec].cambio = 1;    			
			MiEstadoPublicoRecurso[numRec].estado = EST_FUERA_DE_SERVICIO;    			
			tcpcPrintfNiv( CONSOLA_DEBUG, "              PUBLICA  %s\n", cgwname );			
			pthread_mutex_unlock( &tMutexDatos );
			return 0;
		}
		pthread_mutex_unlock( &tMutexDatos );

	}
	return -1;
}

/*
 * Funcion: CPublica::BajaPublicaRecurso
 * Descripcion: Baja de un recurso para publicar los recursos de una pasarela
 * Parametros: nombre del recurso
 * Retorna: resultado
 */

int CPublica::BajaPublicaRecurso(char *cgwname)
{

  struct stMiEstadoPublicoRecurso *ptrecurso;
  
	pthread_mutex_lock( &tMutexDatos );
	ptrecurso = LocalizaRecurso(cgwname);
	if (ptrecurso ==NULL)
	{
		pthread_mutex_unlock( &tMutexDatos );
		return 0;
	}
	memset(ptrecurso, 0, sizeof(struct stMiEstadoPublicoRecurso));
	ptrecurso->cambio = 1;
	pthread_mutex_unlock( &tMutexDatos );
	return 0;
} 

/*
 * Funcion: CPublica::BajaPublicaRecurso
 * Descripcion: Baja de un recurso para publicar los recursos de una pasarela
 * Parametros: numero del recurso
 * Retorna: resultado
 */

int CPublica::BajaPublicaRecurso(unsigned int numRec)
{

  struct stMiEstadoPublicoRecurso *ptrecurso;
	if (numRec < MAX_RECURSOS_TIFX)
	{
		pthread_mutex_lock( &tMutexDatos );
   		memset(&MiEstadoPublicoRecurso[numRec], 0, sizeof(struct stMiEstadoPublicoRecurso));
		MiEstadoPublicoRecurso[numRec].cambio = 1;    			
		pthread_mutex_unlock( &tMutexDatos );
			return 0;
	}
	return -1;
} 

/*
 * Funcion: CPublica::ActualizaPublicaRecurso
 * Descripcion: Actualiza los datos de un recurso para publicarlos
 * Parametros: nombre del recurso, estructura de datos
 * tipo: tipo del recurso (PUBLICA_TLF / PUBLICA_LC)
 * numRec: numero del recurso en la pasarela 0 a MAX_RECURSOS_TIFX -1
 * Retorna: resultado
 */

int CPublica::ActualizaPublicaRecurso(char *cgwname, struct DatosEstadoRecurso *ptDatosEstado, int tipo, unsigned int numRec)
{
  struct stMiEstadoPublicoRecurso *ptpublica;
	//llamo a alta por si no estaba dado de alta
	AltaPublicaRecurso(cgwname, tipo, numRec);
	
	pthread_mutex_lock( &tMutexDatos );
	ptpublica = LocalizaRecurso(cgwname);
	if (ptpublica ==NULL)
	{
		pthread_mutex_unlock( &tMutexDatos );
		PrintefeNiv( CONSOLA_BASICA, "Publica: recurso no dado de alta\n");
		return -1;
	}

	if (ptpublica->estado != (unsigned int)ptDatosEstado->estado)
	{
		ptpublica->estado = (unsigned int)ptDatosEstado->estado;
		ptpublica->cambio = 1;
	}
	switch(ptpublica->tipo)
	{
		case PUBLICA_TLF:
			if (ptpublica->prio_cpipl != ptDatosEstado->prio_cpipl)
			{
				ptpublica->prio_cpipl = ptDatosEstado->prio_cpipl;
				ptpublica->cambio = 1;
			}
			if (ptpublica->ContadorTransitos != ptDatosEstado->ContadorTransitos)
			{
				ptpublica->ContadorTransitos = ptDatosEstado->ContadorTransitos;
				ptpublica->cambio = 1;
			}
			if (ptpublica->tiempo != ptDatosEstado->tiempo)
			{
				ptpublica->tiempo = ptDatosEstado->tiempo;
				ptpublica->cambio = 1;
			}
		break;
	}
	pthread_mutex_unlock( &tMutexDatos );
	tcpcPrintfNiv( CONSOLA_DEBUG, "    ACTUALIZA  PUBLICA  %s\n", cgwname );			
	return 0;
}

/*
 * Funcion: CPublica::PublicaRecursos
 * Descripcion: Publica los estados de los recursos  de una pasarela
 * Parametros: 
 * Retorna: resultado
 */

int CPublica::FormaPublicaRecursos()
{
  int i, nrecursos, len;
  struct st_PBInfoEstadoRecurso *ptrecurso;
  unsigned char *ptmsg, *ptchar;
  struct headerMsgPublicaRecursos *ptheader;
  unsigned char buffer[sizeof(struct headerMsgPublicaRecursos)+	(sizeof(struct stMiEstadoPublicoRecurso)*MAX_RECURSOS_TIFX)];
  /*
  	ptmsg =(unsigned char *)malloc(sizeof(struct headerMsgPublicaRecursos)+
							(sizeof(struct stMiEstadoPublicoRecurso)*MAX_RECURSOS_TIFX));
	if (ptmsg == NULL)
	{
		PrintefeNiv( CONSOLA_BASICA, " No es posible publicar recursos: falta memoria \n");
		return -1;
	}
*/
    ptmsg = buffer;

	memset(ptmsg, 0, sizeof(struct headerMsgPublicaRecursos)+
							(sizeof(struct stMiEstadoPublicoRecurso)*MAX_RECURSOS_TIFX));
	cambioCGW =0;
	ptchar = ptmsg;
  	ptheader = (struct headerMsgPublicaRecursos *)ptmsg;
  
	ptheader->tipo_msg = PUBLICA_EST_CGW;
	strcpy((char *)ptheader->CGWnombre, (char *)CGWNombre);
	
	// averiguo numero de recursos
	pthread_mutex_lock( &tMutexDatos );
	
	nrecursos=0;
	ptheader->nrecursos = nrecursos;
	ptchar = ptchar + sizeof(struct headerMsgPublicaRecursos); 
	
	for (i=0; i< MAX_RECURSOS_TIFX; i++)
	{
		if (MiEstadoPublicoRecurso[i].nombre[0] != 0)
		{
			/* si esta fuera de servicio es como si no esta */
			if (MiEstadoPublicoRecurso[i].estado == EST_FUERA_DE_SERVICIO) 
			{
				if (MiEstadoPublicoRecurso[i].cambio !=0)
				{
					MiEstadoPublicoRecurso[i].cambio =0;
					cambioCGW =1;
				}
			}
			else
			{
				ptrecurso = (struct st_PBInfoEstadoRecurso *)ptchar;
				memset(ptrecurso, 0, sizeof(struct st_PBInfoEstadoRecurso));
				strcpy((char *)ptrecurso->nombre,(char *)MiEstadoPublicoRecurso[i].nombre);
				ptrecurso->tipo = MiEstadoPublicoRecurso[i].tipo;
				ptrecurso->estado = MiEstadoPublicoRecurso[i].estado;
				if (MiEstadoPublicoRecurso[i].cambio !=0)
				{
					MiEstadoPublicoRecurso[i].version++;
					MiEstadoPublicoRecurso[i].cambio =0;
					cambioCGW =1;
				}
				ptrecurso->version = MiEstadoPublicoRecurso[i].version;
				ptrecurso->prio_cpipl = MiEstadoPublicoRecurso[i].prio_cpipl;
				ptrecurso->ContadorTransitos = MiEstadoPublicoRecurso[i].ContadorTransitos;
				ptrecurso->tiempo = MiEstadoPublicoRecurso[i].tiempo;
				nrecursos++;
				ptchar = ptchar + sizeof(struct st_PBInfoEstadoRecurso);
			}
		}
		else
		{
			if (MiEstadoPublicoRecurso[i].cambio !=0)
			{
				MiEstadoPublicoRecurso[i].cambio =0;
				cambioCGW =1;
			}
		}
    	}
	ptheader->nrecursos = nrecursos;	
	if (cambioCGW ==1) version++;
	ptheader->version = version;	
	// Lanza el mensaje de Publica recursos por multicast
	len = ptchar - ptmsg;
	pthread_mutex_unlock( &tMutexDatos );

	EnviaPublicacionRecursos(ptmsg, len);

/*	free(ptmsg);*/
	return 0;
}



/*
 * Funcion: CPublica::PublicaRecursos
 * Descripcion: Publica los estados de los recursos  de una pasarela
 * Parametros: 
 * Retorna: resultado
 */

int CPublica::PublicaRecursos()
{
//despierto al proceso de para publicar 
    pthread_mutex_lock( &tMutexPublica );
    pthread_cond_signal( &tCondVarPublica );                  
    pthread_mutex_unlock( &tMutexPublica );
    return 0;
}

/*
 * Funcion: CEntidadRC::HiloPublica
 * Descripcion: Arranca el hilo queenvia periodicamente y con los cambios los estados de los recursos de una pasarela
 * Parametros: Ninguno.
 * Retorna: Nada.
 */

void CPublica::HiloPublica()
{
    int iIndicePerro, iContPerr,temp_superv;
    struct timeval now;
    struct timespec timeout;
    struct timezone sParaNada; 
    pid_t tPidPublica = -1;   
    
    trpProceso( "Publica", 0 );
    tPidPublica = getpid();
    INCIDENCIA_N2( "Arrancado hilo publica", tPidPublica, 0, 0 );
    tcpcPrintfNiv( CONSOLA_MCAST, "Arrancado hilo publica PID=%d\n", tPidPublica );
    // iIndicePerro = perrDameIndice( "Publica", 0, 4000, uptUpTime() );

    iContPerr=0;

    	if ((sock = socket(PF_INET, SOCK_DGRAM, IPPROTO_UDP)) < 0)
        {
		tcpcPerrorNiv( CONSOLA_BASICA, "Publica: socket() failed.  " );
		INCIDENCIA_N0( "Error en socket()", errno, 0, 0 );
	}
        
        temp_superv = poTemp->Configura_Temp(TM_SUPERVISION_PUBLICA, VenceTemp_Supervision,(long int)this, 0,0);

 
    while(1)
        {
            gettimeofday(&now, &sParaNada);
            timeout.tv_sec = now.tv_sec + 5;
            timeout.tv_nsec = now.tv_usec;
        
            pthread_mutex_lock(&tMutexPublica);
            pthread_cond_timedwait (&tCondVarPublica,&tMutexPublica, &timeout);
            pthread_mutex_unlock(&tMutexPublica);
	    
            FormaPublicaRecursos();
	    
	    //perrRefresca( iIndicePerro, uptUpTime() );
        }    
}


/*
 * Funcion: CPublica::Funcion_Supervisor
 * Descripcion: supervisa que las pasarelas continuamente envian msg, si no es así las da de baja
 * Parametros: 
 * Retorna: resultado
	struct st_PBInfoEstadosCGW infoEstadosCGW[N_MAX_TIFX];
 */

int CPublica::Funcion_Supervisor(void)
{
int i,temp_superv;
struct st_PBInfoEstadosCGW *ptinfoCGW;

	ptinfoCGW = &infoEstadosCGW[0];
	for (i=0; i<N_MAX_TIFX; i++)
	{
		if (ptinfoCGW->nombre[0]!=0)
		{
			if (ptinfoCGW->timer>0)
				if (--ptinfoCGW->timer==0)
					BajaInfoEstadosCGW(ptinfoCGW->nombre);
		}
		ptinfoCGW++;
	}
        temp_superv = poTemp->Configura_Temp(TM_SUPERVISION_PUBLICA, VenceTemp_Supervision,(long int)this, 0,0);

    return 0;
}
/*
 * Funcion: CPublica::LocalizaRecurso
 * Descripcion: localiza el esrado de un recurso dentro de los que se publican
 * Parametros: 
 * Retorna: resultado
 */

struct stMiEstadoPublicoRecurso *CPublica::LocalizaRecurso(char *ptnombre)
{
  int i;

	for (i=0; i< MAX_RECURSOS_TIFX; i++)
	{
		if (MiEstadoPublicoRecurso[i].nombre[0] != 0)
		{
				if (strcmp((char *)MiEstadoPublicoRecurso[i].nombre, (char *)ptnombre) == 0)
				return &MiEstadoPublicoRecurso[i];
		}
	}
	return NULL;
}

/*
 * Funcion: CPublica::EnviaPublicacionRecursos
 * Descripcion: Publica los estados de los recursos  de una pasarela
 * Parametros: 
 * Retorna: resultado
 */

int CPublica::EnviaPublicacionRecursos(unsigned char *buf, int len)
{
	char multicast_ip[CFG_MAX_LONG_URL+1];
	unsigned short multicast_port;
	struct sockaddr_in multicast_addr;
	
#ifdef REDAN
    return 0;
#endif

    if (oGestorDual.DimeInfoEstadoDual() == DUAL_MODO_RESERVA)
        return 0;

	strncpy( multicast_ip, sCfgPasarela.acGrupoMulticast, CFG_MAX_LONG_URL );
        multicast_ip[CFG_MAX_LONG_URL]=0;
        multicast_port = sCfgPasarela.uiPuertoMulticast+1;
        memset(&multicast_addr, 0, sizeof(multicast_addr));
        multicast_addr.sin_family      = AF_INET;
        multicast_addr.sin_addr.s_addr = inet_addr(multicast_ip);
        multicast_addr.sin_port        = htons(multicast_port);

	if (sendto(sock, buf, len, 0, (struct sockaddr *)&multicast_addr, sizeof(multicast_addr)) < 0)
	{
		tcpcPerrorNiv( CONSOLA_BASICA, "Publica: sendto() failed.  " );
		INCIDENCIA_N0( "Error en sendto()", errno, 0, 0 );
		return -1;
	}
	else
	{
		//INCIDENCIA_N0( "sendto() Ok", multicast_port, 0, 0 );
		//tcpcPrintfNiv( CONSOLA_BASICA, "Publica: Envia %d bytes a %s:\n", len,inet_ntoa(multicast_addr.sin_addr));
		return 0;
	}
}

/*
 * Funcion: CPublica::RecibeInfoEstadoCGW
 * Descripcion: Publica los estados de los recursos  de una pasarela
 * Parametros: 
 * Retorna: resultado
 */

void CPublica::RecibeInfoEstadoCGW(char *buf)
{
  struct headerMsgPublicaRecursos *ptheader;
  struct st_PBInfoEstadosCGW *ptInfoEstadosCGW;
  struct st_PBInfoEstadoRecurso *ptInfoEstadoRec;
  char *ptchar;
  int i;
  
  ptheader = (struct headerMsgPublicaRecursos *)buf;
  if (ptheader->tipo_msg != PUBLICA_EST_CGW)
  	return;
  
  ptInfoEstadosCGW = LocalizaInfoCGW(ptheader->CGWnombre);
  if (ptInfoEstadosCGW == NULL)
  {
  	ptInfoEstadosCGW = AltaInfoEstadosCGW(ptheader->CGWnombre);
	  if (ptInfoEstadosCGW == NULL) return;
  }
  ptInfoEstadosCGW->timer = CONT_SUPERVISION_PUBLICA;
  /*if (ptheader->version == ptInfoEstadosCGW->version)
  	return;*/
    ptInfoEstadosCGW->version = ptheader->version;
  ptchar = buf + sizeof(struct headerMsgPublicaRecursos);
  /* Primero doy de baja los que no vengan en el mensaje de la pasarela */
  for(i=0; i < MAX_RECURSOS_TIFX; i++)
  {
  	if (ptInfoEstadosCGW->aInfoEstadoRecurso[i].nombre[0]!=0)
	{
		//printf("Busco en msg recurso =%s \n",ptInfoEstadosCGW->aptInfoEstadoRecurso[i]->nombre);
		if (LocalizaRecEnMsgRx(ptheader->nrecursos, ptchar,ptInfoEstadosCGW->aInfoEstadoRecurso[i].nombre) == 0)
			BajaInfoEstadoRecurso(ptheader->CGWnombre, ptInfoEstadosCGW->aInfoEstadoRecurso[i].nombre); 
	}
  }
  
  /* Luego actualizo y doy de alta */
  ptInfoEstadoRec = (struct st_PBInfoEstadoRecurso *)ptchar;
  for(i=0; i < ptheader->nrecursos; i++)
  {
	if ((ptInfoEstadoRec->nombre[0] !=0) /*&& (ptInfoEstadoRec->tipo == PUBLICA_TLF)*/)
	/*	if (LocalizaInfoEstadoRecurso(ptheader->CGWnombre, ptInfoEstadoRec->nombre) !=NULL)
			AltaInfoEstadoRecurso(ptheader->CGWnombre, ptInfoEstadoRec);
		else*/
			ActualizaInfoEstadoRecurso(ptheader->CGWnombre, ptInfoEstadoRec);
	ptInfoEstadoRec++;
  }
}

/*
 * Funcion: CPublica::LocalizaRecEnMsgRx
 * Descripcion: Dando el nombre de un recurso, localiza si la info de ese recurso está a pastir del puntero
 * Parametros: 
 * Retorna: puntero a la nueva estructura infoestadoCGW
 */
struct st_PBInfoEstadoRecurso *CPublica::LocalizaRecEnMsgRx(unsigned int nrecmsg, char *buf,char *RECnombre)
{
struct st_PBInfoEstadoRecurso *ptrec;
unsigned int i;
	ptrec =(struct st_PBInfoEstadoRecurso *)buf;
	for (i=0; i < nrecmsg; i++)
	{
		if (strcmp(ptrec->nombre, RECnombre)==0)
			return ptrec;
		ptrec++;
	}
	return NULL;

}


/*
 * Funcion: CPublica::AltaInfoEstadosCGW
 * Descripcion: Da de alta yuna CGW en el array de infocGW
 * Parametros: 
 * Retorna: puntero a la nueva estructura infoestadoCGW
 */
struct st_PBInfoEstadosCGW *CPublica::AltaInfoEstadosCGW(char *CGWnombre)
{
struct st_PBInfoEstadosCGW *ptinfoCGW;
int i;	
	if ((ptinfoCGW = LocalizaInfoCGW(CGWnombre)) != NULL)
		return ptinfoCGW;
	
	ptinfoCGW = &infoEstadosCGW[0];
	for (i=0; i<N_MAX_TIFX; i++)
	{
		if (ptinfoCGW->nombre[0] == 0)
		{
			strcpy(ptinfoCGW->nombre, CGWnombre);
			ptinfoCGW->nrecursos =0;
			ptinfoCGW->version=0;
			return ptinfoCGW;
		}
		ptinfoCGW++;
	}
	return NULL;
}

/*
 * Funcion: CPublica::BajaInfoEstadosCGW
 * Descripcion: Da de alta yuna CGW en el array de infocGW
 * Parametros: 
 * Retorna: puntero a la nueva estructura infoestadoCGW
 */
int CPublica::BajaInfoEstadosCGW(char *CGWnombre)
{
struct st_PBInfoEstadosCGW *ptinfoCGW;

	if ((ptinfoCGW = LocalizaInfoCGW(CGWnombre)) == NULL)
		return 0;
	memset(ptinfoCGW, 0, sizeof(struct st_PBInfoEstadosCGW));
	return 0;
}

/*
 * Funcion: CPublica::LocalizaInfoCGW
 * Descripcion:Localiza una CGW en el array de infocGW
 * Parametros: 
 * Retorna: puntero a la nueva estructura infoestadoCGW
 */
struct st_PBInfoEstadosCGW *CPublica::LocalizaInfoCGW(char *CGWnombre)
{
int i;
struct st_PBInfoEstadosCGW *ptinfoCGW;
	ptinfoCGW = &infoEstadosCGW[0];
	for (i=0; i<N_MAX_TIFX; i++)
	{
		if (strcmp(ptinfoCGW->nombre, CGWnombre) == 0)
			return ptinfoCGW;
		ptinfoCGW++;
	}
	return NULL;
}

/*
 * Funcion: CPublica::LocalizaInfoEstadoRecurso
 * Descripcion:Localiza una reurso dentro del array de info de pasarelas
 * Parametros: 
 * Retorna: puntero a la nueva estructura infoestadoRec
 */
struct st_PBInfoEstadoRecurso *CPublica::LocalizaInfoEstadoRecurso(char *CGWnombre, char *RECnombre)
{
struct st_PBInfoEstadosCGW *ptinfoCGW;
struct st_PBInfoEstadoRecurso *ptinfoREC;
int rec, i, nvueltas;

	if ((CGWnombre == NULL) || (CGWnombre[0] == 0))
	{
		nvueltas = N_MAX_TIFX;
		ptinfoCGW = &infoEstadosCGW[0];
	}
	else
	{
		if ((ptinfoCGW = LocalizaInfoCGW(CGWnombre)) == NULL)
			return 0;
		nvueltas=1;
	}	
	for(i=0; i < nvueltas; i++)
	{
		for (rec=0; rec < MAX_RECURSOS_TIFX; rec++)
		{
			ptinfoREC = &ptinfoCGW->aInfoEstadoRecurso[rec];
			if (ptinfoREC->nombre[0]!=0)
				if (strcmp(ptinfoREC->nombre, RECnombre)==0)
					return ptinfoREC;
		}
		ptinfoCGW++;
	}
	return NULL;
}

/*
 * Funcion: CPublica::AltaInfoEstadoRecurso
 * Descripcion:Localiza una reurso dentro del array de info de pasarelas
 * Parametros: 
 * Retorna: puntero a la nueva estructura infoestadoRec
 */
struct st_PBInfoEstadoRecurso *CPublica::AltaInfoEstadoRecurso(char *CGWnombre, struct st_PBInfoEstadoRecurso *ptinfoREC)
{
struct st_PBInfoEstadoRecurso *pt;
struct st_PBInfoEstadosCGW *ptinfoCGW;
int rec;

	pt = LocalizaInfoEstadoRecurso(CGWnombre, ptinfoREC->nombre);
	if (pt==NULL)
	{	
		ptinfoCGW = LocalizaInfoCGW(CGWnombre);
		if (ptinfoCGW == NULL)
			return 0;
		/* busca un hueco para poner un puntero de inforecurso */
		for (rec=0; rec < MAX_RECURSOS_TIFX; rec++)
		{
			pt = &ptinfoCGW->aInfoEstadoRecurso[rec];
			if (pt->nombre[0]==0)
				break;
			else
			    pt =  NULL;
		}
	}
	if (pt != NULL)
		memcpy(pt, ptinfoREC, sizeof(struct st_PBInfoEstadoRecurso));
	return pt;
}

/*
 * Funcion: CPublica::actualizaInfoEstadoRecurso
 * Descripcion:Localiza una reurso dentro del array de info de pasarelas
 * Parametros: 
 * Retorna: puntero a la nueva estructura infoestadoRec
 */
struct st_PBInfoEstadoRecurso *CPublica::ActualizaInfoEstadoRecurso(char *CGWnombre, struct st_PBInfoEstadoRecurso *ptinfoREC)
{
	return(AltaInfoEstadoRecurso(CGWnombre, ptinfoREC));
}

/*
 * Funcion: CPublica::BajaInfoEstadoRecurso
 * Descripcion:da de baja la info de un recurso dentro del array de info de pasarelas
 * Parametros: 
 * Retorna: puntero a la nueva estructura infoestadoRec
 */
int CPublica::BajaInfoEstadoRecurso(char *CGWnombre, char *RECnombre )
{
struct st_PBInfoEstadosCGW *ptinfoCGW;
int rec;
struct st_PBInfoEstadoRecurso *pt;

	tcpcPrintfNiv( CONSOLA_DEBUG, " BajaInfoEstadoRecurso CGW %s REC %s\n", CGWnombre, RECnombre);
	if ((ptinfoCGW = LocalizaInfoCGW(CGWnombre)) == NULL)
		return -1;
	for (rec=0; rec < MAX_RECURSOS_TIFX; rec++)
	{
		pt = &ptinfoCGW->aInfoEstadoRecurso[rec];
		if (pt->nombre[0]!=0)
			if (strcmp(pt->nombre, RECnombre)==0)
			{
				pt->nombre[0] = 0;
				return 0;
			}
	}
	return -1;
}

/*
 * Funcion: CPublica::PintaPublicacionRecursos
 * Descripcion: pinta los estados de los recursos publicados
 * Parametros: 
 * Retorna: resultado
	struct st_PBInfoEstadosCGW infoEstadosCGW[N_MAX_TIFX];
 */

int CPublica::PintaPublicacionRecursos(void)
{
int i,rec;
struct st_PBInfoEstadosCGW *ptinfoCGW;
struct st_PBInfoEstadoRecurso *pt;

	ptinfoCGW = &infoEstadosCGW[0];
	for (i=0; i<N_MAX_TIFX; i++)
	{
		if (ptinfoCGW->nombre[0]!=0)
		{
			tcpcPrintf( "\nCGW=%s:  version=%i\n", ptinfoCGW->nombre, ptinfoCGW->version);
			for (rec=0; rec < MAX_RECURSOS_TIFX; rec++)
			{
				pt = &ptinfoCGW->aInfoEstadoRecurso[rec];
				if (pt->nombre[0]!=0)
				{
					tcpcPrintf( "REC %s estado %i prioridad %i\n", pt->nombre,pt->estado, pt->prio_cpipl);
				}
			}
		}
		ptinfoCGW++;
	}
}
