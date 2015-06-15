using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using MongoDB.Driver;
using M101DotNet.WebApp.Models;
using M101DotNet.WebApp.Models.Home;
using MongoDB.Bson;
using System.Linq.Expressions;

namespace M101DotNet.WebApp.Controllers
{
    public class HomeController : Controller
    {
        public async Task<ActionResult> Index()
        {
            var blogContext = new BlogContext();
            // XXX WORK HERE
            // find the most recent 10 posts and order them
            // from newest to oldest

            var model = new IndexModel
            {
                RecentPosts = recentPosts
            };

            return View(model);
        }

		//Homework 2.2
		//[HttpGet]
		//[AsyncTimeout(8000)]
		//[HandleError(ExceptionType = typeof(TimeoutException), View = "TimedOut")]
		//public async Task<ActionResult> Index()
		//{
		//	List<Grade> gradesDeleted = await DeleteGradesAsync();

		//	return View(gradesDeleted);
		//}

        [HttpGet]
        public ActionResult NewPost()
        {
            return View(new NewPostModel());
        }

        [HttpPost]
        public async Task<ActionResult> NewPost(NewPostModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var blogContext = new BlogContext();
            // XXX WORK HERE
            // Insert the post into the posts collection
            return RedirectToAction("Post", new { id = post.Id });
        }

        [HttpGet]
        public async Task<ActionResult> Post(string id)
        {
            var blogContext = new BlogContext();

            // XXX WORK HERE
            // Find the post with the given identifier

            if (post == null)
            {
                return RedirectToAction("Index");
            }

            var model = new PostModel
            {
                Post = post
            };

            return View(model);
        }

        [HttpGet]
        public async Task<ActionResult> Posts(string tag = null)
        {
            var blogContext = new BlogContext();

            // XXX WORK HERE
            // Find all the posts with the given tag if it exists.
            // Otherwise, return all the posts.
            // Each of these results should be in descending order.

            return View(posts);
        }

        [HttpPost]
        public async Task<ActionResult> NewComment(NewCommentModel model)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction("Post", new { id = model.PostId });
            }

            var blogContext = new BlogContext();
            // XXX WORK HERE
            // add a comment to the post identified by model.PostId.
            // you can get the author from "this.User.Identity.Name"

            return RedirectToAction("Post", new { id = model.PostId });
        }

		static async Task<List<Grade>> DeleteGradesAsync()
	    {
			var connectionString = "mongodb://localhost:27017";
			var client = new MongoClient(connectionString);

			//remove the grade of type "homework" with the lowest score for each student from the dataset

			var db = client.GetDatabase("students");

			var col = db.GetCollection<Grade>("grades");

			//Hint: If you select homework grade-documents, sort by student and then by score, 
			//you can iterate through and find the lowest score for each student by noticing 
			//a change in student id. As you notice that change of student_id, remove the document.

			var builder = Builders<Grade>.Filter;
			var filter = builder.Eq(g => g.type,"homework");

			var list = await col.Find(filter).Sort(Builders<Grade>.Sort.Ascending(g => g.student_id).Ascending(g => g.score)).ToListAsync();

		    int previousStudentId = int.MinValue;
			List<Grade> gradesToDelete = new List<Grade>();
			foreach (var grade in list)
			{
				if (grade.student_id != previousStudentId)
				{
					gradesToDelete.Add(grade);
					previousStudentId = grade.student_id;
				}
			}

			List<Grade> gradesDeleted = new List<Grade>();
			for(var i = 0; i < gradesToDelete.Count; i++)
			{
				var iterator = i;
				gradesDeleted.Add(await col.FindOneAndDeleteAsync<Grade>(g => g.Id.Equals(gradesToDelete[iterator].Id), new FindOneAndDeleteOptions<Grade, Grade>{ MaxTime = TimeSpan.FromSeconds(60)}) ); 
			}
			//var result = await col.DeleteManyAsync(g => gradesToDelete.Select(s => s.Id).Contains(g.Id));

		    return gradesDeleted;
	    }
    }
    }
}