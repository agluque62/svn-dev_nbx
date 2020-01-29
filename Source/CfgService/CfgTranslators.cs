using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;
using U5ki.Infrastructure;
using U5ki.Infrastructure.Helpers;

namespace U5ki.CfgService
{
	static class CfgTranslators
	{
        /// <summary>
        /// Pasa una configuracion de formato WS a formato 'protobuf', de intercambio en 'spread'
        /// </summary>
        /// <param name="cfg">Datos en Formato 'protobuf'. Salida...</param>
        /// <param name="soapCfg">Datos en Formato del WebService de Configuracion</param>
		public static void Translate(ConfiguracionSistema cfg, SoapCfg.ConfiguracionSistema soapCfg)
		{
            /** */
			cfg.ParametrosGenerales = new ParametrosGeneralesSistema();
			Translate(cfg.ParametrosGenerales, soapCfg.ParametrosGenerales);

            /** */
			if (soapCfg.PlanNumeracionATS != null)
			{
				foreach (SoapCfg.NumeracionATS soapi in soapCfg.PlanNumeracionATS)
				{
					NumeracionATS i = new NumeracionATS();
					Translate(i, soapi);
					cfg.PlanNumeracionATS.Add(i);
				}
			}

            /** */
			if (soapCfg.PlanDireccionamientoIP != null)
			{
				foreach (SoapCfg.DireccionamientoIP soapi in soapCfg.PlanDireccionamientoIP)
				{
					DireccionamientoIP i = new DireccionamientoIP();
					Translate(i, soapi);
					cfg.PlanDireccionamientoIP.Add(i);
				}
			}

            /** */
			if (soapCfg.PlanTroncales != null)
			{
				foreach (SoapCfg.ListaTroncales soapi in soapCfg.PlanTroncales)
				{
					ListaTroncales i = new ListaTroncales();
					Translate(i, soapi);
					cfg.PlanTroncales.Add(i);
				}
			}

            /** */
			if (soapCfg.PlanRedes != null)
			{
				foreach (SoapCfg.ListaRedes soapi in soapCfg.PlanRedes)
				{
					ListaRedes i = new ListaRedes();
					Translate(i, soapi);
					cfg.PlanRedes.Add(i);
				}
			}

            /** */
			if (soapCfg.PlanAsignacionUsuarios != null)
			{
				foreach (SoapCfg.AsignacionUsuariosTV soapi in soapCfg.PlanAsignacionUsuarios)
				{
					AsignacionUsuariosTV i = new AsignacionUsuariosTV();
					Translate(i, soapi);
					cfg.PlanAsignacionUsuarios.Add(i);
				}
			}

            /** */
			if (soapCfg.PlanAsignacionRecursos != null)
			{
				foreach (SoapCfg.AsignacionRecursosGW soapi in soapCfg.PlanAsignacionRecursos)
				{
					AsignacionRecursosGW i = new AsignacionRecursosGW();
					Translate(i, soapi);
					cfg.PlanAsignacionRecursos.Add(i);
				}
			}

            /** */
			if (soapCfg.PlanDireccionamientoSIP != null)
			{
				foreach (SoapCfg.DireccionamientoSIP soapi in soapCfg.PlanDireccionamientoSIP)
				{
					DireccionamientoSIP i = new DireccionamientoSIP();
					Translate(i, soapi);
					cfg.PlanDireccionamientoSIP.Add(i);
				}
			}
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="soapUser"></param>
        /// <param name="soapExLinks"></param>
        /// <param name="soapInLinks"></param>
		public static void Translate(ConfiguracionUsuario cfg, SoapCfg.CfgUsuario soapUser, SoapCfg.CfgEnlaceExterno[] soapExLinks, SoapCfg.CfgEnlaceInterno[] soapInLinks)
		{
			cfg.User = new CfgUsuario();
			Translate(cfg.User, soapUser);

			if (soapExLinks != null)
			{
				foreach (SoapCfg.CfgEnlaceExterno soapi in soapExLinks)
				{
					CfgEnlaceExterno i = new CfgEnlaceExterno();
					Translate(i, soapi);
					cfg.RdLinks.Add(i);
				}
			}
			if (soapInLinks != null)
			{
				foreach (SoapCfg.CfgEnlaceInterno soapi in soapInLinks)
				{
                    //Proteccion ante un error del SOAP que envia tags vacios
                    if (soapi != null)
                    {
                        CfgEnlaceInterno i = new CfgEnlaceInterno();
                        Translate(i, soapi);
                        cfg.TlfLinks.Add(i);
                    }
				}
			}
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="soapCfg"></param>
		public static void Translate(ParametrosGeneralesSistema cfg, SoapCfg.ParametrosGeneralesSistema soapCfg)
		{
			cfg.TiempoMaximoPTT = soapCfg.TiempoMaximoPTT;
			cfg.TiempoSinJack1 = soapCfg.TiempoSinJack1;
			cfg.TiempoSinJack2 = soapCfg.TiempoSinJack2;
			cfg.TamLiteralEnlExt = soapCfg.TamLiteralEnlExt;
			cfg.TamLiteralEnlDA = soapCfg.TamLiteralEnlDA;
			cfg.TamLiteralEnlIA = soapCfg.TamLiteralEnlIA;
			cfg.TamLiteralEnlAG = soapCfg.TamLiteralEnlAG;
			cfg.TamLiteralEmplazamiento = soapCfg.TamLiteralEmplazamiento;
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="soapCfg"></param>
		public static void Translate(NumeracionATS cfg, SoapCfg.NumeracionATS soapCfg)
		{
			cfg.CentralPropia = soapCfg.CentralPropia;
			cfg.Throwswitching = soapCfg.Throwswitching;
            cfg.Central = soapCfg.Central;

			if (soapCfg.RangosOperador != null)
			{
				foreach (SoapCfg.Rangos soapi in soapCfg.RangosOperador)
				{
					RangosSCV i = new RangosSCV();
					Translate(i, soapi);
					cfg.RangosOperador.Add(i);
				}
			}
			if (soapCfg.RangosPrivilegiados != null)
			{
				foreach (SoapCfg.Rangos soapi in soapCfg.RangosPrivilegiados)
				{
                    RangosSCV i = new RangosSCV();
					Translate(i, soapi);
					cfg.RangosPrivilegiados.Add(i);
				}
			}
			if (soapCfg.ListaRutas != null)
			{
				foreach (SoapCfg.PlanRutas soapi in soapCfg.ListaRutas)
				{
					PlanRutas i = new PlanRutas();
					Translate(i, soapi);
					cfg.ListaRutas.Add(i);
				}

				// Las rutas ya vienen ordenadas desde la configuración
				//cfg.ListaRutas.Sort(delegate(PlanRutas a, PlanRutas b) { return ((a.TipoRuta == b.TipoRuta) ? 0 : ((a.TipoRuta == "D") ? -1 : 1)); });
			}
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="soapCfg"></param>
		public static void Translate(RangosSCV cfg, SoapCfg.Rangos soapCfg)
		{
			ulong inicial, final;
			ulong.TryParse(soapCfg.Inicial, out inicial);
			ulong.TryParse(soapCfg.Final, out final);

			cfg.IdPrefijo = soapCfg.IdPrefijo;
			cfg.IdAbonado = soapCfg.IdAbonado ?? "";
			cfg.Inicial = inicial;
			cfg.Final = final;
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="soapCfg"></param>
		public static void Translate(PlanRutas cfg, SoapCfg.PlanRutas soapCfg)
		{
			cfg.TipoRuta = soapCfg.TipoRuta ?? "";
			cfg.ListaTroncales.AddRange(soapCfg.ListaTroncales);
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="soapCfg"></param>
		public static void Translate(DireccionamientoIP cfg, SoapCfg.DireccionamientoIP soapCfg)
		{
			cfg.IdHost = soapCfg.IdHost ?? "";
			cfg.IpRed1 = soapCfg.IpRed1 ?? "";
			cfg.IpRed2 = soapCfg.IpRed2 ?? "";
            cfg.IpRed3 = soapCfg.IpRed3 ?? "";
            cfg.TipoHost = (Tipo_Elemento_HW)soapCfg.TipoHost;
            cfg.Interno = soapCfg.Interno;
            cfg.Min = soapCfg.Min;
            cfg.Max = soapCfg.Max;
            cfg.EsCentralIP = soapCfg.EsCentralIP;
            cfg.SrvPresenciaIpRed1 = soapCfg.SrvPresenciaIpRed1;
            cfg.SrvPresenciaIpRed2 = soapCfg.SrvPresenciaIpRed2;
            cfg.SrvPresenciaIpRed3 = soapCfg.SrvPresenciaIpRed3;
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="soapCfg"></param>
		public static void Translate(ListaTroncales cfg, SoapCfg.ListaTroncales soapCfg)
		{
			cfg.IdTroncal = soapCfg.IdTroncal ?? "";
			cfg.NumeroTest = soapCfg.NumeroTest ?? "";

			if (soapCfg.ListaRecursos != null)
			{
				foreach (SoapCfg.PlanRecursos soapi in soapCfg.ListaRecursos)
				{
					PlanRecursos i = new PlanRecursos();
					Translate(i, soapi);
					cfg.ListaRecursos.Add(i);
				}
			}
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="soapCfg"></param>
		public static void Translate(PlanRecursos cfg, SoapCfg.PlanRecursos soapCfg)
		{
			cfg.IdRecurso = soapCfg.IdRecurso ?? "";
			cfg.Tipo = (TipoInterface)soapCfg.Tipo;
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="soapCfg"></param>
		public static void Translate(ListaRedes cfg, SoapCfg.ListaRedes soapCfg)
		{
			cfg.IdRed = soapCfg.IdRed ?? "";
			cfg.Prefijo = soapCfg.Prefijo;

			if (soapCfg.ListaRecursos != null)
			{
				foreach (SoapCfg.PlanRecursos soapi in soapCfg.ListaRecursos)
				{
					PlanRecursos i = new PlanRecursos();
					Translate(i, soapi);
					cfg.ListaRecursos.Add(i);
				}
			}
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="soapCfg"></param>
		public static void Translate(AsignacionUsuariosTV cfg, SoapCfg.AsignacionUsuariosTV soapCfg)
		{
			cfg.IdUsuario = soapCfg.IdUsuario ?? "";
			cfg.IdHost = soapCfg.IdHost ?? "";
            cfg.IpGrabador1 = soapCfg.IpGrabador1;
            cfg.IpGrabador2 = soapCfg.IpGrabador2;
            cfg.RtspPort = soapCfg.RtspPort;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="soapCfg"></param>
		public static void Translate(AsignacionRecursosGW cfg, SoapCfg.AsignacionRecursosGW soapCfg)
		{
			cfg.IdRecurso = soapCfg.IdRecurso ?? "";
			cfg.IdHost = soapCfg.IdHost ?? "";
            cfg.SipPort = soapCfg.SipPort;
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="soapCfg"></param>
		public static void Translate(DireccionamientoSIP cfg, SoapCfg.DireccionamientoSIP soapCfg)
		{
			cfg.IdUsuario = soapCfg.IdUsuario ?? "";

			if (soapCfg.NumerosAbonadoQueAtiende != null)
			{
				foreach (SoapCfg.StrNumeroAbonado soapi in soapCfg.NumerosAbonadoQueAtiende)
				{
					StrNumeroAbonado i = new StrNumeroAbonado();
					Translate(i, soapi);
					cfg.NumerosAbonadoQueAtiende.Add(i);
				}
			}
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="soapCfg"></param>
		public static void Translate(StrNumeroAbonado cfg, SoapCfg.StrNumeroAbonado soapCfg)
		{
            cfg.IdAgrupacion = soapCfg.IdAgrupacion;
			cfg.Prefijo = soapCfg.Prefijo;
			cfg.NumeroAbonado = soapCfg.NumeroAbonado ?? "";
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="soapCfg"></param>
		public static void Translate(CfgUsuario cfg, SoapCfg.CfgUsuario soapCfg)
		{
			cfg.Nombre = soapCfg.Nombre ?? "";
			cfg.IdIdentificador = soapCfg.IdIdentificador ?? "";
			cfg.NumeroEnlacesInternos = soapCfg.NumeroEnlacesInternos;
			cfg.NumeroEnlacesExternos = soapCfg.NumeroEnlacesExternos;

			cfg.Sector = new SectoresSCV();
			Translate(cfg.Sector, soapCfg.Sector);

			cfg.ParametrosDelSector = new ParametrosSectorSCVKeepAlive();
			Translate(cfg.ParametrosDelSector, soapCfg.ParametrosDelSector);

			cfg.TeclasDelSector = new TeclasSectorSCV();
			Translate(cfg.TeclasDelSector, soapCfg.TeclasDelSector);

			cfg.NivelesDelSector = new NivelesSCV();
			Translate(cfg.NivelesDelSector, soapCfg.NivelesDelSector);

			if (soapCfg.ListaAbonados != null)
			{
				foreach (SoapCfg.NumerosAbonado soapi in soapCfg.ListaAbonados)
				{
					NumerosAbonado i = new NumerosAbonado();
					Translate(i, soapi);
					cfg.ListaAbonados.Add(i);
				}
			}
			if (soapCfg.PermisosRedDelSector != null)
			{
				foreach (SoapCfg.PermisosRedesSCV soapi in soapCfg.PermisosRedDelSector)
				{
					PermisosRedesSCV i = new PermisosRedesSCV();
					Translate(i, soapi);
					cfg.PermisosRedDelSector.Add(i);
				}
			}
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="soapCfg"></param>
		public static void Translate(SectoresSCV cfg, SoapCfg.SectoresSCV soapCfg)
		{
			cfg.IdParejaUCS = soapCfg.IdParejaUCS ?? "";
			cfg.TipoPosicion = soapCfg.TipoPosicion ?? "";
			cfg.PrioridadR2 = soapCfg.PrioridadR2 - 1;
			cfg.TipoHMI = soapCfg.TipoHMI;
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="soapCfg"></param>
		public static void Translate(ParametrosSectorSCVKeepAlive cfg, SoapCfg.ParametrosSectorSCVKeepAlive soapCfg)
		{
			cfg.NumLlamadasEntrantesIda = soapCfg.NumLlamadasEntrantesIda;
			cfg.NumLlamadasEnIda = soapCfg.NumLlamadasEnIda;
			cfg.NumFrecPagina = soapCfg.NumFrecPagina;
			cfg.NumPagFrec = soapCfg.NumPagFrec;
			cfg.NumEnlacesInternosPag = soapCfg.NumEnlacesInternosPag;
			cfg.NumPagEnlacesInt = soapCfg.NumPagEnlacesInt;
			cfg.Intrusion = soapCfg.Intrusion;
			cfg.Intruido = soapCfg.Intruido;
			cfg.KeepAlivePeriod = soapCfg.KeepAlivePeriod;
			cfg.KeepAliveMultiplier = soapCfg.KeepAliveMultiplier;
            //cfg.GrabacionEd137 = soapCfg.
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="soapCfg"></param>
		public static void Translate(TeclasSectorSCV cfg, SoapCfg.TeclasSectorSCV soapCfg)
		{
			cfg.TransConConsultaPrev = soapCfg.TransConConsultaPrev;
			cfg.TransDirecta = soapCfg.TransDirecta;
			cfg.Conferencia = soapCfg.Conferencia;
			cfg.Escucha = soapCfg.Escucha;
			cfg.Retener = soapCfg.Retener;
			cfg.Captura = soapCfg.Captura;
			cfg.Redireccion = soapCfg.Redireccion;
			cfg.RepeticionUltLlamada = soapCfg.RepeticionUltLlamada;
			cfg.RellamadaAut = soapCfg.RellamadaAut;
			cfg.TeclaPrioridad = soapCfg.TeclaPrioridad;
			cfg.Tecla55mas1 = soapCfg.Tecla55mas1;
			cfg.Monitoring = soapCfg.Monitoring;
			cfg.CoordinadorTF = soapCfg.CoordinadorTF;
			cfg.CoordinadorRD = soapCfg.CoordinadorRD;
			cfg.IntegracionRDTF = soapCfg.IntegracionRDTF;
			cfg.LlamadaSelectiva = soapCfg.LlamadaSelectiva;
			cfg.GrupoBSS = soapCfg.GrupoBSS;
			cfg.LTT = soapCfg.LTT;
			cfg.SayAgain = soapCfg.SayAgain;
			cfg.InhabilitacionRedirec = soapCfg.InhabilitacionRedirec;
			cfg.Glp = soapCfg.Glp;
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="soapCfg"></param>
		public static void Translate(NivelesSCV cfg, SoapCfg.NivelesSCV soapCfg)
		{
			cfg.CICL = soapCfg.CICL;
			cfg.CIPL = soapCfg.CIPL;
			cfg.CPICL = soapCfg.CPICL;
			cfg.CPIPL = soapCfg.CPIPL;
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="soapCfg"></param>
		public static void Translate(NumerosAbonado cfg, SoapCfg.NumerosAbonado soapCfg)
		{
			cfg.Prefijo = soapCfg.Prefijo;
			cfg.Numero = soapCfg.Numero ?? "";
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="soapCfg"></param>
		public static void Translate(PermisosRedesSCV cfg, SoapCfg.PermisosRedesSCV soapCfg)
		{
			cfg.IdRed = soapCfg.IdRed ?? "";
			cfg.Llamar = soapCfg.Llamar;
			cfg.Recibir = soapCfg.Recibir;
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="soapCfg"></param>
		public static void Translate(CfgEnlaceExterno cfg, SoapCfg.CfgEnlaceExterno soapCfg)
		{
			cfg.Literal = soapCfg.Literal ?? "";
			cfg.TipoFrecuencia = (Tipo_Frecuencia)soapCfg.TipoFrecuencia;
			cfg.ExclusividadTxRx = soapCfg.ExclusividadTxRx;
			cfg.EstadoAsignacion = soapCfg.EstadoAsignacion ?? "";
            //cfg.Prioridad = soapCfg.Prioridad - 1;
            cfg.Prioridad = soapCfg.Prioridad;
            cfg.SupervisionPortadora = soapCfg.SupervisionPortadora;
            cfg.FrecuenciaSintonizada = soapCfg.FrecuenciaSintonizada;
			cfg.ListaPosicionesEnHmi.AddRange(soapCfg.ListaPosicionesEnHmi);
			cfg.DestinoAudio.AddRange(soapCfg.DestinoAudio);

            cfg.MetodoCalculoClimax = soapCfg.MetodoCalculoClimax;
            cfg.VentanaSeleccionBss = soapCfg.VentanaSeleccionBss;
            cfg.SincronizaGrupoClimax = soapCfg.SincronizaGrupoClimax;
            cfg.AudioPrimerSqBss = soapCfg.AudioPrimerSqBss;
            cfg.FrecuenciaNoDesasignable = soapCfg.FrecuenciaNoDesasignable;
            cfg.VentanaReposoZonaTxDefecto = soapCfg.VentanaReposoZonaTxDefecto;
            cfg.PrioridadSesionSip = soapCfg.PrioridadSesionSIP;
            cfg.CldSupervisionTime = soapCfg.CldSupervisionTime;
            cfg.MetodosBssOfrecidos = soapCfg.MetodosBssOfrecidos;
            //cfg.NombreZonaTxDefecto = soapCfg.NombreZonaTxDefecto;
            /*
            if (soapCfg.MetodosBss != null)
            {
                foreach (SoapCfg.MetodosBssDelRecurso m in soapCfg.MetodosBss)
                {
                    MetodosBssDelRecurso i = new MetodosBssDelRecurso();
                    Translate(i, m);
                    cfg.MetodosBss.Add(i);
                }
            }
            */
			if (soapCfg.ListaRecursos != null)
			{
				foreach (SoapCfg.CfgRecursoEnlaceExterno soapi in soapCfg.ListaRecursos)
				{
					CfgRecursoEnlaceExterno i = new CfgRecursoEnlaceExterno();
					Translate(i, soapi);
					cfg.ListaRecursos.Add(i);
				}
			}
            /** 20180321. Rellenar con ALIAS cuando venga en Configuracion */
            cfg.Alias = soapCfg.AliasEnlace ?? "NoAlias";
            cfg.EmplazamientoDefecto = "";
            cfg.TiempoVueltaADefecto = 0;
            if (soapCfg.ModoTransmision == null)
                cfg.ModoTransmision = Tipo_ModoTransmision.Ninguno;
            else
                switch (soapCfg.ModoTransmision)
                {
                    case "R":
                        cfg.ModoTransmision = Tipo_ModoTransmision.UltimoReceptor;
                        cfg.EmplazamientoDefecto = soapCfg.EmplazamientoDefecto;
                        cfg.TiempoVueltaADefecto = 0;
                        if (soapCfg.TiempoVueltaADefecto != null)
                        {
                            int tiempo;
                            if (Int32.TryParse(soapCfg.TiempoVueltaADefecto, out tiempo))
                                cfg.TiempoVueltaADefecto = tiempo;
                        }
                        break;
                    case "M":
                        cfg.ModoTransmision = Tipo_ModoTransmision.Manual;
                        break;
                    case "C":
                        cfg.ModoTransmision = Tipo_ModoTransmision.Climax;
                        break;
                    default:
                        cfg.ModoTransmision = Tipo_ModoTransmision.Ninguno;
                        break;
                }
            cfg.PorcentajeRSSI = 0;
            if (cfg.TipoFrecuencia == Tipo_Frecuencia.FD)
            {
                if (soapCfg.PorcentajeRSSI != null)
                {
                    uint porcentaje;
                    if (UInt32.TryParse(soapCfg.PorcentajeRSSI, out porcentaje))
                        cfg.PorcentajeRSSI = porcentaje;
                }
            }
		}

        private static void Translate(MetodosBssDelRecurso i, SoapCfg.MetodosBssDelRecurso m)
        {
            i.IdMetodo = m.idMetodo;
            i.NombreMetodo = m.nombreMetodo;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="soapCfg"></param>
		public static void Translate(CfgRecursoEnlaceExterno cfg, SoapCfg.CfgRecursoEnlaceExterno soapCfg)
		{
			cfg.IdRecurso = soapCfg.IdRecurso ?? "";
			cfg.Tipo = soapCfg.Tipo;
			cfg.Estado = soapCfg.Estado ?? "";
			cfg.ModoConfPTT = soapCfg.ModoConfPTT;
			cfg.NumFlujosAudio = soapCfg.NumFlujosAudio;
			cfg.IdEmplazamiento = soapCfg.IdEmplazamiento ?? "";

            cfg.NombreZona = soapCfg.NombreZona ?? "";
            //cfg.NameTablaBss = soapCfg.NameTablaBss ?? "";
            //cfg.MetodoBss = soapCfg.MetodoBss ?? "";
            cfg.GrsDelay = soapCfg.GrsDelay;
            //cfg.CldSupervision = soapCfg.CldSupervisionTime;
            cfg.OffSetFrequency = soapCfg.OffSetFrequency;
            cfg.EnableEventPttSq = soapCfg.EnableEventPttSq;
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="soapCfg"></param>
		public static void Translate(CfgEnlaceInterno cfg, SoapCfg.CfgEnlaceInterno soapCfg)
		{
                cfg.Literal = soapCfg.Literal ?? "";
                cfg.PosicionHMI = soapCfg.PosicionHMI;
                cfg.TipoEnlaceInterno = soapCfg.TipoEnlaceInterno ?? "";
                cfg.Dependencia = soapCfg.Dependencia ?? ""; //Id grupo
                cfg.Prioridad = soapCfg.Prioridad - 1;
                cfg.OrigenR2 = soapCfg.OrigenR2 ?? "";
                cfg.Dominio = soapCfg.Dominio ?? "";

                if (soapCfg.ListaRecursos != null)
                {
                    foreach (SoapCfg.CfgRecursoEnlaceInternoConInterface soapi in soapCfg.ListaRecursos)
                    {
                        CfgRecursoEnlaceInterno i = new CfgRecursoEnlaceInterno();
                        Translate(i, soapi);
                        cfg.ListaRecursos.Add(i);
                    }

                    cfg.ListaRecursos.Sort(delegate(CfgRecursoEnlaceInterno a, CfgRecursoEnlaceInterno b)
                    {
                        return (int)a.Prefijo - (int)b.Prefijo;
                    });
                }
            }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="soapCfg"></param>
		public static void Translate(CfgRecursoEnlaceInterno cfg, SoapCfg.CfgRecursoEnlaceInternoConInterface soapCfg)
		{
			switch (soapCfg.Prefijo)
			{
				case 2:
					cfg.Prefijo = Cd40Cfg.INT_DST;
					break;
				case 32:
					cfg.Prefijo = Cd40Cfg.PP_DST;
					break;
				default:
					cfg.Prefijo = soapCfg.Prefijo;
					break;
			}

			cfg.NombreRecurso = soapCfg.NombreRecurso ?? "";
			cfg.NumeroAbonado = soapCfg.NumeroAbonado ?? "";
			cfg.Interface = (TipoInterface)soapCfg.Interface;
            cfg.NombreMostrar = soapCfg.NombreMostrar ?? "";
		}

        /// <summary>
        /// Carga de la Configuracion HF.
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="pool"></param>
        public static void Translate(Cd40Cfg cfg, SoapCfg.PoolHfElement hf)
        {
            PoolHfElement cfghf = new PoolHfElement()
            {
                Id = hf.Id, IpGestor = hf.IpGestor,  Oid=hf.Oid, SipUri = hf.SipUri, 
            };

            if (hf.Frecs != null)
            {
                foreach (SoapCfg.HfRangoFrecuencias rango in hf.Frecs)
                {
                    cfghf.Frecs.Add(new HfRangoFrecuencias() { fmin = rango.FMin, fmax = rango.FMax });
                }
            }

            cfg.PoolHf.Add(cfghf);
        }

        /// <summary>
        /// Carga de la Configuracion HF.
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="pool"></param>
        public static void Translate(Cd40Cfg cfg, SoapCfg.Node nm)
        {
            Node cfgNode = new Node();
            new DataHelper().CopyTo(nm, cfgNode);
            cfgNode.idEmplazamiento = nm.IdEmplazamiento;

            //Node cfgNode = new Node()
            //{
            //    Id = nm.Id, 
            //    IpGestor = nm.IpGestor,  
            //    Oid=nm.Oid, 
            //    SipUri = nm.SipUri, 
            //    EsReceptor = nm.EsReceptor, 
            //    EsTransmisor=nm.EsTransmisor, 
            //    FormaDeTrabajo = (Tipo_Formato_Trabajo)nm.FormaDeTrabajo, 
            //    FrecuenciaPrincipal = nm.FrecuenciaPrincipal, 
            //    Prioridad = (uint)nm.Prioridad, 
            //    TipoDeCanal = (Tipo_Canal)nm.TipoDeCanal,
            //    TipoDeFrecuencia = (Tipo_Frecuencia)nm.TipoDeFrecuencia,
            //    Puerto = nm.Puerto,
            //    Canalizacion = (GearChannelSpacings)Enum.Parse(typeof(GearChannelSpacings), nm.Canalizacion.ToString()), 
            //    Modulacion = nm.Modulacion, 
            //    Offset = nm.Offset, 
            //    Potencia = nm.Potencia, 
            //    FormatoFrecuenciaPrincipal = (Tipo_Formato_Frecuencia) nm.FormatoFrecuenciaPrincipal
            //};

            if (nm.Frecs != null)
            {
                foreach (SoapCfg.HfRangoFrecuencias rango in nm.Frecs)
                {
                    cfgNode.Frecs.Add(new HfRangoFrecuencias() { fmin = rango.FMin, fmax = rango.FMax });
                }
            }

            cfg.Nodes.Add(cfgNode);
        }
	}
}
