using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using U5ki.Infrastructure;
using NLog;

namespace U5ki.RdService
{
    /// <summary>
    /// 
    /// </summary>
    static class RdMixer
	{
        private static Logger _Logger = LogManager.GetCurrentClassLogger();
        /// <summary>
        /// 
        /// </summary>
        /// <param name="srcType"></param>
        /// <param name="srcIds"></param>
        /// <param name="dst"></param>
		public static void Link(PttSource srcType, IEnumerable<int> srcIds, int dst)
		{
			List<int> dstIds = new List<int>(1);
			dstIds.Add(dst);

			foreach (int src in srcIds)
			{
				Link(srcType, src, dstIds);
			}
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="srcType"></param>
        /// <param name="srcIds"></param>
        /// <param name="dstIds"></param>
		public static void Link(PttSource srcType, IEnumerable<int> srcIds, IEnumerable<int> dstIds)
		{
			foreach (int src in srcIds)
			{
				Link(srcType, src, dstIds);
			}
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="srcType"></param>
        /// <param name="src"></param>
        /// <param name="dstIds"></param>
		public static void Link(PttSource srcType, int src, IEnumerable<int> dstIds)
		{
            //Este caso sería un error que hay que depurar
            if (srcType == PttSource.NoPtt)
            {
                _Logger.Error("Link error NoPtt!!!");
                return;
            }

			// Evitar que el audio recibido de un SQ implicado en un grupo de retransmisión
			// no debe ser escuchado en el puesto.
			//if (srcType == PttSource.Avion)
			//    return;

			if (srcType == PttSource.Hmi)
			{
				int srcInstructor = src | ((int)CORESIP_SndDevType.CORESIP_SND_INSTRUCTOR_MHP << 16);
				int srcAlumn = src | ((int)CORESIP_SndDevType.CORESIP_SND_ALUMN_MHP << 16);

				foreach (int dst in dstIds)
				{
					_Mixer.Link(srcInstructor, Mixer.UNASSIGNED_PRIORITY, dst, Mixer.UNASSIGNED_PRIORITY);
					_Mixer.Link(srcAlumn, Mixer.UNASSIGNED_PRIORITY, dst, Mixer.UNASSIGNED_PRIORITY);
				}
			}
			else
			{
				if (srcType == PttSource.Instructor)
				{
					src |= ((int)CORESIP_SndDevType.CORESIP_SND_INSTRUCTOR_MHP << 16);
				}
				else if (srcType == PttSource.Alumn)
				{
					src |= ((int)CORESIP_SndDevType.CORESIP_SND_ALUMN_MHP << 16);
				}

				foreach (int dst in dstIds)
				{
					_Mixer.Link(src, Mixer.UNASSIGNED_PRIORITY, dst, Mixer.UNASSIGNED_PRIORITY);
				}
			}
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="srcType"></param>
        /// <param name="srcIds"></param>
        /// <param name="dst"></param>
		public static void Unlink(PttSource srcType, IEnumerable<int> srcIds, int dst)
		{
			List<int> dstIds = new List<int>(1);
			dstIds.Add(dst);

			foreach (int src in srcIds)
			{
				Unlink(srcType, src, dstIds);
			}
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="srcType"></param>
        /// <param name="srcIds"></param>
        /// <param name="dstIds"></param>
		public static void Unlink(PttSource srcType, IEnumerable<int> srcIds, IEnumerable<int> dstIds)
		{
			foreach (int src in srcIds)
			{
				Unlink(srcType, src, dstIds);
			}
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="srcType"></param>
        /// <param name="src"></param>
        /// <param name="dstIds"></param>
		public static void Unlink(PttSource srcType, int src, IEnumerable<int> dstIds)
		{
			Debug.Assert(srcType != PttSource.NoPtt);

			if (srcType == PttSource.Hmi)
			{
				int srcInstructor = src | ((int)CORESIP_SndDevType.CORESIP_SND_INSTRUCTOR_MHP << 16);
				int srcAlumn = src | ((int)CORESIP_SndDevType.CORESIP_SND_ALUMN_MHP << 16);

				foreach (int dst in dstIds)
				{
					_Mixer.Unlink(srcInstructor, dst);
					_Mixer.Unlink(srcAlumn, dst);
				}
			}
			else
			{
				if (srcType == PttSource.Instructor)
				{
					src |= ((int)CORESIP_SndDevType.CORESIP_SND_INSTRUCTOR_MHP << 16);
				}
				else if (srcType == PttSource.Alumn)
				{
					src |= ((int)CORESIP_SndDevType.CORESIP_SND_ALUMN_MHP << 16);
				}

				foreach (int dst in dstIds)
				{
					_Mixer.Unlink(src, dst);
				}
			}
		}

		#region Private Members
        /// <summary>
        /// 
        /// </summary>
		private static Mixer _Mixer = new Mixer();

		#endregion
	}
}
