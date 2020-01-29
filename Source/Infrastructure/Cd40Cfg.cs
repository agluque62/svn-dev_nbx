using System;
using System.Collections.Generic;
using System.Text;

namespace U5ki.Infrastructure
{
    /// <summary>
    /// 
    /// </summary>
	public partial class ConfiguracionSistema
	{
        /// <summary>
        /// 
        /// </summary>
        /// <param name="hostId"></param>
        /// <returns></returns>
		public string GetHostIp(string hostId)
		{
			foreach (DireccionamientoIP host in _PlanDireccionamientoIP)
			{
				if (string.Compare(host.IdHost, hostId, true) == 0)
				{
					return host.IpRed1;
				}
			}

			return null;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="hostId"></param>
		/// <param name="ip"></param>
		/// <returns></returns>
		public string GetHostIp(string hostId, int ip)
		{
			foreach (DireccionamientoIP host in _PlanDireccionamientoIP)
			{
				if (string.Compare(host.IdHost, hostId, true) == 0)
				{
					return ip == 1 ? host.IpRed1 : host.IpRed2;
				}
			}

			return null;
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
		public string GetUserHost(string userId)
		{
			foreach (AsignacionUsuariosTV tv in _PlanAsignacionUsuarios)
			{
				if (string.Compare(tv.IdUsuario, userId, true) == 0)
				{
					return tv.IdHost;
				}
			}

			return null;
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
		public string GetMainUser(string host)
		{
			foreach (AsignacionUsuariosDominantesTV tv in _PlanAsignacionUsuariosDominantes)
			{
				if (string.Compare(tv.IdHost, host, true) == 0)
				{
					return tv.IdUsuario;
				}
			}

			return null;
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        public List<StrNumeroAbonado> GetHostAddresses(string host)
		{
			List<StrNumeroAbonado> alias = new List<StrNumeroAbonado>();

			foreach (AsignacionUsuariosTV tv in _PlanAsignacionUsuarios)
			{
				if (string.Compare(tv.IdHost, host, true) == 0)
				{
                    /* 
                     * JCAM 06/10/2015
                     * Se elimina de la lista los identificadores de usuario
                     * para evitar que se registren en el proxy con el 
                     * identificador y con el número de abonado
					StrNumeroAbonado num = new StrNumeroAbonado();

					num.Prefijo = 2;
					num.NumeroAbonado = tv.IdUsuario;

					addresses.Add(num);
                     */

					alias.AddRange(GetUserAlias(tv.IdUsuario));
				}
			}

			return alias;
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
		public List<StrNumeroAbonado> GetUserAlias(string userId)
		{
			foreach (DireccionamientoSIP sip in _PlanDireccionamientoSIP)
			{
				if (string.Compare(sip.IdUsuario, userId, true) == 0)
				{
					return sip.NumerosAbonadoQueAtiende;
				}
			}

			return new List<StrNumeroAbonado>();
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
		public string GetUserAlias(string userId, uint prefix)
		{
			List<StrNumeroAbonado> nums = GetUserAlias(userId);

			foreach (StrNumeroAbonado num in nums)
			{
				if (num.Prefijo == prefix)
				{
					return num.NumeroAbonado;
				}
			}

			return userId;
		}

		//public string GetUserIp(string userId)
		//{
		//   foreach (AsignacionUsuariosTV tv in _PlanAsignacionUsuarios)
		//   {
		//      if (string.Compare(tv.IdUsuario, userId, true) == 0)
		//      {
		//         return GetHostIp(tv.IdHost);
		//      }
		//   }

		//   return null;
		//}

		//public string GetUserFromHost(string hostId)
		//{
		//   foreach (AsignacionUsuariosTV tv in _PlanAsignacionUsuarios)
		//   {
		//      if (string.Compare(tv.IdHost, hostId, true) == 0)
		//      {
		//         return tv.IdUsuario;
		//      }
		//   }

		//   return null;
		//}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="alias"></param>
        /// <returns></returns>
		public string GetUserFromAlias(uint prefix, string alias)
		{
			foreach (DireccionamientoSIP sip in _PlanDireccionamientoSIP)
			{
				foreach (StrNumeroAbonado num in sip.NumerosAbonadoQueAtiende)
				{
					if ((num.Prefijo == prefix) && (string.Compare(num.NumeroAbonado, alias, true) == 0))
					{
						return sip.IdUsuario;
					}
				}
			}

			return null;
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="resourceId"></param>
        /// <returns></returns>
		public string GetGwRsHost(string resourceId)
		{
			foreach (AsignacionRecursosGW gw in _PlanAsignacionRecursos)
			{
				if (string.Compare(gw.IdRecurso, resourceId, true) == 0)
				{
					return gw.IdHost  + ":" + gw.SipPort;
				}
			}

			return null;
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="resourceId"></param>
        /// <returns></returns>
		public string GetGwRsIp(string resourceId, out string gw)
		{
			gw = GetGwRsHost(resourceId);
			if (gw != null)
			{
                string[] host = gw.Split(':');  // idHost:SipPort
                string hostIp = GetHostIp(host[0]);
                if (hostIp != null)
                {
                    if (hostIp.Contains(":"))
                        return hostIp;
                    else
                        return hostIp + ":" + host[1];  // IPHost:SipPort
                }
            }

			return null;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="resourceId"></param>
		/// <param name="ip"></param>
		/// <returns></returns>
		public string GetGwRsIp(string resourceId, int ip)
		{
			string gw = GetGwRsHost(resourceId);
			if (gw != null)
			{
                string[] host = gw.Split(':');  // idHost:SipPort
                string hostIp = GetHostIp(host[0], ip);
                if (hostIp != null)
                    return hostIp + ":" + host[1];  // IPHost:SipPort
			}

			return null;
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="trunkId"></param>
        /// <returns></returns>
		public List<PlanRecursos> GetTrunkResources(string trunkId)
		{
			foreach (ListaTroncales trunk in _PlanTroncales)
			{
				if (string.Compare(trunk.IdTroncal, trunkId, true) == 0)
				{
					return trunk.ListaRecursos;
				}
			}

			return new List<PlanRecursos>();
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="numero"></param>
        /// <returns></returns>
		public List<PlanRecursos> GetNetResources(uint prefix, string numero)
		{
			if (prefix == 3)
			{
				ulong num;

				if (ulong.TryParse(numero, out num))
				{
					foreach (NumeracionATS centralAts in _PlanNumeracionATS)
					{
						bool foundCentralAts = false;

						foreach (RangosSCV range in centralAts.RangosOperador)
						{
							if ((range.Inicial <= num) && (range.Final >= num))
							{
								foundCentralAts = true;
								break;
							}
						}

						if (!foundCentralAts)
						{
							foreach (RangosSCV range in centralAts.RangosPrivilegiados)
							{
								if ((range.Inicial <= num) && (range.Final >= num))
								{
									foundCentralAts = true;
									break;
								}
							}
						}

						if (foundCentralAts)
						{
							List<PlanRecursos> resources = new List<PlanRecursos>();

							foreach (PlanRutas ruta in centralAts.ListaRutas)
							{
								foreach (string trunk in ruta.ListaTroncales)
								{
									resources.AddRange(GetTrunkResources(trunk));
								}
							}

							return resources;
						}
					}
				}
			}
			else
			{
				foreach (ListaRedes net in _PlanRedes)
				{
					if (net.Prefijo == prefix)
					{
						return net.ListaRecursos;
					}
				}
			}

			return new List<PlanRecursos>();
		}
	}
    /// <summary>
    /// Prefijos utilizados en el sistema de dos dígitos
    /// </summary>
	public partial class Cd40Cfg
	{
        public const uint INT_DST = 0; //Puestos de operador internos
		public const uint PP_DST = 1;  //Líneas internas, punto a punto y tlf IP de centralita interna
		public const uint IP_DST = 2;  
		public const uint ATS_DST = 3; //ATS analógica y digital
		public const uint RTB_DST = 4;
		public const uint EyM_DEST = 50;
        public const uint MD_DST = 90;
        public const uint UNKNOWN_DST = 99;  // Destinos entrantes desconocidos

		public const uint RD_RX = 0;
		public const uint RD_TX = 1;
		public const uint RD_RXTX = 2;
	}
}
