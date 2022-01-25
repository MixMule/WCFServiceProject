using System;
using System.Collections.Generic;
using System.ServiceModel;
using WCFServiseProject.Models;
using WCFServiseProject.DataAccess;

namespace WCFServiseProject
{
    /// <summary>
    /// Service class that implements the service contract.
    /// </summary>
    [ServiceBehavior(IncludeExceptionDetailInFaults = true)] 
    public class Service : IService  
    {
        public IList<TimeRegistration> GetAll()
        {
            return TimeRegistrationRepository.GetAll().GetAwaiter().GetResult();
        }

        public TimeRegistration GetById(int id)
        {
            return TimeRegistrationRepository.Get(id).GetAwaiter().GetResult();
        }

        public void Delete(int id)
        {
            TimeRegistrationRepository.Delete(id).GetAwaiter().GetResult(); 
        }

        public int Add(DateTime timeIn, DateTime timeOut)
        {
            return TimeRegistrationRepository.Insert(timeIn, timeOut).GetAwaiter().GetResult();
        }

        public void Update(int id, DateTime timeIn, DateTime timeOut)
        {
            TimeRegistrationRepository.Update(id, timeIn, timeOut).GetAwaiter().GetResult();
        }
    }
}


