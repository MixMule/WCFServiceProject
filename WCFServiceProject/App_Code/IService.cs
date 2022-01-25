using System;
using System.Collections.Generic;
using System.ServiceModel;
using WCFServiseProject.Models;

namespace WCFServiseProject
{
	/// <summary>
	/// Defines a service contract
	/// </summary>
	[ServiceContract]
	public interface IService
	{
		[OperationContract]
		IList<TimeRegistration> GetAll();

		[OperationContract]
		TimeRegistration GetById(int id);

		[OperationContract]
		void Delete(int id);

		[OperationContract]
		int Add(DateTime TimeIn, DateTime TimeOut);

		[OperationContract]
		void Update(int id, DateTime TimeIn, DateTime TimeOut);
	}
}