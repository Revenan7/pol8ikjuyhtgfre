using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace qq
{
    internal class BDconnection
    {
        private static k8sLoverEntities _k8SLoverEntities;

        public static k8sLoverEntities DB = GetContext();
        public static k8sLoverEntities GetContext()
        {
            if(_k8SLoverEntities == null )
            {
                _k8SLoverEntities = new k8sLoverEntities();
            }
            return _k8SLoverEntities;
        }
    }
}