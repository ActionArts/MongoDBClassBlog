using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using M101DotNet.WebApp.Models.Students;
using MongoDB.Driver;
using M101DotNet.WebApp.Models;
using M101DotNet.WebApp.Models.Home;
using MongoDB.Bson;
using System.Linq.Expressions;

namespace M101DotNet.WebApp.Controllers
{
    public class HomeController : Controller
    {
		//Homework 2.2
		//[HttpGet]
		//[AsyncTimeout(8000)]
		//[HandleError(ExceptionType = typeof(TimeoutException), View = "TimedOut")]
		//public async Task<ActionResult> Index()
		//{
		//	List<Grade> gradesDeleted = await DeleteGradesAsync();

		//	return View(gradesDeleted);
		//}

		//Homework 3.1
		//[HttpGet]
		//[AsyncTimeout(8000)]
		//[HandleError(ExceptionType = typeof(TimeoutException), View = "TimedOut")]
		//public async Task<ActionResult> Index()
		//{
		//	List<Student> studentsProcessed = await DeleteLowestHomeworkScoresAsync();

		//	return View(studentsProcessed);
		//}

		//Homework 3.2
		public async Task<ActionResult> Index()
		{
			var blogContext = new BlogContext();

			// XXX WORK HERE
			// find the most recent 10 posts and order them
			// from newest to oldest
			var recentPosts = await blogContext.Posts.Find(new BsonDocument())
				.SortByDescending(p => p.CreatedAtUtc)
				.Limit(10)
				.ToListAsync();

			var model = new IndexModel
			{
				RecentPosts = recentPosts
			};

			return View(model);
		}

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

            // XXX WORK HERE
            // Insert the post into the posts collection
			var post = await InsertPostIntoCollection(model);

	        return RedirectToAction("Post", new { id = post.Id });
        }

	    private async Task<Post> InsertPostIntoCollection(NewPostModel model)
	    {
			var blogContext = new BlogContext();
		    Post post = new Post();
		    post.Author = this.User.Identity.Name;
		    post.Title = model.Title;
		    post.Content = model.Content;
		    post.CreatedAtUtc = DateTime.UtcNow;
		    if (!String.IsNullOrWhiteSpace(model.Tags))
		    {
			    post.Tags = model.Tags.Split(new char[] {','}).ToList();
		    }
			post.Comments = new List<Comment>();

		    await blogContext.Posts.InsertOneAsync(post);

			return post;
	    }

	    [HttpGet]
        public async Task<ActionResult> Post(string id)
        {
            var blogContext = new BlogContext();

            // XXX WORK HERE
            // Find the post with the given identifier
			var objectId = new ObjectId(id);
			var post = await blogContext.Posts.Find(p => p.Id == objectId).SingleOrDefaultAsync();

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
	        List<Post> posts;
	        if (tag != null)
	        {
		       posts = await blogContext.Posts.Find(p => p.Tags.Contains(tag))
				.SortByDescending(p => p.CreatedAtUtc)
				.ToListAsync(); 
	        }
	        else
	        {
		        posts = await blogContext.Posts.Find(new BsonDocument()).ToListAsync();
	        }
			

            return View(posts);
        }

		[HttpGet]
		public async Task<ActionResult> DeletePosts()
		{
			var blogContext = new BlogContext();

			await blogContext.Posts.DeleteManyAsync(new BsonDocument());

			return RedirectToAction("Index");
		}

        [HttpPost]
        public async Task<ActionResult> NewComment(NewCommentModel model)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction("Post", new { id = model.PostId });
            }

			// XXX WORK HERE
			// add a comment to the post identified by model.PostId.
			// you can get the author from "this.User.Identity.Name"
	        Comment comment = new Comment
	        {
		        Author = this.User.Identity.Name,
				Content = model.Content,
				CreatedAtUtc = DateTime.UtcNow
	        };

			var objectId = new ObjectId(model.PostId);

            var blogContext = new BlogContext();

	        var result = await blogContext.Posts.FindOneAndUpdateAsync<Post>(
				p => p.Id == objectId,
		        Builders<Post>.Update.Push(p => p.Comments, comment),
		        new FindOneAndUpdateOptions<Post, Post>
		        {
			        ReturnDocument = ReturnDocument.After
		        });

			return RedirectToAction("Post", new { id = result.Id });
        }

		private async Task<List<Grade>> DeleteGradesAsync()
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

		private async Task<List<Student>> DeleteLowestHomeworkScoresAsync()
		{
			var connectionString = "mongodb://localhost:27017";
			var client = new MongoClient(connectionString);

			//Write a program in the language of your choice that will remove the 
			//lowest homework score for each student. Since there is a single document
			//for each student containing an array of scores, you will need to update 
			//the scores array and remove the homework.

			//Remember, just remove a homework score. Don't remove a quiz or an exam!

			//Hint/spoiler: With the new schema, this problem is a lot harder and that 
			//is sort of the point. One way is to find the lowest homework in code and 
			//then update the scores array with the low homework pruned.

			var db = client.GetDatabase("school");

			var col = db.GetCollection<Student>("students");

			var students = await col.Find(new BsonDocument())
				.ToListAsync();

			var lowestHomeworkScores = students
				.Select(student => student.scores
					.FindAll(sc => sc.type == "homework")
					.OrderBy(sc => sc.score)
					.First()
				).ToList();

			var studentsWithScoresRemoved = new List<Student>();

			int index = 0;
			foreach (var score in students)
			{
				int id = score.Id;

				var result = await col.FindOneAndUpdateAsync<Student>(
					s => s.Id == id,
					Builders<Student>.Update.Pull(s => s.scores, lowestHomeworkScores[index]),
					new FindOneAndUpdateOptions<Student, Student>
					{
						ReturnDocument = ReturnDocument.After
					});
					studentsWithScoresRemoved.Add(result);

				index++;
			}

			return studentsWithScoresRemoved;
		}
    
    }
}