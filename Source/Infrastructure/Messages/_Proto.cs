using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace U5ki.Infrastructure
{

    public class BaseProto<T> : BaseDto
    {
        public static T Serialize(byte[] content)
        {
            MemoryStream ms = new MemoryStream(content);
            return Serializer.Deserialize<T>(ms);
        }
    }

    public partial class Nodes : BaseProto<Nodes>
    {
        public static string __Type = Identifiers.TypeId(typeof(Nodes));
    }

    public partial class Cd40Cfg : BaseProto<Cd40Cfg>
    {
        public static string __Type = Identifiers.TypeId(typeof(Cd40Cfg));

        public const uint INT_DST = 0;
        public const uint PP_DST = 1;
        public const uint IP_DST = 2;
        public const uint ATS_DST = 3;
        public const uint RTB_DST = 4;
        public const uint EyM_DEST = 50;

        public const uint RD_RX = 0;
        public const uint RD_TX = 1;
        public const uint RD_RXTX = 2;
    }

    public partial class ConfiguracionSistema : BaseProto<ConfiguracionSistema>
    {
        public static string __Type = Identifiers.TypeId(typeof(ConfiguracionSistema));

        #region Logic

        public string GetHostIp(string hostId)
		{
			foreach (DireccionamientoIP host in _PlanDireccionamientoIP)
				if (string.Compare(host.IdHost, hostId, true) == 0)
					return host.IpRed1;

			return null;
		}

		public string GetHostIp(string hostId, int ip)
		{
			foreach (DireccionamientoIP host in _PlanDireccionamientoIP)
				if (string.Compare(host.IdHost, hostId, true) == 0)
					return ip == 1 ? host.IpRed1 : host.IpRed2;

			return null;
		}

		public string GetUserHost(string userId)
		{
			foreach (AsignacionUsuariosTV tv in _PlanAsignacionUsuarios)
				if (string.Compare(tv.IdUsuario, userId, true) == 0)
					return tv.IdHost;

			return null;
		}
    
		public string GetMainUser(string host)
		{
			foreach (AsignacionUsuariosDominantesTV tv in _PlanAsignacionUsuariosDominantes)
				if (string.Compare(tv.IdHost, host, true) == 0)
					return tv.IdUsuario;

			return null;
		}
       
		public List<StrNumeroAbonado> GetHostAlias(string host)
		{
			List<StrNumeroAbonado> alias = new List<StrNumeroAbonado>();

			foreach (AsignacionUsuariosTV tv in _PlanAsignacionUsuarios)
			{
				if (string.Compare(tv.IdHost, host, true) == 0)
				{
					StrNumeroAbonado num = new StrNumeroAbonado();

					num.Prefijo = 2;
					num.NumeroAbonado = tv.IdUsuario;

					alias.Add(num);
					alias.AddRange(GetUserAlias(tv.IdUsuario));
				}
			}

			return alias;
		}
       
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
        
		public string GetGwRsHost(string resourceId)
		{
			foreach (AsignacionRecursosGW gw in _PlanAsignacionRecursos)
			{
				if (string.Compare(gw.IdRecurso, resourceId, true) == 0)
				{
					return gw.IdHost;
				}
			}

			return null;
		}
        
		public string GetGwRsIp(string resourceId)
		{
			string gw = GetGwRsHost(resourceId);
			if (gw != null)
			{
				return GetHostIp(gw);
			}

			return null;
		}

		public string GetGwRsIp(string resourceId, int ip)
		{
			string gw = GetGwRsHost(resourceId);
			if (gw != null)
			{
				return GetHostIp(gw, ip);
			}

			return null;
		}

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

        #endregion

    }

}
