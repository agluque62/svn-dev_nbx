using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using U5ki.Infrastructure;

namespace U5ki.RdService.Helpers
{

    public class FrecuencyHelper : BaseHelper
    {

        private IDictionary<string, RdFrecuency> _frecuencies;

        public FrecuencyHelper()
        {
            _frecuencies = new Dictionary<string, RdFrecuency>();
        }
        public FrecuencyHelper(IDictionary<string, RdFrecuency> Frecuencies)
        {
            _frecuencies = Frecuencies;
        }

        public RdFrecuency FrecuencyGet(String frecuencyId)
        {
            if (_frecuencies.ContainsKey(frecuencyId))
                return _frecuencies[frecuencyId];
            return null;
        }

#region Frecuency's Resources
        //JOI FREC_DES
        public Boolean ResourceRemoveEmplaz(RdFrecuency frecuency, String key)
        {
            return ResourceRemove(frecuency, key);
        }
        //JOI FREC_DES  FIN     
     
        private Boolean ResourceRemove(RdFrecuency frecuency, String key)
        {
            if (!frecuency.RdRs.ContainsKey(key))
                return false;
           //Si está conectado, lo tengo que desconectar antes de borrar 
           //para que no se queden sesiones abiertas
           RdResource resource = frecuency.RdRs[key];
           if (resource.Connected)
           {
                frecuency.RemoveSipCall(resource);
           }
           resource.Dispose();
           return frecuency.RdRs.Remove(key);
        }

        public String ResourceIdGet(String sipUri, RdRsType type)
        {
            return sipUri.ToUpper() + (Int32)type;
        }


        protected Boolean ResourceSet(RdFrecuency frecuency, String sipUri, RdResource resource, String idEmplazamiento, bool isMaster) //#3603
        {
            if (null == frecuency)
                return false;

            String keyNew = ResourceIdGet(sipUri, resource.Type);
            // JOI FREC_DES
            // Buscar los recursos de esta frecuencia.
            foreach (RdResource rdres in frecuency.RdRs.Values)
            {
                if (rdres.Type == resource.Type && rdres.Site == idEmplazamiento)
                {
                    String keyFound1 = ResourceIdGet(rdres.Uri1, resource.Type); 
                    // En caso de que este exista, eliminarlo.
                    if (keyFound1 != keyNew)
                    {
                        ResourceRemove(frecuency, keyFound1);
                        break;
                    }
                }
            }

            // Añadir el nuevo recurso.
            frecuency.ResourceAdd(keyNew, resource, isMaster);
            return true;
        }



        public Boolean ResourceSet(RdFrecuency frecuency, String resourceId, String sipUri, RdRsType type, string idEmplazamiento, RdFrecuency.NewRdFrequencyParams confParams, RdResource.NewRdResourceParams newRDRP, bool isMaster) //#3603
        {
            return ResourceSet(
                frecuency,
                sipUri,
                new RdResource(resourceId, sipUri, type, frecuency.Frecuency, idEmplazamiento, confParams, newRDRP), idEmplazamiento, isMaster); //#3603		

        }

        private Boolean ResourceSet(String frecuencyId, String sipUri, RdResource resource, String idEmplazamiento, bool isMaster) //#3603
        {
            return ResourceSet(
                FrecuencyGet(frecuencyId),
                sipUri,
                 resource, idEmplazamiento, isMaster); //#3603

        }

        public Boolean ResourceFree(RdFrecuency frecuency, String sipUri, RdRsType resourceType, Boolean removeResource = false)
        {
            String key = ResourceIdGet(sipUri, resourceType);
            
            if (!frecuency.RdRs.ContainsKey(key))
                return false;

            RdResource resource = frecuency.RdRs[key];

            frecuency.RemoveSipCall(resource);
            resource.Dispose();
            resource.IsForbidden = true;
            frecuency.RdRs.Remove(key);
            return true;
        }

        /// <summary>
        /// 2016116. AGL. Para limpiar una inmediatamente antes de ocuparla. (por tipo de recurso)
        /// </summary>
        /// <param name="frecuency"></param>
        /// <param name="resourceType"></param>
        /// <param name="removeResource"></param>
        /// <returns></returns>
        public Boolean ResourceFree(RdFrecuency frecuency, RdRsType resourceType)
        {
            foreach (RdResource rdres in frecuency.RdRs.Values)
            {
                if (rdres.Type == resourceType)
                {
                    frecuency.RemoveSipCall(rdres);
                    rdres.Dispose();
                    rdres.IsForbidden = true;
                }
            }
            return true;
        }
        /// <summary>
        /// 20161117. AGL. Para limpiar (si procede) una frecuencia antes de ocuparla...
        /// </summary>
        /// <param name="frecuency"></param>
        /// <param name="resourceType"></param>
        /// <param name="sipUri"></param>
        /// <returns></returns>
        public Boolean RsIsInFrec(RdFrecuency frecuency, RdRsType resourceType, String sipUri)
        {
            String key = ResourceIdGet(sipUri, resourceType);

            // Si la frecuencia ya tiene el recurso, retorna y no hay que seguir con el proceso de asignacion
            if (frecuency.RdRs.ContainsKey(key))
            {
                if (frecuency.RdRs[key].IsForbidden == false)
                    return true;
            }            
            return false;
        }
#endregion

        #region sharing

        /** 20180316. MNDISABEDNODES */
        public static void MNDisabledNodesPublish(List<string> nodes)
        {
            Task.Factory.StartNew(() =>
            {
                var nodes2send = new MNDisabledNodes();
                nodes.ForEach(node =>
                {
                    nodes2send.nodes.Add(node);
                });
                RdRegistry.PublishMNDisabledNodes(nodes2send);
            });
        }

        #endregion

    }

}
