using MongoDB.Bson;

namespace M101DotNet.WebApp.Models.Students
{
	public class Grade
	{
		public ObjectId Id { get; set; }
		public int student_id { get; set; }
		public double score { get; set; }
		public string type { get; set; }
	}
}