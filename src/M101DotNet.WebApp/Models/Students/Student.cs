using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MongoDB.Bson;

namespace M101DotNet.WebApp.Models.Students
{
	public class Student
	{
		public ObjectId Id { get; set; }
		public string name { get; set; }
		public List<Score> scores { get; set; }
	}
}