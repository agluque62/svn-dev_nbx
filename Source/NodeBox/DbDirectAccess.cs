using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace U5ki.NodeBox
{
    /// <summary>
    /// 
    /// </summary>
    public class SystemUserData
    {
        public string id { get; set; }
        public string pwd { get; set; }
        public int perfil { get; set; }
    }
    /// <summary>
    /// 
    /// </summary>
    public class DbDirectAccess
    {
        public string ServerIp { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="iduser"></param>
        /// <param name="pwd"></param>
        /// <param name="perfilmin"></param>
        /// <returns></returns>
        public bool AuthenticateUser(string iduser, string pwd, int perfilmin)
        {
            ReadUsersData();
            SystemUserData UserInDb = UsersData.Where(u => u.id == iduser && u.pwd == pwd).FirstOrDefault();
            return (UserInDb == null || UserInDb.perfil >= perfilmin) ? false : true;
        }

        protected void ReadUsersData()
        {
        }

        static protected List<SystemUserData> UsersData = new List<SystemUserData>();
    }
}
