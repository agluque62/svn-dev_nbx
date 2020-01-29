using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.InteropServices;
using U5ki.Infrastructure;


namespace U5ki.PresenceService
{
    public class BinaryResource /*: BaseCode */
    {
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="arr"></param>
        /// <param name="offset"></param>
        /// <param name="obj"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        protected int CopyObject2ByteArray<T>(ref byte[] arr, int offset, T obj/*, int size*/)
        {
            try
            {
                int size = Marshal.SizeOf(obj);

                Array.Resize(ref arr, offset + size);
                IntPtr ptr = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(obj, ptr, true);
                Marshal.Copy(ptr, arr, offset, size);
                Marshal.FreeHGlobal(ptr);
                return offset + size;
            }
            catch (Exception x)
            {
                throw new Exception(String.Format("Copy2ByteArray Exception: {0}", x.Message));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="arr"></param>
        /// <param name="offset"></param>
        /// <param name="data"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        protected int CopyString2ByteArray(ref byte[] arr, int offset, string data, int size)
        {
            byte[] bdata = ASCIIEncoding.ASCII.GetBytes(data);
            Array.Resize(ref bdata, size);

            Array.Resize(ref arr, offset + size);
            bdata.CopyTo(arr, offset);
            return arr.Length;
        }

    }
}
